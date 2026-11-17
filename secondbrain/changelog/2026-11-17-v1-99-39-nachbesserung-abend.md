---
type: changelog
version: 1.99.39
date: 2026-11-17
---
# v1.99.39 — Nachbesserung Abend: Nachlesen nach dem Senden, Artikel-Quelle AKE, Seite hinter der letzten

Bündel `feature/2026-08-07-ideal-teile-1-5`, Basis `ab75c336`. Commits `69a39a8b` (F3), `ebff14e0` (Manifest), `78014b9a` (Pagination),
`8d419875` (Version/Changelog/TS). Goal-Lauf [[2026-11-17-goal-lauf-nachbesserung-abend]]. Nicht gemergt, qa-agent ausstehend.

## Kernpunkte

- **QA-Befund F3** aus [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]]: Eine 2xx-Antwort ohne `ErrorText` ist nur vorläufig.
  `SageFaArbeitsgangService` liest den FA danach per GET (`ISageFaStandLookup`) und setzt „angelegt“ nur, wenn die Sage-AG-Nummer an einer
  AG-Position (`RessourceNummer`, P-5) steht. Kein Fund = Fehler mit Text, Lesefehler = „Antwort unklar“ (nie automatisch wiederholt).
  `SageFaArbeitsgangClient.Auswerten` verlangt keinen Handle mehr. Fallstrick 49 Nachtrag. Schalter bleibt aus.
- **Manifest** `Pruefung/ArtikelQuelleSData` (AKE, Nachher, nur anzeigen): `Sync:Quelle:Artikel` nicht exakt `SData` → Hinweis, dass
  `ArticleSageDetails` leer bleiben (Sage-Spalten Bestellvorschläge, Maße Beschichtung AKE). Keine automatische Umschaltung. Befund
  [[2026-11-17-ake-4445-vs-prod-sichtpruefung]]. Betriebsdoku 03/04.
- **Pagination:** globaler Result-Filter `PaginationSeiteKlemmenFilter` (nur GET) leitet bei `page` > letzte Seite auf die letzte Seite um
  (0 Treffer: 1). Ursache und Warum: Fallstrick 50. Kein Controller geändert.

## Deploy-Risiken

- Keine Migration, Web + Dienst (Dienst wegen F3, wirkt nur mit Schalter an).
- AKE-Prod-Update: Hand-Schritt `Sync:Quelle:Artikel = SData` laut Prüfung, sonst fehlen die Sage-Details.
- Pagination-Redirect gilt für alle Listen; Lesezeichen mit zu hoher Seite landen auf der letzten Seite.

Tests: Web 3254 bestanden / 1 übersprungen, Dienst 769 bestanden. TS-159 Nachtrag (x6/x7), TS-160.
