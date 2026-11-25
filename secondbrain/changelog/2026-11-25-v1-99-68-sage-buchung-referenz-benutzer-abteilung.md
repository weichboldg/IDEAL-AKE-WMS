---
type: changelog
version: 1.99.68
date: 2026-11-25
---
# v1.99.68 — Sage-Buchungen: Referenz mit Benutzer und Abteilung

Spec [[2026-11-24-sage-buchung-referenz-benutzer-abteilung-spec]], Lauf [[2026-11-25-goal-lauf-beschichtung-sage-buchungen]].
Commit `19244bfa`, QA in der Spec (2026-11-25, geprüft auf HEAD `eff16911`). **SQL/167** + EF-Migration
`20261125134408_AddStockMovementAbteilung` (FreshInstall an beiden Stellen). TS-190. Nicht gemergt.

## Kernpunkte

- Neu `IDEALAKEWMSService/Services/SageReferenz.cs`: `Kurz(kennung, abteilung, benutzer)` = `<Kennung> <Abteilung> <Benutzer>`, auf **31** Zeichen
  (`KHKLagerplatzbuchungen.Referenz` varchar(31)) am Ende gekürzt – erst Benutzer, dann Abteilung, Kennung nie; `Voll(kennung, benutzer, abteilung)` =
  `WMS: <Kennung> <Benutzer>[ Abteilung:<x>]`.
- Lagerbuchungs-Payload (`SageLagerbuchungPayloadBuilder`): je Zeile `Referenz` = Kurz, `Memo` = Voll. **Kopf-Memo unverändert**
  (`SageBookingCorrelation.Memo(kennung)`, Korrelation/Lookup byte-gleich). Benutzer = `StockMovement.CreatedBy`, nie der Queue-Eintrag
  (Nacherfassung schreibt `system:sage-booking`); Requeue/Recovery senden denselben Text.
- Sage-WE-Kopf-Memo: `WMS: WE#…# <Benutzer>` (`SageReferenz.Voll`, Benutzer aus `PurchaseOrderReceipt.CreatedBy`); enthält `WE#…#`, Memo-Suche
  und Wiedererkennung bleiben gültig.
- Ausbuchung und Lagerplatz ausbuchen: Auswahlfeld **Abteilung** (optional, Werkbank-Namen ohne „nicht in Sage“), serverseitig gegen die Liste
  geprüft (unbekannt = nichts gebucht, Meldung). Beim Lagerplatz ausbuchen gilt sie für alle Zeilen. Gespeichert als Schnappschuss `StockMovements.Abteilung`.
- Sage-Buchungen: Spalte „Abteilung“ (Zahnrad, standardmäßig aus).

## Migration/SQL

- `SQL/167_AddStockMovementAbteilung.sql`: `StockMovements.Abteilung NVARCHAR(200) NULL`, `COL_LENGTH`-Guard, History-Insert separat; additiv, kein Backfill.
- EF `20261125134408_AddStockMovementAbteilung`. `SQL/AgentJobs/` nicht betroffen.
- **Dienst erst nach dem Schema starten** (geteiltes EF-Modell, der Dienst liest `StockMovement.Abteilung`).

## Deploy

Web **+ Service + SQL**. Reihenfolge: DB-Backup (Routine) → Web stoppen → Schema SQL/167 → Web → Dienst. Manifest `scripts/deploy/nachlauf.psd1`:
`SQL/167` (Schema, automatisch, beide) und `v1.99.68-SageReferenzInfo` (Info, Nachher: Lager informieren).

## Risiken

- **P-2 offen (Sage-Testsystem, nur Mensch):** Sage füllt `KHKLagerplatzbuchungen.Referenz` heute selbst aus dem Kopf-Memo. Ob die gesendete
  Zeilen-`Referenz` das übersteuert und ob das Zeilen-`Memo` sichtbar ankommt, ist nicht belegt (QA R-1). Auf DEV wurde nichts an Sage gesendet.
- Freigabe-Antwort „Abteilung max. 10 wie DPS“ nicht als eigene Grenze umgesetzt, nur Abschneiden am Ende (bestätigte Regel, QA R-2).
- Altdaten: bestehende Bewegungen ohne Abteilung (NULL), Referenz dann ohne Abteilung.

## Commit / Merge

`19244bfa` im Bündel. **Nicht gemergt** (wartet Schranke 2).
