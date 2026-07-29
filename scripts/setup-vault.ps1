# =====================================================================
# setup-vault.ps1
# ZWECK: Legt den neuen Second-Brain-Vault (secondbrain/) mit Struktur,
#        Templates und Startdateien an. Idempotent: existierende Dateien
#        werden NIE ueberschrieben.
# VORAUSSETZUNGEN:
#   - Ausfuehren im Repo-Root (C:\Git\IDEAL-AKE-WMS)
#   - Obsidian geschlossen (sonst kann das Umbenennen des alten Vaults
#     an einer Dateisperre scheitern)
#   - PowerShell 5.1 oder 7
# NICHT AUTOMATISIERT:
#   - Loescht NICHTS. Der alte Vault wird nur nach secondbrain_alt/
#     umbenannt (reversibel). Endgueltiges Entfernen = deine Entscheidung
#     per git rm nach eigener Sichtung.
#   - Installiert kein Obsidian und keine Plugins (siehe Anleitung).
#   - Passt .gitignore nicht an (Hardlink-Zeilen manuell entfernen,
#     siehe Anleitung Schritt 2).
#   - Migriert keine Inhalte aus CLAUDE.md (eigener Schritt mit Claude).
# =====================================================================
[CmdletBinding()]
param(
    [string]$Root = "secondbrain"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path ".git")) {
    Write-Error "Bitte im Repo-Root ausfuehren (dort wo .git liegt)."
}

# --- 1. Alten Vault erkennen und beiseite stellen (reversibel) -------
if ((Test-Path "$Root\00_Inbox") -or (Test-Path "$Root\90_Archive")) {
    $backup = "secondbrain_alt"
    if (Test-Path $backup) {
        $backup = "secondbrain_alt_{0:yyyyMMdd_HHmmss}" -f (Get-Date)
    }
    Write-Host "Alter Vault erkannt -> umbenennen nach $backup (nichts wird geloescht)."
    try {
        Rename-Item -Path $Root -NewName $backup
    } catch {
        Write-Error ("Umbenennen fehlgeschlagen (Obsidian offen? Datei gesperrt?): " + $_.Exception.Message)
    }
    Write-Host "HINWEIS: Danach 'git add -A' nicht vergessen, damit git die Verschiebung sieht."
}

# --- 2. Ordnerstruktur ------------------------------------------------
$dirs = @(
    "$Root",
    "$Root\architektur\adr",
    "$Root\architektur\muster",
    "$Root\codebase",
    "$Root\glossar",
    "$Root\ideen",
    "$Root\backlog",
    "$Root\specs\entwurf",
    "$Root\specs\freigegeben",
    "$Root\aufgaben",
    "$Root\bugs",
    "$Root\tests",
    "$Root\changelog",
    "$Root\_templates"
)
foreach ($d in $dirs) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d | Out-Null; Write-Host "created  $d" }
    else                     { Write-Host "exists   $d" }
}

# --- 3. Idempotenter Datei-Writer ------------------------------------
function New-FileIfAbsent {
    param([string]$Path, [string]$Content)
    if (-not (Test-Path $Path)) {
        $Content | Set-Content -Path $Path -Encoding UTF8
        Write-Host "created  $Path"
    } else {
        Write-Host "exists   $Path (unveraendert)"
    }
}

# --- 4. Einstieg / README --------------------------------------------
New-FileIfAbsent "$Root\README.md" @'
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
| `backlog/` | Formlose neue Anforderungen (Mensch legt ab) |
| `specs/entwurf/` | Vom Spec-Agent ausgearbeitete Specs (Status: Entwurf) |
| `specs/freigegeben/` | Freigegebene Specs = Startsignal Entwicklung (Schranke 1) |
| `aufgaben/` | Task-Tracking je Aufgabe |
| `bugs/` | Bugs + bekannte Probleme mit Status |
| `tests/` | Index der Testszenarien, verlinkt ../docs/TESTSZENARIEN.md |
| `changelog/` | Aenderungshistorie (Basis der Release-Seite) |

## Status-Automat

NEU -> SPEZIFIZIERT (Entwurf) -> FREIGEGEBEN -> IN_UMSETZUNG -> TESTBEREIT -> GEMERGED

Schranke 1 (manuell): Spec von entwurf/ nach freigegeben/ verschieben
UND `status: Freigegeben` setzen (macht sync-onedrive-specs.ps1 bzw. der Mensch).
Schranke 2 (manuell): Merge nach main erst nach erfolgreichem manuellem Test.
'@

# --- 5. Dashboard (Dataview) -----------------------------------------
New-FileIfAbsent "$Root\HOME.md" @'
# WMS Second Brain - Dashboard

## Pipeline

### Backlog (noch ohne Spec)
```dataview
LIST FROM "backlog" SORT file.ctime DESC
```

### Specs im Entwurf (warten auf Freigabe - Schranke 1)
```dataview
TABLE status, created, open_questions FROM "specs/entwurf" WHERE type = "spec" SORT created DESC
```

### Freigegeben / in Umsetzung / testbereit
```dataview
TABLE status, branch, updated FROM "specs/freigegeben" WHERE type = "spec" SORT updated DESC
```

## Offene Bugs
```dataview
TABLE status, severity, file.mtime AS "Geaendert" FROM "bugs" WHERE status != "behoben" SORT severity DESC
```

## Letzte ADRs
```dataview
TABLE id, status, date FROM "architektur/adr" SORT date DESC LIMIT 10
```
'@

# --- 6. Templates -----------------------------------------------------
New-FileIfAbsent "$Root\_templates\spec.md" @'
---
type: spec
title: <Kurztitel>
slug: <yyyy-mm-dd-slug>
status: Entwurf
created: <yyyy-mm-dd>
updated: <yyyy-mm-dd>
source_backlog: ""
task: ""
worktree: ""
branch: ""
affected_code: []
open_questions: []
---

## Ziel / Nutzen (das Warum)

## Umfang (In-Scope / Out-of-Scope)

## Fachliche Anforderungen

## Technischer Loesungsentwurf
<!-- Muster nennen (Repository/Decorator, Listen-View-Pattern), betroffene Module verlinken -->

## Migrations-/SQL-Auswirkungen
<!-- Migration + SQL/XX_*.sql mit OBJECT_ID-Guard? FreshInstall? -->

## Audit-Feld-Auswirkungen

## Akzeptanzkriterien
<!-- testbar formuliert, nummeriert -->

## Test-Szenarien
<!-- Verweis auf docs/TESTSZENARIEN.md Kapitel; neue Szenarien skizzieren -->

## Offene Rueckfragen
'@

New-FileIfAbsent "$Root\_templates\aufgabe.md" @'
---
type: aufgabe
title: <Kurztitel>
status: NEU
spec: ""
assignee: agent
blocked_by: []
created: <yyyy-mm-dd>
updated: <yyyy-mm-dd>
---

## Verlauf
<!-- Agenten haengen hier Statuswechsel mit Zeitstempel an -->
'@

New-FileIfAbsent "$Root\_templates\adr.md" @'
---
type: adr
id: NNNN
title: <Entscheidung als Aussage>
status: accepted
date: <yyyy-mm-dd>
supersedes: ""
superseded_by: ""
---

## Kontext und Problem

## Betrachtete Optionen

## Entscheidung

## Konsequenzen
<!-- positiv / negativ / Risiken -->
'@

New-FileIfAbsent "$Root\_templates\bug.md" @'
---
type: bug
title: <Kurzbeschreibung>
status: offen
severity: mittel
created: <yyyy-mm-dd>
affected_code: []
spec: ""
---

## Symptom

## Reproduktion

## Root Cause

## Fix / Verweis
'@

# --- 7. Startdateien ---------------------------------------------------
New-FileIfAbsent "$Root\feature-map.md" @'
---
type: feature-map
updated: <wird von Agenten gepflegt>
---
# Feature-Landkarte

<!-- Nach jedem GEMERGED ergaenzt der Merge-Nachlauf hier eine Zeile. -->
<!-- Die Erst-Befuellung uebernimmt die einmalige Nacherfassung (Anleitung Schritt 5). -->

| Feature | Status | Spec | Code-Einstieg |
|---|---|---|---|
'@

New-FileIfAbsent "$Root\architektur\README.md" @'
# Architektur

- `adr/` - Architecture Decision Records im MADR-Format, fortlaufend nummeriert (0001-...).
  Einmalige Nacherfassung der Bestandsarchitektur: siehe Anleitung Schritt 5.
- `muster/` - verbindliche Muster (Repository/Decorator, Listen-View-Pattern, Controller-Muster).
- [[fallstricke]] - bekannte Stolperfallen mit Begruendung.

Regel: Neue Entscheidung = neuer ADR. ADRs werden nie umgeschrieben, nur superseded.
'@

New-FileIfAbsent "$Root\architektur\fallstricke.md" @'
---
type: referenz
---
# Bekannte Fallstricke

<!-- Wird bei der CLAUDE.md-Diaet (Anleitung Schritt 4) aus CLAUDE.md befuellt. -->
'@

New-FileIfAbsent "$Root\codebase\README.md" @'
# Codebase-Karte

Navigierbare Karte - KEIN Code-Dump. Verweise auf Dateien/Ordner, 1-3 Saetze Zweck.

- [[module]] - Web-App (IdealAkeWms/), Windows-Service (IDEALAKEWMSService/), Tests, SQL/
- [[controller]] - Controller mit Zugriffsfiltern
- [[services]] - Services, Repositories, Decorators
- [[datenmodell]] - Entitaeten, AuditableEntity, Migrationen
- [[integrationen]] - Sage, OSEON, enaio
'@

foreach ($f in @("module","controller","services","datenmodell","integrationen")) {
    New-FileIfAbsent "$Root\codebase\$f.md" @"
---
type: codebase-karte
---
# $f

<!-- Wird bei der Nacherfassung (Anleitung Schritt 5) befuellt und danach je Merge aktualisiert. -->
"@
}

New-FileIfAbsent "$Root\glossar\glossar.md" @'
---
type: glossar
---
# Domaenen-Glossar

| Begriff | Bedeutung |
|---|---|
| FA | Fertigungsauftrag (Produktionsauftrag) |
| Kommissionierung | Picking: Bereitstellen von Bauteilen zu einem FA |
| Artikelnummer | Geraete-Artikelnummer (NICHT fuer Bauteil-Operationen) |
| Ressourcenummer | Bauteil-Artikelnummer - immer diese fuer Bauteil-Operationen |

<!-- Bei der Nacherfassung erweitern: Rollenkonzept-Begriffe, BDE, Leitstand, Vorbau, Lagerbestellung ... -->
'@

New-FileIfAbsent "$Root\backlog\README.md" @'
# Backlog

Hier legst DU (oder ein Kollege) formlose Anforderungen als .md ab.
Dateiname: YYYY-MM-DD-kurzer-slug.md. Inhalt: Freitext genuegt -
der Spec-Agent macht daraus eine vollstaendige Spec in specs/entwurf/.

Eine Backlog-Datei gilt als "verarbeitet", sobald eine Spec mit
source_backlog-Verweis auf sie existiert.
'@

New-FileIfAbsent "$Root\tests\testszenarien-index.md" @'
---
type: test-index
---
# Testszenarien-Index

Single Source of Truth der manuellen Abnahme ist ../docs/TESTSZENARIEN.md.
Dieser Index verlinkt Kapitel <-> Feature/Spec, damit Agenten und Menschen
schnell finden, welches Szenario zu welcher Aenderung gehoert.

| Kapitel in TESTSZENARIEN.md | Feature / Spec |
|---|---|
'@

New-FileIfAbsent "$Root\changelog\README.md" @'
# Changelog / Aenderungshistorie

Je Merge ein Eintrag (Datei YYYY-MM-DD-version-slug.md oder Sammel-Datei je Version).
Basis fuer die Release-Seite (Views/Help/Changelog.cshtml). Der Merge-Nachlauf
(nach Schranke 2) schreibt hier; die Release-Seite wird weiterhin im Code gepflegt.
'@

Write-Host ""
Write-Host "Vault-Setup fertig (idempotent). Naechste Schritte: siehe docs/SECOND-BRAIN-ANLEITUNG.md"
