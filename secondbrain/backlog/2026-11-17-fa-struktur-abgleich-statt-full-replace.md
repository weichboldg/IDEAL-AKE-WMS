---
type: backlog
status: offen
created: 2026-11-17
quelle: Gerald, Chat 2026-11-17 (/spec)
---
# FA-Struktur: Datenbankstruktur mit Abgleich statt Full-Replace

Löst R2/R4 aus [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]]. Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig
`feature/2026-08-07-ideal-teile-1-5` (Stand v1.99.34).

## Grundlage

- `FaHierarchySyncService.cs:99-104`: DELETE + Insert je Lauf (Full-Replace).
- Befund in [[2026-11-17-pruefabfragen-gerald]]: `vwDPSBS_CWS_PPS_FaBelegeInfo` filtert `IstBeendet = 0 AND Aktiv = -1`.
- IDEAL-PROD hat 0 `FaHierarchyNodes` → keine Datenmigration nötig.
- 43 Code-Stellen nutzen `FaHierarchyNode`.

## Entscheidungen Gerald 2026-11-17

- **(a)** Beendete Haupt-FAs bleiben im WMS: Abgleich (neu/geändert/verschwunden) statt Full-Replace, für View- und SData-Quelle gleich.
- **(b)** Lebenslauf je FA: `ErstGesehen`, `ZuletztGesehen`, `VerschwundenSeit`, `VerschwundenGrund`. Nur für verschwundene FAs eine
  Einzelabfrage in Sage (beendet → behalten, gelöscht/storniert → entfernen).
- **(c)** Aufräumen, wenn alle zugehörigen WMS-FAs erledigt sind oder nach einstellbarer Frist.
- **(d)** Fachlicher Schlüssel Haupt-FA, Vater-FA, Sub-FA, Position, Artnr (wie `UX_BdeTeilMeldungen_Schluessel`) eindeutig;
  BelID/StrukturID mitführen, wo verfügbar.
- **(e)** R4: Artikeltyp für FA-Knoten vom Artikel (`USER_OSArtikeltyp`); Grundsatz Artikelfelder aus dem Artikelstamm; bestehende Spalten
  vorerst behalten.
- **(f)** `FaHierarchyOrderInfos` und `ProductionOrderPositions` mit derselben Abgleich-/Lebenslauf-Logik (Grundlage für die
  AG-Rückspielung, [[2026-10-30-fa-rueckmeldung-an-sage]]).

## Schwerpunkte

- Datenmodell und Migration (keine Altdaten).
- Sync-Ablauf mit Transaktion und Schutz vor leerem Read.
- Verhalten in FA-Struktur, Vormontage, Beschichtung, Leitstand, BDE-Teileliste (verschwundene FAs kennzeichnen).
- Prüfabfragen.
- Erster Dev-Schritt: `OBJECT_DEFINITION(vw_IDEAL_AKE_WMS_FAListe)` lesen (enthält die View beendete FAs?).
- `PositionText` (SQL/157) beachten.

## Grenzen

Nur die Spec, kein Code. Version/SQL beim Dev-Start prüfen. Nichts an Sage schreiben.
