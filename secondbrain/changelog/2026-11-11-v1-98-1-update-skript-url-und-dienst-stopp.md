---
type: changelog
version: 1.98.1
date: 2026-11-11
---
# v1.98.1 — Update-Skript: Url aus ziele.psd1 vor IIS-Bindung, Dienst-Stopp mit 60-s-Limit

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commit `e1ecc0c0` (auf v1.98.0 `550f10d5`). Spec [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]]
(Freigabe-Nachtrag 2026-11-11, QA-Befunde F3 und F7). Nicht gemergt, nicht gepusht.

## Kernpunkte

- F3: Startseiten-URL = `-SiteUrl` > `Url` aus `ziele.psd1` (nicht mit `-WebPfad`) > IIS-Bindung. Die Bindung wird sonst nur verglichen
  (Schema/Port abweichend → Warnung). `ziele.psd1` traegt die `Url` fuer alle 5 Systeme; eine Url ohne http/https bricht beim Einlesen ab.
  Behebt den Fall https-Bindung ohne Host-Namen (AKE-Test 4444, Test-IDEAL 4445), bei dem Schritt 6 erst nach dem Kopieren abbrach.
- F7: Dienst-Stopp ueber `ServiceController.Stop()` + `WaitForStatus` 60 s; bei Timeout Abbruch vor dem Kopieren, kein Kill.
- Drift-Test `Ziele_AlleFuenfSystemeMitGueltigerHttpUrl`, TS-125.17/125.18.

## Deploy-Risiken

Keine Migration. Nur Skripte, Versionsnummer und Changelog-Karte. F4, F6, F8 bleiben offen.
