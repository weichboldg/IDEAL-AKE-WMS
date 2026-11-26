---
type: changelog
version: 1.99.84
date: 2026-11-26
---
# v1.99.84 — IDEAL: FA-Liste/FA-Struktur Nachbesserung (Gerald)

Nachtrag v1.99.84 in [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-2-spec]], Lauf-Notiz
[[2026-11-26-fa-liste-hauptfa-fa-struktur-goal-lauf]]. Worktree `feature/2026-08-07-ideal-teile-1-5`, Commits `d6e2183f`
(Umsetzung) + `2bde92f9` (QA B1 Manifest-Text) + `7268dcb6` (Fotos). **Keine Migration, keine neue Einstellung.** TS-204.8. Nicht gemergt.

## Kernpunkte

- FA-Liste: Zeichen „HauptFA“ entfällt (seit v1.99.82 ist jede IDEAL-Zeile HauptFA); Kombigerät-Zeichen bleibt.
- FA-Struktur: Spalte Arbeitsgänge an Position 3 direkt nach Matchcode (ColumnDefinitions, Kopf-/Filterzeile, `#column-config`,
  Knotenzeile — positionsgleich, `column-preferences.js` arbeitet nach Position); Bezeichnung `DefaultHidden`.
- Trennlinie (`border-right`) der angehefteten Matchcode-Spalte entfernt.

## Deploy-Risiken

- Gespeicherte Zahnrad-Präferenzen haben Vorrang: Benutzer mit alter Einstellung sehen Bezeichnung weiter und Arbeitsgänge hinten
  (gewollt, Gerald). Neue Reihenfolge erst nach Zurücksetzen der Präferenz.
- Paket 23 = Web + Dienst (nur `AppVersion`).
