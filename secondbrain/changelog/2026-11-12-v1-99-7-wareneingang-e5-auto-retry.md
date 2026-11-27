---
type: changelog
version: 1.99.7
date: 2026-11-12
---
# v1.99.7 — Sage-Wareneingänge: automatisch wiederholen mit Sage-Prüfung, Klären, Q-2, Menü

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `f8d74399` (Code), `af0a75f2` (TS-116.20–116.24), QA-Korrektur `bb788f0e`.
Spec [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]], Abschnitt „Freigabe-Nachtrag 2026-11-12: E5 Variante A …“.
Nicht gemergt, nicht gepusht. Angelegt vom qa-agent (fehlte; der Nachlauf-Eintrag verweist als Quelle hierher).

## Kernpunkte

- E5 Variante A: `Fehler` nach echtem Sende-Versuch wird automatisch wiederholt (`Sync:SageWareneingangMaxWiederholungen` 0–10, Standard 3;
  `Sync:SageWareneingangWiederholAbstandMinuten` 5–1440, Standard 15). Vor jedem erneuten Senden SData-Suche `EinkaufBelegeInfo` nach Memo
  `WE#<Id>#`: 1 Treffer = übergeben ohne POST, mehrere = Fehler; Suche scheitert = nicht senden.
- Sicherungsnetz (nur automatisch, 0 Memo-Treffer): Bestellung + Belegdatum `ge @yyyy-MM-dd@` + Lieferschein → neuer Status **Klären** (`6`).
- „Als übergeben markieren“ (Belegnummer Pflicht) für Fehler/Klären; „Erneut senden“ auch für Klären, gesperrt bei Schalter aus.
- Q-2: Schalter aus → Offen/Fehler gehen auf Klären, Hinweis in Liste und /ServiceSettings; beim Wiedereinschalten nichts automatisch.
- Menü „Lager“: eigener Block „Sage-Lagerbuchungen“ / „Sage-Wareneingänge“ (Umbenennung der Liste).
- QA-Korrektur `bb788f0e`: Status nach der Sage-Suche neu lesen (Handgriff in der Web-Seite während der Suche wird nicht überschrieben, kein POST);
  Q-2 nur bei tatsächlich gelesenem „aus“ (kein Safe-Fallback).

## Deploy-Risiken

Keine Migration. Web und Dienst deployen. Nachlauf `v1.99.7-SageWeWiederholen` (Einstellungen bestätigen), `v1.99.7-EinkaufKlaeren` (Info).
Wer beim Deploy offene/fehlerhafte Sage-WE bei ausgeschaltetem Schalter hat, findet sie danach auf „Klären“ (gewollt, Q-2).
