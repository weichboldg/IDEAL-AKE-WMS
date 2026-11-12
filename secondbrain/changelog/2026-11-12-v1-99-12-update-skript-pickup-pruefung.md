---
type: changelog
version: 1.99.12
date: 2026-11-12
---
# v1.99.12 — Update-Skript: Prüfung MailSettings:PickupDirectory

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commit `56906459` (auf v1.99.11 `0dc0cea0`). Spec
[[2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung-spec]] (Freigabe-Nachtrag 2026-11-12 17:04). Nicht gemergt, nicht gepusht. **Testbereit** (QA 2026-11-12: Build 0 Fehler, Tests 2715/1/0 + 599/0/0, DEV AKE+IDEAL v1.99.12, `-NurPruefen` Test-Ziel Hinweis, simuliertes Prod Abbruch vor dem Stoppen; Hinweise H7–H9 in der Spec).

## Kernpunkte

- Neue Prüfung `Pruefung/MailPickup` (Manifest-Typ `Pruefung`, Phase `Vorher`, neues Feld `Appsettings='MailSettings:PickupDirectory'` statt SQL).
  Gelesen mit `Get-Einstellung` (dieselbe Logik wie die DB-Ermittlung; Umgebungsdatei vor `appsettings.json`), Leerraum = nicht gesetzt (wie `MailService`).
- Prod-Ziel (`Prod=$true`) und gesetzt → Abbruch in der Vorprüfung nach dem Plan, vor Backup/Stoppen, mit Wert und Datei; keine Reparatur. Gilt auch für `-NurPruefen`.
- Test-Ziel und gesetzt → Hinweis „Mails gehen nur in <Ordner> (<Datei>)“; die Nachlauf-Liste zeigt die Prüfung erneut (Stand nachher).
- Ermittlung: Zeile `Mail           SMTP <Host> | Ordner <Pfad>`.

## Deploy-Risiken

Keine Migration, kein SQL. Nur `deploy\Update-IdealAkeWms.ps1`, `nachlauf.psd1`, Version, Changelog-Karte. Ein Prod-System mit gesetztem
`PickupDirectory` lässt sich erst nach Entfernen des Werts aktualisieren (gewollt). Test: TS-126.28–126.31.
