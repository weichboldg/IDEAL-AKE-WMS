---
type: backlog
created: 2026-11-20
status: in-spec
quelle: Gerald 2026-11-20 11:59 (Anhang Mitschnitt-Analyse)
modul: Lagerplatz-Sync / Stammdaten nach Sage
spec: "[[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]]"
---

> [!info] Spec-Entwurf 2026-11-24
> [[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]] – nach Entscheid Gerald 2026-11-24: jetzt mit Direkt-SQL in Sage (`spSysTan`), Schreibweg
> hinter einer austauschbaren Schnittstelle, Übergabe über die bestehende Outbox, eigene ADR. 6 offene Rückfragen.
# Lager und Lagerplätze aus dem WMS in Sage anlegen (vorgemerkt)

Vorgemerkt, noch nicht in den Spec-Lauf (Status `neu`). Referenz: [[2026-11-20-WMS_Lagerplatzanlage_Sage_v2]]
(SQL-Mitschnitt der Sage-Oberfläche, Testmandant `IDEAL_TEST_2026_05_03`: Tabellen, `spSysTan`, Basisplatz,
Platzbezeichnung, Zähler im Lagerort, Umbenennen, Referenz-SQL – Referenz-SQL laut Dokument noch ungetestet).

## Warum vorgemerkt statt Spec
- **Grundsatzentscheidung offen:** Das Dokument schreibt **direkt per SQL in Sage-Tabellen** (`KHKLagerorte`, `KHKLagerplaetze`,
  `KHKTan`). Bisher schreibt das WMS nach Sage nur über SData (z. B. [[2026-07-29-sage-lagerbuchungen-spec]] Titel:
  „Sage-100-Lagerbuchungen über SData-API“). Ein direkter Schreibweg braucht eine eigene ADR (Rechte des SQL-Logins,
  Updatefestigkeit bei Sage-Versionswechsel, Support durch DPS).
- **Bezug G5** in [[2026-11-06-sage-sdata-stammdaten-sync-spec]]: „Lagerplätze … im WMS anlegen und nach Sage übergeben –
  dafür gibt es heute KEINEN SData-Service“. Frage an DPS ist gestellt (Mail an Michael Wiesner 2026-11-20, Block 5,
  [[2026-11-20-dps-consulting-tag-fragen]]). Antwort abwarten, dann SData-Weg vs. Direkt-SQL entscheiden.
- Heute liest der Lagerplatz-Sync aus Sage (Sage führend); bei Anlage im WMS kehrt sich die Datenhoheit um → mit
  [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]] abstimmen.

## Offene Punkte aus dem Dokument (Abschnitt 9)
- WarenflussKZ `WA` nur abgeleitet; `HWE`/`HWA` im WMS nicht zulassen.
- Umbenennen mit „Nein“ nicht mitgeschnitten.
- Lagertechnik eines bestehenden Lagers ändern: nicht untersucht → nicht anbieten.
- Abnahmetest: identisches Lager über Sage und WMS anlegen, Tabellen spaltenweise vergleichen.
