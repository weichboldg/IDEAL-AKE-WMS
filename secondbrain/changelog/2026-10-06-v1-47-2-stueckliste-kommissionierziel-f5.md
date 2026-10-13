---
type: changelog
version: 1.47.2
date: 2026-10-06
---
# v1.47.2 — Stückliste: Komm.-Ziel-Auswahl übersteht F5 (Befund B-ABN-1)

Bugfix im **Bündel-**Worktree `feature/2026-08-07-ideal-teile-1-5`, Commit `1df7665c`. Befund aus der
Browser-Abnahme [[2026-10-06-ts-79-browser-testbericht]] zu [[2026-09-25-kommissionierung-nur-hauptfa-spec]]
(Abschnitt „Befund aus der Browser-Abnahme“, B-ABN-1). **Keine Migration.**

**Warum:** Die Auswahl im Komm.-Ziel-Dropdown der hierarchischen Stückliste („Alle Ziele“ oder Einzelziele)
ging nach F5 verloren, der Baum stand zugeklappt da. Vorbestehend seit v1.42.0 (`17af86bd`), sichtbar erst
mit TS-79.6.

## Kernpunkte

- Ursache: Der Select2-Handler setzte `input.value` direkt; der `sessionStorage` (`tableFilters:Bom`) wird nur
  über `window.setColumnFilter` → `table-filter.js` `applyFilters()` → `saveFiltersToStorage()` geschrieben.
- Fix (nur `Views/Picking/Bom.cshtml`, `table-filter.js` unverändert): Handler und „Alle Filter zurücksetzen“
  schreiben über `window.setColumnFilter`; beim Laden werden Treffer-Äste für jeden aktiven Komm.-Ziel-Wert
  aufgeklappt.
- Entscheidung: Für das Komm.-Ziel gewinnt die Auswahl der Sitzung vor dem Profil-Standard; der Profil-Standard
  gilt beim ersten Öffnen und nach dem Leeren.
- Wächter `BomKommissionierzielPersistenzTests` (Quelltext), TS-79.6 erweitert, QA-Review (qa-agent) ohne
  Critical/Important.

## Deploy-Risiken

Nur Web-Publish, geht mit dem Bündel. Bekannte Grenzen (QA-Review M1/M2): alle Stücklisten einer Sitzung teilen
sich einen `sessionStorage`-Schlüssel — ein Einzelziel wird in die nächste Stückliste mitgenommen (sichtbar per
Badge und gelbem Hinweis, wie bei den anderen drei Filtern schon immer); für Beschaffung, Artikelgruppe und
Bezeichnung 1 gewinnt beim Laden weiter der Profil-Standard (Zusatzfund, nicht mitgebaut).
