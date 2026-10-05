---
type: changelog
version: 1.42.0
date: 2026-09-21
---
# v1.42.0 — Stückliste: Komm.-Ziel-Dropdown-Filter + gespeicherter Standardfilter + Badge für alle vier

Umsetzung der freigegebenen Spec [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] im
**Bündel-**Worktree `feature/2026-08-07-ideal-teile-1-5` (kein neuer, „als zusätzliche Etappe").
Umsetzungsnotiz [[2026-09-21-stueckliste-kommissionierziel-filter-umsetzung]]. Migration **92**.
Reihenfolge eingehalten: Bildschirm (In-Scope 1–6) zuerst, Druck (In-Scope 7) zuletzt.

**Warum:** [[2026-09-13-kommissionierung-nur-hauptfa]] macht die HauptFA-Stückliste zur Vollstruktur →
lang. Ein Kommissionierziel-Filter + gespeicherter Standard lässt jeden Kommissionierer nur seinen
Bereich sehen.

## Umgesetzt

- **Neues Feld `User.DefaultFilterBomKommissionierziel`** (`string?` NVARCHAR(200), Migration
  `20260921131329` + `SQL/92_*.sql` COL_LENGTH-Guard + FreshInstall an beiden Stellen), 1:1 nach Vorlage
  `DefaultFilterBomDescription1`. Durchgereicht über UserEdit/Profile-VM → Users/Account-Controller →
  PickingController.Bom → BomViewModel.
- **Zwei Orte, zwei Formen (FA 2/S4):** In der Stückliste ein **Select2-Mehrfach-Dropdown** (DISTINCT
  der geladenen Items, OR); in den Benutzereinstellungen ein **Textfeld** mit OR-Syntax (kein Dropdown —
  dort sind keine Items geladen).
- **Einstellungsfeld nur bei Master `true` (In-Scope 6/S1):** `UsersController` **und**
  `AccountController` lesen jetzt `ProduktionsauftragHierarchisch` (`IServiceSettingRepository`, neu
  injiziert; heute las es keiner) → `HierarchicalMaster` ins ViewModel → `@if` in beiden Views.
  `DefaultFilterBomDescription1` bleibt hausweit sichtbar.
- **Badge + Ein-Klick-Reset für ALLE VIER Standardfilter (B1/AK 7/15):** Beschaffung, Artikelgruppe,
  Bezeichnung 1 und Komm.-Ziel — ein Bedienelement, je Feld konfiguriert. Schließt den **vorbestehenden
  stillen Filter** von Beschaffung/Artikelgruppe (filterten seit Einführung still).
- **Benannter Leerzustand (AK 8):** trifft der Komm.-Ziel-Standard 0 Positionen → „Standardfilter
  Kommissionierziel `<Wert>` aktiv — keine Treffer in dieser Stückliste" + Reset. Teiltreffer (OR) →
  keine Meldung.
- **Auto-Aufklappen der Treffer-Äste (AK 14):** Muster FA-Liste — ein Wert aus einem zugeklappten Ast
  filtert nicht scheinbar „nichts".
- **Druck-Durchschlag (In-Scope 7/AK 10):** `PrintBomItem` um Kommissionieren/Hauptlagerplatz/Ebene
  erweitert + Mapping; `PrintBom.cshtml` neue `ShowCol`-Kopf-/Zellblöcke; `colNames`-Map (Bom.cshtml
  Druck-Handler) von numerisch auf **col-key→Label** umgestellt (getActiveFilters liefert col-keys) inkl.
  hierarchischer Spalten; Kopf-Hinweis „Gefilterte Ansicht". **Schließt den offenen BOM-Bridge-Druckbefund**
  (Komm.-Ziel/Ebene fehlten im Ausdruck).

## Technik-Notiz

Client-Mode-Ansicht (ADR 0005-Ausnahme, kein Server-Filter-Umbau). Der Select2-Dropdown ersetzt den
Freitext **visuell**, der `<input data-col-key="kommissionieren">` bleibt als **verstecktes
Quell-Element** — so bleiben `getActiveFilters`/`setColumnFilter`/`updateBomVisibility`/Default-Filter/
Storage-Restore unverändert; das Select2 synct beim Change nur in den Input. Dauerwissen [[fallstricke]] §14.

## Deploy

- **Web: ja · Service: nein · Migration: ja** (Migration 92, nullable Spalte an `Users`, nicht
  daten-destruktiv). Migrationsskript vor/mit dem Web-Deploy einspielen. Geht im selben Publish/Merge des
  Bündels mit.
- **Merge-Commit:** offen (Schranke 2, Mensch).

## Nachweis

Build 0 Fehler; **Web 1391 grün +1 skip, Service 277 grün** (Controller-Tests um `IServiceSettingRepository`-
Mock ergänzt). Testszenarien **TS-70 → Abschnitt „Komm.-Ziel-Filter"** (Client-Mode = Manual-UAT).

## Zugehörig

Spec [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] ·
Umsetzung [[2026-09-21-stueckliste-kommissionierziel-filter-umsetzung]] · [[fallstricke]] §14 ·
Anlass [[2026-09-13-kommissionierung-nur-hauptfa]] · Bündel [[2026-08-07-ideal-teile-1-5]].
