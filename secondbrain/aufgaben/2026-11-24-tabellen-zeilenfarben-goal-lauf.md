---
type: aufgabe
status: in-arbeit
created: 2026-11-24
worktree: .claude/worktrees/2026-08-07-ideal-teile-1-5
branch: feature/2026-08-07-ideal-teile-1-5
---
# /goal-Lauf 2026-11-24 – Tabellen-Zeilenfarben global

Auftrag Gerald 2026-11-24: `/goal-run` für [[2026-11-23-tabellen-streifen-hover-ci-hellblau-spec]] im Bündel-Worktree.
Vorprüfung: Status Freigegeben, worktree/branch leer, 9/9 Freigabe-Antworten, freigabe_entscheidung A, kein Blocker – bestanden.

| Spec | Typ | SQL-Block | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-23-tabellen-streifen-hover-ci-hellblau-spec]] | Spec | – | 1.99.62 | Testbereit | `7fbdd2b7`, Doku `d53cff1e` | – |

## Freigabe-Abweichungen vom Rumpf (Antworten gehen vor)

- R5 Nein: Hover **ohne** `@media (hover: hover)`; AK 16 / TS-187.12 entfallen bzw. erwartet „Hover bleibt bis zum nächsten Tippen“.
- R9 Ja: Vault-/enaio-Symbol-Kontrast wird **hier** behoben (Rumpf hatte Out-of-Scope).
- R2: zusätzlich `fa-head-table` als Ausnahme.

## QA (qa-agent, 2026-11-24)

[[2026-11-23-tabellen-streifen-hover-ci-hellblau-spec]] → **Testbereit**. Build 0 Fehler; Tests Web 3492/1 übersprungen/0, Service 790/0/0.
DEV AKE + IDEAL aus dem Worktree (`-OhneDienst`), Fußzeile v1.99.62, Exit 0. Pixelmessung (PNG + System.Drawing): Streifen #F2F2F2, Hover #A1D9F7,
Fokus #D9F4FF + Balken #052C65, `table-danger` Hover #E5C7CA, Gruppenkopf #E5E5E5, Link #052C65, `text-danger` #58151C, Vault #1C6EA4, enaio #A35400,
Baum-Hover #F5F5F5 unverändert. Review: keine blockierenden Befunde (N-1 bis N-5 Hinweise, Details in der Spec). Kein Publish/Paket (Auftrag).
Offen für den Menschen: 11 Punkte der Restprüfung in der Spec (u. a. Druck, Bäume „Hervorheben“, Leitstand, Modals, Touch).

## Definition of Done (2026-11-24)

- **Doku** (Worktree `d53cff1e`): Anwender Kap. 10 neuer Abschnitt 5 „Zeilen lesen“ mit Foto `10-07-tabellen-streifen-hover.jpg` (DEV AKE, nur angesehen),
  Kap. 05 Verweis, Kap. 18 FA-Struktur (1.99.60/61), README Stand + Versionstabelle 1.99.60–62; Betrieb `00-index.md` Stand v1.99.62 + Zeile 1.99.59–1.99.62.
- **Für spätere Confluence-Übernahme** (geänderte Kapitel): Anwender 05, 10, 18, README; Betrieb 00-index.
- **Paket** `publish\paket16-v1.99.62.zip` (Publish aus `d53cff1e`; 346 Web, 93 Dienst, 150 SQL, 5 deploy, publish-info.txt; enthält auch 1.99.60/61):
  237 050 821 Bytes, SHA-256 `D7B3B84F442E841CB26D8FC01419604B811CAA6EDE0E3403B4A3C5BB0BECD6FB`.
  `\AKENET01.ake.at\Installsourcen` und `\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen`: Größe + SHA-256 gleich (OK), vorher nicht vorhanden.
- Offen: Schranke 2 (Manual-UAT laut Restprüfung in der Spec, dann Merge). Kein Merge, kein Push.
- Nachtrag 2026-11-24: Paket 16 brach auf IDEAL-Prod in der Vorprüfung ab (FreshInstallIndizes) → v1.99.63 + Paket 17, siehe [[2026-11-24-v1-99-63-sql-151-fresh-install-indizes]]. Paket 16 nicht mehr verwenden.
