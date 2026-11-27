---
type: changelog
version: 1.99.8
date: 2026-11-12
---
# v1.99.8 — Kommissionierung: Matchcode in der Auftragsliste und im Auftragsdruck

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commit `557ea63b` (Code + TS-120.17). Spec [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]],
Abschnitt „Freigabe-Nachtrag 2026-11-12 (Gerald): Matchcode in der Auftragsliste Kommissionierung“ (der Nachtrag nannte 1.99.7; die ging an Wareneingang E5).
Nicht gemergt, nicht gepusht. Angelegt vom qa-agent (fehlte).

## Kernpunkte

- `/Picking` (Leitstand an): Spalte „Matchcode“ nach „Artikelnummer“, Quelle `Article.Matchcode` des FA-Artikels über `MatchcodeLookup.BuildAsync`
  (wie FA-Liste), Server-Spaltenfilter vor dem Blättern, Zahnrad. Matchcode nicht eindeutig: nur Anzeige/Filter.
- Auftragsdruck (`PrintPicking`): „Matchcode: …“ im Kopf unter „Artikel“, ohne Matchcode keine Zeile.
- QA 2026-11-12: Befunde N-1 (kein Drei-Quellen-Wächter für `Picking/Index`) und N-2 (Testkommentar verrutscht), beide niedrig, offen.

## Deploy-Risiken

Keine Migration, kein SQL, keine Einstellung, kein Nachlauf. Web; Dienst nur wegen `AppVersion.cs`.
