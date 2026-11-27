---
type: changelog
version: 1.99.41
date: 2026-11-18
---
# v1.99.41 — Beschichtungsauftrag: Haken, Versendet, Lackierer, Druck je Position

Bündel `feature/2026-08-07-ideal-teile-1-5`, Basis `c39126c3`, Etappen-Commits `5c042c3d` (E1), `b525cfba` (E2), `4b2b490c` (E3),
`5ff2c7a8` (E4), `b1ce8608` (E5), `cf11a777` (E6). Goal-Lauf [[2026-11-18-goal-lauf-beschichtungsauftrag-ausbau]], Spec [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]]. Nicht gemergt.

## Kernpunkte

- **Haken „Verpackt“:** In der Vorschlagsliste (Liste je Position, Summiert je Artnr = alle freien Positionen) nicht gespeichert, nur Auswahl.
  Angelegt wird nur, was angehakt ist; nichts angehakt = keine Nummer verbraucht. Am Auftrag gespeichert (`BeschichtungsauftragPosition.Verpackt`).
- **Positionen ohne FA** (`OhneFa`): Artikel + Menge; AKE Maße aus `ArticleSageDetails` ×10, IDEAL 0 mit Hinweis (P-3). Nie in den Vergaben.
- **Status Versendet (35):** Entwurf → Übergeben → Versendet → Erledigt; Versendet nur mit allen Haken; Übergeben → Erledigt entfällt.
  In Übergeben sind Positionen änderbar, der Kopf nicht. Ein Speichern (`BearbeitenAsync`), die Aktion `PositionenHinzufuegen` ist darin aufgegangen.
- **Lackierer aus Kreditoren** (`CreditorId`, aktiv + `IsCoater`, Server prüft; Freitext als Rückfall). IDEAL-Vorbelegung über den Kurznamen nur bei genau einem Treffer.
- **Druck/PDF je Position:** laufende Nr., Menge `0.###`, HauptFA in der Kopftabelle, Verpackt zuletzt, Brief-Anschrift live aus dem Kreditor.
  Reihenfolge Detail = Druck (`BeschichtungsauftraegeDruck.Sortiert`, ohne FA zuletzt).
- **Bemerkung (A12):** „nicht gespeichert“ am Feld + Rückfrage vor Drucken/PDF/Speichern/Status (`beschichtung-bemerkung.js`).
- Fallstrick 52.

## Deploy-Risiken

- **Migration SQL/161** `20261118120203_AddBeschichtungsauftragVersandUndVerpackt`, additiv (CreditorId mit FK + Index, VersendetAt/-ByUserId,
  Verpackt/OhneFa Default 0). Kein Backfill. Web; Dienst nur wegen `AppVersion.cs`.
- **Altbestand (Rückfrage 15):** Alle vorhandenen Positionen starten „offen“. Offene Übergeben-Aufträge müssen vor „Versendet“ angehakt werden.
  Manifest-Prüfung `Pruefung/BeschichtungsauftragUebergebenOffen` zeigt die Anzahl (P-4).
- **Ohne Beschichter-Kreditor** ist die Lackierer-Auswahl leer und der Druck hat keine Anschrift (Prüfung `Pruefung/BeschichterKreditoren`).
- Anschrift aus `Creditors.Street/PostalCode/City/Country`: ob das bei Sage-Kreditoren die Briefanschrift ist, ist **unverifiziert** (P-2).

Testszenario TS-162.
