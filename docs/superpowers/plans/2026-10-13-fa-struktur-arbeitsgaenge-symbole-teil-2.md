# FA-Struktur Arbeitsgänge als Symbole — Teil 2 (Status) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Status je Arbeitsgang-Kürzel im FA-Baum (erledigt / läuft / läuft+überfällig / überfällig / offen / kein BDE-Arbeitsgang) bei BDE global + Werkbank aktiv; Kontextzeilen-Kennzeichnung AA-konform statt `opacity`; leerer Offset in den Arbeitsgang-Stammdaten wird abgewiesen.

**Architecture:** Zwei gebündelte Repository-Abfragen (`WorkOperationRepository`: WorkOperations der Seiten-HauptFAs als Projektion; Status-Mengen erledigt/läuft für die Ids). Reine Funktion `FaArbeitsgangStatus.Zuordnen` (Zuordnung Antwort 6/W-2/W-3, Zusammenfassung W-4, Soll-Termin über einen übergebenen Rechner = `OseonDueDateCalculator`, Offset-Lookup, Sammelliste „nicht konfiguriert“, Mehrfach-Liste fürs Log). Controller `FaHierarchy.Index` ruft sie nur bei BDE global an. Status erweitert `FaArbeitsschrittSymbol` (kein zweiter Typ).

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec.md` (Rumpf + Antworten 1-11 + FREIGABE-NACHTRAG 2026-10-13, der vorgeht).

## Global Constraints

- Direkt im Bündel, Start `d56a340e` (v1.54.0); Version **1.55.0**; **TS-88** (+ TS-87.3 nachziehen); keine Migration; Web-App nicht lokal starten.
- `OseonDueDateCalculator`, `BusinessDayService`, `OperationConfigRepository`/Decorator, `Views/OperationConfigs/Index.cshtml`, Migration, `SQL/95` ohne Diff; einzige Ausnahme Offset-Pflege: `OperationConfigsController.Add`/`Update` (`int?`).
- Abfragebudget je Seitenaufbau bei BDE an: 1 WorkOperation-Abfrage + 1 Status-Methode (≤ 2 Statements) + Feiertage + Offset-Lookup; BDE aus: keine davon.
- aria-label/title = `<Teil-1-Beschriftung> – <Status>`; Farbe nie allein (Zeichen + Text); WCAG AA in ALLEN Zeilen (normal, Hover, Hervorhebung, Filtertreffer, Kontext) — Kontextzeilen ohne `opacity`.
- TempData nur Success/Warning; Sammelhinweis als ViewModel-Feld, nicht TempData.

## Review Focus

1. Kürzel-Schreibweise/Leerzeichen (`ka` vs `KA`, ` KA `) in WorkOperation und Offset-Lookup → trifft (Test).
2. Blatt unter fehlendem Vater (Waise) mit alter Vater-WorkOperation → „kein BDE-Arbeitsgang“ (Test W-3).
3. Kombigerät mit `null`-FE_Termin-Zeilen → frühester nicht leerer; alle null → nie überfällig (Test).
4. Werkbank ohne Zuordnung (`ProductionWorkplaceId` null) → gilt als nicht BDE-aktiv → neutral (Test).
5. Leerer Offset per manipuliertem POST → Warnung, nichts gespeichert; 0/negativ gültig (Test AK 19).

### Task 1: FA 4a — `OperationConfigsController` `int?` + Tests
### Task 2: Repository-Abfragen + Tests (InMemory, ToQueryString)
### Task 3: Reine Status-Funktion + Erweiterung `FaArbeitsschrittSymbol` + Tabellentests
### Task 4: Controller-Verdrahtung, ViewModel-Feld, View, CSS (frontend-design), Kontextzeilen
### Task 5: Version, Changelog, Hilfe, TS-88/TS-87.3, Brain, QA
