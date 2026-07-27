# 🏠 IDEAL-AKE-WMS – Second Brain

Zentraler Einstieg für alle Projekt-Notizen.

## 📂 Bereiche

| Ordner | Inhalt |
|--------|--------|
| [[00_Inbox]] | Schnelle Captures, noch nicht sortiert |
| [[01_Daily]] | Tagesnotizen |
| [[02_Decisions]] | Architecture Decision Records (ADRs) |
| [[03_Features]] | Feature-Specs und Modulnotizen |
| [[04_Bugs]] | Bug-Investigations und Post-Mortems |
| [[05_Research]] | Spikes, Bibliotheksvergleiche, Lernen |
| [[06_Meetings]] | Besprechungsnotizen |
| [[07_Snippets]] | SQL-, PowerShell-, C#-Häppchen |
| [[90_Archive]] | Abgeschlossen / nicht mehr aktiv |

## 🔥 Aktive Arbeit

### Offene Bugs
```dataview
TABLE severity, component, file.mtime AS "Geändert"
FROM "04_Bugs"
WHERE status = "open"
SORT severity DESC, file.mtime DESC
```

### Features in Arbeit
```dataview
TABLE status, owner, file.mtime AS "Geändert"
FROM "03_Features"
WHERE status = "in-progress" OR status = "spec"
SORT file.mtime DESC
```

### Offene Action Items aus Meetings
```dataview
TASK
FROM "06_Meetings"
WHERE !completed
SORT file.mtime DESC
LIMIT 20
```

## 🧭 Entscheidungen

```dataview
TABLE status, deciders, file.mtime AS "Geändert"
FROM "02_Decisions"
SORT file.mtime DESC
LIMIT 10
```

## 📅 Letzte Tage

```dataview
LIST
FROM "01_Daily"
SORT file.name DESC
LIMIT 7
```

## 🔗 Externe Projektdokumente

Diese Dateien liegen im Repo-Root und sind nur außerhalb des Vaults zu finden:

- `../CLAUDE.md` – Prompt-Konvention für Claude Code
- `../README.md` – Projekt-Readme
- `../PROJECT_STATUS.md` – Aktueller Projektstatus
- `../ANALYSIS.md` – Analyse-Dokumente
- `../docs/` – Erweiterte Doku
- `../SQL/` – SQL-Migrationen

> 💡 Tipp: Mit Junctions/Symlinks (`mklink`) können diese auch direkt im Vault sichtbar gemacht werden.
