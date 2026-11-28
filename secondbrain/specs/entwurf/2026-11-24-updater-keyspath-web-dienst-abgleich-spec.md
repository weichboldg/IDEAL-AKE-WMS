---
type: spec
title: Update-Skript prüft DataProtection:KeysPath Web = Dienst und DEV-Werte in den appsettings
slug: 2026-11-24-updater-keyspath-web-dienst-abgleich-spec
status: Entwurf
created: 2026-11-24
updated: 2026-11-28
source_backlog: "[[2026-11-16-updater-keyspath-web-dienst-abgleich]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - "scripts/deploy/Update-IdealAkeWms.ps1:414-425 (Appsettings-Pruefung: neue Felder AppsettingsGleichWeb und AppsettingsMuster, Platzhalter {WebWert} {WebDatei} {Treffer})"
  - "scripts/deploy/Update-IdealAkeWms.ps1:218-225 und :662 (Ermittlung: KeysPath auch aus dem Dienst lesen, Anzeigezeile Schluessel)"
  - "scripts/deploy/IdealAkeWms.Deploy.psm1:141-142 (Standardpfad des Schluesselrings als exportierte Funktion, eine Stelle fuer Ordner-Pruefung und Vergleich)"
  - "scripts/deploy/Install-IdealAkeWms.ps1:347-366 (DataProtection:KeysPath auch in die Dienst-appsettings.json schreiben)"
  - "scripts/deploy/nachlauf.psd1 (Kopfkommentar :15-17 + zwei Eintraege Pruefung/DataProtectionKeysPathGleich, Pruefung/AppsettingsDevPfad)"
  - "IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs:180-200 (genau eines von Pruefung/Appsettings/AppsettingsMuster; neuer Test fuer beide Eintraege)"
  - "docs/betrieb/07-troubleshooting.md:48 (Zeile SData-Kennwort nicht entschluesselbar)"
  - "README.md:107-111 (Update-Abschnitt, neuer Punkt nach Mail-Ablageordner)"
  - "docs/TESTSZENARIEN.md (TS-126, neue Zeilen ab 126.32)"
  - "IdealAkeWms/AppVersion.cs, IDEALAKEWMSService/AppVersion.cs, Views/Help/Changelog.cshtml (Versionssprung)"
open_questions:
  - "1. Eigener Schluesselordner je Instanz statt C:\\AKE\\keys\\dev (teilen sich 4444 und 4445 den Ordner)? Wenn ja: Zielpfad je Instanz nennen; der Wechsel ist ein Hand-Schritt je Instanz mit Neueingabe des SData-Kennworts (siehe Rueckfrage-Text)."
  - "2. Abweichung KeysPath Web/Dienst auf einem Nicht-Prod-System: nur Hinweis (bestehende Regel der Phase Vorher, Empfehlung) oder auch dort Abbruch vor dem Stoppen?"
  - "3. Soll das DEV-Muster neben D:\\Dev\\ auch localhost enthalten? Ein legitimer localhost-Wert in einer Prod-appsettings wuerde das Prod-Update vor dem Stoppen abbrechen."
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

Befund akenet01:4445 (v1.99.23, Backlog): Der Dienst meldete „SData-Kennwort nicht entschlüsselbar“, zwei Sage-Wareneingänge blieben
„offen (wird gesendet)“. Der Grund: Die Web-App hat `KeysPath = C:\AKE\keys\dev`, der Dienst hat `D:\Dev\AKE\keys`. Der Dienst-Wert ist ein
DEV-Pfad, offenbar aus DEV kopiert. ADR [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] („Deploy-relevant“) und
[[fallstricke]] §33 verlangen denselben Wert in Web und Dienst. Das Update-Skript prüft diese Gleichheit bisher nicht.

**Belegte Lücke (Bündel-Worktree, Stand v1.99.63):**

| Befund | Beleg |
|---|---|
| Das Skript liest `DataProtection:KeysPath` **nur aus dem Web** | `scripts/deploy/Update-IdealAkeWms.ps1:223` |
| Die Ordner-Prüfung `v1.99.14-DataProtectionDienst` setzt `{DataProtection}` = dieser Web-Wert. Am 4445 hat sie also das Leserecht des Dienstkontos auf `C:\AKE\keys\dev` geprüft, nicht auf den Ordner, den der Dienst wirklich liest | `nachlauf.psd1:337-338` (Backlog nennt noch `:212-215`, inzwischen verschoben); `IdealAkeWms.Deploy.psm1:142, 147`; Aufrufe `Update-IdealAkeWms.ps1:750, 852` |
| Ohne Wert gilt in Web und Dienst derselbe Standard `%ProgramData%\IdealAkeWms\DataProtection-Keys` | `IdealAkeWms/Services/SageSdata/SdataZugangsdaten.cs:118-124`; Skript-Kopie `IdealAkeWms.Deploy.psm1:142` |
| Der Dienst liest `appsettings.json` und `appsettings.<Umgebung>.json` aus dem Exe-Ordner (Standard von `Host.CreateApplicationBuilder`, Arbeitsordner = Exe-Ordner) | `IDEALAKEWMSService/Program.cs:11, 19` |
| Der Dienst öffnet den Ring nur lesend; ein anderer Ordner ergibt „nicht entschlüsselbar“, ohne Rückfall | `SdataZugangsdaten.cs:131-136`; ADR 0024 „Reihenfolge“ |
| Der Installer schreibt `KeysPath` nur in die Web-`appsettings.Production.json`, nicht in die Dienst-`appsettings.json`. Mit einem anderen Pfad als dem Standard (Parameter `-DataProtectionPfad`) entsteht dieselbe Abweichung schon bei der Installation | `Install-IdealAkeWms.ps1:55, 268` (Web) gegen `:347-366` (Dienst: nur Verbindungen und Mail) |
| Das Muster „Einstellung aus der Dienst-appsettings prüfen, Phase Vorher, Prod = Abbruch vor dem Stoppen, sonst Hinweis“ gibt es schon (`Pruefung/MailPickup`, v1.99.12) | `Update-IdealAkeWms.ps1:414-425, 705, 745`; `nachlauf.psd1:15-17, 241-244`; Drift-Test `NachlaufManifestDriftTests.cs:208-222` |
| Die Nachlauf-Liste zeigt jede Prüfung noch einmal mit dem Stand danach | `Update-IdealAkeWms.ps1:923-930` |

Nutzen: Vor dem Stoppen sieht der Mensch beide Werte und die Datei, aus der sie stammen. Ein kopierter DEV-Wert fällt beim Update auf
und nicht erst beim nächsten Sage-Lauf.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Prüfung `Pruefung/DataProtectionKeysPathGleich` vergleicht den wirksamen Schlüsselring-Pfad von Web und Dienst (Abschnitt „Fachliche
   Anforderungen“ A).
2. Prüfung `Pruefung/AppsettingsDevPfad` sucht DEV-Werte in den Web- und Dienst-appsettings eines Nicht-DEV-Systems (B).
3. Die Ermittlung zeigt eine Zeile „Schluessel“ mit Web- und Dienst-Pfad (C).
4. Der Installer schreibt `DataProtection:KeysPath` auch in die Dienst-`appsettings.json`, also dieselbe Ursache an der Quelle (D).
5. Betriebshandbuch `07-troubleshooting.md:48`, README-Update-Abschnitt, TS-126, Drift-Test, Versionssprung.

**Out-of-Scope**
- Automatische Korrektur der appsettings. Die Dateien sind nicht im Paket und werden nie überschrieben (ADR
  [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]], „Paket“). Es bleibt bei Anzeige bzw. Abbruch.
- Umzug auf einen eigenen Schlüsselordner je Instanz. Das ist Rückfrage 1. Der Kern dieser Spec gilt mit jeder Antwort.
- Konfiguration über Umgebungsvariablen (`DataProtection__KeysPath` in `web.config` bzw. in der Dienst-Registry). Die Lese-Logik
  `Get-Einstellung` (`Update-IdealAkeWms.ps1:144-154`) kennt nur die JSON-Dateien, genau wie die DB-Ermittlung. Dieser Weg wird heute
  nirgends genutzt (Grep `KeysPath` über `*.json`: kein Treffer im Repo).
- DPAPI-Schutz des Schlüsselrings (ADR 0024, Negativ/Risiken: eigene Aufgabe).
- Kein Lauf gegen akenet01 (4443/4444/4445), idealweb01, AKESQL20 oder Sage durch Dev oder QA. Zielsysteme fährt nur Gerald von Hand
  (wie [[2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung-spec]], „Grenzen“).

## Fachliche Anforderungen

### A) `Pruefung/DataProtectionKeysPathGleich`

- **Wirksamer Pfad** je Seite = Wert aus `Get-Einstellung` (Web: `$e.Web`, `$e.UmgebungWeb`; Dienst: `$e.Dienst`, `$e.UmgebungDienst`).
  Ein leerer Wert oder einer nur aus Leerzeichen bedeutet den Standard `%ProgramData%\IdealAkeWms\DataProtection-Keys`, genau wie
  `SdataZugangsdaten.cs:121-123`.
- **Vergleich:** `Trim()`, `TrimEnd('\')`, ohne Unterscheidung von Groß- und Kleinschreibung (Windows-Pfade). Umgebungsvariablen werden
  **nicht** aufgelöst. Grund: `SchluesselPfad` löst sie auch nicht auf, `%X%` stünde für beide Prozesse als Text im Pfad. Deshalb wird
  `Get-PfadNorm` (`:161`) hier nicht verwendet.
- **Ergebnis:** 0 = gleich, 1 = verschieden.
- **Anzeige:** beide wirksamen Werte und je die Quelle, also die Datei oder `Standard (kein Wert)`. Die Pfade sind kein Geheimnis und dürfen
  ins Protokoll.
- **Phase `Vorher`, `Ausfuehrung = anzeigen`, Firma `beide`, alle Systeme.** Es gilt die bestehende Regel der Phase Vorher (`:705`, `:745`):
  - Prod-System (`Prod=$true`): Abbruch vor dem Stoppen, „Nichts gestoppt, Web und Dienst laufen weiter“.
  - Andere Systeme: Hinweis in der Vorprüfung und erneut gelb im Block „Prüfungen“ der Nachlauf-Liste.
  - Rückfrage 2 entscheidet, ob Nicht-Prod-Systeme auch abbrechen. Ohne Antwort gilt die bestehende Regel.
- Gilt auch bei `-NurPruefen` und `-Komponente Web|Dienst`, wie alle Prüfungen (kein Versionsfilter, `nachlauf.psd1:12`).

### B) `Pruefung/AppsettingsDevPfad`

- Durchsucht **alle Textwerte** (rekursiv, auch in Arrays) in vier Dateien, soweit vorhanden:
  - Web: `appsettings.json` und `appsettings.<UmgebungWeb>.json`;
  - Dienst: `appsettings.json` und `appsettings.<UmgebungDienst>.json`.
- Muster (Regex, ohne Unterscheidung von Groß- und Kleinschreibung): `D:\Dev\` und, falls Rückfrage 3 nicht dagegen entscheidet, `localhost`
  als ganzes Wort. Die Werte stammen aus dem Backlog. Die DEV-Zeilen in `ziele.psd1:14-15` zeigen, dass DEV auf `D:\Dev\…` und
  `http://localhost:808x/` liegt.
- **Ergebnis:** Anzahl Treffer.
- **Anzeige** je Treffer: `<Datei>: <Abschnitt:Schluessel>` (JSON-Pfad mit `:` wie in .NET), **ohne den Wert**. Werte unter
  `ConnectionStrings` und `MailSettings` können Kennwörter enthalten (`Install-IdealAkeWms.ps1:359`), und das Protokoll ist ein Transcript.
- **Nur Nicht-DEV-Systeme:** Manifest-Feld `System=@('Prod','Test','Test-IDEAL')`. Das vorhandene Filter `Test-PasstFirma` (`:263-267`)
  vergleicht innerhalb der Firma, die Liste deckt also beide Firmen ab (`ziele.psd1:9-13`). Es braucht keinen neuen Ausschluss-Mechanismus.
  Grenze: Ein neues Zielsystem muss in diese Liste eingetragen werden.
- Phase `Vorher`, `anzeigen`, Firma `beide`: Prod bricht vor dem Stoppen ab, die übrigen Systeme bekommen einen Hinweis. Das ist dieselbe
  Regel wie in A.
- Der Fall 4445 (Dienst-Wert `D:\Dev\AKE\keys`) löst A und B aus. Beide Meldungen erscheinen. Das ist gewollt: A nennt die Folge, B die
  Ursache.

### C) Ermittlung

- Neue Anzeigezeile nach „Mail“ (`:662`):
  `Schluessel     Web <wirksam> | Dienst <wirksam>` (bei Standard: Suffix `(Standard)`).
- Die Ermittlung bricht wegen der Abweichung **nicht** ab. Das bleibt Sache der Prüfung, damit die Regel „Prod = Abbruch, sonst Hinweis“
  nur an einer Stelle steht (`:705`).

### D) Installer

- `Install-IdealAkeWms.ps1` schreibt `DataProtection.KeysPath = $DataProtectionPfad` auch in die Dienst-`appsettings.json`, im Block
  `:347-365`. Hat das JSON-Objekt den Abschnitt nicht, wird er angelegt (`Add-Member -Force`), denn die Dienst-`appsettings.json` des Repos
  hat keinen Abschnitt `DataProtection`. Die Erfolgsmeldung `:366` nennt „DataProtection“ mit.
- Das Verhalten bleibt sonst unverändert.

## Technischer Loesungsentwurf

Es kommt kein neuer Manifest-Typ und kein neuer Ablaufschritt dazu. Die bestehende Appsettings-Prüfung (v1.99.12, `:414-425`) bekommt zwei
optionale Felder. Der Rest läuft über den vorhandenen Weg: Vorprüfung `:699-708`, Prod-Sperre `:705/:745`, Nachlauf `:923-930`.

| Manifest-Feld (neu) | Wirkung in `Get-PruefStand` | Platzhalter in `Format-PruefText` |
|---|---|---|
| `AppsettingsGleichWeb=$true` (nur zusammen mit `Appsettings`) | wirksamer Dienst-Wert und wirksamer Web-Wert desselben Schlüssels: gleich → 0, sonst 1 | `{Wert}`/`{Datei}` = Dienst (wie bisher), neu `{WebWert}`/`{WebDatei}`; Standard → Datei `Standard (kein Wert)` |
| `AppsettingsMuster='<regex>'` (statt `Pruefung`/`Appsettings`) | Anzahl Treffer über die vier Dateien | neu `{Treffer}` = Liste `<Datei>: <Schluessel>`, mit `; ` getrennt |

- **Standardpfad an einer Stelle:** Eine neue exportierte Funktion `Get-DataProtectionPfad([string]$wert)` im Modul ersetzt die Inline-Zeile
  `IdealAkeWms.Deploy.psm1:142`. Die Ordner-Prüfung und der Vergleich nutzen beide diese Funktion. Die C#-Regel bleibt
  `SdataZugangsdaten.SchluesselPfad` und wird nicht dupliziert, außer als dieser eine Standardpfad (heute schon so, `:142`).
- **JSON-Durchlauf für B:** eine kleine rekursive Funktion über `PSCustomObject`-Eigenschaften und Arrays aus `ConvertFrom-Json`. Bei
  ungültigem JSON bricht sie lesbar ab wie `Get-Einstellung` (`:149`).
- **Platzhalter-Kollision:** `Format-Text` ersetzt `{Web}` und `{Dienst}` (`:271-272`). `{WebWert}` und `{WebDatei}` enthalten die Zeichenkette
  `{Web}` nicht, es gibt also keine Kollision. Die Reihenfolge bleibt: zuerst `Format-Text`, dann die Appsettings-Platzhalter (`:417-418`).
- **Bekannte Grenze** (im Skript als `ponytail:`-Kommentar):
  - `Get-Einstellung` übernimmt einen leeren Wert der Umgebungsdatei nicht (`:150`).
  - .NET dagegen überschreibt mit leer, und das bedeutet dann den Standard.
  - Abweichen kann das Ergebnis nur, wenn `appsettings.json` selbst einen `KeysPath` trägt. Das tut heute keine Datei im Repo (Grep `KeysPath`
    über `*.json`: kein Treffer).
  - Upgrade-Pfad: In `Get-Einstellung` einen vorhandenen, leeren Schlüssel als „gesetzt auf leer“ werten.
- Nur ASCII in `scripts/deploy/*`, weil der Drift-Test `DeploySkripte_NurAscii…` das prüft.
- Die psd1-Literale sind einfach gequotet. `\b` und `\\` stehen in `'…'` wörtlich, so wie es das Regex braucht.

**Manifest-Einträge (Vorgabe; der Dev-Lauf darf den Text glätten, nicht fachlich ändern):**

```powershell
@{ Id='Pruefung/DataProtectionKeysPathGleich'; Version='<neue Version>'; Typ='Pruefung'; Firma='beide'; Phase='Vorher'; Ausfuehrung='anzeigen'
   Appsettings='DataProtection:KeysPath'; AppsettingsGleichWeb=$true
   Text='Schluesselring verschieden: Web {WebWert} ({WebDatei}), Dienst {Wert} ({Datei}). Der Dienst kann das SData-Kennwort dann nicht entschluesseln und sendet nichts an Sage (ADR 0024). Dienst-Wert in der Datei auf den Web-Wert setzen, dann Skript neu starten.'
   Quelle='<changelog-slug>' }
@{ Id='Pruefung/AppsettingsDevPfad'; Version='<neue Version>'; Typ='Pruefung'; Firma='beide'; System=@('Prod','Test','Test-IDEAL'); Phase='Vorher'; Ausfuehrung='anzeigen'
   AppsettingsMuster='(?i)D:\\Dev\\|\blocalhost\b'
   Text='DEV-Werte in der Konfiguration dieses Systems (offenbar kopiert): {Treffer}. Werte von Hand auf dieses System korrigieren, dann Skript neu starten.'
   Quelle='<changelog-slug>' }
```

**Drift-Test** (`NachlaufManifestDriftTests.cs`):
- `:182`: Jeder Eintrag hat genau eines von `Pruefung`, `Appsettings`, `AppsettingsMuster`.
- Für `AppsettingsMuster` gilt dieselbe Regel wie für `Appsettings` (`:188-189`): Phase `Vorher|Nachher`, nur `anzeigen`.
  Das Muster muss als .NET-Regex kompilieren.
- `AppsettingsGleichWeb` nur zusammen mit `Appsettings`.
- Neuer Test `Pruefung_KeysPathUndDevPfad_VorherAnzeigen` prüft:
  - beide Ids, Firma `beide`, Phase `Vorher`;
  - `AppsettingsDevPfad.System` enthält `DEV` nicht;
  - das Muster trifft `D:\Dev\AKE\keys`, aber nicht `C:\AKE\keys\dev`. Das ist der 4445-Fall: Der Web-Wert ist korrekt und darf nicht treffen.
  - das Skript enthält `{WebWert}` und `AppsettingsMuster`.

## Migrations-/SQL-Auswirkungen

Keine. Es gibt kein SQL-Skript, keine Migration und keine FreshInstall-Änderung. Die Prüfungen lesen nur Dateien und brauchen keinen
DB-Zugriff. Sie laufen in der Vorprüfung, nach der Frage „Verbindung … herstellen“ (`:671`), so wie `Pruefung/MailPickup`.

## Einstellungen

keine. `DataProtection:KeysPath` ist ein bestehender appsettings-Schlüssel (ADR 0008), Bedeutung und Default ändern sich nicht. Die Spec
fügt keinen Schlüssel in AppSettings oder ServiceSettings hinzu.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Es gibt keine DB-Schreibzugriffe. Die Prüfungen schreiben keinen Marker (wie alle Prüfungen, Vorgänger-Spec v1.99.0, Ablauf Punkt 5).
Nachweis ist das Protokoll (Transcript).

## Akzeptanzkriterien

1. Web- und Dienst-`KeysPath` verschieden, Nicht-Prod-System, `-NurPruefen`:
   - Die Vorprüfung zeigt `[Pruefung/DataProtectionKeysPathGleich] Abweichung 1 -> Hinweis.` mit beiden wirksamen Pfaden und beiden Dateien.
   - Exit 0, nichts gestoppt.
2. Dieselbe Lage auf einem Prod-System (Volllauf): `ABBRUCH: Produktivsystem …: [Pruefung/DataProtectionKeysPathGleich] Abweichung 1: …`
   **vor** der Backup-Frage und vor dem Stoppen. App-Pool und Dienst laufen weiter, Exit 1.
3. Beide Werte gleich, auch bei anderer Schreibweise (Groß/Klein, abschließender `\`): `OK`.
4. Beide Werte leer bzw. nicht vorhanden: `OK`.
5. Web gesetzt auf einen Pfad ungleich Standard, Dienst leer: `Abweichung 1`. Der Dienst-Wert zeigt dann den Standardpfad mit Datei
   `Standard (kein Wert)`.
6. Der Dienst-Wert steht nur in `appsettings.<UmgebungDienst>.json`: Die Prüfung nimmt ihn von dort, die Umgebungsdatei gewinnt wie bei der
   DB-Ermittlung.
7. Die Ermittlung zeigt `Schluessel     Web <Pfad> | Dienst <Pfad>`.
8. Ein Wert `D:\Dev\…` in einer der vier Dateien eines Nicht-DEV-Systems ergibt `[Pruefung/AppsettingsDevPfad] Abweichung n`. Die Liste zeigt
   Datei und Schlüssel und **keinen Wert**. Prod-System: Abbruch vor dem Stoppen.
9. Ein DEV-System (`System='DEV'`) bekommt `Pruefung/AppsettingsDevPfad` nicht (die Prüfung fehlt in der Liste). Die Gleichheitsprüfung
   läuft dort trotzdem.
10. `C:\AKE\keys\dev` trifft das DEV-Muster nicht.
11. Steht `localhost` in einem Verbindungs-String, zeigt die Ausgabe nur `<Datei>: ConnectionStrings:<Name>` und nie den Verbindungs-String.
    Gilt nur, solange `localhost` im Muster bleibt (Rückfrage 3).
12. Die Nachlauf-Liste zeigt beide Prüfungen im Block „Prüfungen (Stand jetzt)“.
13. Eine Installation mit `-DataProtectionPfad D:\X` schreibt `DataProtection:KeysPath = D:\X` in Web-`appsettings.Production.json` **und**
    Dienst-`appsettings.json`. Ein anschließendes `Update-IdealAkeWms.ps1 -NurPruefen` meldet `OK`. Der Nachweis erfolgt per Durchlauf gegen
    DEV-Ordner oder per Code-Durchsicht im QA-Lauf, kein Lauf auf einem Zielserver.
14. `NachlaufManifestDriftTests` ist grün, inklusive des neuen Tests. Rotprobe: Wird `System` von `Pruefung/AppsettingsDevPfad` auf `DEV`
    erweitert, wird der Test rot.
15. `docs/betrieb/07-troubleshooting.md:48` nennt den Fall „Dienst-appsettings mit kopiertem DEV-Pfad“ und die beiden neuen Prüfungen. Die
    Quellenangabe zeigt auf den aktuellen Manifest-Eintrag, `nachlauf.psd1:212-215` ist veraltet.
16. Die `scripts/deploy/*` bleiben ASCII. Kein Agent hat einen Lauf gegen ein Zielsystem gemacht.

## Test-Szenarien

Erweiterung `docs/TESTSZENARIEN.md` TS-126. Die nächste freie Zeile ist 126.32; der Dev-Lauf prüft das.
- **Vorbedingungen:** DEV AKE (`ziele.psd1:14`), Paketkopie. Web-`KeysPath` = `D:\Dev\AKE\keys`, gleich in der Dienst-`appsettings.DEV.json`
  (`docs/TESTSZENARIEN.md:11831`). Für Prod-Fälle setzt die `ziele.psd1` der Paketkopie die DEV-Zeile auf `Prod=$true`, wie bei 126.29.

| Nr | Lage | Erwartung |
|---|---|---|
| 126.32 | Ausgangslage DEV, `-NurPruefen` | Ermittlung `Schluessel Web D:\Dev\AKE\keys \| Dienst D:\Dev\AKE\keys`; `[Pruefung/DataProtectionKeysPathGleich] OK`; `Pruefung/AppsettingsDevPfad` fehlt (DEV) |
| 126.33 | Dienst-`appsettings.DEV.json` `KeysPath` = `D:\Dev\AKE\keys-anders`, `-NurPruefen` | `Abweichung 1 -> Hinweis.` mit beiden Pfaden und Dateien, Exit 0; Nachlauf zeigt die Zeile gelb |
| 126.34 | wie 126.33, DEV-Zeile `Prod=$true`, Volllauf | `ABBRUCH: Produktivsystem …` vor der Backup-Frage; Zustand App-Pool/Dienst laufen; Exit 1 |
| 126.35 | Dienst-`KeysPath` entfernt, Web gesetzt | `Abweichung 1`, Dienst-Seite `C:\ProgramData\IdealAkeWms\DataProtection-Keys (Standard (kein Wert))` |
| 126.36 | Schreibweise `d:\dev\ake\keys\` im Dienst | `OK` |
| 126.37 | Paketkopie: `System`-Filter von `Pruefung/AppsettingsDevPfad` entfernt (Test-Manifest wie 126.12), `-NurPruefen` | Treffer für `D:\Dev\…`-Werte und `localhost` (falls im Muster) mit `<Datei>: <Schluessel>`, keine Werte (Protokoll durchsuchen: kein `Password=`, kein `Server=`) |
| 126.38 | wie 126.37 mit `Prod=$true` | Abbruch vor dem Stoppen mit derselben Liste |
| 126.39 | Install-Durchlauf gegen DEV-Testordner (falls ohne Zielserver möglich, sonst Code-Durchsicht) mit `-DataProtectionPfad` | `KeysPath` in Web- und Dienst-Datei gleich |
| 126.40 | Drift-Test | grün; Rotprobe aus AK 14 |
| 126.41 | **Mensch (Gerald):** `-NurPruefen` mit dem neuen Paket auf 4444, 4445, 4443, IDEAL-Test, IDEAL-Prod | Gleichheit OK bzw. Abweichung beheben; DEV-Treffer prüfen (Rückfrage 3); 4445: nach der Korrektur des Dienst-Werts Dienst neu starten und den Sage-WE-Lauf im Aktivitäts-Protokoll prüfen |

Nach jedem Fall Dateien der DEV-Umgebung zurücksetzen. Der Dev-Lauf trägt die Fälle in den Testindex ein ([[testszenarien-index]]).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Vorläufig. Der QA-Lauf bestätigt das gegen den echten Diff.*

- **Web-App:** ja, nur Versionssprung (`IdealAkeWms/AppVersion.cs`, Anwender-Changelog). Das Update-Skript vergleicht die Fußzeile mit der
  Paketversion.
- **Service:** ja, nur Versionssprung (`IDEALAKEWMSService/AppVersion.cs`).
- **Migration:** nein.
- **Paket-Inhalt, der wirkt:** `deploy/Update-IdealAkeWms.ps1`, `deploy/IdealAkeWms.Deploy.psm1`, `deploy/Install-IdealAkeWms.ps1`,
  `deploy/nachlauf.psd1`. Die Prüfungen wirken ab dem ersten Lauf aus diesem Paket.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):
  - `Pruefung/DataProtectionKeysPathGleich`: Version = neue Version, Typ `Pruefung`, Firma `beide`, Phase `Vorher`, Ausfuehrung `anzeigen`.
    Text siehe Lösungsentwurf (verschiedene Schlüsselringe, Folge Sage, Korrektur der Dienst-Datei). Datei-Prüfung, kein SQL.
  - `Pruefung/AppsettingsDevPfad`: Version = neue Version, Typ `Pruefung`, Firma `beide`, `System=@('Prod','Test','Test-IDEAL')`, Phase
    `Vorher`, Ausfuehrung `anzeigen`. Text siehe Lösungsentwurf (DEV-Werte mit Datei und Schlüssel, ohne Wert).
  - Kein Daten-, Einmal- oder Schema-Eintrag, kein Ordner-Eintrag. `v1.99.14-DataProtectionDienst` (`:337-338`) bleibt unverändert; nach
    Herstellen der Gleichheit prüft er den richtigen Ordner.
  - Der Hinweis im Info-Eintrag `:472` („…braucht die Dienst-appsettings.json denselben Wert“) bleibt stehen. Ab jetzt prüft das Skript ihn
    selbst.
- **Hand-Schritt am 4445** (Befund, nicht Teil des Pakets): Dienst-`appsettings.Production.json:48` von `D:\Dev\AKE\keys` auf den Web-Wert
  `C:\AKE\keys\dev` setzen. Je nach Rückfrage 1 kommt ein anderer Wert. Danach den Dienst neu starten und die zwei offenen Sage-WE prüfen.
  Ob das schon geschehen ist, weiß nur Gerald. Die neue Prüfung zeigt es beim nächsten Lauf.
- **Version:** nächste freie Patch-Version nach v1.99.63 (`IdealAkeWms/AppVersion.cs:5`); der Dev-Lauf prüft das.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
  Danach `SQL\` und `deploy\` ins Paket kopieren wie bisher.

**Brain-Nachträge (Hauptcheckout, Dev-Lauf):**
- Changelog-Eintrag;
- [[fallstricke]] §33, erster Punkt, um den Satz „Das Update-Skript prüft die Gleichheit (`Pruefung/DataProtectionKeysPathGleich`, Phase
  Vorher)“ ergänzen;
- Testindex;
- Die Backlog-Notiz setzt Gerald.

## Offene Rueckfragen

1. **Eigener Schlüsselordner je Instanz statt `C:\AKE\keys\dev`?** Laut Backlog teilen sich 4444 und 4445 den Ordner womöglich. Das ist nicht
   belegt.
   - **Prüfschritt P-1 (Gerald, akenet01):** `DataProtection:KeysPath` in Web- und Dienst-`appsettings*.json` von 4443, 4444 und 4445 ablesen.
     Ab dem neuen Paket zeigt `-NurPruefen` das in der Zeile „Schluessel“.
   - Belegt zur Einordnung: Alle Instanzen nutzen denselben `ApplicationName` (`IdealAkeWms/Program.cs:46`) und denselben Session-Cookie-Namen
     (`:60`). Der Session-Speicher liegt im Arbeitsspeicher je Prozess (`:54`).
   - Ein gemeinsamer Ring macht `dp1:`-Werte zwischen den Instanzen gegenseitig entschlüsselbar, etwa nach einer DB-Kopie.
   - **Folge eines Wechsels** (ADR 0024, Negativ/Risiken):
     - Je Instanz sind Web- und Dienst-Datei anzupassen und das Recht zu setzen. Das Recht setzt das Update-Skript in 5a.
     - Das SData-Kennwort ist unter `/ServiceSettings` neu einzugeben.
     - Angemeldete Sitzungen und offene Formulare verlieren ihre Tokens.
   - Wenn ja: Zielpfad je Instanz nennen. Das wird dann ein Info- bzw. Hand-Schritt im Manifest dieser Spec.
2. **Abweichung KeysPath Web/Dienst auf einem Nicht-Prod-System:** Reicht ein Hinweis, oder soll das Skript auch dort vor dem Stoppen abbrechen?
   - **Empfehlung: Hinweis.** Das ist die bestehende Regel der Phase Vorher (`Update-IdealAkeWms.ps1:705`) mit Drift-Test
     (`NachlaufManifestDriftTests.cs:220`). Eine Sonderregel nur für diese Prüfung bräuchte ein neues Feld.
   - Gegenargument: Der Befund stammt von einem Testsystem (4445), und dort blieben echte Sage-WE hängen.
3. **Soll das DEV-Muster neben `D:\Dev\` auch `localhost` enthalten?**
   - Ob eine Prod- oder Test-appsettings `localhost` legitim nutzt (z. B. SMTP-Relay oder SQL auf demselben Server), ist im Repo nicht
     ablesbar. Die Zieldateien liegen nur auf den Servern; die Repo-appsettings enthalten kein `localhost`.
   - Ein legitimer Treffer würde das Prod-Update vor dem Stoppen abbrechen, bis der Wert geklärt ist. Es wird dabei nichts geändert.
   - **Empfehlung:** `localhost` aufnehmen. Vor dem ersten Prod-Lauf zeigt `-NurPruefen` (126.41) die Treffer gefahrlos.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

– (keine Fachbereichsfragen; kein Jira-Vorgang)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Eigener Schlüsselordner je Instanz statt `C:\AKE\keys\dev` (Zielpfad je Instanz)? →
2. KeysPath-Abweichung auf Nicht-Prod: Hinweis (Empfehlung) oder Abbruch? →
3. `localhost` im DEV-Muster (Empfehlung ja)? →
