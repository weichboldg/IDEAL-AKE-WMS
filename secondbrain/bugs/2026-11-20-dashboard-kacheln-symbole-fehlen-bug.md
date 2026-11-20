---
type: bug
title: "Dashboard-Kacheln ohne Symbole seit v1.99.45 (Abweichung von der Spec)"
status: offen
severity: niedrig
created: 2026-11-20
affected_code:
  - "IdealAkeWms/Services/Navigation/NavigationKatalog.cs:8 (NavZiel ohne SymbolPfad)"
  - "IdealAkeWms/Views/Home/Index.cshtml:21-29 (Kachel rendert nur Titel und Beschreibung)"
  - "docs/anwender/18-startseite-und-menue.md:48 (dokumentiert „keine Symbole mehr“)"
source_backlog: "[[2026-11-20-bugfix-dashboard-kacheln-symbole-fehlen]]"
spec: "[[2026-11-20-bugfix-dashboard-kacheln-symbole-fehlen-spec]]"
---

## Symptom

Die Startseite (Dashboard) zeigt seit v1.99.45 keine Symbole mehr auf den Kacheln, nur noch Titel und Kurzbeschreibung. Gemeldet von
Gerald am 2026-11-20 („die icons am dashboard sind weg, hier möchte ich wieder welche“). Betrifft AKE und IDEAL gleich.

## Reproduktion

1. Bündel-Stand v1.99.45 (Worktree `2026-08-07-ideal-teile-1-5`) auf DEV AKE oder DEV IDEAL, beliebiger Benutzer mit mindestens einer Kachel.
2. Startseite öffnen.
3. Erwartet laut [[2026-11-19-dashboard-kacheln-nach-kategorie-und-berechtigung-spec]] Z. 257: Inline-SVG-Symbol je Kachel.
   Tatsächlich: keine Symbole.

## Root Cause

Abweichung der Umsetzung von der freigegebenen Spec, im QA-Lauf nicht als Befund gewertet:

- Die Spec sah `NavZiel(..., string? SymbolPfad)` vor (Spec Z. 230) und „Symbole bleiben Inline-SVG wie heute, mit `aria-hidden="true"`“ (Z. 257).
- Umgesetzt wurde `NavZiel(Key, Titel, Controller, Action, Kategorie = null, Beschreibung = null)` ohne `SymbolPfad`
  (`IdealAkeWms/Services/Navigation/NavigationKatalog.cs:8`). Eine Suche nach `SymbolPfad` in `*.cs`/`*.cshtml` des Worktrees findet nichts.
- `Home/Index.cshtml:21-29` rendert je Kachel nur `<h4>` Titel und `<small>` Beschreibung.
- Der QA-Lauf hielt das als „Beobachtung ohne Befund: Dashboard ohne SVG-Symbole (bewusste Vereinfachung)“ fest (Spec Z. 444, AK 12 Z. 469
  „Symbole entfallen“). Die Anwenderdoku übernahm das als gewollt (`docs/anwender/18-startseite-und-menue.md:48`).
- Kein Wächter-Test prüft ein Symbol. `NavigationSichtTests.cs:81` verlangt für Kacheln nur `Beschreibung != null`.

## Fix / Verweis

[[2026-11-20-bugfix-dashboard-kacheln-symbole-fehlen-spec]]: `SymbolPfad` im Katalog ergänzen, Kachel rendert es als dekoratives Inline-SVG,
Wächter-Test „jede Kachel hat ein Symbol“, TS-174.
