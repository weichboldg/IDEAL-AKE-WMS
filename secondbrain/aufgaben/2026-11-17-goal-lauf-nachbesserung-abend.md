---
type: aufgabe
status: offen
created: 2026-11-17
quelle: Gerald, Chat 2026-11-17 (/goal)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-17 Abend: Nachbesserungen v1.99.39

Auftrag Gerald 2026-11-17: drei Nachbesserungen im Bündel-Worktree (Basis `ab75c336`, v1.99.38), gemeinsam als **v1.99.39** (geprüft: frei;
keine SQL; TS-159 erweitern bzw. TS-160 bei Bedarf).
Grenzen: nichts an Sage, kein Schalter einschalten, keine Einstellungen auf Test/Prod, Publish nur `<Worktree>\publish\paket4-v1.99.39\`
(Paket 1–3 unverändert), kein Merge, kein Push.

1. [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]] QA-Befund F3: Erfolg erst nach Nachlesen der angelegten AG-Position in Sage.
2. Manifest `scripts/deploy/nachlauf.psd1`: Hinweis/Prüfschritt AKE-Prod `Sync:Quelle:Artikel` auf SData – Befund
   [[2026-11-17-ake-4445-vs-prod-sichtpruefung]]; Betriebsdoku.
3. Pagination „Eintraege 26–8 von 8“ (QA-Zusatzfund Lauf [[2026-11-17-goal-lauf-sage-ag-anlage]]): Seite > letzte Seite → letzte Seite.

## Übersicht

| Punkt | Version | Status | Commit |
|---|---|---|---|
| 1 F3 Nachlesen nach Senden | 1.99.39 | umgesetzt (qa-agent ausstehend) | `69a39a8b` |
| 2 Manifest Sync:Quelle:Artikel AKE | 1.99.39 | umgesetzt (qa-agent ausstehend) | `ebff14e0` |
| 3 Pagination klemmen | 1.99.39 | umgesetzt (qa-agent ausstehend) | `78014b9a` (+ Version/Doku `8d419875`) |

## Protokoll

- 2026-11-17: Lauf gestartet, Nummer 1.99.39 geprüft.
- 2026-11-17: Umsetzung v1.99.39 ([[2026-11-17-v1-99-39-nachbesserung-abend]]). F3: `SageFaArbeitsgangService.NachlesenAsync` – nach 2xx GET über
  `ISageFaStandLookup`, angelegt nur bei Fund der AG-Nummer (`RessourceNummer`), sonst Fehler mit Text, Lesefehler = unklar; `Auswerten` ohne
  Handle-Pflicht. Manifest: `Pruefung/ArtikelQuelleSData` (AKE, Nachher, anzeigen; Key belegt `ServiceSettingDefinitions.cs:141,324`).
  Pagination: Ursache = Controller rechnen Skip/Take aus `page` vor der Trefferzahl, `PaginationState.cs:18-19` (`FirstItemNumber`) ungeklemmt;
  Fix = globaler Result-Filter `Filters/PaginationSeiteKlemmenFilter` (Redirect, nur GET), kein Controller angefasst. Build 0 Fehler;
  Web 3254/1 übersprungen, Dienst 769 grün. Kein Publish, kein Merge, nichts an Sage. Offen: qa-agent.
