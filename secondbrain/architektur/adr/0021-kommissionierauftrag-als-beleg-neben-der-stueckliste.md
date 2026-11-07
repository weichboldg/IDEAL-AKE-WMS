---
type: adr
id: 0021
title: Kommissionierauftrag als Beleg neben der Stueckliste – Stueckliste bleibt Soll-Quelle, Fortschritt am Auftrag
status: accepted
date: 2026-11-08
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]] (v1.88.0), Freigabe Gerald 2026-11-07 19:17. Fortschreibung des
> Satelliten-Gedankens aus [[0009-app-status-in-satelliten-tabellen-neben-sage-master]] (Sage-Daten bleiben unberuehrt, WMS-Eigenes daneben).

## Kontext und Problem

Kommissionieren war ein Zustand der Stueckliste: `PickingItems` hielt je FA und Stuecklistenzeile `IsPicked`/`IsTransferred`, angelegt beim
ersten Oeffnen und danach nie nachgezogen. Es gab keinen Beleg, keine Nummer, keine Teilmengen, keine Historie und keine Aufteilung nach
Bereichen (Lager, Glas, Vorkommissionierung). Spaetere Stuecklistenaenderungen kamen nie an.

## Entscheidung

- Eigener Beleg `PickingOrders`/`PickingOrderLines` mit Nummernkreis `KO-JJJJ-NNNNN` (gemeinsamer Helfer `Nummernkreis` mit dem
  Beschichtungsauftrag), je FA und Kommissionierbereich (`PickingAreas` + Regeln) ein Auftrag, 1–n Auftraege je FA bis vollstaendig
  (Folgeauftrag mit `UrsprungsauftragId`).
- Die Stueckliste bleibt **Soll-Quelle** und wird nicht mehr abgehakt. Erzeugt wird idempotent ueber Restmengen
  (`PickingOrderRestmengen`: Soll minus Abdeckung aller nicht stornierten Auftraege) – dieselbe Funktion bei Freigabe, Nachziehen und
  Oeffnen ohne Leitstand.
- `ProductionOrderPickingStatus.IsDonePicking` bleibt **gespeichert** (vier weitere Schreiber, viele Leser im Dienst) und wird beim Abschluss
  gesetzt, wenn kein Auftrag mehr offen/in Arbeit ist – nicht berechnet.
- Kein Doppelpfad und kein globaler Schalter (Antwort 13): Altpfade (`TogglePicked`, `TransferPicked`, `SetPickingStatus`, `PrintPicking`,
  `PickingRepository`) entfernt; `PickingItems` bleibt nur als Altbestand, Ueberfuehrung per Einmal-Skript ([[0015-einmal-datenskripte-ausserhalb-der-nummerierten-sql-reihe]]).
- Kommissionier-Umbuchungen bleiben direkt am Context (nie Sage, [[fallstricke]]) und tragen jetzt den verknuepften FA (`StockMovement.ProductionOrderId`).

## Konsequenzen

- Positiv: Nummer/Druck/Barcode, Teilmengen, Historie wer/wann/welche Menge, Nachziehen bei Stuecklistenaenderung, Bereiche je Regel.
- Negativ: Umstellung nur im Wartungsfenster (Altbestand-Skript, danach Nachziehen); Sammelkommissionierung und Typ Lager-/Glasbestellung
  sind im Modell vorbereitet (`Typ`, nullable `ProductionOrderId`), aber nicht gebaut.
- Regel fuer Folgearbeiten: neuer Kommissionier-Schreibweg nur ueber `PickingOrderWorkflow` bzw. `PickingTransferService.TransferOrderLinesAsync`.
