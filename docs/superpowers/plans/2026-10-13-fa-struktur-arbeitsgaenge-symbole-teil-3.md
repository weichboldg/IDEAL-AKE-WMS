# FA-Struktur Arbeitsgänge — Teil 3 (BDE-Terminal) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans (native, ein Lauf). Steps use checkbox syntax.

**Goal:** Am BDE-Terminal „Arbeitsgang wählen“ (Normalmodus) die angebotenen Arbeitsgänge je Auftrag gruppiert, nach
Fertigungstermin sortiert und mit Status aus Teil 2 (offen / läuft / läuft + überfällig / überfällig) zeigen; fremd gebuchte
Arbeitsgänge sichtbar als „läuft“ (Antwort 6 = A). NurFA unverändert.

**Architecture:** Status-Regel aus Teil 2 in eine gemeinsame statische Klasse `Services/ArbeitsgangStatusRegel` ziehen
(`Ableiten`, `Soll`), Teil 2 und Terminal rufen sie; Statusdarstellung (CSS-Klasse + SVG) aus dem Razor-Partial in
`FaAgStatusDarstellung` (eine Quelle für FA-Baum und Terminal). Reine Funktion `Services/BdeTerminalAuftraege`
(Fertigungstermin-Regel FA 3, Sortierung FA 4). `BdeApiController.GetAvailableOperations` reichert die Antwort an; JS gruppiert
nach `productionOrderId` und baut Blöcke mit escapten Texten; Statusfarben über CSS-Variablen aus `site.css`.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec.md`
(Rumpf + Antworten 1–9 + Abgleich 2026-10-13).

## Global Constraints

- Direkt im Bündel, Start `d3746373` (v1.56.0) → **1.57.0**, **TS-90**; keine Migration; Web-App nicht lokal starten.
- NurFA-Zweig (Controller + JS) byte-gleich; Scan-Weg, Buchungslogik, `BdeBookingService` ohne Diff.
- Abfragebudget: höchstens +3 Abfragen (Kopfzeilen, Feiertage, Offsets; gecachte zählen nicht).
- aria-label `<OperationNumber> – <Name> – <Status>` (Teil-2-Text wiederverwenden), Farbe nie allein, Kontrast wie v1.55.0.

## Review Focus

1. Eigene laufende Buchung bleibt ausgeblendet, fremde erscheint als „läuft“ (beide Werker im Test).
2. IDEAL-Kopfzeilen mit nur leerem `FE_Termin` → `ProductionDate` (Antwort 9), Kombigerät frühester.
3. Kein Termin → am Ende, nie überfällig; Gleichstand OrderNumber/SubOrderNumber; Blöcke zusammenhängend.
4. HTML in `Description1` wird Text.
5. Teil-2-Verhalten nach dem Refactor unverändert (bestehende Tabellentests grün).

### Task 1: Gemeinsame Status-Regel + Darstellung (Refactor Teil 2, Tests grün)
### Task 2: `BdeTerminalAuftraege` (Termin-Regel, Sortierung) + Unit-Tests
### Task 3: `BdeApiController` (Ctor, Antwort, Antwort 6) + 9 Test-Konstruktionen + InMemory-Tests
### Task 4: JS/CSS/View (frontend-design)
### Task 5: Version, Changelog, Hilfe, TS-90 (inkl. P-T1/P-T2), Brain, QA
