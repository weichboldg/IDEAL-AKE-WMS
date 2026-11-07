---
type: changelog
version: 1.81.0
date: 2026-11-08
---
# v1.81.0 — Karte „Meine Fehlteile“ je Bestelltyp, Werkbank-Auswahl „Nur meine Werkbänke“

Spec [[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec]], Bug [[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-bug]],
Lauf [[2026-11-08-buendel-2026-11-08-goal-lauf]]. Commit `747c1ced`. **Keine Migration**, keine Einstellung. Web; Dienst nur Version. TS-113.

## Kernpunkte

- `GetShortageCountsForUserAsync(userId, type)`: Karte zählt den aktiven Bestelltyp, Links tragen `type` (kippt die Entscheidung „typübergreifend“
  aus dem Glas-Plan 2026-07-03, Freigabe-Antwort 1).
- `MissingParts`: Werkbank-Auswahl im Modus „Nur meine“ bedienbar, nur eigene Werkbänke; fremde `workplaceId` → Hinweis (`ForeignWorkplaceSelected`).
- Haken „Nur meine Werkbänke“ per Klick abschaltbar (verstecktes `false`-Feld hinter der Checkbox).

## Deploy-Risiken

- Keine. Nur Lesepfad und Anzeige.
