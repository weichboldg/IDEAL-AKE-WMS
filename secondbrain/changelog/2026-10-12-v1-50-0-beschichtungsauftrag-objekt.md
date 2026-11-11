---
type: changelog
version: 1.50.0
date: 2026-10-12
---
# v1.50.0 — IDEAL: Beschichtungsauftrag als eigenes Objekt (Backend, ohne Oberfläche)

Beschichtungsauftrag-Ausbau **Teil 3** ([[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]], Übersicht
[[2026-10-06-beschichtungsauftrag-ausbau-uebersicht]]). **Direkt im Bündel** `feature/2026-08-07-ideal-teile-1-5`, Start
`020bf8af` (v1.49.0), Commits `1410fc5c`, `51c6bc79`, `a1f7fcfd`, QA-Fix `a5b27550` (Fehlerbehandlung außerhalb der Serializable-Transaktion, Context nach Fehler verwerfen, DB-Fehler loggen). **Migration 94** (`SQL/94_AddBeschichtungsauftrag.sql`,
EF `20261012073551_AddBeschichtungsauftrag`), rein additiv. Umsetzung [[2026-10-12-beschichtungsauftrag-ausbau-teil-3-umsetzung]].

**Warum:** Der Beschichtungsauftrag war nur eine gefilterte Sicht auf den Sage-Cache — keine Nummer, kein Status, keine
Spur, was wann an welchen Beschichter ging. Teil 3 führt das gespeicherte Objekt ein; die Bedienung kommt mit Teil 4.

## Kernpunkte

- Tabellen `Beschichtungsauftraege` (Kopf je HauptFA) und `BeschichtungsauftragPositionen` (Herkunftsschlüssel +
  Snapshot, kein FK auf den Cache). Nummer `BA-JJJJ-NNNNN` je Datenbank, UNIQUE, Zähler je Jahr ab 1.
- `BeschichtungsauftragWorkflow`: Entwurf → Übergeben (nur mit Dienstleister) → Erledigt, Storno nur aus Entwurf (gibt
  frei); Doppelvergabe-Sperre nur für Erstaufträge (sperrend: Status ≠ Storniert, inkl. Reklamationen); Anlegen nimmt
  nur freie Positionen und meldet vergebene; Reklamation nur aus Erledigt mit Kopf vom Ursprung, Ketten und
  Mehrfach-Reklamation erlaubt; Hinzufügen im Entwurf (Erst: freie HauptFA-Positionen, Reklamation: nur Ursprung).
- Nummernvergabe `MAX+1`, UNIQUE-Index entscheidet, Retry; Erstauftrags-Pfade in Serializable-Transaktion.
- Knoten ungecacht gelesen (nicht der 5-Minuten-Decorator). `TimeProvider.System` neu registriert.
- TS-83; Anwender-Changelog „Vorbereitung, noch nichts sichtbar".

## Deploy-Risiken

Web + **Migration 94** (SQL-Skript vor dem IIS-Recycle; `Migrate()` beim Start würde die rein additive Migration auch
selbst anwenden). Kein Service-Anteil, nicht daten-destruktiv. Vor der Abnahme Prüfschritte TS-83.1 (Schlüssel-
Eindeutigkeit) und TS-83.2 (View-Filter) auf der Sage-DB IDEAL.
