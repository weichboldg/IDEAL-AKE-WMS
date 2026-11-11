---
typ: bug
created: 2026-11-08
quelle: "[[2026-11-08-einkauf-bestellvorschlaege-spec]]"
---
# Meldebestand-Mail: Bestand falsch berechnet (Umbuchung als Zugang, Artikel ohne Bewegung fehlen)

Zusatzfund beim Spezifizieren des Einkauf-Epics (Codelesung im Bündel-Worktree v1.78.0, nicht zur Laufzeit geprüft), aufgenommen in
[[2026-11-08-buendel-uebersicht]]:
- `StockCheckService` zählt eine Umbuchung zwischen zwei regulären Lagerplätzen als Zugang (`StockCheckService.cs:41-42`, gleiche Logik im
  HAVING) → Bestand zu hoch, die Meldebestand-Mail (`Notifications:MeldebestandEnabled`) kann ausbleiben.
- Artikel ohne Bewegung fallen durch den INNER JOIN ganz heraus (`:58`).
- Das Einkauf-Epic nutzt diesen Code deshalb nicht.

Zuerst mit einem Artikel nahe Meldebestand und einer Umbuchung auf DEV nachstellen.
