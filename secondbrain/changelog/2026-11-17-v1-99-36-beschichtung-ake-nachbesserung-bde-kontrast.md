---
type: changelog
version: 1.99.36
date: 2026-11-17
---
# v1.99.36 — Beschichtung AKE Nachbesserung (Nachtrag 4) und BDE-Kontrast

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `8e0a346e` (Beschichtung) und `e40ecfcb` (BDE). Keine Migration, kein SQL.
Specs [[2026-11-16-beschichtungslogik-je-standort-spec]] (Nachtrag 4) und [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]]
(Nachtrag Kontrast). Goal-Lauf [[2026-11-17-goal-lauf-sammel-abend]]. Nicht gemergt.

## Kernpunkte

- **Kopf je WA (AKE flach):** `BeschichtungFlacheQuelle.KopfInfosAsync` bildet `FaHierarchyOrderInfo` mit `Id = 0` (Kunde, Fertigungstermin,
  berechneter Beschichtungstermin; Terminkontext einmal je Liste). Die Kopf-Partials zeigen bei `Id == 0` die WA-Felder.
- **Maße:** `ArticleSageDetails.Breite/Hoehe/Laenge × 10` = mm (Laenge = Tiefe). Einheit am 2026-11-17 gegen `USER_*mm` bestätigt
  (113.473 von 113.523 Artikeln gleich). Ohne Sage-Zeile 0 und Logger-Warnung.
- **Kommissionierte WA** (`IsDonePicking`) fehlen in der Gesamtliste; Query `kommissionierte=true` (Häkchen, Partial
  `_KommissioniertSchalter`) holt sie; Anlage/Hinzufügen/Vergabe sehen jeden offenen WA. Sammelmeldung: nicht kommissionierte mit Nummer,
  kommissionierte als Anzahl.
- F1 natürliche Sortierung „Positionen hinzufügen“; Modultext „Das Beschichtungsauftragsmodul ist deaktiviert“; F2-Tests; F3 Betriebsdoku.
- **BDE:** dunkle Schrift auf `.btn-success`/`.btn-secondary` nur unter `.bde-terminal` (`bde.css`), Modals mit der Klasse; `site.css`
  unverändert.

## Deploy-Risiken

- Keine Schemaänderung. Web und Dienst (Version). Tests: TS-145.14–145.19, TS-152.12.
