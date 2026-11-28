---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: offen
modul: Update-Skript scripts/deploy (Update-IdealAkeWms.ps1, IdealAkeWms.Deploy.psm1, nachlauf.psd1, ziele.psd1)
stand: "Bündel v1.99.22, 90bbbbd7, feature/2026-08-07-ideal-teile-1-5"
---
# Review-Befunde Update-Skript (Teil A3, lesend)

Gegenstand: `scripts/deploy/` im Bündel-Worktree. Nichts behoben, nichts ausgeführt (Schranke 1). Bezug:
[[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]], [[2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung-spec]],
[[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]], [[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]].
Install-Skript-Befund zum SData-Kennwort steht in [[2026-11-14-review-befunde-sdata]] (SD-5), die Web-Lücke der Kennwort-Prüfung dort als SD-4.

## UP-1 – Neustart nach Abbruch ab Schritt 5 lässt offene Daten-Einträge still verschwinden
- **Schwere:** mittel
- **Fundstelle:** `scripts/deploy/Update-IdealAkeWms.ps1:836-837` (deploy-info.txt wird schon beim Kopieren geschrieben), `:675-688`
  (installierte Version: Fußzeile, sonst `deploy-info.txt`), `:268,524` (`Test-Neuer`/`Test-Faellig`), `:848,899` (Filter 5b/Nachlauf),
  `:318` (Text „bietet offene Einträge erneut an“)
- **Befund:** `deploy-info.txt` mit der **Paket**-Version landet in Schritt 5 im Ziel. Bricht der Lauf danach ab (5a Pflicht-Recht fehlt,
  5b, Schritt 6 Start/Fußzeile/Dienst), ist der App-Pool gestoppt; beim Neustart liefert die Fußzeile keine Version (503), also gilt
  `installiert = Paketversion`. `Test-Faellig` lässt Typ `Daten` nur bei `Version > installiert` zu → **alle** Daten-Einträge des Bündels
  (z. B. `SQL/147` VorStart, `SQL/110` Nachher-Backfill) werden weder angeboten noch in der Nachlauf-Liste gezeigt. Nur `Einmalig` bleibt
  (immer fällig). `Show-Zustand` verspricht für 5/5a/5b das Gegenteil.
- **Szenario:** AKE-Test, Schritt 6 bricht ab (Fußzeile HTTP 500) → Ursache behoben, Skript neu gestartet → `SQL/110` (Backfill
  `StockMovement.ProductionOrderId`) steht nirgends mehr, Lauf meldet Erfolg. „Melden statt still behandeln“ verletzt.
- **Vorschlag:** `deploy-info.txt` erst nach erfolgreichem Start (Schritt 6) schreiben **oder** für `Daten` den Versionsfilter fallen lassen
  (der Marker in `dbo.DeployScriptRuns` verhindert Doppelläufe ohnehin, ADR 0023). Pester-/Drift-Test für den Neustart-Fall.
- **DEV-Nachmessung 2026-11-14:** im Code unverändert (`Update-IdealAkeWms.ps1:836-837` schreibt `deploy-info.txt` beim Kopieren, `:682-683` liest sie als installierte Version, `:268` `Test-Neuer`). `-NurPruefen` AKE-DEV zeigt den Versionsfilter („SQL/110: nicht faellig (eingefuehrt mit v1.87.0, installiert v1.99.22)“). Abbruch in Schritt 5/6 nur über einen Volllauf mit Stopp auslösbar → auf DEV nicht gefahrlos, nicht gemacht. ([[04-uebrige-module]])

## UP-2 – COMMIT im Daten-Skript beendet die äußere Transaktion samt Marker
- **Schwere:** niedrig
- **Fundstelle:** `scripts/deploy/Update-IdealAkeWms.ps1:352-356` (`Test-SqlFuerTransaktion`), `:385-400`
- **Befund:** Abgelehnt werden nur sqlcmd-Direktiven und `ROLLBACK`. Ein unpaariges `COMMIT` (oder `USE <andere DB>`) in einem Fluss-A/B-Skript
  setzt `@@TRANCOUNT` auf 0: die Datenänderung ist sofort festgeschrieben, der Marker läuft außerhalb der Transaktion, „verwerfen“ greift nicht
  mehr (Rollback wirft, Meldung „zurückgerollt, DB unverändert“ wäre falsch).
- **Vorschlag:** `\bCOMMIT\b` und `^\s*USE\b` ebenfalls ablehnen (wie `ROLLBACK`), Drift-Test `Manifest_DatenUndEinmalSkripte_*` erweitern.

## UP-3 – Marker-Zeitpunkt nach SQL-Server-Uhr
- **Schwere:** Hinweis
- **Fundstelle:** `scripts/deploy/Update-IdealAkeWms.ps1:336` (`ExecutedAt = SYSDATETIME()`)
- **Befund:** ADR [[0025-zeitstempel-in-lokalzeit-des-schreibenden-rechners]] verlangt Lokalzeit des schreibenden Rechners und nennt die
  Zeitzone der SQL-Server ausdrücklich „nicht belegt“. Der Marker nimmt die Server-Uhr; Anzeige „erledigt am …“ (Zeile 332) kann um die
  Zeitzonen-Differenz abweichen. Kein Fehlverhalten, nur Konventionsbruch.
- **Vorschlag:** `@Jetzt = Get-Date` als Parameter, oder ADR-0025-Ausnahme dokumentieren.
- **DEV-Nachmessung 2026-11-14:** im Code unverändert (`:336`); auf DEV ohne sichtbare Wirkung (SQL-Server = DEV-PC). ([[04-uebrige-module]])

## Geprüft ohne Befund
- Prod-Schutz: `FIRMA-PROD` eintippen (case-sensitiv) je Prod-System vor dem Transcript (`:932-935`), Prod immer zuletzt (`:87`),
  Pruefung Phase Vorher auf Prod = Abbruch vor dem Stoppen (`:701,738`), `-NurPruefen` schreibt nichts.
- Abbruchpfade/Schleifen: alle Eingabeschleifen enden bei leerer/geschlossener Eingabe (`Read-Eingabe`, `Read-Ja` max. 5, Firma/System,
  Backup-Ordner, Fluss B, uebernehmen/verwerfen), Dienst-Stopp 60 s ohne Kill, App-Pool-Stopp 60 s; `SQLCMDPASSWORD` wird im `finally`
  entfernt (`:944`), Passwort nie in Argumenten/Transcript.
- Idempotenz: Marker vor Spur vor Ausführung, SCHARF nur mit Backup aus diesem Lauf, Trockenlauf-Literal genau einmal, Datei-Hash vorher=nachher,
  Schema nur fehlende MigrationIds, Kopie ohne appsettings/web.config/Branding/App_Data/Fotos mit Schutzstand-Vergleich.
- Rechte nur hinzufügend (`icacls /grant`, vorher `/save`), Lese-SQL mit Schreibwort-Sperre (`Invoke-Lesen`).
- `ziele.psd1`: Dubletten-Prüfung je Firma, DEV-Ziele `Prod=$false`, Drift-Test `Ziele_AlleSystemeMitGueltigerHttpUrl_DevNieProd`
  (H-2 „DEV-Zeilen auf Servern“ bereits in [[2026-11-13-buendel-v1-99-16-goal-lauf]] notiert).
- Drift-Tests `NachlaufManifestDriftTests` grün (im gezielten Testlauf enthalten); Pester-Tests gibt es nicht (Skriptlogik wie UP-1 ungetestet).

## Nachtrag aus den Antworten Gerald (2026-11-13 22:47, [[2026-11-14-wochenend-goal-lauf]])

- **UP-4 (niedrig):** `Pruefung/LieferverzugSammellisteTage` (`scripts/deploy/nachlauf.psd1:350-353`) meldet einen Treffer, solange
  `Lieferverzug:TageNachTermin` nicht genau `1` ist. Entscheidung Gerald: Katalog-Default **bleibt 3** (neutral), AKE setzt 1 per Einstellung
  (Keyuser Till); eine Prüfung darf **keinen Wert erzwingen**. Vorschlag: Prüfung in einen reinen Hinweis umwandeln (Text „AKE: Vorgabe Einkauf 1 Tag –
  aktuellen Wert prüfen“, Firma AKE, ohne Trefferbedingung auf `= 1`) bzw. den aktuellen Wert anzeigen.

## Sichtung 2026-11-28 (gegen main v1.99.89)

Offen: UP-1, UP-2, UP-3, UP-4.
