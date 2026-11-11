---
type: bug
title: Fehlteile „Nur meine Werkbaenke“ filtert nur die geladene Seite, Trefferzahl und Tab-Badges falsch
status: offen
severity: mittel
created: 2026-11-07
affected_code:
  - "IdealAkeWms/Controllers/MissingPartsController.cs:52-63, 96-129"
  - "IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs:389-447"
  - "IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs:65-71"
source_backlog: "[[2026-10-28-fehlteile-nur-meine-werkbaenke-trefferzahl]]"
spec: "[[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec]]"
---

## Symptom

Werker mit **zwei oder mehr** zugeordneten Werkbaenken sieht unter `/MissingParts` (Default `mineOnly=true`):
- nur die eigenen Fehlteile, die zufaellig auf der aktuell geladenen Seite (nach Datum sortiert, ueber **alle**
  Werkbaenke) liegen; eigene Fehlteile auf spaeteren Seiten fehlen,
- eine Trefferzahl/Seitennavigation, die nur die Zeilen dieser einen Seite zaehlt,
- Tab-Badges („Offene Fehlteile“ / „Wird nicht nachgeliefert“), die **alle** Werkbaenke zaehlen.

Werker mit genau einer Werkbank sind nicht betroffen (dort geht die Id in SQL, `MissingPartsController.cs:61-62`).

## Reproduktion

1. User U mit Werkbaenken A und B; fremde Werkbank C. Seitengroesse 10.
2. 15 Fehlteile (Closed, NoRestock) auf C mit juengstem `ClosedAt`, danach 3 auf A.
3. `/MissingParts?tab=NoRestock` als U.
4. Ist: Seite 1 leer (die 10 juengsten sind C), Trefferzahl 0, Badge „Wird nicht nachgeliefert“ = 18.
   Soll: 3 Zeilen, Trefferzahl 3, Badge 3.

Code-Variante (ohne Daten): `MissingPartsController.cs:96-101` holt Seite `page` ueber alle Werkbaenke
(`effectiveWorkplaceId == null`), `:104-111` filtert diese Seite im Speicher nach Werkbank-Namen und setzt
`total = filtered.Count`.

## Root Cause

`GetMissingPartsAsync` nimmt nur **eine** Werkbank (`int? workplaceFilter`,
`WarehouseRequisitionRepository.cs:392`, Filter `:406-407`). Fuer mehrere Werkbaenke umgeht der Controller das,
indem er nach `CountAsync`/`Skip`/`Take` (`:424-427`) im Speicher nachfiltert (`MissingPartsController.cs:104-112`)
— Verstoss gegen ADR 0005 (filtern vor zaehlen und paginieren). Die Badge-Aufrufe (`:120-129`) bekommen dieselbe
`null`-Werkbank und zaehlen deshalb alle Werkbaenke.

## Fix / Verweis

[[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec]]
