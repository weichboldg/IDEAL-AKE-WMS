---
type: changelog
version: 1.99.25
date: 2026-11-16
---
# v1.99.25 — Beschichtungsquelle je Standort (Teilumfang E1/E3)

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `c61b7a52` (E1), `318abf1f` (E3), `4a635534` (E6 Teil), `3f247f11` (QA-Befunde
Q-1..Q-6). Spec [[2026-11-16-beschichtungslogik-je-standort-spec]], ADR [[0028-beschichtungsquelle-je-achse-ueber-appsettings-mit-automatisch]],
Lauf [[2026-11-16-goal-lauf-beschichtung-fa-sdata-arbeitsgaenge]]. **QA Teilumfang grün, Status InUmsetzung** (E2/E4/E5 blockiert durch
Haltepunkte H1–H3). Nicht gemergt. Version 1.99.24 bleibt für [[2026-11-14-sicherheit-anmeldung-und-installer-spec]] reserviert.

## Kernpunkte

- Zwei Auswahl-Einstellungen in `/Settings`: „Quelle Beschichtet-Kennzeichen“ (Automatisch | Artikelkategorie | FaStruktur) und „Quelle
  Beschichtungstermin“ (Automatisch | Berechnet | View), Default Automatisch = heutiges Verhalten. Unpassende Kombination wird beim Speichern
  abgelehnt. Die Termin-Quelle wirkt erst mit E2/E4 (Beschreibung sagt das).
- `BeschichtungResolver.ResolveQuellen` als einzige Auflösung für Web und Dienst.
- Dienst: genau ein Lauf schreibt `HasCoatingParts` (Lackierteil-Erkennung oder FA-Materialisierung); Audit-Felder in `SetCoatingPartsAsync`.
- Sage-Import-Hook läuft nur noch mit `Sync:CoatingDetectionEnabled` (Freigabe-Antwort 12).

## Migration / SQL

Keine. Web und Dienst gemeinsam deployen.

## Deploy-Risiken

- **Import-Hook:** flaches System mit `Sync:BomCacheEnabled` = true und `Sync:CoatingDetectionEnabled` = false → neue Aufträge ohne
  Lack-Kennzeichen. Manifest-Prüfung `Pruefung/LackierteilErkennungImportHook` (Nachher, anzeigen) meldet es; Testsystem AKE hatte den
  Schalter am 2026-10-20 an, Prod unbekannt.
- `Pruefung/BeschichtungQuelleKombination` (Haltepunkt, anzeigen) meldet unpassende oder fremde Quellwerte.
