---
type: spec
title: "Update-Skript: Pruef-, Warn- und Fehlertexte in einem Stil (Fall, Ergebnis TREFFER / keine Zeile TREFFER, Exit ungleich 0 getrennt, Handlung)"
slug: 2026-11-19-updater-warntext-treffer-exitcode-spec
status: Gemerged
created: 2026-11-19
updated: 2026-11-28
source_backlog: "[[2026-11-13-updater-warntext-treffer-exitcode]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - "scripts/deploy/Update-IdealAkeWms.ps1:358 (neue Hilfsfunktion Format-Ergebnis neben Get-Treffer) und :554-559 (Invoke-Trockenlauf liefert zusaetzlich den Pfad der Ausgabedatei)"
  - "scripts/deploy/Update-IdealAkeWms.ps1:567-589 (Fluss A: erster und zweiter Trockenlauf, Texte nach Muster)"
  - "scripts/deploy/Update-IdealAkeWms.ps1:720-725 (Vorpruefung Haltepunkt-Trockenlauf, Text nach Muster; Rueckfrage 1 beantwortet: ja)"
  - "scripts/deploy/Update-IdealAkeWms.ps1:363,404 (Transaktion), :431-462 (Invoke-Pruefung), :701-703 und :921 (Pruefungs-Listen), :738 (Prod-Sperre), :638 (robocopy), :801 (Schema-Skript), :816 (Haltepunkt entfaellt)"
  - "IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs (neuer Waechter DeploySkripte_PruefTexte_FolgenDemMuster mit Rotprobe)"
  - "docs/betrieb/07-troubleshooting.md:110 (Zeile zu den Warnungen des zweiten Trockenlaufs ersetzen)"
  - "docs/betrieb/03-installation-update-rollback.md (Abschnitt 5.3/5.4: Muster der Meldungen kurz beschreiben; Beleg-Zeilennummern nachziehen)"
  - "docs/TESTSZENARIEN.md (neu TS-167; Erwartungstexte 126.21, 126.26, 126.27, 126.28, 126.29, 133.7 nachziehen)"
  - "IdealAkeWms/AppVersion.cs, IDEALAKEWMSService/AppVersion.cs, IdealAkeWms/Views/Help/Changelog.cshtml (Vorschlag v1.99.45, endgueltig im Dev-Lauf)"
open_questions:
  - "1. (beantwortet, Freigabe-Antwort 1, Gerald 2026-11-19 20:10) Soll der Dev-Lauf die verwandte Stelle in der Vorpruefung (Update-IdealAkeWms.ps1:725) im selben Zug angleichen? Dort steht bei Exit 0 ohne Zeile TREFFER nur 'nicht bestimmbar (Exit 0)'. Empfehlung: ja, ein Zusatz ', keine Zeile TREFFER' nach dem Muster von :567."
  - "2. Umfangsgrenze: Texte ohne Ergebnis und ohne Exit-Code (Gruppe D: Eingabe, Paket, Ermittlung, Stoppen/Starten, Benutzer-Ablehnungen) und die Ordner-/Rechte-Texte in IdealAkeWms.Deploy.psm1 (auch vom Install-Skript genutzt) bleiben woertlich. Einverstanden? Empfehlung: ja."
  - "3. Die Vorpruefung uebergeht heute still einen Haltepunkt-Eintrag, dessen Datei fehlt oder das Literal 'DECLARE @Trockenlauf BIT = 1;' nicht enthaelt (Update-IdealAkeWms.ps1:721); er erscheint nur als 'Backup PFLICHT (<Id>)' im Plan. Soll dort eine Zeile nach Muster dazukommen (keine Logik)? Empfehlung: ja."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: "1 ja + alle Prüftexte (20:10), 2–3 wie Empfehlung (20:40); freigegeben 20:38"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-19"
---

## Ziel / Nutzen (das Warum)

Seit v1.99.16 meldet das Update-Skript nach dem Scharflauf (Fluss A, ADR
[[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]]) eine Warnung, wenn der zweite Trockenlauf kein sauberes
Ergebnis liefert. Diese Warnung lautet in zwei verschiedenen Fällen gleich: „zweiter Trockenlauf ohne Zeile TREFFER (Exit n)“. Das
stimmt nicht, wenn die Zeile `TREFFER` da war und nur der Exit-Code ungleich 0 ist. Wer am Zielserver steht, sucht dann in der
Ausgabedatei nach einer fehlenden Zeile statt nach dem Fehler. Herkunft ist der QA-Hinweis H-1 zu
[[2026-11-13-updater-zweiter-trockenlauf-altbestand-spec]]. Der Wortlaut dort war von der Spec vorgegeben (Teil 2, AK 7).

Es gibt kein Datenrisiko. Marker, Ablauf und Abbruchverhalten bleiben gleich. Geändert werden nur Texte.

**Erweiterung durch Freigabe-Antwort 1 (Gerald 2026-11-19 20:10):** Nicht nur `:587` und `:725`, sondern alle Prüf-, Warn- und
Fehlertexte des Update-Skripts, die ein Ergebnis (Treffer, Abweichung) oder einen Exit-Code melden, folgen einem Muster: **Fall, Ergebnis,
Exit-Code getrennt, Folge, Handlung**. Wer am Zielserver steht, liest damit an jeder Stelle gleich ab, was geprüft wurde, was herauskam,
was das Skript daraus macht und was er selbst tun muss. Ein Drift-Wächter hält das Muster.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Muster festlegen (Abschnitt „Muster der Meldungen“) und eine Hilfsfunktion `Format-Ergebnis` für das Trockenlauf-Ergebnis
   (`Update-IdealAkeWms.ps1`, neben `Get-Treffer` `:358`).
2. Alle Texte der Gruppen A (Trockenlauf/Daten), B (Prüfungen) und C (Exit-Code von `sqlcmd`/`robocopy`, Fehler in der Transaktion) nach der
   Bestandsaufnahme umstellen. Das schließt `:587` (Fall trennen) und `:725` (Rückfrage 1: ja) ein.
3. Neuer Drift-Wächter `DeploySkripte_PruefTexte_FolgenDemMuster` mit Rotprobe in `NachlaufManifestDriftTests`.
4. Betriebsdoku: `docs/betrieb/07-troubleshooting.md:110` ersetzen; `docs/betrieb/03-installation-update-rollback.md` §5.3/§5.4 um einen
   kurzen Absatz „Meldungen lesen“ ergänzen und die Beleg-Zeilennummern auf `Update-IdealAkeWms.ps1` nachziehen, die sich verschieben.
5. `docs/TESTSZENARIEN.md`: TS-167 neu; Erwartungstexte von TS-126.21, 126.26, 126.27, 126.28, 126.29 und 133.7 nachziehen.
6. Version (Vorschlag v1.99.45, endgültig im Dev-Lauf), Anwender-Changelog, Brain-Nachträge.
7. Nur falls Rückfrage 3 = ja: neue Zeile in der Vorprüfung für den heute stillen Fall `:721`.

**Out-of-Scope**
- Bedingungen, Reihenfolge der Fälle, Marker-Logik, Abbruch/Haltepunkt, Eingabe-Prompts (`Read-Ja`/`Read-Eingabe`): alles bleibt unverändert.
- Gruppe D (Texte ohne Ergebnis und ohne Exit-Code, Liste unten) und alle Texte in `IdealAkeWms.Deploy.psm1` bleiben wörtlich (Rückfrage 2).
  `Install-IdealAkeWms.ps1` nutzt dasselbe Modul (`Install-IdealAkeWms.ps1:95`, `:407` `Test-OrdnerUndRechte`); weil das Modul unverändert
  bleibt, ist das Install-Skript nicht betroffen.
- `docs/betrieb/08-diagnose-abfragen.md`: zitiert keinen Text des Skripts. Es nutzt nur den Spaltennamen `Abweichung` in Abfragen
  (`:100-110`, `:186`, `:302-306`, `:370`); das Wort bleibt im Muster, also keine Änderung.
- ~~Der erste Trockenlauf in Fluss A (`:567`). Er trennt die Fälle schon heute.~~ *ueberholt durch Freigabe-Antwort 1: `:567` wird auf das
  Muster umgestellt (Inhalt bleibt, Form ändert sich).*
- ~~Neue Drift-Tests.~~ *ueberholt durch Freigabe-Antwort 1: ein Wächter kommt dazu (In-Scope 3).*
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
- `docs/TESTSZENARIEN.md:12079` (TS-133.7): Dort wird der Fall „keine Zeile TREFFER, Exit 1“ zitiert. ~~Dieser Text bleibt wörtlich gleich,
  TS-133.7 bleibt also gültig.~~ *ueberholt durch Freigabe-Antwort 1: der Text folgt jetzt dem Muster, TS-133.7 wird nachgezogen.*
- Weitere TS-Zeilen, die heutige Texte zitieren (Grep über `docs/TESTSZENARIEN.md`): 126.21 (`:11611`), 126.26 (`:11616`), 126.27
  (`:11617`), 126.28 (`:11625`), 126.29 (`:11626`) ändern sich; 126.30/126.31 (`:11627-11628`, „[Pruefung/MailPickup] OK“), 133.3 (`:12059`,
  „OK … zweiter Trockenlauf TREFFER: 0“) und 133.8 (`:12080`, „TREFFER: 1 (erwartet 0)“) bleiben gültig.
- `docs/betrieb/07-troubleshooting.md:110`: Die Zeile nennt nur den alten Leerwert „TREFFER: (leer)“ vor v1.99.16, keinen der beiden
  heutigen Texte. Sie wird nachgezogen (In-Scope 2).
- `docs/betrieb/03-installation-update-rollback.md:227` („Zweiter Trockenlauf muss `TREFFER: 0` zeigen“): zitiert keinen Warntext,
  keine Änderung.
- Pester-Tests zum Skript gibt es nicht (Glob `**/*.Tests.ps1`: keine Treffer). Die einzigen automatischen Prüfungen der Deploy-Skripte
  stehen in `NachlaufManifestDriftTests` (ASCII `:258-269`, `"$Name?"` `:236-244`).

### Muster der Meldungen (verbindlich ab dieser Spec)

```
[OK  ]<Fall>: [<Vorgang> ]<Ergebnis>[ -> <Folge>][. <Handlung>].
```

| Teil | Regel | Werte |
|---|---|---|
| `<Fall>` | Manifest-Id (bei Prüfungen schon `Pruefung/…`, daher **kein** zusätzliches Wort „Pruefung“ davor) bzw. Objekt (`robocopy <von> -> <nach>`) | `SQL/141`, `Pruefung/RCSI`, `Einmalig/…` |
| `<Vorgang>` | was lief; entfällt, wenn der Fall es schon sagt (Prüfungen) | `Trockenlauf`, `Trockenlauf (Vorpruefung)`, `zweiter Trockenlauf`, `Reparatur`, `Schema-Skript` |
| `<Ergebnis>` | genau eine Form je Art | Trockenlauf: `TREFFER: n` \| `keine Zeile TREFFER`. Prüfung: `OK` \| `Abweichung n` \| `nicht auswertbar (<Meldung>)` \| `nach Schema` (nur Listen der Vorprüfung). Lauf ohne TREFFER-Zeile: nur der Exit-Teil |
| Exit-Code | **nur bei ≠ 0** und **immer getrennt** als `, mit Fehler beendet (Exit k)` hinter dem Ergebnis bzw. allein; Exit 0 wird nicht genannt. „keine Zeile TREFFER“ sagt nie etwas über den Exit-Code | `keine Zeile TREFFER, mit Fehler beendet (Exit 1)` |
| `-> <Folge>` | was das Skript daraus macht; immer mit `->` (heute gemischt `->` und ` - `) | `-> kein Scharflauf`, `-> entfaellt, kein Halt`, `-> Abbruch, Web und Dienst bleiben gestoppt` |
| `<Handlung>` | was der Mensch tut; bei Trockenläufen mit Pfad der Ausgabedatei | `Ausgabe <datei> pruefen.`, `Von Hand: <Text des Manifests>` |

Form: reines ASCII (`ue`, `ae`), kein `"$Name?"` (bestehende Wächter `NachlaufManifestDriftTests.cs:237`, `:258`). `OK  ` und `  !!  `
setzen weiter `Write-Ok`/`Write-Warnung` (`IdealAkeWms.Deploy.psm1:6-7`), `ABBRUCH: ` weiter `Stop-Mit` (`Update-IdealAkeWms.ps1:43`).

**Beispiele**

```
  !!  SQL/141: zweiter Trockenlauf TREFFER: 0, mit Fehler beendet (Exit 1) -> Scharflauf ist uebernommen (Marker Scharf). Ausgabe C:\AKE\deploy-logs\..._trockenlauf.txt pruefen.
  !!  SQL/141: zweiter Trockenlauf keine Zeile TREFFER -> Scharflauf ist uebernommen (Marker Scharf). Ausgabe C:\AKE\deploy-logs\..._trockenlauf.txt pruefen.
  SQL/141: Trockenlauf (Vorpruefung) keine Zeile TREFFER -> nicht bestimmbar, Backup Pflicht, Trockenlauf erneut in 4a. Ausgabe ..._trockenlauf.txt pruefen.
ABBRUCH: Haltepunkt SQL/141: Trockenlauf TREFFER: 3, mit Fehler beendet (Exit 1) -> kein Scharflauf. Ausgabe ..._trockenlauf.txt pruefen. Web und Dienst bleiben gestoppt.
  !!  Pruefung/Kommissionierbereiche: Abweichung 1 -> Reparatur abgelehnt, Abweichung bleibt.
ABBRUCH: SQL/159: Schema-Skript mit Fehler beendet (Exit 1) -> Abbruch, Web und Dienst bleiben gestoppt. Ausgabe oben bzw. im Protokoll pruefen.
```

Hilfsfunktion (einzige Stelle, die TREFFER und Exit in Text fasst; neben `Get-Treffer` `:358`):

```powershell
function Format-Ergebnis($t) { return "$(if ($null -eq $t.Treffer) { 'keine Zeile TREFFER' } else { "TREFFER: $($t.Treffer)" })$(if ($t.Exit -ne 0) { ", mit Fehler beendet (Exit $($t.Exit))" })" }
```

`Invoke-Trockenlauf` (`:554-559`) gibt zusätzlich `Ausgabe = $aus` zurück (die Datei schreibt `Invoke-SqlDatei` heute schon, `:259`).

### Bestandsaufnahme (Worktree v1.99.44, alle Zeilen `scripts/deploy/Update-IdealAkeWms.ps1`, sofern nicht anders genannt)

Gelesen: alle `Write-Warnung`, `Stop-Mit`, `Write-Ok`, gelben `Write-Host`-Zeilen und `$nein`-Texte der Datei sowie
`IdealAkeWms.Deploy.psm1`. `<Id>` = `$($m.Id)`, `<datei>` = `$($t.Ausgabe)`. Der Haltepunkt-Rahmen von `$nein` (`:563`: „Haltepunkt <Id>:
<t> Web und Dienst bleiben gestoppt.“ bzw. „<Id>: <t> Eintrag bleibt offen.“) bleibt; geändert wird nur `<t>`.

**Gruppe A – Trockenlauf und Daten-/Einmal-Einträge**

| Zeile | Heute | Fall | Neu |
|---|---|---|---|
| `:567` | `Trockenlauf Exit n[, keine Zeile TREFFER] -> kein Scharflauf.` | erster Trockenlauf Fluss A, Exit ≠ 0 oder keine Zeile | `Trockenlauf <Format-Ergebnis> -> kein Scharflauf. Ausgabe <datei> pruefen.` |
| `:569` | `OK  <Id>: entfaellt (TREFFER: 0)` | erster Trockenlauf 0 Treffer | `OK  <Id>: Trockenlauf TREFFER: 0 -> entfaellt` |
| `:573` | `  TREFFER: n` (gelb) | erster Trockenlauf mit Treffern | `  <Id>: Trockenlauf TREFFER: n -> Scharflauf nach Backup und SCHARF moeglich` (gelb) |
| `:587` (Fall 1) | `<Id>: zweiter Trockenlauf ohne Zeile TREFFER (Exit n). Scharflauf ist uebernommen (Marker Scharf); Ausgabe im Log-Ordner pruefen.` | zweiter Trockenlauf, keine Zeile (Exit egal) | `<Id>: zweiter Trockenlauf <Format-Ergebnis> -> Scharflauf ist uebernommen (Marker Scharf). Ausgabe <datei> pruefen.` |
| `:587` (Fall 2) | derselbe Text (**falsch**, H-1) | Zeile da, Exit ≠ 0 | wie Fall 1, also `… TREFFER: x, mit Fehler beendet (Exit n) -> …` |
| `:588` | `<Id>: zweiter Trockenlauf TREFFER: n (erwartet 0).` | Exit 0, Treffer > 0 | `<Id>: zweiter Trockenlauf TREFFER: n (erwartet 0) -> Scharflauf ist uebernommen (Marker Scharf). Ausgabe <datei> pruefen.` |
| `:589` | `OK  <Id>: zweiter Trockenlauf TREFFER: 0` | sauber | unverändert (passt schon) |
| `:722` + `:725` | `  -> <Id>: TREFFER: 0 - entfaellt, kein Halt` / `TREFFER: n - Backup in Schritt 2 Pflicht, Haltepunkt in 4a` / `nicht bestimmbar (Exit n) - Backup Pflicht, Trockenlauf erneut in 4a` | Vorprüfung Haltepunkt-Trockenlauf | `  <Id>: Trockenlauf (Vorpruefung) TREFFER: 0 -> entfaellt, kein Halt` / `… TREFFER: n -> Backup in Schritt 2 Pflicht, Haltepunkt in 4a` / `… <Format-Ergebnis> -> nicht bestimmbar, Backup Pflicht, Trockenlauf erneut in 4a. Ausgabe <datei> pruefen.` (Exit 0 ohne Zeile ergibt damit `keine Zeile TREFFER`, Rückfrage 1) |
| `:721` | – (still) | Vorprüfung: Datei fehlt bzw. ohne Literal | nur falls Rückfrage 3 = ja: `  <Id>: Trockenlauf (Vorpruefung) nicht moeglich (Datei fehlt oder ohne 'DECLARE @Trockenlauf BIT = 1;') -> Backup Pflicht, Grund meldet 4a.` |
| `:816` | `OK  <Id>: entfaellt (TREFFER: 0 in der Vorpruefung), kein Halt` | 4a nach Vorprüfung 0 | `OK  <Id>: Trockenlauf (Vorpruefung) TREFFER: 0 -> entfaellt, kein Halt` |
| `:363` | `<name>: abgelehnt - <grund>. Nichts ausgefuehrt.` | Transaktion abgelehnt (Direktive/ROLLBACK) | `<name>: abgelehnt (<grund>) -> nichts ausgefuehrt.` |
| `:404` | `<name>: Fehler in Batch n - zurueckgerollt, kein Marker, DB unveraendert: <msg>` | Fehler in der Transaktion (SqlClient, kein Exit-Code) | `<name>: Fehler in Batch n (<msg>) -> zurueckgerollt, kein Marker, DB unveraendert.` |

**Gruppe B – Prüfungen (Manifest-Typ „Pruefung“; alle 36 Ids beginnen mit `Pruefung/`, Grep `nachlauf.psd1`)**

| Zeile | Heute | Fall | Neu |
|---|---|---|---|
| `:431` | `Pruefung <Id>: <st>` (z. B. `Fehler: …`) | Abfrage nicht auswertbar | `<Id>: nicht auswertbar (<st>) -> keine Reparatur. Abfrage von Hand pruefen.` |
| `:432` | `OK  Pruefung <Id>: OK` | 0 | `OK  <Id>: OK` |
| `:433` | `  Pruefung <Id>: Abweichung st - <Text>` (gelb) | Abweichung, Reparatur folgt | `  <Id>: Abweichung st -> Reparatur nach Bestaetigung. <Text>` |
| `:434` | `Pruefung <Id>: Abweichung st, von Hand: <Text>` | Abweichung, nur anzeigen | `<Id>: Abweichung st -> keine automatische Reparatur. Von Hand: <Text>` |
| `:445` | `Pruefung <Id>: Reparatur abgelehnt, Abweichung st bleibt.` | Nein zur Reparatur | `<Id>: Abweichung st -> Reparatur abgelehnt, Abweichung bleibt.` |
| `:449` | `Reparatur <Id> fehlgeschlagen (Exit n). Web und Dienst bleiben gestoppt.` | Reparatur per Datei, `sqlcmd` Exit ≠ 0 | `<Id>: Reparatur mit Fehler beendet (Exit n) -> Abbruch, Web und Dienst bleiben gestoppt. Ausgabe oben pruefen.` |
| `:454` | `Reparatur <Id> fehlgeschlagen: <msg>. Web und Dienst bleiben gestoppt.` | Reparatur ohne Transaktion | `<Id>: Reparatur fehlgeschlagen (<msg>) -> Abbruch, Web und Dienst bleiben gestoppt.` |
| `:458` | `Reparatur <Id> fehlgeschlagen. Web und Dienst bleiben gestoppt.` | Reparatur in Transaktion zurückgerollt | `<Id>: Reparatur zurueckgerollt (Meldung siehe Warnung davor) -> Abbruch, Web und Dienst bleiben gestoppt.` |
| `:461` | `Pruefung <Id> nach der Reparatur: <stand>. Web und Dienst bleiben gestoppt.` | Nachprüfung ≠ 0 | `<Id>: nach der Reparatur <stand> -> Abbruch, Web und Dienst bleiben gestoppt.` |
| `:462` | `OK  Pruefung <Id>: repariert (vorher st, jetzt 0)` | repariert | `OK  <Id>: repariert (vorher Abweichung st, jetzt OK)` |
| `:702-703` | `    [<Id>] <stand> - PROD, Abbruch vor dem Stoppen: <Text>` / ` - Hinweis: <Text>` / ` - wird in 4a nach Bestaetigung repariert` / ` - von Hand: <Text>` | Liste Vorprüfung | ` -> PROD: Abbruch vor dem Stoppen. <Text>` / ` -> Hinweis. <Text>` / ` -> Reparatur in 4a nach Bestaetigung` / ` -> von Hand: <Text>` (Trenner ` - ` wird ` -> `) |
| `:701`, `:738` | `ABBRUCH: Produktivsystem X: [<Id>] <Text> Keine automatische Reparatur (…): Wert von Hand entfernen, dann Skript neu starten. Nichts gestoppt, Web und Dienst laufen weiter.` | Prod-Sperre Phase Vorher | Eintrag in `$sperre` (`:701`) wird `[<Id>] Abweichung st: <Text>`; Rest von `:738` bleibt. Die Literale `$m.Phase -eq 'Vorher' -and $st -is [int] -and $st -gt 0 -and $e.Ziel.Prod` und `if ($sperre) { Stop-Mit` bleiben **unverändert** (Test `Pruefung_MailPickup_AbbruchAufProdVorDemStoppen`, `NachlaufManifestDriftTests.cs:215-220`) |
| `:921` | `    [<Id>] <stand> - <Text>` | Liste Nachlauf | `    [<Id>] <stand> -> <Text>` |

**Gruppe C – Exit-Code ohne TREFFER-Zeile**

| Zeile | Heute | Fall | Neu |
|---|---|---|---|
| `:801` | `<Id> fehlgeschlagen (Exit n). Web und Dienst bleiben gestoppt.` (auch bei SQL/95 mit `ABBRUCH` in der Ausgabe und Exit 0) | Schema-Skript | Exit ≠ 0: `<Id>: Schema-Skript mit Fehler beendet (Exit n) -> Abbruch, Web und Dienst bleiben gestoppt. Ausgabe oben bzw. im Protokoll pruefen.`; SQL/95 mit Exit 0: `SQL/95: Schema-Skript meldet ABBRUCH in der Ausgabe -> Abbruch, …` (heute steht dort irreführend „fehlgeschlagen (Exit 0)“) |
| `:638` | `robocopy <von> -> <nach> mit Exit n.` | Kopieren, Exit ≥ 8 | `robocopy <von> -> <nach>: mit Fehler beendet (Exit n) -> Abbruch, Web und Dienst bleiben gestoppt. Ausgabe oben pruefen.` |

**Gruppe D – bleibt wörtlich (kein Ergebnis, kein Exit-Code; Rückfrage 2)**

- Eingabe/Paket/sqlcmd/Ermittlung: `:46-48` (trap), `:59`, `:67`, `:80`, `:84`, `:92`, `:103`, `:105`, `:111`, `:118-120`, `:130-131`, `:134`,
  `:149`, `:156-157`, `:167`, `:180`, `:190`, `:202`, `:208`, `:211`, `:214`, `:232`, `:238`, `:285`, `:292`.
- Fluss A/B ohne Ergebnis (haben schon die Form `<Grund> -> <Folge>`): `:564`, `:565`, `:574`, `:576`, `:580`, `:583`, `:584`, `:550`, `:600`,
  `:602`, `:607`; Marker `:345`; Spur `:276`, `:280-281`, `:541`; Prüfabfrage `:531`; Transaktion verworfen `:407`.
- Ablauf: `:427`, `:664-665`, `:678`, `:681` (HTTP-Status, kein Exit), `:690-691`, `:720`, `:755`, `:757`, `:760`, `:770`, `:777`, `:787`, `:794`,
  `:797`, `:799`, `:803`, `:807`, `:831`, `:838`, `:845`, `:860-861`, `:864`, `:867`, `:876-877`, `:883`, `:885`, `:934`, `:941`, `:948`.
- `IdealAkeWms.Deploy.psm1`: `:15`, `:40`, `:44-45`, `:49`, `:113-116`, `:121`, `:202` (Ordner/Rechte: Ergebnis steht in der Status-Spalte
  OK/FEHLT/WARNUNG, `:195-201`), `:208`, `:212`.

### ~~Soll: Meldung des zweiten Trockenlaufs (`:587`)~~ *ueberholt durch Freigabe-Antwort 1 (Muster und Gruppe A oben)*

Ursprünglicher Entwurf, als Audit-Spur stehen gelassen. Die Fall-Trennung gilt weiter; „wörtlich wie heute“ in Zeile 1 gilt nicht mehr.

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

### Vorprüfung (`:725`) – Rückfrage 1 beantwortet: ja

Der `else`-Zweig nennt das Ergebnis über `Format-Ergebnis` (Gruppe A oben). ~~`nicht bestimmbar (Exit n, keine Zeile TREFFER) - …`~~
*ueberholt: Form nach Muster, Exit 0 wird nicht genannt, also `keine Zeile TREFFER -> nicht bestimmbar, …`.* Plan, Backup-Pflicht und
`VorTrocken` bleiben unverändert (`:724`, `:727-728`).

## Technischer Loesungsentwurf

- `Format-Ergebnis` (eine Zeile, Abschnitt „Muster“) neben `Get-Treffer` `:358`; benutzt an `:567`, `:587-588` und `:725`. Eine Funktion
  statt drei Inline-Verzweigungen, weil dieselbe Regel (TREFFER/keine Zeile, Exit getrennt) an drei Stellen gilt und der Wächter sie an
  einem Ort prüfen kann. Die drei Zweige `:587-589` bleiben als Bedingungen stehen; `:587` und `:588` rufen beide `Format-Ergebnis` bzw.
  schreiben `TREFFER: n (erwartet 0)`.
- `Invoke-Trockenlauf` (`:558`) gibt `Ausgabe = $aus` mit zurück; die Handlung nennt die Datei statt „im Log-Ordner“.
- Alle übrigen Änderungen sind reine String-Änderungen nach den Tabellen der Bestandsaufnahme. Den Kommentar `:586` um den Stand der neuen
  Version ergänzen.
- ~~Eine Inline-Verzweigung im bestehenden Warnzweig `:587`, keine neue Funktion. Zum Beispiel
  `$grund = if ($null -eq $t2.Treffer) { "ohne Zeile TREFFER (Exit $($t2.Exit))" } else { "mit Fehler beendet (Exit $($t2.Exit), TREFFER: $($t2.Treffer))" }`
  und daraus die bestehende Warnung bauen.~~ *ueberholt durch Freigabe-Antwort 1 (`Format-Ergebnis`).*
- **Drift-Wächter** `DeploySkripte_PruefTexte_FolgenDemMuster` in `IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs` (Text-Test wie
  die vorhandenen, kein PowerShell im Test, Muster `DeploySkripte_KeineVariableMitFragezeichenImText` `:236-244`). Prüft im Text von
  `Update-IdealAkeWms.ps1`:
  1. jede Interpolation `Exit $` steht unmittelbar hinter `mit Fehler beendet (` (Regex `Exit \$` ohne vorangehendes
     `mit Fehler beendet (` = Fehler; heute sechs Fehler an `:449`, `:567`, `:587`, `:638` (`Exit $LASTEXITCODE`), `:725`, `:801`);
  2. `keine Zeile TREFFER` steht genau einmal (in `Format-Ergebnis`) und `ohne Zeile TREFFER` nie;
  3. kein `"Pruefung $($m.Id)` (doppeltes „Pruefung“);
  4. Rotprobe: der heutige Text von `:587` als Eingabe macht den Wächter rot (wie `VariableMitFragezeichen("…")` `:241`).
  Die Hilfsprüfung als `internal static List<string> PruefTextFehler(string text)` wie `DatenFehler` (`:131`), damit die Rotprobe sie mit
  einem Literal aufrufen kann.
- Vorhandene Wächter, die grün bleiben müssen (alle in `NachlaufManifestDriftTests.cs`): ASCII `:258-269`, `"$Name?"` `:236-244`,
  Lese-Wächter-Literal `:178-179`, Prod-Sperre-Literale `:207-222`, Auswahl-Schleifen `:273-287`. Pester-Tests gibt es nicht (Glob
  `scripts/**/*.Tests.ps1`: keine Treffer); das Verhalten am Server bleibt Manual-UAT (TS-167).
- Pflichten der Deploy-Skripte: Die Datei bleibt **reines ASCII** (`DeploySkripte_NurAscii_OhneSage_RechteNurHinzufuegend`,
  `NachlaufManifestDriftTests.cs:258`), also „uebernommen“ und „pruefen“ ohne Umlaute. Im Text steht **kein** `"$Name?"`
  (`DeploySkripte_KeineVariableMitFragezeichenImText`, `:237`).
- `publish/` ist Build-Ausgabe und wird nicht von Hand bearbeitet.
- ~~ponytail: Kein neuer Drift-Test für den Warntext. Ein Test, der einen Konsolentext festschreibt, schützt keine Logik. Ergänzen, falls
  der Text noch einmal unbemerkt zurückfällt.~~ *ueberholt durch Freigabe-Antwort 1: der Wächter prüft das Muster, nicht einzelne Sätze.*

## Migrations-/SQL-Auswirkungen

Keine. Keine Migration, kein `SQL/XX`, kein `00_FreshInstall.sql`, keine `SQL/AgentJobs`, kein SQL-/Einmal-Skript geändert.

## Einstellungen

keine

## Audit-Feld-Auswirkungen

Keine. Es werden nur Konsolen- bzw. Protokolltexte geändert. Der Marker `DeployScriptRuns` wird vorher geschrieben (`:582`), Text und
Bedingung ändern daran nichts.

## Akzeptanzkriterien

*Fassung nach Freigabe-Antwort 1. Die Fassung des ersten Entwurfs (AK 1 „mit Fehler beendet (Exit n, TREFFER: x)“, AK 2 „wörtlich wie
v1.99.16, TS-133.7 bleibt gültig“, AK 6 bedingt) ist ueberholt; sie steht in der Git-Historie dieser Datei.*

1. **Muster:** Jede Meldung der Gruppen A, B, C hat die Form `<Fall>: [<Vorgang> ]<Ergebnis>[ -> <Folge>][. <Handlung>]` mit dem Wortlaut
   der Spalte „Neu“ der Bestandsaufnahme. Gruppe D und `IdealAkeWms.Deploy.psm1` sind byte-gleich zu v1.99.44.
2. **Zweiter Trockenlauf, Zeile da, Exit ≠ 0:** `<Id>: zweiter Trockenlauf TREFFER: x, mit Fehler beendet (Exit n) -> Scharflauf ist
   uebernommen (Marker Scharf). Ausgabe <datei> pruefen.` Kein „ohne Zeile TREFFER“, kein „(erwartet 0)“ (der Exit-Code hat Vorrang).
3. **Zweiter Trockenlauf, keine Zeile:** Exit 0 → `… zweiter Trockenlauf keine Zeile TREFFER -> …`; Exit ≠ 0 → `… keine Zeile TREFFER, mit
   Fehler beendet (Exit n) -> …`.
4. **Zweiter Trockenlauf Exit 0:** `TREFFER: n > 0` → `… TREFFER: n (erwartet 0) -> Scharflauf ist uebernommen (Marker Scharf). Ausgabe
   <datei> pruefen.`; `TREFFER: 0` → `OK  <Id>: zweiter Trockenlauf TREFFER: 0` (unverändert).
5. **Erster Trockenlauf (`:567`):** jede der drei Kombinationen (Exit ≠ 0 mit/ohne Zeile, Exit 0 ohne Zeile) nennt `Format-Ergebnis`,
   `-> kein Scharflauf` und die Ausgabedatei; beim Haltepunkt mit Rahmen `Haltepunkt <Id>: … Web und Dienst bleiben gestoppt.`
6. **Vorprüfung (`:725`, Rückfrage 1):** Exit 0 ohne Zeile → `<Id>: Trockenlauf (Vorpruefung) keine Zeile TREFFER -> nicht bestimmbar,
   Backup Pflicht, Trockenlauf erneut in 4a. Ausgabe <datei> pruefen.`; Exit ≠ 0 entsprechend mit `, mit Fehler beendet (Exit n)`. Plan,
   Backup-Pflicht, Haltepunkt-Liste sind unverändert.
7. **Prüfungen:** keine Meldung beginnt mehr mit `Pruefung Pruefung/`; Listen der Vorprüfung (`:703`) und des Nachlaufs (`:921`) trennen mit
   ` -> `; die Prod-Sperre nennt `Abweichung n`.
8. **Exit-Code ohne TREFFER:** Schema-Skript (`:801`) und `robocopy` (`:638`) melden `mit Fehler beendet (Exit n)`; SQL/95 mit `ABBRUCH`
   in der Ausgabe bei Exit 0 meldet `meldet ABBRUCH in der Ausgabe`, nicht „Exit 0“.
9. **Verhalten gleich:** Bedingungen, Reihenfolge, Marker, Abbrüche, Haltepunkte, Fragen und Exit-Code des Skripts sind unverändert. Der
   Diff in `Update-IdealAkeWms.ps1` enthält außer Strings nur `Format-Ergebnis`, `Ausgabe = $aus` und deren Aufrufe.
10. Jede `Write-Warnung` aus Gruppe A/B/C steht auch in der Sammelliste „Warnungen:“ am Ende des Laufs (`:924-925`).
11. **Wächter:** `DeploySkripte_PruefTexte_FolgenDemMuster` ist grün auf dem neuen Skript und rot auf der Rotprobe (heutiger Text `:587`).
    Alle vorhandenen Tests in `NachlaufManifestDriftTests` bleiben grün (ASCII, `"$Name?"`, Lese-Wächter, Prod-Sperre-Literale,
    Auswahl-Schleifen).
12. **Doku:** `docs/betrieb/07-troubleshooting.md:110` nennt die Ergebnisformen des zweiten Trockenlaufs mit Bedeutung und Maßnahme;
    `docs/betrieb/03-installation-update-rollback.md` §5.3/§5.4 beschreibt das Muster in einem Absatz und hat stimmende Beleg-Zeilen;
    `docs/betrieb/08-diagnose-abfragen.md` unverändert.
13. `docs/TESTSZENARIEN.md`: TS-167 steht; 126.21, 126.26, 126.27, 126.28, 126.29 und 133.7 zitieren die neuen Texte; Testindex nachgezogen.
14. `dotnet build IdealAkeWms.slnx` und `dotnet test` grün. Version in beiden `AppVersion.cs` und im Anwender-Changelog.
15. Nur falls Rückfrage 3 = ja: Vorprüfung meldet einen Haltepunkt-Eintrag mit fehlender Datei bzw. ohne Literal mit der Zeile aus Gruppe A
    (`:721`); Backup-Pflicht unverändert.

## Test-Szenarien

**TS-167 „Update-Skript: Prüf-, Warn- und Fehlertexte nach Muster“** für `docs/TESTSZENARIEN.md` (Nummer bleibt; TS-166 vergeben
`docs/TESTSZENARIEN.md:13255`, TS-167 frei, im Dev-Lauf erneut prüfen). Danach `secondbrain/tests/testszenarien-index.md` nachziehen.

**Vorbedingungen (Agent, nur DEV, wie TS-133):** DB-Kopie der DEV, Paketkopie mit synthetischen Fluss-A-Einträgen (Phase Nachher bzw.
Haltepunkt, Firma passend), die in eine Probe-Tabelle `dbo.TS167_Probe` schreiben, und einer synthetischen Prüfung `Pruefung/TS167` (lesend,
liefert 1, Reparatur über Transaktion). Keine echten Daten- oder Einmal-Skripte scharf ausführen (ADR 0023). Danach alles entfernen
(Probe-Tabelle, Marker-Zeilen, Paketkopie, Lauf-Backup).

| Nr | Szenario | Erwartet |
|---|---|---|
| 167.1 | `Test/TrefferDannFehler`: Trockenlauf `TREFFER: n`; nach dem Scharflauf gibt der zweite Trockenlauf `TREFFER: 0` aus und wirft danach `RAISERROR(…, 16, 1)`. `SCHARF` | Marker `Scharf`. Warnung „zweiter Trockenlauf TREFFER: 0, mit Fehler beendet (Exit 1) -> Scharflauf ist uebernommen (Marker Scharf). Ausgabe …_trockenlauf.txt pruefen.“, **kein** „ohne Zeile TREFFER“; auch in der Sammelliste |
| 167.2 | `Test/SperreAlt` wie TS-133.7 | „zweiter Trockenlauf keine Zeile TREFFER, mit Fehler beendet (Exit 1) -> Scharflauf ist uebernommen …“ |
| 167.3 | `Test/OhneZeile`: zweiter Trockenlauf Exit 0 ohne Zeile | „zweiter Trockenlauf keine Zeile TREFFER -> …“, **kein** „Exit 0“ |
| 167.4 | `Test/Rest` wie TS-133.8 | „zweiter Trockenlauf TREFFER: 1 (erwartet 0) -> Scharflauf ist uebernommen …“ |
| 167.5 | `Test/Sperre` wie TS-133.3 | „OK … zweiter Trockenlauf TREFFER: 0“, keine Warnung |
| 167.6 | Haltepunkt-Eintrag, Trockenlauf Exit 0 ohne Zeile, `-NurPruefen` | Vorprüfung „Trockenlauf (Vorpruefung) keine Zeile TREFFER -> nicht bestimmbar, Backup Pflicht, Trockenlauf erneut in 4a. Ausgabe … pruefen.“; Plan „Backup PFLICHT (…)“ wie bisher |
| 167.7 | Haltepunkt-Eintrag mit 0 Treffern, Volllauf | Vorprüfung „Trockenlauf (Vorpruefung) TREFFER: 0 -> entfaellt, kein Halt“; 4a „OK … Trockenlauf (Vorpruefung) TREFFER: 0 -> entfaellt, kein Halt“ |
| 167.8 | `Pruefung/TS167` (Abweichung 1), `-NurPruefen` | Liste „[Pruefung/TS167] Abweichung 1 -> Reparatur in 4a nach Bestaetigung“ |
| 167.9 | dieselbe Prüfung, Volllauf, Reparatur `n` | „!!  Pruefung/TS167: Abweichung 1 -> Reparatur abgelehnt, Abweichung bleibt.“; kein „Pruefung Pruefung/“ |
| 167.10 | dieselbe Prüfung, Reparatur `j` | „OK  Pruefung/TS167: repariert (vorher Abweichung 1, jetzt OK)“ |
| 167.11 | Schema-Skript einer Paketkopie, das mit `RAISERROR(…, 16, 1)` endet | „ABBRUCH: SQL/…: Schema-Skript mit Fehler beendet (Exit 1) -> Abbruch, Web und Dienst bleiben gestoppt. …“; Zustand wie bisher |
| 167.12 | `dotnet test` | neuer Wächter grün, Rotprobe eingebaut; alle übrigen Drift-Tests grün |

**Negativfälle**

| Nr | Szenario | Erwartet |
|---|---|---|
| 167.13 | 167.1, aber mit `TREFFER: 3` vor dem Fehler | „TREFFER: 3, mit Fehler beendet (Exit 1)“, **nicht** „(erwartet 0)“ |
| 167.14 | Erster Trockenlauf mit Exit 1 (Nachher-Eintrag) | „<Id>: Trockenlauf … mit Fehler beendet (Exit 1) -> kein Scharflauf. Ausgabe … pruefen. Eintrag bleibt offen.“, kein Scharflauf |
| 167.15 | wie 167.14 als Haltepunkt | „ABBRUCH: Haltepunkt <Id>: Trockenlauf … -> kein Scharflauf. … Web und Dienst bleiben gestoppt.“ |
| 167.16 | Gruppe D unverändert: `-System Gibtsnicht` | „ABBRUCH: System 'Gibtsnicht' gibt es fuer … nicht in ziele.psd1 …“ wie bisher |

**Bestehende Szenarien nachziehen** (neue Erwartung): 126.21 „Abweichung 2 -> Reparatur in 4a nach Bestaetigung“; 126.26 „Trockenlauf
(Vorpruefung) TREFFER: n -> Backup in Schritt 2 Pflicht, Haltepunkt in 4a“; 126.27 wie 167.7; 126.28 „[Pruefung/MailPickup] Abweichung 1 ->
Hinweis. Mails gehen nur in …“; 126.29 „-> PROD: Abbruch vor dem Stoppen. …“ und „ABBRUCH: Produktivsystem …: [Pruefung/MailPickup] Abweichung 1:
Mails gehen nur in …“; 133.7 wie 167.2.

**Mensch (Gerald):** Kein Prüfschritt am Zielserver nötig; beim nächsten regulären Update `-NurPruefen` die Prüfungsliste einmal lesen.

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
- **Paket-Inhalt, der wirkt:** `deploy/Update-IdealAkeWms.ps1` (`IdealAkeWms.Deploy.psm1` unverändert). Das neue Skript wirkt ab dem Lauf
  aus diesem Paket; Betriebsdoku `docs/betrieb/03` und `07` gehören zum selben Stand.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]] / 0023):
  keine.
- **Version:** Vorschlag v1.99.45, endgültig im Dev-Lauf (nächste freie Patch-Version prüfen).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
  Danach `SQL\` und `deploy\` ins Paket kopieren wie bei v1.99.16.

**Brain-Nachträge (Hauptcheckout):** Changelog-Eintrag in `secondbrain/changelog/`, `feature-map.md`, Testindex. Backlog-Notiz
[[2026-11-13-updater-warntext-treffer-exitcode]] als erledigt markieren. ~~`fallstricke.md` braucht keinen neuen Punkt, weil kein neues
Dauerwissen entsteht.~~ *ueberholt durch Freigabe-Antwort 1:* Das Muster ist Dauerwissen für jede künftige Meldung im Update-Skript. Es
gehört als kurzer Nachtrag zu ADR [[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]] bzw. als Punkt in
`fallstricke.md` (§30 Update-Skript) mit Warum (H-1: ein Text für zwei Fälle schickte den Menschen zur falschen Ursache) und Verweis auf den
Wächter.

**Final (QA 2026-11-19, aus dem echten Diff `f1747e10..f3cdd143`):**
- web: **ja** (Änderungen unter `IdealAkeWms/`).
- service: **ja, nur Versionsnummer** (`IDEALAKEWMSService/AppVersion.cs` → 1.99.45, damit Web und Dienst denselben Stand melden; keine Dienst-Logik geändert).
- migration: **nein** (keine Datei unter `*/Migrations/`, kein neues `SQL/`-Skript, kein Einmal-Skript).
- Paket fertig publiziert: `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (IDEALAKEWMSWeb + IDEALAKEWMSWebService + SQL + deploy), `publish-info.txt` Version 1.99.45, Commit `f3cdd14397d69935c974a25eaaeb045c9d10cacb`.
- Ablauf Mensch: Paket aufs Testsystem → `deploy\Update-IdealAkeWms.ps1` → testen → mergen. Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.
- Reihenfolge: kein Schema, kein Daten-Skript → kein zusätzlicher Backup-/Skript-Schritt aus diesem Paket; Web und Dienst wie üblich über das Update-Skript.
- Manifest (`scripts/deploy/nachlauf.psd1`): kein Eintrag nötig (Skript-Texte); das neue Skript wirkt ab dem ersten Lauf mit diesem Paket (`publish\deploy\Update-IdealAkeWms.ps1`).

## Offene Rueckfragen

*Rückfrage 1 ist beantwortet (Freigabe-Antwort 1, Gerald 2026-11-19 20:10: ja, und alle Prüf-/Warntexte auf denselben Stil); der Rumpf
setzt das um. Text unten als Audit-Spur unverändert.*

1. Soll der Dev-Lauf die verwandte Stelle in der Vorprüfung (`Update-IdealAkeWms.ps1:725`) im selben Zug angleichen? Dort steht bei Exit 0
   ohne Zeile `TREFFER` nur „nicht bestimmbar (Exit 0)“, ohne Grund. Empfehlung: ja. Das ist ein Zusatz „, keine Zeile TREFFER“ nach dem
   Muster von `:567`, ohne neue Logik (AK 6, TS 167.6). Bei nein entfallen AK 6 und 167.6.
2. **Umfangsgrenze.** Texte ohne Ergebnis und ohne Exit-Code (Gruppe D: Eingabe, Paket, Ermittlung, Stoppen/Starten, Ablehnungen durch den
   Menschen) und alle Texte in `IdealAkeWms.Deploy.psm1` bleiben wörtlich. Das Modul nutzt auch `Install-IdealAkeWms.ps1` (`:95`, `:407`);
   die Ordner-/Rechte-Meldungen haben schon eine Status-Spalte OK/FEHLT/WARNUNG (`IdealAkeWms.Deploy.psm1:195-202`). Einverstanden?
   Empfehlung: ja. Bei nein: Gruppe D und Modul ebenfalls in die Bestandsaufnahme, Install-Skript wird mitbetroffen (eigener Testfall).
3. **Stiller Fall in der Vorprüfung.** `Update-IdealAkeWms.ps1:721` übergeht einen Haltepunkt-Eintrag, dessen Datei fehlt oder das Literal
   `DECLARE @Trockenlauf BIT = 1;` nicht enthält, ohne Meldung; er steht danach nur als „Backup PFLICHT (<Id>)“ im Plan (`:728`, `:734`), den
   Grund nennt erst 4a (`:564-565`). Soll dort eine Zeile nach Muster dazukommen (nur Text, keine Logik; AK 15)? Empfehlung: ja.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Vorprüfung `:725` im selben Zug angleichen („, keine Zeile TREFFER“)? → **ja, und darüber hinaus alle Prüf-/Warntexte im Update-Skript auf denselben Stil durchgehen** (Gerald 2026-11-19 20:10). Umfang wächst → Rumpf durch den spec-agent erweitern (Bestandsaufnahme aller Prüftexte mit Zeilen, einheitliches Muster, Tests).
2. Umfangsgrenze: Gruppe D und `IdealAkeWms.Deploy.psm1` bleiben wörtlich? → **ja, Gruppe D und IdealAkeWms.Deploy.psm1 bleiben wörtlich** (Gerald 2026-11-19 20:40)
3. Zeile nach Muster für den stillen Fall der Vorprüfung `:721` (Datei fehlt / ohne Literal)? → **ja, Zeile nach Muster für den stillen Fall :721** (Gerald 2026-11-19 20:40)

## QA-Nachweis (qa-agent, 2026-11-19, Sammellauf v1.99.45)

Unabhängiger Lauf, synchron, nicht delegiert. Worktree `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`.
Geprüfter Commit-Bereich `f1747e10..HEAD` (a1ba945d Spec 1, 980beee9 Spec 2, b524d262 Spec 3, 0df40eeb Spec 4, defa0c05 Release, 947bb014 Doku) plus QA-Fix **f3cdd143**.

### 1. Build und Tests (selbst ausgeführt, nach dem QA-Fix wiederholt)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 30 Warnungen (Bestand, u. a. CS8321 FaWorklist, CS8620 BfAuftragWorkflowTests:388).
- `dotnet test`: IdealAkeWms.Tests **3390 bestanden / 1 übersprungen / 0 Fehler** (3391); IDEALAKEWMSService.Tests **790 / 0 / 0**.

### 2. Review (eigener Lauf, Diff samt Umfeld gelesen)
Befunde des Sammellaufs – alle **nicht blockierend**, alle in **f3cdd143** behoben, danach Build und Tests erneut grün, DEV neu deployt:
- **F1 (Spec BF-Warenkorb):** Manifest-Info-Eintrag fehlte, obwohl die Spec genau einen verlangt → `v1.99.45-BfWarenkorbVorschlaege` (Info, IDEAL, Nachher) ergänzt.
- **F2 (Spec Artikelinfo / Release):** Anwender-Changelog legte nahe, Kommissionierwagen zählten in der Artikelinfo nicht mehr – falsch, dort zählen sie weiter (Rückfrage 2 betrifft nur den Lagerplatz-Text der Lager-/Glasbestellung) → Satz korrigiert.
- **F3 (Spec Updater):** `Invoke-Pruefung` gab bei reinen Anzeige-Prüfungen erst die gelbe Zeile „Reparatur nach Bestaetigung“ und danach die Warnung „nur Anzeige“ aus – widersprüchlich → Anzeige-Prüfung kehrt jetzt vor der gelben Zeile zurück (Ergebnis/Exit unverändert, Parse-Check 0 Fehler).

Beobachtungen ohne Befund: Dashboard ohne SVG-Symbole (bewusste Vereinfachung); Kongruenztest T2 prüft Einzelrollen (Regeln monoton, ausreichend); Abschnitt nutzt `aria-label` statt `aria-labelledby` (geringfügig); `limit` von `api/bf-auftraege/artikel` ohne Obergrenze (Bestand vor diesem Diff); `StockLocationInfo.LagerplatzText` sortiert `ThenBy(Code)` kulturabhängig (geringfügig).

Gezielt geprüfte Fehlerklassen: kein `@Html.Raw` in `<script>` (bf-artikelsuche.js baut Optionen per `createElement`); kein `hidden` neben `d-*`; keine stillen Ersatzwerte (leere Artikelnummer → leerer Text ohne Repository-Aufruf); keine Parallel-Array-Bindung; keine reine Browser-Validierung (Suche bleibt serverseitig `SucheArtikelAsync`); Sichtbarkeitsregel je Menüziel an EINER Stelle (`NavigationSicht`, ADR 0034). Menü-zu-Filter-Kongruenz: alte Menübedingungen aus `_Layout.cshtml` gegen die Regeln in `NavigationKatalog.cs` einzeln verglichen – gleichwertig, einzige Neuerung „Artikelinfo“ unter Stammdaten.

### 5. CLAUDE.md-Checkliste
Migration/SQL/FreshInstall/AgentJobs: entfällt (kein Schema). Audit-Felder: entfällt (keine Entität geändert). Version 1.99.45 in beiden `AppVersion.cs` + `Views/Help/Changelog.cshtml`: ja. `docs/TESTSZENARIEN.md` TS-167…170 im Worktree, Testindex im Hauptcheckout Zeilen 167–170: ja. Hilfeseite, Anwender-/Betriebsdoku (Kapitel 18 mit Fotos): ja. 5a Einstellungen (ADR 0020): keine neue oder geänderte Einstellung.

### 6. DEV-Deploy
Nach dem QA-Fix aus dem Worktree, erhöhte Shell, Web und Dienst je Standort: `dev-deploy.ps1 -Standort AKE` Exit 0, „HTTP 200, Fusszeile v1.99.45“; `-Standort IDEAL` Exit 0, Fusszeile v1.99.45. DEV-Schalter nur gelesen, nichts umgestellt, nichts an Sage geschrieben.

### 7. Publish
`C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\IDEALAKEWMSWeb` und `\IDEALAKEWMSWebService` vorher geleert, beide `dotnet publish -c Release` Exit 0; `SQL\` → `publish\SQL`, `scripts\deploy\` → `publish\deploy`; `publish-info.txt`: Version 1.99.45, Commit `f3cdd14397d69935c974a25eaaeb045c9d10cacb`, Branch feature/2026-08-07-ideal-teile-1-5, Spec 2026-11-19-v1-99-45-sammellauf, Erstellt 2026-11-19 21:19, Komponenten IDEALAKEWMSWeb + IDEALAKEWMSWebService.

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1–8, 15 | Auto-Wächter `NachlaufManifestDriftTests.DeploySkripte_PruefTexte_FolgenDemMuster` (mit Rotprobe) + Review jeder geänderten Meldung gegen die Spalte „Neu“; echte Laufausgabe Manual-UAT |
| 1 (Gruppe D, psm1 byte-gleich) | Diff-Beleg: `IdealAkeWms.Deploy.psm1` nicht im Diff |
| 9 | Review: außer Strings nur `Format-Ergebnis`, `Ausgabe = $aus` und Aufrufe – plus QA-Fix F3 (frühere Rückkehr der Anzeige-Prüfung vor der gelben Zeile; nur Ausgabe, Ergebnis/Exit unverändert) |
| 10 | Review: neue A/B/C-Meldungen laufen über `Write-Warnung` → Sammelliste |
| 11 | Auto grün, übrige Drift-Tests grün |
| 12, 13 | Diff-Beleg (Betrieb 03/07, TESTSZENARIEN TS-167 + 126.21/26/27/28/29, 133.7) |
| 14 | grün; Version 1.99.45 beidseitig |

### 4. Geänderte bestehende Tests
Keine geändert; neuer Wächter in `NachlaufManifestDriftTests`.

### 6c. DEV-Schritte TS-167
Das Update-Skript zielt auf Testsystem/Prod; DEV wird über `dev-deploy.ps1` aktualisiert. TS-167 = **NICHT MÖGLICH auf DEV**, bleibt für den Testsystem-Lauf. Parse-Check des Skripts nach dem Fix: 0 Fehler.

### Manuelle Checkliste (offen für den Menschen)
1. Testsystem-Lauf mit `publish\deploy\Update-IdealAkeWms.ps1`: Meldungen von Vorprüfung, Trockenläufen und Prüfungen nach Muster (TS-167).
2. Mit Daten-Skript mit Trockenlauf im Paket: Texte des zweiten Trockenlaufs (TREFFER / keine Zeile / Exit).
3. Anzeige-Prüfung: nur Warnung „nur Anzeige“, keine gelbe „Reparatur nach Bestaetigung“-Zeile (F3).
4. Sammelliste „Warnungen:“ am Ende enthält jede Warnung.
5. Danach Merge.
