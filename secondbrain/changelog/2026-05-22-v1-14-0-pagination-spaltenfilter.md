---
type: changelog
version: 1.14.0
date: 2026-05-22
---
# v1.14.0 — Pagination + Server-Side-Spaltenfilter als Pflicht-Pattern

- **Einheitliche Pagination** in 22 Listen: `PageSize.Resolve` + `PaginationState` +
  `_Pagination`-Partial. Groessen 25/50/100/„Alle" (Cap 5000, mit sichtbarem Hinweis statt stillem
  Abschneiden). User-Default `User.DefaultPageSize`.
- **Server-Side-Spaltenfilter** (`?colf_*`): Filter wirken ueber alle Seiten. Datumsspalten werden
  in C# **nach** der Termin-Berechnung gefiltert, nicht in SQL →
  [[0005-listen-view-pattern-mit-server-side-spaltenfilter]].
- Das Ganze als **Pflicht-Pattern** in der Projekt-Doku verankert; drei uebersehene Listen
  (FaCompletion, SyncLog, Tracking) wurden nachgezogen, der harte 200er-Cap im Protokoll durch
  Pagination ersetzt.
- **Leitstand** wird eigenes Hauptmenue (vorher Unterpunkt der Kommissionierung).
- **FA-Vervollstaendigung** hinter Feature-Toggle `FaCompletionAktiv`.
- Lagerbestellungen: Notiz je Position + Autosave; **Mengen auf `int` umgestellt** wegen des
  Culture-Bugs — „4.0000" wurde von der deutschen Request-Culture als **40000** geparst.
- `StorageLocation.Code` auf NVARCHAR(50) erweitert (manuelle Codes bleiben auf 12 Zeichen wegen
  Barcode-Lesbarkeit).
- OSEON-Tabellen-Hover angeglichen; Bestand: Source-Lagerplatz-Vorschlag bei Sage-Bestand.
