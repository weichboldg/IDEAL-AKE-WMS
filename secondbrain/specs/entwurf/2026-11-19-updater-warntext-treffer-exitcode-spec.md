---
type: spec
title: "Update-Skript: Warntext zweiter Trockenlauf je Fall trennen (keine Zeile TREFFER / Exit ungleich 0)"
slug: 2026-11-19-updater-warntext-treffer-exitcode-spec
status: Entwurf
created: 2026-11-19
updated: 2026-11-19
source_backlog: "[[2026-11-13-updater-warntext-treffer-exitcode]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - "scripts/deploy/Update-IdealAkeWms.ps1:585-589 (Fluss A, Meldung des zweiten Trockenlaufs: Text je Fall trennen)"
  - "scripts/deploy/Update-IdealAkeWms.ps1:725 (Vorpruefung, Text 'nicht bestimmbar (Exit 0)' ohne Hinweis auf fehlende Zeile TREFFER - nur falls Rueckfrage 1 = ja)"
  - "docs/betrieb/07-troubleshooting.md:110 (Zeile zur Warnung des zweiten Trockenlaufs nachziehen)"
  - "docs/TESTSZENARIEN.md (Vorschlag TS-167, naechste freie Nummer, im Dev-Lauf pruefen)"
  - "IdealAkeWms/AppVersion.cs, IDEALAKEWMSService/AppVersion.cs, IdealAkeWms/Views/Help/Changelog.cshtml (naechste freie Patch-Version)"
open_questions:
  - "1. Soll der Dev-Lauf die verwandte Stelle in der Vorpruefung (Update-IdealAkeWms.ps1:725) im selben Zug angleichen? Dort steht bei Exit 0 ohne Zeile TREFFER nur 'nicht bestimmbar (Exit 0)'. Empfehlung: ja, ein Zusatz ', keine Zeile TREFFER' nach dem Muster von :567."
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

Seit v1.99.16 meldet das Update-Skript nach dem Scharflauf (Fluss A, ADR
[[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]]) eine Warnung, wenn der zweite Trockenlauf kein sauberes
Ergebnis liefert. Diese Warnung lautet in zwei verschiedenen Fällen gleich: „zweiter Trockenlauf ohne Zeile TREFFER (Exit n)“. Das
stimmt nicht, wenn die Zeile `TREFFER` da war und nur der Exit-Code ungleich 0 ist. Wer am Zielserver steht, sucht dann in der
Ausgabedatei nach einer fehlenden Zeile statt nach dem Fehler. Herkunft ist der QA-Hinweis H-1 zu
[[2026-11-13-updater-zweiter-trockenlauf-altbestand-spec]]. Der Wortlaut dort war von der Spec vorgegeben (Teil 2, AK 7).

Es gibt kein Datenrisiko. Marker, Ablauf und Abbruchverhalten bleiben gleich. Geändert wird nur der Text.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. `Update-IdealAkeWms.ps1:587`: Die Warnung unterscheidet zwei Fälle, „keine Zeile TREFFER“ und „Zeile TREFFER vorhanden, Exit ≠ 0“.
2. `docs/betrieb/07-troubleshooting.md:110`: Die Zeile zur Warnung des zweiten Trockenlaufs nennt beide Texte.
3. Testszenario (Vorschlag TS-167), Version (nächste freie Patch-Version), Anwender-Changelog, Brain-Nachträge.
4. Nur wenn Rückfrage 1 mit ja beantwortet wird: Zusatz in der Vorprüfung `:725`.

**Out-of-Scope**
- Bedingungen, Reihenfolge der Fälle, Marker-Logik, Abbruch/Haltepunkt: alles bleibt unverändert.
- Der erste Trockenlauf in Fluss A (`:567`). Er trennt die Fälle schon heute (Beleg unten).
- Neue Drift-Tests. Der Fix ändert nur einen Text und keine Logik. Die bestehenden Wächter für ASCII und `"$Name?"` decken die Form ab
  (Begründung unten).
- Entscheidung H-2 aus v1.99.16 (DEV-Zeilen in `ziele.psd1` bleiben). Sie steht nur zur Information in der Backlog-Notiz, hier gibt es
  nichts zu tun.

## Fachliche Anforderungen

### Belegter Ist-Stand (Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, v1.99.44)

| Stelle | Beleg | Bewertung |
|---|---|---|
| Treffer-Auswertung: Regex `(?m)^\s*TREFFER: (\d+)\s*$`, ohne Zeile → `$null` | `Update-IdealAkeWms.ps1:358` (`Get-Treffer`) | Grundlage für alle Fälle |
| Trockenlauf über `sqlcmd … -b` (Exit ≠ 0 bei Fehler mit Schwere > 10), Rückgabe `Exit` + `Treffer` | `:249`, `:251-261`, `:554-559` | Exit und Treffer sind voneinander unabhängig. Beide Kombinationen kommen vor, z. B. ein `PRINT 'TREFFER: 0'` und danach ein Fehler |
| **Zweiter Trockenlauf (Fluss A):** `if ($null -eq $t2.Treffer -or $t2.Exit -ne 0) { Write-Warnung "…ohne Zeile TREFFER (Exit n)…" }` | `:585-587` | **Fehler laut Backlog:** Ein Text für zwei Fälle |
| übrige Zweige des zweiten Trockenlaufs (`TREFFER: n (erwartet 0)`, `OK … TREFFER: 0`) | `:588-589` | richtig, bleibt |
| Erster Trockenlauf (Fluss A): `"Trockenlauf Exit n$(if ($null -eq $t.Treffer) { ', keine Zeile TREFFER' }) -> kein Scharflauf."` | `:567` | trennt schon richtig. **Vorbild** für den Fix |
| Vorprüfung: `VorTrocken = if ($t.Exit -eq 0) { $t.Treffer } else { $null }`. Text bei `$null`: `"nicht bestimmbar (Exit n)"` | `:724-725` | Bei Exit ≠ 0 richtig. Bei **Exit 0 ohne Zeile TREFFER** steht „nicht bestimmbar (Exit 0)“ ohne Grund. Verwandt, aber nicht falsch, deshalb Rückfrage 1 |
| `Write-Warnung` schreibt den Text auch in die Sammelliste am Ende des Laufs | `IdealAkeWms.Deploy.psm1:7`, `Update-IdealAkeWms.ps1:924-925` | Der neue Text erscheint in beiden Ausgaben, nichts weiter zu tun |

Weitere Fundstellen des Textes (Grep `ohne Zeile TREFFER|zweiter Trockenlauf|keine Zeile TREFFER` über den Worktree ohne `publish/`):
- `IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs:117,126,143`: Das ist die **Fehlermeldung des Drift-Tests** `DatenFehler`
  („<Id>: ohne Zeile TREFFER“ für Trockenlauf-Dateien ohne `TREFFER: `). Sie ist nicht die Warnung des Update-Skripts und bleibt unverändert.
  Kein Test prüft den Text von `:587`.
- `docs/TESTSZENARIEN.md:12079` (TS-133.7): Dort wird der Fall „keine Zeile TREFFER, Exit 1“ zitiert. Dieser Text bleibt wörtlich gleich,
  TS-133.7 bleibt also gültig.
- `docs/betrieb/07-troubleshooting.md:110`: Die Zeile nennt nur den alten Leerwert „TREFFER: (leer)“ vor v1.99.16, keinen der beiden
  heutigen Texte. Sie wird nachgezogen (In-Scope 2).
- `docs/betrieb/03-installation-update-rollback.md:227` („Zweiter Trockenlauf muss `TREFFER: 0` zeigen“): zitiert keinen Warntext,
  keine Änderung.
- Pester-Tests zum Skript gibt es nicht (Glob `**/*.Tests.ps1`: keine Treffer). Die einzigen automatischen Prüfungen der Deploy-Skripte
  stehen in `NachlaufManifestDriftTests` (ASCII `:258-269`, `"$Name?"` `:236-244`).

### Soll: Meldung des zweiten Trockenlaufs (`:587`)

Die Bedingung bleibt gleich. Im Warnzweig wird nur der Text je Fall gewählt:

| Fall | Text (ASCII, wie im übrigen Skript) |
|---|---|
| `$null -eq $t2.Treffer` (unabhängig vom Exit) | `<Id>: zweiter Trockenlauf ohne Zeile TREFFER (Exit n). Scharflauf ist uebernommen (Marker Scharf); Ausgabe im Log-Ordner pruefen.` (**wörtlich wie heute**) |
| Zeile `TREFFER` vorhanden, `$t2.Exit -ne 0` | `<Id>: zweiter Trockenlauf mit Fehler beendet (Exit n, TREFFER: x). Scharflauf ist uebernommen (Marker Scharf); Ausgabe im Log-Ordner pruefen.` |
| `TREFFER: n > 0`, Exit 0 | unverändert `<Id>: zweiter Trockenlauf TREFFER: n (erwartet 0).` |
| `TREFFER: 0`, Exit 0 | unverändert `OK <Id>: zweiter Trockenlauf TREFFER: 0` |

Hinweise:
- Bei Exit ≠ 0 mit `TREFFER: 0` greift der zweite Fall: weiter eine Warnung und kein OK, denn der Exit-Code hat Vorrang (wie heute).
- Der QA-Vorschlag aus H-1 lautete „ohne gültiges Ergebnis (Exit n, TREFFER x)“. Die Spec nimmt „mit Fehler beendet“, weil das den Fall
  genauer benennt. Gerald kann den Wortlaut an Schranke 1 ändern, eine Rückfrage braucht es dafür nicht.

### Nur falls Rückfrage 1 = ja: Vorprüfung (`:725`)

Im `else`-Zweig nach dem Muster von `:567` ergänzen: `nicht bestimmbar (Exit n, keine Zeile TREFFER) - …`. Der Zusatz erscheint nur bei
`$null -eq $t.Treffer`. Plan, Backup-Pflicht und `VorTrocken` bleiben unverändert (`:724`, `:727-728`).

## Technischer Loesungsentwurf

- Eine Inline-Verzweigung im bestehenden Warnzweig `:587`, keine neue Funktion. Zum Beispiel
  `$grund = if ($null -eq $t2.Treffer) { "ohne Zeile TREFFER (Exit $($t2.Exit))" } else { "mit Fehler beendet (Exit $($t2.Exit), TREFFER: $($t2.Treffer))" }`
  und daraus die bestehende Warnung bauen. Den Kommentar `:586` um den Stand der neuen Version ergänzen.
- Pflichten der Deploy-Skripte: Die Datei bleibt **reines ASCII** (`DeploySkripte_NurAscii_OhneSage_RechteNurHinzufuegend`,
  `NachlaufManifestDriftTests.cs:258`), also „uebernommen“ und „pruefen“ ohne Umlaute. Im Text steht **kein** `"$Name?"`
  (`DeploySkripte_KeineVariableMitFragezeichenImText`, `:237`).
- `publish/` ist Build-Ausgabe und wird nicht von Hand bearbeitet.
- ponytail: Kein neuer Drift-Test für den Warntext. Ein Test, der einen Konsolentext festschreibt, schützt keine Logik. Ergänzen, falls
  der Text noch einmal unbemerkt zurückfällt.

## Migrations-/SQL-Auswirkungen

Keine. Keine Migration, kein `SQL/XX`, kein `00_FreshInstall.sql`, keine `SQL/AgentJobs`, kein SQL-/Einmal-Skript geändert.

## Einstellungen

keine

## Audit-Feld-Auswirkungen

Keine. Es wird nur ein Konsolen- bzw. Protokolltext geändert. Der Marker `DeployScriptRuns` wird vorher geschrieben (`:582`), Text und
Bedingung ändern daran nichts.

## Akzeptanzkriterien

1. Zweiter Trockenlauf mit Zeile `TREFFER: x` und Exit ≠ 0: Die Warnung lautet „zweiter Trockenlauf mit Fehler beendet (Exit n, TREFFER: x).
   Scharflauf ist uebernommen (Marker Scharf); Ausgabe im Log-Ordner pruefen.“ Sie enthält **nicht** „ohne Zeile TREFFER“.
2. Zweiter Trockenlauf ohne Zeile `TREFFER` (Exit 0 oder ≠ 0): Die Warnung ist wörtlich wie in v1.99.16 („ohne Zeile TREFFER (Exit n) …“).
   TS-133.7 bleibt gültig.
3. `TREFFER: n > 0` mit Exit 0 und `TREFFER: 0` mit Exit 0: Ausgabe unverändert (Warnung „(erwartet 0)“ bzw. OK).
4. Jede Warnung aus AK 1/2 steht auch in der Sammelliste „Warnungen:“ am Ende des Laufs.
5. Marker `Scharf` steht in allen Fällen. Es gibt keinen Abbruch, keinen Haltepunkt und keine Rückfrage mehr als heute.
6. Nur falls Rückfrage 1 = ja: Die Vorprüfung zeigt bei Exit 0 ohne Zeile `TREFFER` „nicht bestimmbar (Exit 0, keine Zeile TREFFER)“. Bei
   Exit ≠ 0 bleibt der Text gleich. Plan und Backup-Pflicht sind unverändert.
7. `docs/betrieb/07-troubleshooting.md` nennt beide Warntexte des zweiten Trockenlaufs mit Bedeutung und Maßnahme (Ausgabedatei im
   Log-Ordner prüfen, Scharflauf ist übernommen).
8. `dotnet build IdealAkeWms.slnx` und `dotnet test` sind grün, die Drift-Tests ASCII und `"$Name?"` eingeschlossen. Version steht in beiden
   `AppVersion.cs` und im Anwender-Changelog.

## Test-Szenarien

Vorschlag **TS-167 „Update-Skript: Warntext zweiter Trockenlauf je Fall“** für `docs/TESTSZENARIEN.md`. TS-165 ist reserviert, TS-166
vergeben (`docs/TESTSZENARIEN.md:13255`). Ein paralleler Spec-Lauf kann die nächste Nummer beanspruchen, daher gilt: **nächste freie Nummer,
im Dev-Lauf prüfen**. Danach `secondbrain/tests/testszenarien-index.md` nachziehen.

**Vorbedingungen (Agent, nur DEV, wie TS-133):** DB-Kopie der DEV, Paketkopie mit synthetischen Fluss-A-Einträgen (Phase Nachher, Firma
passend), die in eine Probe-Tabelle `dbo.TS167_Probe` schreiben. Keine echten Daten- oder Einmal-Skripte scharf ausführen (ADR 0023). Danach
alles entfernen (Probe-Tabelle, Marker-Zeilen, Paketkopie, Lauf-Backup).

| Nr | Szenario | Erwartet |
|---|---|---|
| 167.1 | `Test/TrefferDannFehler`: zählt im Trockenlauf `TREFFER: n`. Nach dem Scharflauf gibt der zweite Trockenlauf `TREFFER: 0` aus und wirft danach `RAISERROR(…, 16, 1)`. `SCHARF` | Marker `Scharf`. Warnung „zweiter Trockenlauf mit Fehler beendet (Exit 1, TREFFER: 0) …“, **kein** „ohne Zeile TREFFER“. Sie steht auch in der Sammelliste |
| 167.2 | `Test/SperreAlt` wie TS-133.7 (Sperre ohne Trockenlauf-Zweig) | Warnung wörtlich „zweiter Trockenlauf ohne Zeile TREFFER (Exit 1). Scharflauf ist uebernommen …“ |
| 167.3 | `Test/OhneZeile`: Der zweite Trockenlauf endet mit Exit 0, aber ohne Zeile `TREFFER` | Warnung „ohne Zeile TREFFER (Exit 0) …“ |
| 167.4 | `Test/Rest` wie TS-133.8 | „zweiter Trockenlauf TREFFER: 1 (erwartet 0).“ unverändert |
| 167.5 | `Test/Sperre` wie TS-133.3 | „OK … zweiter Trockenlauf TREFFER: 0“, keine Warnung |
| 167.6 | Nur falls Rückfrage 1 = ja: Haltepunkt-Eintrag, dessen Trockenlauf Exit 0 ohne Zeile `TREFFER` liefert, `-NurPruefen` | Vorprüfung „nicht bestimmbar (Exit 0, keine Zeile TREFFER) - Backup Pflicht …“ |
| 167.7 | `dotnet test` | alle Drift-Tests grün (ASCII, `"$Name?"`) |

**Negativfälle**

| Nr | Szenario | Erwartet |
|---|---|---|
| 167.8 | 167.1, aber mit `TREFFER: 3` vor dem Fehler | Warnung „mit Fehler beendet (Exit 1, TREFFER: 3)“, **nicht** „TREFFER: 3 (erwartet 0)“. Der Exit-Code hat Vorrang |
| 167.9 | Erster Trockenlauf mit Exit 1 (Fluss A `:567`) | Text unverändert „Trockenlauf Exit 1 -> kein Scharflauf.“ bzw. „…, keine Zeile TREFFER …“, kein Scharflauf |

**Mensch (Gerald):** Kein Prüfschritt am Zielserver nötig. Der Fall tritt nur bei fehlerhaften Skripten auf.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Vorläufig. Der QA-Lauf bestätigt das gegen den echten Diff.*

- **Web-App:** ja, nur Versionssprung (`IdealAkeWms/AppVersion.cs`) und Anwender-Changelog. Das Update-Skript vergleicht die Fußzeile mit der
  Paketversion.
- **Service:** ja, nur Versionssprung (`IDEALAKEWMSService/AppVersion.cs`).
- **Migration:** nein.
- **Paket-Inhalt, der wirkt:** `deploy/Update-IdealAkeWms.ps1`. Das neue Skript wirkt ab dem Lauf aus diesem Paket.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]] / 0023):
  keine.
- **Version:** nächste freie Patch-Version (Vorschlag v1.99.45, im Dev-Lauf prüfen).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
  Danach `SQL\` und `deploy\` ins Paket kopieren wie bei v1.99.16.

**Brain-Nachträge (Hauptcheckout):** Changelog-Eintrag in `secondbrain/changelog/`, `feature-map.md`, Testindex. Backlog-Notiz
[[2026-11-13-updater-warntext-treffer-exitcode]] als erledigt markieren. `fallstricke.md` braucht keinen neuen Punkt, weil kein neues
Dauerwissen entsteht.

## Offene Rueckfragen

1. Soll der Dev-Lauf die verwandte Stelle in der Vorprüfung (`Update-IdealAkeWms.ps1:725`) im selben Zug angleichen? Dort steht bei Exit 0
   ohne Zeile `TREFFER` nur „nicht bestimmbar (Exit 0)“, ohne Grund. Empfehlung: ja. Das ist ein Zusatz „, keine Zeile TREFFER“ nach dem
   Muster von `:567`, ohne neue Logik (AK 6, TS 167.6). Bei nein entfallen AK 6 und 167.6.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Vorprüfung `:725` im selben Zug angleichen („, keine Zeile TREFFER“)? →
