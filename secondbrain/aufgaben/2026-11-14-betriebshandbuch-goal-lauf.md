---
type: aufgabe
status: erledigt
created: 2026-11-14
---
# /goal-Lauf 2026-11-14: Betriebshandbuch für Administratoren (docs/betrieb/)

Auftrag Gerald 2026-11-14 (Chat, /goal). Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Stand v1.99.23 (`08672fcc`).
Ziel: `docs/betrieb/` 00-index bis 10 + `AGENTS.md` (KI-Leitfaden), Deutsch für IT-Kollegen ohne Entwicklerwissen, jede Aussage belegt
(Datei:Zeile oder Brain-Notiz), Unbelegtes „zu prüfen“, keine Geheimnisse (Platzhalter + Ablageort). Geheimnis-Scan über `docs/` und
`secondbrain/` – Funde nur melden. Grenzen: kein Anwendungscode, nichts an Sage, keine Einstellungen, kein Merge, kein Push.

| Teil | Kapitel | Bearbeitung | Status |
|---|---|---|---|
| A | 01 Architektur, 02 Systeme, 03 Installation/Update/Rollback, 10 Backup/Restore | Agent A | erledigt (238/171/309/141 Zeilen) |
| B | 04 Konfiguration, 05 Schnittstellen, 09 Sicherheit/Konten/Rechte | Agent B | erledigt (221/203/201 Zeilen) |
| C | 06 Logs/Überwachung, 07 Troubleshooting, 08 Diagnose-Abfragen | Agent C | erledigt (277/137/518 Zeilen) |
| D | 00 Index, AGENTS.md, Geheimnis-Scan, Brain | Hauptlauf | erledigt |

## Geheimnis-Scan 2026-11-14 (nur melden, nichts geändert)

Umfang: `docs/` im Bündel-Worktree **und** im Hauptcheckout, `secondbrain/` (alle Dateitypen `*.md, *.sql, *.json, *.ps1, *.psd1, *.txt, *.config`).
Muster: Zuweisungen `password|passwort|kennwort|pwd|secret|apikey|token|client_secret` mit `:`/`=` und Wert; Connection-String `Password=`/`Pwd=`
und `User Id=…;Password=`; Kennwort-Wörter mit Wert in Backticks/Anführungszeichen; `sqlcmd -P`, `-Password '…'`, `ConvertTo-SecureString '…'`;
DataProtection-Blobs `dp1:` mit Inhalt; PEM-Private-Keys, AWS-Keys, JWT; lange Hex-Ketten (≥ 40).

**Funde (Wert hier bewusst nicht wiedergegeben):**

| Datei:Zeile | Art | Einschätzung |
|---|---|---|
| `docs/TESTSZENARIEN.md:88`, `:93`, `:110` (Worktree und Hauptcheckout) | Klartext-Kennwort des lokalen Testbenutzers `testuser` bzw. absichtlich falsches Kennwort im Login-Szenario | niedrig (Testkonto); prüfen, ob `testuser` auf einem Prod-/Testsystem mit diesem Kennwort existiert, sonst auf Platzhalter umstellen |
| `docs/TESTSZENARIEN.md:821` (Worktree) bzw. `:810` (Hauptcheckout) | Klartext-Kennwort für das Anlege-Szenario `neueruser` | niedrig (Testkonto, nur angelegt im Testlauf) |

**Geprüft, kein Geheimnis:** `secondbrain/tests/Claude outputs/Install-IdealAkeWms-IDEAL.ps1:264/307/360/364` (Kennwort nur aus Variable/Credential-Objekt);
`__RequestVerificationToken` in `docs/superpowers/plans/*` (Code-Beispiele); 40-stellige Hex-Ketten = Git-Commit-Hashes; `dp1:` nur als Präfix-Erwähnung,
kein Blob; `SData:Password`, `SQLCMDPASSWORD`, `SettingType.Geheim` nur als Namen; QA-Testbenutzer „ohne Passwort“
(`secondbrain/specs/freigegeben/2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-spec.md:300`).
**Grenze:** Musterbasiert – Kennwörter in freiem Fließtext ohne Schlüsselwort/Anführung findet der Scan nicht.

## Ergebnis

- Worktree-Commit `8bfd8c73` (`docs/betrieb/` 00–10 + `AGENTS.md`, rund 2 600 Zeilen). Kein Anwendungscode, nichts an Sage, keine Einstellungen,
  kein Merge, kein Push.
- **Belegprüfung:** 673 Datei-Fundstellen maschinell geprüft (Datei vorhanden, Zeile im Bereich) – 0 echte Fehler (übrig nur Paketpfade `deploy\…`
  und eine Pfad-Erwähnung `architektur/fallstricke.md` im Index); 6 Zufallsstichproben inhaltlich passend. Rund 60 Stellen „zu prüfen“.
- **Geheimnis-Scan `docs/betrieb/`:** ohne Fund.

## Befunde beim Erstellen (nur gemeldet, nicht behoben – Entscheidung Gerald)

| Nr | Schwere | Befund | Beleg (selbst nachgeprüft) |
|---|---|---|---|
| BH-1 | **hoch** | Anmeldeformular akzeptiert ein **leeres Kennwort** für jeden aktiven Benutzer ohne `PasswordHash`; AD-Benutzer werden ohne Hash angelegt → Anmeldung mit bekanntem WMS-Namen ohne Kennwort. Keine ADR-Entscheidung dazu ([[0002-dual-auth-session-login-plus-windows-sso]] regelt nur die Doppelanmeldung). | `IdealAkeWms/Controllers/AccountController.cs:94-101`, `UsersController.cs:199-209` |
| BH-2 | hoch (Neuinstallation) | `SQL/00_FreshInstall.sql:1260-1263` legt Indizes auf `ProductionOrders.IsReleasedForPicking`/`AssignedPickerId` an – Spalten liegen in `ProductionOrderPickingStatus`; Erstinstallation bricht ab („Meldung 207“, 2026-11-04). | Tabelle ab `SQL/00_FreshInstall.sql:388` ohne diese Spalten |
| BH-3 | mittel | `Install-IdealAkeWms.ps1:268-273`, `:362-369` schreiben übergebene SData-/SMTP-Zugangsdaten im Klartext in appsettings (entgegen ADR 0024). | Agent B |
| BH-4 | mittel | Installer-Schutz `Sync.ProductionOrdersEnabled=false` in der Dienst-appsettings wirkt nicht – der Dienst liest nur die DB (Default `true`). | `IDEALAKEWMSService/…/SyncWorker.cs:43`, `ServiceSettingDefinitions.cs:199` (Agent B) |
| BH-5 | niedrig | `admin` startet ohne Kennwort; Session-Cookie ohne `Secure`; SMTP ohne TLS möglich. | `Program.cs:260-279`, `:55-61`, `MailService.cs:78-82` (Agent B) |
| BH-6 | Doku | Veraltet im Brain: [[services]] (36 Keys, SData-Zugang appsettings-only, Sage-WE ohne Auto-Retry), [[integrationen]] (SData-Zugang, `OseonRueckmeldungAktiv` entfernt v1.79.0, `Security:AdDomain`), `README.md` (BdeAutoPauseWorker, Dienst-Logpfad, Self-Contained). Nicht geändert (Auftrag: nur melden). | Kapitel 04/05/06 |
| BH-7 | Doku | Install-Skript erwartet `<FIRMA>-Prod-1/2-*.sql` im Paket – nicht im Repo; Mindestzahl Migrationen 75 veraltet (heute 99). | `Install-IdealAkeWms.ps1:47`, `:229-235`, `:433` (Agent A) |

Widersprüche zwischen Quellen (Pfade, Dienstname, IDEAL-Prod-URL 80/443, Rollen-Prüfung AKE/beide) stehen jeweils im Kapitel.

