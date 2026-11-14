---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-betriebshandbuch-goal-lauf]]"
status: neu
modul: Anmeldung, Benutzerverwaltung, Erstinstallation (00_FreshInstall.sql, Install-IdealAkeWms.ps1), Mail
stand: "Bündel v1.99.23, Worktree .claude/worktrees/2026-08-07-ideal-teile-1-5, Zweig feature/2026-08-07-ideal-teile-1-5"
---
# Bugfix Sicherheit: Anmeldung und Installer (Befunde BH-1 bis BH-5)

Auftrag Gerald 2026-11-14 (Chat, /spec). Befunde aus der Erstellung des Betriebshandbuchs, Tabelle „Befunde beim Erstellen“ in
[[2026-11-14-betriebshandbuch-goal-lauf]]. Basis v1.99.23. Nächste freie Nummern: SQL 151, TS-139, ADR 0027.

## Schwerpunkte

1. **(hoch) BH-1:** Formular-Anmeldung mit leerem Kennwort für Benutzer ohne `PasswordHash` unterbinden (`AccountController.cs:93-101`).
   AD-Benutzer (`WindowsUserName` gesetzt) nur per Windows-SSO oder mit gesetztem Kennwort. Wirkung auf bestehende Benutzer ohne Hash erheben
   (lesende Prüfabfrage für Gerald, je System) und Übergang festlegen; ADR dazu (0027).
2. **(hoch) BH-2:** `SQL/00_FreshInstall.sql:1260-1263` auf `ProductionOrderPickingStatus` korrigieren bzw. entfernen (Indizes existieren schon
   `:456-460`); Erstinstallation auf leerer DB nachweisen.
3. **BH-3:** `Install-IdealAkeWms.ps1:268-273` / `:362-369` – keine Zugangsdaten im Klartext in appsettings (ADR 0024).
4. **BH-4:** `Sync.ProductionOrdersEnabled` im Installer wirkungslos – entweder DB-Einstellung setzen oder Schalter entfernen und dokumentieren.
5. **BH-5:** `admin` ohne Kennwort beim Start (`Program.cs:260-279`), Cookie `Secure` bei HTTPS, SMTP-TLS – je Punkt Entscheidung vorschlagen.

Danach `docs/betrieb/` (Kapitel 07, 09) nachziehen.

## Grenzen

Nichts an Sage, keine Einstellungen auf Test-/Prod-Systemen, kein Merge, kein Push.
