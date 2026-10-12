# Beschichtungsauftrag Teil 4 — Oberflaeche Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bedienung des gespeicherten Beschichtungsauftrags (Teil 3): Anlage aus der Vorschlagsliste, Liste, Detail mit Entwurfsbearbeitung und Positionsauswahl, Status, Reklamation, Druck/PDF mit Briefkopf — plus Schreibrolle.

**Architecture:** Neuer `BeschichtungsauftraegeController` (Class-Level Lesen wie die Beschichtungs-Ansicht, jede schreibende Action zusaetzlich `[RequireBeschichtungsauftragWriteAccess]`), je Anfrage genau EIN Workflow-Aufruf, danach `RedirectToAction` (Teil-3-Hinweis: Context wird nach Fehler verworfen). Vergabe-Markierung der Vorschlagsliste ueber eine kleine Service-Klasse auf `IBeschichtungsauftragRepository.GetVergabenAsync(hauptFas)`. Auftragsdruck summiert durch Wiederverwendung von `BeschichtungsauftragService.AggregateSummiert` (Positionen → Knoten abgebildet), Briefkopf-Partial aus Teil 2 mit festem Empfaenger.

**Tech Stack:** ASP.NET Core 10 MVC/Razor, Bootstrap 5, xUnit/FluentAssertions/Moq, EF InMemory.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec.md`

## Global Constraints

- Direkt im Buendel, Start `a5b27550`; Version **1.51.0**; TS-84; **keine Migration** (Spec); Web-App nicht lokal starten.
- Rolle: Key `beschichtungsauftrag_schreiben`, Anzeige „Beschichtungsaufträge (Schreibrecht)"; Lesen bleibt `beschichtungsauftrag`; Schreibrolle (und admin) erfuellen auch die Leseebene. Drei Stellen: Attribut, `controller.md`, `RoleOverview.cshtml`.
- Ein Workflow-Aufruf je Request, danach Redirect; RowVersion als Hidden-Feld (Base64 → `byte[]`), Konflikt → `WarningMessage`. TempData nur `SuccessMessage`/`WarningMessage`.
- Liste ADR 0005 (Pagination, Filterkarte Status, Server-Spaltenfilter jede Spalte, `ColumnDefinitions` + Switch, Zahnrad-Host §19). Detail-Tabellen Client-Mode.
- Druck/PDF nur ab Uebergeben (Entwurf/Storniert → Redirect + Warnung), Briefkopf-Partial Teil 2, Empfaenger = Dienstleister des Auftrags (fehlend: „Empfänger fehlt — am Auftrag nachtragen"), summiert je Artnr mit Maßen + „Maße uneinheitlich", Barcode = Nummer, PDF-Dateiname mit Nummer.
- Reklamations-Button nur bei Erledigt; Positionsauswahl je Entwurfsart (Ueberschrift/Leertext laut Spec).
- frontend-design, Konsistenz zu den Beschichtungs-Ansichten; Status-Badges mit Text.

## Review Focus

1. Leere Felder/ungueltiges Datum im Entwurfs-Formular (Start leer) — darf nicht abstuerzen, leer = null. Test `Bearbeiten_LeereDaten_Null`.
2. Positions-Schluessel aus dem Formular ungueltig/manipuliert („abc") — Klartext statt Ausnahme. Test `PositionenHinzufuegen_UngueltigerSchluessel_Warnung`.
3. Detail/Print fuer unbekannte Id → NotFound. Test `Details_Unbekannt_NotFound`.
4. Vorschlagsliste ohne Schreibrecht: kein Anlege-Button, aber Nummern sichtbar. Test im Controller-ViewModel (`DarfSchreiben=false`).
5. PDF-Fehler → WarningMessage + Redirect Detail (kein Absturz). Test `Pdf_RenderFehler_RedirectDetail`.

---

### Task 1: Rolle + Filter + Vergabe-Info + PdfFileNameBuilder
`RoleKeys.BeschichtungsauftragSchreiben`, `ICurrentUserService.HasBeschichtungsauftragWriteAccessAsync`, Leseebene erweitert, `RequireBeschichtungsauftragWriteAccessAttribute`; `Vergabe` um `HauptFA`, `GetVergabenAsync(IReadOnlyCollection<int>)`; `BeschichtungsauftragVergabeAnzeige.ErmittelnAsync(hauptFas)` → je HauptFA Auftraege (Id, Nummer), freie Anzahl, je Schluessel Nummern; `PdfFileNameBuilder.Build(string art, string kennung, DateTime)`. Tests.

### Task 2: BeschichtungsauftraegeController + ViewModels
Index, Details, Create, Bearbeiten, PositionenHinzufuegen, Bemerkung, Status, Reklamation GET/POST, Print, Pdf; Repository `GetAlleAsync()`, `GetFolgeauftraegeAsync(id)`; `ColumnDefinitions.Beschichtungsauftraege` (+ `BeschichtungsauftragPositionen` fuer Detail/Druckspalten). Controller-Tests inkl. TS-83.6 (veraltete RowVersion → Warnung; dieselbe freie Position in zwei Entwuerfe → zweiter Warnung), Schreibfilter per Reflection an jeder POST-Action, Druck nur ab Uebergeben, Reklamation nur Erledigt.

### Task 3: Views + Vorschlagsliste + Navigation
frontend-design; Index/Details/Reklamation/Print; Gruppenkopf in `FaHierarchyBeschichtung/Index` + `Summiert` (Nummern als Link, Button nur mit Schreibrecht und freien Positionen), Badge „in BA-…" in der Beschichtet-Zelle der Einzelliste; Layout-Navigation. Waechter: Zahnrad-Host, Vier-Quellen-Konsistenz fuer die neue Liste.

### Task 4: Doku, Version, Rolle-Doku, TS-84, Brain, QA
