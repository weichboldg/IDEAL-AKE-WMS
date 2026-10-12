---
type: changelog
version: 1.51.0
date: 2026-10-12
---
# v1.51.0 — IDEAL: Beschichtungsaufträge bedienen (Vorschlagsliste, Detail, Reklamation, Druck)

Beschichtungsauftrag-Ausbau **Teil 4** ([[2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec]], Übersicht
[[2026-10-06-beschichtungsauftrag-ausbau-uebersicht]]). **Direkt im Bündel** `feature/2026-08-07-ideal-teile-1-5`, Start
`a5b27550` (v1.50.0), Commits `3b5149af`, `8d60cb10`, `17bbf3be`, `5af265f4`, `91c77105`, QA-Fix `5764c664`. **Keine Migration.** Umsetzung
[[2026-10-12-beschichtungsauftrag-ausbau-teil-4-umsetzung]].

**Warum:** Teil 3 hat das gespeicherte Objekt geschaffen, aber niemand konnte es bedienen. Teil 4 liefert den Ablauf
Vorschlagsliste → Entwurf → Übergeben → Druck → Erledigt, Reklamation als Folgeauftrag.

## Kernpunkte

- Vorschlagsliste (Liste + Summiert): Gruppenkopf nennt vergebene Aufträge als Link, Knopf „Aus Vorschlagsliste
  erstellen" nur mit Schreibrecht und freien Positionen; Badge „in BA-…" je vergebener Position. Eine Vergabe-Abfrage je
  Seite (`BeschichtungsauftragVergabeAnzeige`).
- `BeschichtungsauftraegeController` (`/Beschichtungsauftraege`): ADR-0005-Liste mit Statusfilter, Detail mit
  Statusverlauf, Entwurf bearbeiten, Positionsauswahl (Erst-/Reklamations-Entwurf), Bemerkung, Status, Reklamation nur
  aus Erledigt, Druck/PDF ab Übergeben mit Briefkopf (Empfänger = Dienstleister des Auftrags), summiert je Artnr, Barcode
  = Nummer. Je Request genau ein Workflow-Aufruf, RowVersion als Hidden-Feld.
- Neue Rolle `beschichtungsauftrag_schreiben` + `RequireBeschichtungsauftragWriteAccess` (ADR 0006), schliesst Lesen ein.
- Mengen kulturunabhängig gelesen ([[fallstricke]] §22), nur eine filterbare Tabelle je Seite (ebd.).
- TS-84; Tests Web 1619 (+51), Service 272.

## Deploy-Risiken

Nur Web (Service nur Versions-Bump), keine Migration — **setzt Migration 94 aus Teil 3 voraus** (gleiches Bündel).
**Rolle `beschichtungsauftrag_schreiben` unter `/Roles` anlegen und zuweisen** (kein Seed), sonst sieht niemand die
Schreib-Knöpfe. Testaufträge der Abnahme verbrauchen echte Nummern (stornieren). Offen: Kultur des App-Pool-Kontos
(Zusatzfund Z2 der Spec).
