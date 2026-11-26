---
type: bug
title: "Lagerbestellung: Artikelnummer wird ungetrimmt bzw. in Eingabe-Schreibweise gespeichert"
status: offen
severity: niedrig
created: 2026-11-26
affected_code:
  - IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs (AddItemAsync :115-157, einziger Schreibweg der Spalte)
  - IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs (AddItem :65-108, QuickAdd :171-296)
  - IdealAkeWms/Controllers/WarehouseRequisitionsController.cs (FehlteilHinweiseAsync :203-221, Reihenfolge der Listennummern)
source_backlog: "[[2026-11-26-lagerbestellung-artikelnummer-varianten]]"
spec: "[[2026-11-26-lagerbestellung-artikelnummer-varianten-spec]]"
---

## Symptom

QA v1.99.86 (H-3, [[2026-11-26-lagerbestellung-hinweis-fehlteil-bereits-bestellt-spec]]): Lagerbestellung #81 auf DEV AKE enthält `EK013128`
und ` ek013128` (mit führendem Leerzeichen) als zwei Positionen. Der Unique-Index auf (Liste, Artikelnummer) hat das nicht verhindert.
Nebenbefund H-1/H-2: Die Reihenfolge der Listennummern im Hinweis „bereits als Fehlteil bestellt“ hängt an `Distinct()` und hat bei gleichem
`ClosedAt` keine feste Sortierung.

## Reproduktion

- Die DEV-Daten von #81 hat die QA **per SQL** angelegt, nicht über die Oberfläche (QA-Nachweis der Spec v1.99.86: Testdaten „QA207 …“, Schritt 207.8
  „alle Entwurfspositionen auf DEV entstanden per SQL“). Über die Anwendung ist die Doppelung bei CI-Kollation nicht nachstellbar, siehe Root Cause.
- Über die Anwendung reproduzierbar ist die **Variante als Einzelwert**:
  1. `POST /api/warehouserequisitions/{id}/items` mit `{"articleNumber":"ek013128","quantity":1}` → gespeichert wird `ek013128` statt der
     Stammschreibweise.
  2. Stücklisten-Schnellerfassung mit einer Ressourcenummer in abweichender Groß/Klein-Schreibung → gespeichert wird die Stücklisten-Schreibweise.

## Root Cause

Der einzige Schreibweg `WarehouseRequisitionRepository.AddItemAsync` (`:137-148`) speichert den übergebenen Text unverändert. Seine drei Aufrufer
geben die Eingabe weiter und nicht die Artikelnummer des Stammartikels, obwohl sie ihn schon geladen haben:
- `AddItem` gibt `body.ArticleNumber` ungetrimmt weiter (`WarehouseRequisitionsApiController.cs:68,99`).
- `QuickAdd` gibt die getrimmte Eingabe in Eingabe-Schreibweise weiter (`:206,214,281`).
- Der DUMMY-Weg gibt die Konstante weiter (`:153`) und ist unkritisch.

Echte Doppel in einer Liste verhindert heute die SQL-Kollation: Der Duplikat-Guard (`:122-124`) und der Unique-Index vergleichen in SQL Server
CI und ohne nachlaufende Leerzeichen. Ein **führendes** Leerzeichen bleibt aber ein Unterschied. Die Kollation der Bestands-DBs ist nicht belegt
(FreshInstall legt `Latin1_General_CI_AS` an, `SQL/00_FreshInstall.sql:11`), siehe Prüfabfrage P-1 der Spec.

## Fix / Verweis

[[2026-11-26-lagerbestellung-artikelnummer-varianten-spec]]
