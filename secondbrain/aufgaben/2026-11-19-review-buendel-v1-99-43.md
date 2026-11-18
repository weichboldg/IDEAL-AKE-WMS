---
type: aufgabe
created: 2026-11-18
status: offen
thema: Kritische Prüfung Bündel v1.99.27–v1.99.43 vor Schranke 2
worktree: .claude/worktrees/2026-08-07-ideal-teile-1-5
branch: feature/2026-08-07-ideal-teile-1-5
---

# Review Bündel v1.99.27–v1.99.43 (kritische Prüfung)

Gegenstand ist der Zweig `feature/2026-08-07-ideal-teile-1-5` (HEAD 3a6372a5, v1.99.43) gegen `main`. Merge-Base ist 1753c79e. Der Gesamtdiff umfasst 1103 Dateien und 546 Commits. Schwerpunkt waren die Versionen v1.99.27 bis v1.99.43, ab 5163df17 (v1.99.26).
Die Prüfung war rein lesend: kein Code geändert, kein Status gesetzt, kein Merge. Zugehörig: [[project_buendel_2026_11_08_lauf2]], [[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]], [[0004-migrations-und-sql-disziplin]].

## Ergebnis je Schwerpunkt

| Schwerpunkt | Befund |
|---|---|
| SQL 154–163 idempotent, in Reihenfolge | Alle Skripte haben Guards (`COL_LENGTH`/`OBJECT_ID`/`sys.indexes`), die DDL steht in eigenen Batches und die History separat. Zwei kleine Abweichungen, siehe H-1 und H-2 |
| FreshInstall vollständig | Ja, Schema und MigrationId für 152–163 vorhanden, siehe H-6 |
| nachlauf.psd1 konsistent | Ja, Schema-Einträge `scripts/deploy/nachlauf.psd1:99-122` stehen in Deploy-Reihenfolge, die Einmal-Skripte sind je zweimal referenziert. Risiko liegt bei der Vorher-Prüfung, siehe S-1 |
| Sage-Schreibwege hinter Schaltern | Sauber, siehe H-5 |
| Einstellungen nach ADR 0020 | Die neuen Keys sind vollständig. Lücken gibt es nur im Bestand, siehe H-7 |
| Keine fest codierten USER-Felder | Kein Verstoß. Die Lesewege laufen über `SageUserfelder`. Literale stehen nur als Default, als logischer Name oder in Meldungstexten, siehe H-3 und H-4 |
| Konflikte mit main | Keine. `git merge-tree --write-tree main HEAD` endet mit Exit 0. main ist 191 Commits voraus, berührt aber nur Brain und `.claude/agents/{qa,spec}-agent.md`; der Zweig ändert `secondbrain/` nicht |

## BLOCKER

Keine. Ich habe gezielt nach Gründen gesucht, die gegen den Merge sprechen: Reihenfolge und Abbruchpfade der SQL-Skripte, die FreshInstall-Lücke, Sage-Schreibwege ohne Schalter, Settings ohne Pflichtangaben und Merge-Konflikte. Bei keinem dieser Punkte bin ich auf einen belegten Fehler gestoßen. Das größte Risiko betrifft den **Deploy**, nicht den Merge, und steht deshalb als S-1 da.

## SOLLTE

**S-1 — Die Vorher-Prüfung `FaStrukturSchluesselDoppelt` kann den Prod-Deploy sperren, und es gibt keinen Reparaturweg.**
- `scripts/deploy/nachlauf.psd1:127`: Phase `Vorher`, Ausführung `anzeigen`, Text „nicht automatisch bereinigen“.
- `scripts/deploy/Update-IdealAkeWms.ps1:701`: Ein Treffer größer 0 bricht auf Prod vor dem Stoppen ab.
- `SQL/159_FaStrukturAbgleich.sql:76-79` und `:89-92`: RAISERROR plus `SET NOEXEC ON`, falls doch Dubletten da sind.
- Ob Prod oder Test heute Dubletten in `FaHierarchyNodes` oder `FaHierarchyOrderInfos` hat, weiß niemand. Der alte Dienst schreibt per Full-Replace.

Vorschlag: Gerald lässt die Prüfabfrage vor dem Wartungsfenster **lesend** auf Test und Prod laufen. Das ist das `Pruefung=`-Statement aus `nachlauf.psd1:127`, oder `Update-IdealAkeWms.ps1 -NurPruefen`. Gibt es Treffer, gehört der Bereinigungsweg (wer, wie, P-AB1/P-AB2) in [[2026-11-12-deploy-fahrplan-buendel-v1-99]]. Rückfrage dazu: *Ist die Abfrage auf Prod schon gelaufen, und wer bereinigt bei Treffern?*

**S-2 — Die Prüfabfrage ist strenger als der Index und kann den Deploy ohne Grund sperren.**
- `nachlauf.psd1:127` gruppiert über `NULLIF(LTRIM(RTRIM(Artnr)), '')`, genauso für `ABNr` und `Pos`.
- `SQL/159_FaStrukturAbgleich.sql:68-70` setzt nur leer auf NULL, und der UNIQUE-Index (`:82`, `:95`) vergleicht den Rohwert.
- Der Abgleich normalisiert ebenfalls nur mit `TrimEnd` (`IDEALAKEWMSService/Services/FaStrukturAbgleich.cs:19`).
- Folge: Werte, die sich nur durch **führende** Leerzeichen unterscheiden (`' 4711'` gegen `'4711'`), zählt die Prüfung als Dublette, obwohl Index und Abgleich sie akzeptieren. Auf Prod endet das in einem Abbruch ohne echten Grund.

Vorschlag: Die Prüfung auf `NULLIF(RTRIM(x), '')` ohne `LTRIM` angleichen. Das ist eine Zeile im Manifest, keine Migration.

## HINWEIS

**H-1 — Der History-Insert in SQL/155 und SQL/156 prüft das Objekt nicht.**
`SQL/155_AddArticleLastOrderUnassigned.sql:146` und `SQL/156_...:198` tragen die MigrationId ein, ohne zu prüfen, ob die DDL gelaufen ist; 157–163 prüfen das. Über das Update-Skript ist das unkritisch, weil sqlcmd mit `-b` beim ersten Fehler abbricht (`Update-IdealAkeWms.ps1:245-250`). Ein Risiko besteht nur bei einem Lauf von Hand in SSMS.

**H-2 — Die SQL-Nummern folgen nicht den Zeitstempeln der Migrationen.**
Die Zeitstempel sind 155 = 20261116…, 154 = 20261117075856 und 153 = 20261117085624. Das Manifest führt 152, 155, 154, 153 (`nachlauf.psd1:99-122`), also in Zeitstempel-Reihenfolge. Es gibt keine Abhängigkeit zwischen den drei, daher ist das unkritisch. Beim Lauf von Hand gilt die Reihenfolge des Manifests, nicht die Dateinummer.

**H-3 — Das Literal in `SQL/163_AddBfPositionArbeitsschritte.sql:34` ist kein ADR-0033-Verstoß.**
Dort steht `d.SyncFieldName = N'USER_OSArbeitsschritte'`, das ist der **logische** Merkmalname. Die Blechteile-Merkmale werden immer unter dem logischen Namen gespeichert, egal wie das Sage-Feld konfiguriert ist (`IDEALAKEWMSService/Services/Sdata/SdataArtikelReader.cs:348`). Der Laufzeit-Leseweg liest denselben Namen (`IdealAkeWms/Services/BfArtikelFelder.cs:18`, `IdealAkeWms/Data/Repositories/BfAuftragRepository.cs:90`).
Nebeneffekt: Der Nachtrag füllt `Arbeitsschritte` auch an schon abgeschickten BF-Aufträgen nach. Eine bereits geschriebene CSV bleibt unverändert, die Anzeige zeigt danach aber einen Wert, der nicht in der CSV stand.

**H-4 — USER-Literale: Alle Lesewege laufen über die Einstellung, verbliebene Literale sind Defaults oder Texte.**
- Lesewege über `SageUserfelder` (ADR 0033):
  - `SdataFaStrukturReader.cs:97-100` und `:120-129`
  - `KontokorrentSyncService.cs:92-114`
  - `SdataArtikelReader.cs:300-302` und `:348-361`
  - `EinkaufBestellungenSyncService.cs:277-278`
  - `SageArbeitsplatzReader.cs:27-29`
  - Views über `SageUserfelder.Feld`: `PurchaseProposals/Index.cshtml:9`, `Debitors/Details.cshtml:100`, `Creditors/Details.cshtml:106`
- Die verbliebenen Literale sind logische Namen oder Default-Konstanten:
  - `SdataFaStrukturReader.cs:167-178`, `:260-272`
  - `SageArbeitsplatzReader.cs:84`, `:102` (Default, zeichengleich zum Bestand)
  - `BfArtikelFelder.cs:13-19`
- Einige Literale stehen nur in **Meldungstexten**: `WorkOperationStructureDetectionService.cs:235` und `:293`, `ProductionWorkplaceSyncService.cs:95`, `OperationConfigSyncService.cs:132`. Benennt die Einstellung das Feld um, nennt die Meldung noch den Default-Namen. Das ist kosmetisch, ein späterer Kandidat wäre `felder.Feld(...)` im Text.
- Die Beschriftungen in `Views/Shared/_ArtikelSageKarten.cshtml:36` und `:40` zeigen ebenfalls den Default-Namen. Ob `U(...)` dort den konfigurierten Namen einsetzt, habe ich nicht geprüft.

**H-5 — Sage-Schreibwege: alle hinter Schaltern mit Default `false`.**
- Defaults in `IdealAkeWms/Models/ServiceSettingDefinitions.cs:297`, `:298`, `:300`, `:311`, `:317`, `:342`, `:353` und `:402`.
- Abfragen im Worker `IDEALAKEWMSService/Workers/SageBookingWorker.cs`:
  - `:66` Lagerbuchung
  - `:73` WE-Beleg
  - `:254` BF-CSV
  - `:268` FA-Arbeitsgänge
  - `:327` Umbuchung
- Der Web-Client liest nur per GET (`IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:97`, `:248`, `:280`).
- Kleine Uneinheitlichkeit: `:66` liest mit `GetBoolSafe`, `:73` und `:268` mit `GetBoolAsync`. Beide liefern im Fehlerfall den Default `false`, also kein Schreiben.

**H-6 — FreshInstall ist für 152–163 vollständig.**
- Schema, Auswahl der Fundstellen in `SQL/00_FreshInstall.sql`:
  - `Users.Kuerzel` `:28` und `:572-573`
  - BdeTeileliste `:362`
  - ErstGesehen `:1966`, `:2000`, `:2039`
  - die beiden UX-Indizes `:1973` und `:2006`
  - AbNummer `:2407`
  - BdeTeilMeldungen `:2663` und `:2686`
  - VersendetByUserId `:2820`
  - Verpackt `:2857`
  - BestellerKuerzel `:2898`
- MigrationIds bei etwa `:3488-3537`.
- `VersendetByUserId` hat bewusst keinen FK, passend zur EF-Migration.

**H-7 — ADR 0020: Die neuen Einstellungen sind vollständig.**
- Neue ServiceSettings seit v1.99.26:
  - Anzeigename, Verwendung, Standort und Warnhinweis: `ServiceSettingDefinitions.cs:58-157`, mit Warnhinweisen dort, wo sie nach außen wirken: `:75-78` (Sage-AG) und `:109-122` (WiCAM-CSV, Mail).
  - Int mit Grenzen: `:215-233`.
  - YES/NO als Choice: `:116` und `:118`.
- Neue AppSettings (BF, Kommissionierliste) haben Grenzen, Abhängigkeit und Warnhinweis.
- **Bestand** aus der Zeit vor dem Bündel: 15 Int-Keys ohne min/max, z. B. `Sync:SageLagerbuchungBatchSize`, `Sync:BomCacheWeeks`, `WorkerSettings:SyncIntervalMinutes`. Das ist Teil des ADR-0020-Bestandsumbaus, kein Bündelbefund.

**H-8 — Reihenfolge Dienst-Stopp vor SQL/159.**
SQL/159 setzt voraus, dass der Dienst gestoppt ist, bevor das Skript läuft (Kopfkommentar). Das Update-Skript stoppt die Dienste vor den Schema-Skripten (`Update-IdealAkeWms.ps1:767`). Bei einem Deploy **von Hand** muss diese Reihenfolge im Fahrplan stehen.

## Empfehlung

**BEREIT ZUR FREIGABE** — Merge nach Schranke 2 (Manual-UAT). Vor dem Prod-Deploy S-1 klären (Prüfabfrage lesend auf Prod) und S-2 angleichen.
