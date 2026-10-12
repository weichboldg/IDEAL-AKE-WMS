# Arbeitsgang-Stammdaten vereinheitlichen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `OseonOperationConfigs` wird zur gemeinsamen Arbeitsgang-Stammtabelle `OperationConfigs` (Variante U + Audit), Pflege im Reiter „Arbeitsgänge" der BDE-Stammdaten (nur Admin, unabhängig von BdeAktiv), Offset-Lookup und Liste „nicht konfiguriert" für FA-Struktur Teil 2/3; OSEON-Verhalten unverändert.

**Architecture:** Eine EF-Migration, deren `Up()`/`Down()` vollständig durch guarded `migrationBuilder.Sql` (sp_rename je DB-Form, Konvergenz UQ_→IX_) ersetzt sind; identisch in `SQL/95`, FreshInstall legt den Zielzustand an; Drift-Test über alle vier Dateien. Neuer `OperationConfigsController` ([RequireAdminAccess], kein [RequireBdeActive]) mit Listen-View-Pattern; Reiterleiste als Partial für BdeMasterData + OperationConfigs.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec.md`

## Global Constraints

- Direkt im Bündel, Start `87db96c8`; Version **1.54.0**; **TS-87**; **SQL/95** (frei in Bündel + main); Web-App nicht lokal starten, Migration nicht gegen echte DB.
- Snapshot/Designer bleiben wie von `migrations add` erzeugt; Migration ohne strukturierte Operation; `has-pending-model-changes` leer; `migrations script` ohne ungeguardetes DROP/CREATE/sp_rename.
- OSEON-Pfade (`GetAllAsDictionaryAsync` case-sensitiv, Reporting case-insensitiv) bitgleich; `OseonDueDateCalculator`, `WorkOperationStructureDetectionService` ohne Diff; OSEON-ViewModel-Property `IsOseonRelevant` bleibt.
- Neuer Lookup/Liste: getrimmt, OrdinalIgnoreCase, ohne "01"; Audit bei Add/Update/Seed.
- Liste: Server-Spaltenfilter, Pagination, `data-col-key` auf jedem `<th>` (auch CSV-Export v1.53.0), kein Zahnrad (Spec), Karte „Nicht konfigurierte Kürzel" oberhalb.

## Review Focus

1. DB-Form mit UQ-Constraint (Skript-/FreshInstall-Installation) → keine 3701/15248 beim Migrate (Drift-Test: keine strukturierte Operation; Manual S9).
2. Kürzel mit Rand-Leerzeichen/anderer Schreibweise → Lookup trifft, Add lehnt Dublette ab (Test).
3. Doppelte normalisierte Kürzel → Warnhinweis, Lookup nimmt kleinste Id (Test).
4. Shiftlead/masterdata → AccessDenied auf GET und POST, Reiter unsichtbar (Reflection-Test + Partial-Logik).
5. Alte URL `/Settings/OperationConfig` → 302 (Test), POST-Actions weg (Reflection).

### Task 1: Modell + Migration + SQL/95 + FreshInstall + Drift-Test
### Task 2: Umbenennung der Verwender + Repository (Lookup, Liste) + Decorator + Tests
### Task 3: Controller, Views (Partial-Reiter), Layout, Redirect, Begriffe + Tests (frontend-design)
### Task 4: Version, Changelog, Hilfe, TS-87 + Nachzüge, Brain, QA
