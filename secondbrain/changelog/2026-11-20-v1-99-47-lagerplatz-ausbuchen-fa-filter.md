---
type: changelog
version: 1.99.47
date: 2026-11-20
---
# v1.99.47 — Lagerplatz ausbuchen: FA-Feld filtert

Bündel `feature/2026-08-07-ideal-teile-1-5`, Basis `2af81077` (v1.99.46). Spec [[2026-08-05-wms-bugs-improvements-teil-6-spec]],
Aufgabe [[2026-11-20-dev-lagerplatz-ausbuchen-fa-filter]]. Commits `29577a3a`, `d28a6488`, QA-Fix `0b6fff36`.
Kein SQL, keine Migration, keine Einstellung. Manifest: Info `v1.99.47-LagerplatzAusbuchenFaFilter`. TS-173 + TS-2.4 neu. Nicht gemergt.

## Kernpunkte

- Spalte „Fertigungsauftrag“ je Artikel (FA nur aus Bewegungen mit Restbestand am Paar). Neue Repository-Methode
  `GetProductionOrdersByArticleAtLocationAsync` → `FaAmPaar` (Nummern, Zugangs-Nummern, Zugang ohne FA); `GetProductionOrdersAtLocationAsync`
  davon abgeleitet (Wagen-Konflikt unverändert).
- FA-Feld mit Wert filtert (exakte Einzelnummer, case-insensitiv); Regel an einem Ort `FaBuchungsText.PruefeZeile`/`IstGemischt`, JS bildet sie
  als Vorschau nach. Gemischte Zeilen (G1 weitere FA, G2 Zugang ohne FA, G3 FA nur aus Ausbuchung) werden im Modal je Zeile abgefragt.
- Server prüft beim Absenden gezeigte treffende/gemischte Zeilen (`SetEquals`) und Stückzahl je Zeile (invariante Schreibweise); jede
  Abweichung → nichts buchen, Warnung, Redirect mit Platz + Eingabe. Leeres Feld = Verhalten wie bisher.
- IDEAL (Master `ProduktionsauftragHierarchisch`): Haupt-FA schließt Sub-FA jeder Ebene ein (Textweg über `GetBySubOrderNumbersAsync`), Sub-FA
  nur sich selbst. Wagen: Vorbelegung bleibt und filtert, Rest „bleibt am Wagen“.

## Deploy-Risiken

Verhaltensänderung am Kommissionierwagen (Artikel ohne FA bleiben am Wagen) → Lagermitarbeiter informieren (Manifest-Info). Web + Dienst
(Dienst nur Versionsnummer).

## QA (2026-11-22)

QA-Fix `0b6fff36`: `data-fa="@Json.Serialize(...)"` schrieb rohes JSON ins Attribut → `JSON.parse` warf, Filter/Modal/Wagen waren im Browser
tot (kein Test rendert die View). Jetzt `.ToString()`, Wächter `JsonImAttributWaechterTests`, [[fallstricke]] §59. DEV AKE + IDEAL geprüft.
Status Testbereit; Publish `publish\` mit Commit `0b6fff36`.
