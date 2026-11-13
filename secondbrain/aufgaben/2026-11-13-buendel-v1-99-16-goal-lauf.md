---
type: aufgabe
status: erledigt
created: 2026-11-13
---
# /goal-Lauf 2026-11-13 (2): Updater zweiter Trockenlauf, doppelte Kommissionier-Prio, UTC-Schreiber auf Lokalzeit

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Start v1.99.15 `e92894c9`.
Auftrag Gerald 2026-11-13 (/goal). Reihenfolge laut Auftrag; Versionen ab 1.99.16, TS ab 133, SQL ab 148 in Lauf-Reihenfolge
(Freigabe-Nachtrag 13:58 je Spec, geht dem Rumpf vor). Grenzen: nichts an Sage schreiben, keine Mails an externe Adressen, kein Lauf
gegen Zielsysteme (Update-Skript nur gegen DEV), kein Merge, kein Push. Nach jedem Build beide Apps nach `<Worktree>\publish`.
Vorgänger-Lauf: [[2026-11-13-buendel-v1-99-13-goal-lauf]]. DEV-Browser: Browser 2 (Gerald).

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-13-updater-zweiter-trockenlauf-altbestand-spec]] | Spec (Bugfix) | – | 1.99.16 | TS-133 | Testbereit (QA 2026-11-13, H-1..H-3 Hinweise) | `cb204c96` | – |
| [[2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme-spec]] | Spec (Variante B) | – | 1.99.17 | TS-134 | Testbereit (QA 2026-11-13, keine Befunde, Hinweise H-1..H-3) | `73911686` | – |
| [[2026-11-13-utc-schreiber-auf-lokalzeit-spec]] | Spec | – | 1.99.18 | TS-135 | Testbereit (QA 2026-11-13, F1 TS-135.6-Text behoben, Hinweise H-1/H-2; Publish `d9e4c89e`) | `99399be4`, `c703c1e3`, `4716888b`, QA `d9e4c89e` | – |

- Vorprüfung 2026-11-13: alle drei Freigegeben (Gerald 2026-11-13), Freigabe-Antworten vollständig, Prio = Variante B (`freigabe_entscheidung`),
  worktree/branch = Bündel. Updater-Nachtrag erweitert den Umfang (`-NurPruefen`-Endlosschleife ohne Eingabe, DEV-Ziele in `ziele.psd1`).

## Abschluss (2026-11-13)

- Alle drei Specs Testbereit. Publish `<Worktree>\publish` = v1.99.18, Commit `d9e4c89e` (Web + Dienst, keine Migration; SQL 148 und TS-136 frei).
- v1.99.16 `cb204c96` (TS-133), v1.99.17 `73911686` (TS-134), v1.99.18 `99399be4`/`c703c1e3`/`4716888b` + QA `d9e4c89e` (TS-135, ADR 0025).
- Kein Sage-Schreibzugriff, keine Mail, keine Prod-DB angesprochen, Update-Skript nur gegen DEV, kein Push, kein Merge.
- DEV-Testdaten (Liste je QA-Nachweis in den Specs): u. a. AKE_WMS_DEV FAs mit Prio 33–41 (zurückgenommen bzw. 37/38 freigegeben),
  KO-2026-00009–00016, KO-2026-00014 Zeile 2109 per SQL „Gepickt“; Testbenutzer Users 21/22 (AKE, deaktiviert; 22 = WMS-Datensatz zu einem
  echten AD-Konto, AD unverändert), User 9 (IDEAL, deaktiviert), Rolle 18 `qa_ts135`, Empfängergruppe 5 mit `qa.ts135@example.invalid`.
  KO-2026-00008 unberührt.
- Offen für Gerald: Schranke 2 je Spec (Checklisten); Prod-Trefferzahlen `Pruefung/DoppeltePrio` + H-1-Prüfabfrage (v1.99.17); scharfer
  Hand-Lauf TS-133.9; Testsystem-Läufe mit aktiven Syncs (TS-135.8–135.13); Entscheidungen: v1.99.16 H-1 (Warnungstext bei Exit≠0 mit
  TREFFER-Zeile) und H-2 (DEV-Zeilen in `ziele.psd1` auf Servern); v1.99.18 H-1 (`GETDATE()` in `OseonSyncService.cs:744/751` – ADR 0025 nur
  für neuen Code oder später auf `@Jetzt`); altes `publish\deploy.zip` (13.11. 08:15) nicht weitergeben.
