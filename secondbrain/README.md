# IdealAkeWms Second Brain

Single Source of Truth des Projekts. Regeln:

1. **Brain-first**: Jeder Agent liest hier, bevor er handelt, und schreibt
   nach dem Handeln zurueck. Keine Aufgabe ist fertig ohne Brain-Update.
2. **Agenten lesen, Menschen schreiben**: `architektur/`, `codebase/`,
   `glossar/` pflegt primaer der Mensch; Agenten ergaenzen dort nur additiv
   (neue ADRs, neue Glossarbegriffe) und ueberschreiben nie Bestehendes.
   `specs/`, `aufgaben/`, `bugs/`, `changelog/` sind agenten-geschrieben.
3. **Status lebt im Frontmatter** (`status:`) - Ordner sind die menschliche
   Geste, Frontmatter die maschinenlesbare Wahrheit.

## Navigation

| Ordner | Inhalt |
|---|---|
| [[feature-map]] | Was existiert, Status, Code-Verweise |
| `architektur/` | ADRs (MADR), Muster, [[fallstricke]] - das "Warum" |
| `codebase/` | Navigierbare Karte: Module, Controller, Services, Datenmodell, Integrationen |
| `glossar/` | Domaenensprache (FA, Kommissionierung, Rollen ...) |
| `ideen/` | Denkraum VOR dem Backlog - reifen lassen, loest nichts aus |
| `backlog/` | Formlose neue Anforderungen (Mensch legt ab) - Ankunft startet die Kette |
| `specs/entwurf/` | Vom Spec-Agent ausgearbeitete Specs (Status: Entwurf) |
| `specs/freigegeben/` | Freigegebene Specs = Startsignal Entwicklung (Schranke 1); auch Ablage nach dem Merge |
| `specs/merge-freigegeben/` | Merge-Freigabe (Schranke 2) — Spec hierher verschieben, dann `approve-merge.ps1` |
| `aufgaben/` | Task-Tracking je Aufgabe |
| `bugs/` | Bugs + bekannte Probleme mit Status |
| `tests/` | Index der Testszenarien, verlinkt ../docs/TESTSZENARIEN.md |
| `changelog/` | Aenderungshistorie (Basis der Release-Seite) |

## Ausserhalb des Vaults — wer besitzt was

Der Vault ist die Wahrheit fuer Architektur, Wissen und Prozess. Betrieb, Installation und
Anwenderdoku bleiben bewusst im Repo. Damit nichts unerreichbar ist, hier die vollstaendige
Aussenkarte — **immer der genannten Datei folgen, nicht raten**:

| Repo-Datei | Besitzt (Single Source of Truth fuer …) |
|---|---|
| `../CLAUDE.md` | Die verbindlichen Regeln (Constitution). Verweist zurueck ins Brain. |
| `../README.md` | Betrieb + Anwendung: Voraussetzungen, **IIS-Konfiguration**, Installation/Skript-Reihenfolge, Service-Publish, **AppSettings-Tabelle (35 Keys)**, Corporate Design |
| `../PROJECT_STATUS.md` | **Eingefroren (07/2026)** — nur noch Stub mit Wegweiser; Release-Historie jetzt in `changelog/`, offene Deploy-Punkte in `aufgaben/` |
| `../docs/TESTSZENARIEN.md` | Manuelle Abnahme, 55 Kapitel → Index: [[testszenarien-index]] |
| `../docs/superpowers/specs/` + `plans/` + `cutover/` | Historische Specs/Plaene (vor dem Brain) → zugeordnet in [[feature-map]] |
| `../docs/SECOND-BRAIN-ANLEITUNG.md` | Aufbau und Betrieb dieser Pipeline selbst |
| `../docs/CLAUDE-full-backup-2026-07.md` | **Archiv** der CLAUDE.md-Vorfassung (382 Zeilen) — nicht pflegen |
| `../ANALYSIS.md`, `../HANDOFF.md` | Historische Analyse (05/2026) bzw. Session-Handoff (v1.20.0) — Altbestand |

**Konfiguration ist auf zwei Orte verteilt** (nicht verwechseln):
`AppSettings` = fachliche Feature-Toggles der Web-App, gepflegt unter `/Settings` → Liste in
`../README.md`, Konzept in [[0011-feature-toggles-ueber-appsettings]].
`ServiceSettings` = Service-Verhalten, gepflegt unter `/ServiceSettings` → vollstaendige
36-Key-Tabelle in [[services]], Konzept in
[[0008-servicesettings-db-first-mit-typisiertem-katalog]].

## Status-Automat

(IDEE) -> NEU -> SPEZIFIZIERT (Entwurf) -> FREIGEGEBEN -> IN_UMSETZUNG -> TESTBEREIT -> GEMERGED

Schranke 0 (manuell, optional): Idee in ideen/ reifen lassen; per Verschieben
nach backlog/ zur Aufgabe machen. Erst dieser Umzug startet die Kette - ideen/
wird vom Watcher nicht beobachtet.
Schranke 1 (manuell): Spec von entwurf/ nach freigegeben/ verschieben
UND `status: Freigegeben` setzen (macht sync-onedrive-specs.ps1 bzw. der Mensch).
Der Watcher setzt dann autonom bis TESTBEREIT fort.
Schranke 2 (manuell): nach erfolgreichem manuellem Test die Spec von
freigegeben/ nach merge-freigegeben/ verschieben und `scripts/approve-merge.ps1`
starten — der Merge laeuft nie autonom aus einem Dateiereignis.
