# Second Brain – IDEAL-AKE-WMS

Obsidian-Vault für Projektnotizen, Architekturentscheidungen, Bugs und Recherche zum
WMS-Projekt. Notizen werden zusammen mit dem Code in Git versioniert.

## Setup auf einem neuen Rechner

1. Repo klonen.
2. Obsidian öffnen → **„Open folder as vault"** → diesen Ordner (`secondbrain/`) auswählen.
3. Beim ersten Start vertraue dem Vault, wenn nach Community-Plugins gefragt wird.
4. Empfohlene Community-Plugins installieren (siehe unten).

## Ordnerstruktur

```
00_Inbox/       Schnelle Captures, kommen später sortiert
01_Daily/       Tagesnotizen (YYYY-MM-DD)
02_Decisions/   Architecture Decision Records (ADR-NNNN-titel)
03_Features/    Feature-Specs / Modulnotizen
04_Bugs/        Bug-Investigations, Root-Cause-Analysen
05_Research/    Spikes, Bibliotheksvergleiche, Lernnotizen
06_Meetings/    Besprechungsnotizen
07_Snippets/    Wiederverwendbare Code-Häppchen
90_Archive/     Abgeschlossenes / nicht mehr aktives
_Templates/     Vorlagen (von „Templates"-Core-Plugin genutzt)
_Attachments/   Bilder, PDFs, sonstige Anhänge
```

## Konventionen

- **Dateiname:** `kurz-aussagekräftig-und-mit-bindestrichen.md`. Bei ADRs zusätzlich Nummer: `ADR-0007-cached-bom-repository.md`.
- **Frontmatter:** Jede Notiz beginnt mit YAML-Frontmatter (`date`, `tags`, je nach Typ
  weitere Felder). Templates liefern die richtige Struktur.
- **Verlinkung:** `[[Notizname]]` für interne Links. Tags mit `#tag` ergänzend zu Frontmatter.
- **Anhänge:** Bilder werden automatisch in `_Attachments/` abgelegt.

## Empfohlene Plugins

**Core (in Obsidian aktivieren)**
- Daily notes (Vorlage: `_Templates/Daily.md`, Ordner: `01_Daily/`)
- Templates (Ordner: `_Templates/`)
- Backlinks, Outgoing links, Tag pane, Outline, Canvas, Bookmarks

**Community**
- **Dataview** – Queries auf der HOME-Seite
- **Templater** – Erweiterte Templates
- **Obsidian Git** – Auto-Commit/Pull
- **QuickAdd** – Hotkey-Captures
- **Excalidraw** – Skizzen direkt im Vault

## Git-Workflow

Auto-Commit alle 10 Min via Obsidian Git ist OK, kollidiert aber gelegentlich mit
manuellen Code-Commits. Bei aktiven Code-Sessions Auto-Commit pausieren und Notizen
manuell committen.

In `.gitignore` (Repo-Root) sind ignoriert:
- `secondbrain/.obsidian/workspace*.json`
- `secondbrain/.obsidian/cache`
- `secondbrain/.trash/`

Geteilt (versioniert) bleiben: Plugins, Hotkeys, Themes, App-Konfiguration.
