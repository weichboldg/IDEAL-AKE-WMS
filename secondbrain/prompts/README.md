# Prompts

Alle Claude-Auftraege der Pipeline als versionierte Dateien - damit sie
auffindbar sind (nicht in fluechtigen Chats verstreut) und in beiden Modi
identisch verwendet werden:

- **headless**: der Watcher uebergibt den Prompt an `claude -p`.
- **interaktiv**: du referenzierst ihn in deiner offenen Session mit `@`,
  z. B. `@secondbrain/prompts/dev.md`, und Claude fuehrt ihn mit vollem
  Kontext und ohne Turn-Limit aus.

Dateien:

| Datei | Zweck | Wann/Wie |
|---|---|---|
| `spec.md` | Backlog -> Spec(s) | pro neuer Backlog-Datei |
| `dev.md` | freigegebene Spec -> Umsetzung + QA -> Testbereit | pro Freigabe |
| `epic-stage.md` | naechste Etappe eines Epic | pro Etappe |
| `merge.md` | Merge-Freigabe ausfuehren (Schranke 2) | nach manuellem Test |

In den Prompts steht `<SPEC_PATH>` / `<BACKLOG_PATH>` als Platzhalter.
- Der Watcher ersetzt ihn automatisch.
- Interaktiv: entweder den konkreten Pfad einsetzen, oder den Prompt so
  referenzieren und in der Session den Pfad nennen
  ("... fuer secondbrain/specs/freigegeben/2026-08-xy-spec.md").

Diese Prompts sind Auftraege an Claude, KEINE Denk-Inhalte - sie werden vom
Watcher gelesen und bleiben ansonsten stabil. Aenderungen hier wirken sofort
auf beide Modi.
