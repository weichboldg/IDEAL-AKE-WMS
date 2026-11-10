---
type: changelog
version: 1.30.4
date: 2026-11-08
---
# v1.30.4 — Sicherheits-Hotfix: Kommissionierfotos nur mit Anmeldung

Hotfix auf Basis `main` im Worktree `feature/2026-11-08-hotfix-kommissionierfotos`, Spec
[[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-spec]], Bug [[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-bug]].
Commit `29a2bfe2`. **Keine Migration.** Nicht gemergt (Schranke 2).

## Kernpunkte

- Fotos aus `wwwroot/Fotos/Kommissionierung` nach `App_Data/Fotos/Kommissionierung` (Verschiebung beim Start, idempotent), Auslieferung unter der
  alten URL nur mit Anmeldung + Kommissionier-Recht (`PhotoController.GetFile`).
- Login-Ausnahme „Punkt im Pfad“ nur noch für echte Dateien in `wwwroot` (`LoginRedirectMiddleware`).
- Foto löschen nur für Fotos des übergebenen FA.

## Deploy-Risiken

Nur Web, AKE und IDEAL. App-Pool braucht **Schreibrecht auf `App_Data`** (vorher anlegen, `icacls`), sonst bricht der Start ab. Fotos vorher
zählen, nach dem Start Log „Kommissionierfotos verschoben: n“ abgleichen. Danach Vorwärts-Merge ins Bündel (Mensch), vor Lauf B (QM-Fotos).
Zusatzfunde (nicht gebaut): Upload ohne Typ-/Größenprüfung, kein Antiforgery-Check auf der Foto-API.

## Merge

2026-11-10 in main: Merge-Commit `df54dee8` (`feature/2026-11-08-hotfix-kommissionierfotos`, `29a2bfe2`). Build und Tests nach dem Merge grün. Deploy nur Web, AKE und IDEAL; Vorwärts-Merge ins Bündel offen.
