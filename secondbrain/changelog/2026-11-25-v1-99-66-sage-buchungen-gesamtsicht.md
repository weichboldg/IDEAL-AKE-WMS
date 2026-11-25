---
type: changelog
version: 1.99.66
date: 2026-11-25
---
# v1.99.66 — Sage-Buchungen: Lagerbuchungen und Sage-Wareneingänge in einer Liste

Spec [[2026-11-23-sage-buchungen-gesamtsicht-spec]], Lauf [[2026-11-25-goal-lauf-beschichtung-sage-buchungen]].
Commits `0725fc3e`, Foto `2c20cfea`, QA `56851a66` (Brain). Kein SQL. Manifest: Info `v1.99.66-SageBuchungenInfo`. TS-189.1–189.14.
Nicht gemergt.

## Kernpunkte

- „Sage-Lagerbuchungen“ heißt **„Sage-Buchungen“** (`SageBookingQueueController.Index`) und zeigt auch die Sage-Wareneingänge mit allen Knöpfen
  (Erneut senden, Als übergeben markieren, CSV erneut schreiben). Eigene Seite/Menüpunkt „Sage-Wareneingänge“ entfällt;
  `GET /PurchaseOrders/SageWareneingaenge` und `/EinkaufPurchaseOrders/SageWareneingaenge` sind 301 auf `/SageBookingQueue?herkunft=Wareneingang`.
- Reine Logik an einer Stelle: `Services/SageBuchungenGesamtsicht.cs` (Anzeigestatus wartet/gesendet/übergeben/Fehler/klären, Herkunft,
  „Erfasst von“ mit „(nacherfasst)“, Zeitraum-Regel, Zelltexte für View **und** Spaltenfilter). Originalstatus in Spalte „Status im Weg“.
- Zeitraum: ohne „von“ die letzten 30 Tage, offene Buchungen immer. Repository-Signaturen neu:
  `ISageBookingQueueRepository.GetForMonitoringAsync(von, bis)`, `GetSageWareneingaengeAsync(bestellung, von, bis)`; `SageWeListeAsync` entfällt.
- Neuer Filter `[RequireStockReadOrEinkaufAccess]` (stock_read-Kreis oder Einkauf bei `EinkaufAktiv`); Einkauf ohne Lager-Recht sieht nur WE-Zeilen ohne Knöpfe.
- „Nur in Sage buchen“ (NurInSage) und NichtNoetig stehen nicht in der Liste. WE-Spalten Lieferschein/Barcode/Positionsmemo/Memo/CSV im Zahnrad standardmäßig aus.

## Migration/SQL

Keine.

## Deploy

Web. Dienst nur wegen `AppVersion.cs`. Manifest `v1.99.66-SageBuchungenInfo` (Info, Nachher: Lager/Einkauf informieren, Menüpunkt entfällt, alte Links leiten um).

## Risiken

- Lesezeichen auf die alte WE-Seite laufen über die Umleitung; Status wird über die Zuordnung übersetzt (`Offen`/`Bestaetigt` binden bewusst nicht).
- QA-Befund B3 (Einkauf/Admin sieht WE-Zeilen ohne Modul) → in v1.99.69 geändert. B2 (Leerzeichen Intro) → v1.99.67.
- Nicht auf DEV prüfbar: Erneut senden (kein Sage-POST), 189.9/189.12 (zweiter Benutzer), 390 px.

## Commit / Merge

`0725fc3e`, `2c20cfea` im Bündel. **Nicht gemergt** (wartet Schranke 2).
