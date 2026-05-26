---
id: ADR-0001
date: 2026-05-12
status: accepted
deciders: Gerald
tags: [adr, architecture, tooling]
---
# ADR-0001: Obsidian als Second Brain im Repo

## Status
accepted

## Kontext
Das WMS-Projekt wächst, und Wissen verteilt sich aktuell auf `CLAUDE.md`,
`PROJECT_STATUS.md`, `ANALYSIS.md`, `README.md`, lose Markdown-Dateien in `docs/`
und externe Notizen. Es fehlt ein zentraler Ort für:

- Architecture Decision Records
- Bug-Investigations mit Root-Cause-Historie
- Feature-Specs vor der Implementierung
- Recherchen / Spikes
- Meeting-Protokolle und Action Items

## Entscheidung
Es wird ein Obsidian-Vault im Repo unter `secondbrain/` geführt. Notizen werden
mit dem Code versioniert. Struktur folgt einer leichten PARA-Variante mit
nummerierten Ordnern.

## Begründung
- **Co-Lokation:** Notizen leben mit dem Code. Branches, History, Diffs gelten auch für Wissen.
- **Markdown-nativ:** Keine Lock-in, alle Editoren können mit den Files umgehen.
- **Obsidian-Features:** Backlinks, Graph, Dataview ermöglichen Wissensvernetzung,
  die in losen Markdown-Files nicht entsteht.
- **Offline / lokal:** Keine Cloud-Abhängigkeit, kein Vendor-Lock-in.

## Konsequenzen
**Positiv**
- Wissen geht nicht mehr verloren, ist durchsuchbar und vernetzt.
- ADR-Historie ist über Git nachvollziehbar.
- Auf jedem Dev-Rechner verfügbar nach `git pull`.

**Negativ / Trade-offs**
- Gelegentliche Merge-Konflikte bei parallelem Editieren.
- Disziplin nötig, sonst entstehen wieder lose Inbox-Notizen.

**Folgearbeit**
- `.gitignore` um Obsidian-Workspace-Dateien erweitern.
- Obsidian Git Plugin für Auto-Backup konfigurieren.

## Alternativen
1. **Confluence / Notion** – verworfen wegen Cloud-Abhängigkeit und Trennung vom Code.
2. **Nur Markdown im `docs/`-Ordner** – verworfen, weil ohne Backlinks/Graph schnell
   wieder unübersichtlich.
3. **Wiki im Git-Hoster** – verworfen, weil außerhalb des Repos und ohne lokalen Editor.

## Referenzen
- [[HOME]]
- `../CLAUDE.md`
