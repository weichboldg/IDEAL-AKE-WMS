---
type: adr
id: 0001
title: Datenzugriff ueber Repository-Interfaces, Caching als Decorator
status: accepted
date: 2026-03-10
supersedes: ""
superseded_by: ""
---

> **Nachtraeglich erfasst** (2026-07-27). Diese Entscheidung wurde vor Einfuehrung des
> Second Brain getroffen und gelebt; der ADR dokumentiert den Bestand, nicht eine neue Wahl.

## Kontext und Problem

Die Anwendung liest aus mehreren, sehr unterschiedlichen Quellen: der eigenen WMS-Datenbank
(EF Core), der Sage-Datenbank (Views + raw SQL), OSEON (Stored Procedures) und enaio (Views).
Controller sollten davon nichts wissen. Zusaetzlich ist die Stuecklisten-Abfrage (BOM) teuer —
sie laeuft gegen eine Sage-View mit Fallback auf eine OSEON-SP und wird pro FA-Aufruf mehrfach
gebraucht. Caching direkt in die Abfrage-Klasse zu bauen haette Datenzugriff und
Caching-Lebensdauer in einer Klasse vermischt.

## Betrachtete Optionen

- **DbContext direkt in Controllern** — kein Abstraktionsaufwand, aber nicht testbar
  (InMemory-Provider deckt raw SQL nicht ab) und Fremdsysteme lassen sich nicht einheitlich
  anbinden.
- **Repository-Interfaces + Caching innerhalb der Repository-Implementierung** — weniger
  Klassen, aber Cache-Invalidierung und Abfragelogik im selben Typ; Tests muessten den Cache
  mit-simulieren.
- **Repository-Interfaces + separater Cache-Decorator** — eine Klasse pro Verantwortung,
  Cache im DI-Container austauschbar.

## Entscheidung

Jeder Datenzugriff laeuft ueber ein Interface in `../../../IdealAkeWms/Data/Repositories/`
(`IBomRepository`, `IProductionOrderRepository`, `IStockMovementRepository`, …).
Caching wird als **Decorator** um die echte Implementierung gelegt und im DI-Container
registriert, sodass Aufrufer nur das Interface sehen:

| Decorator | umschliesst | Lebensdauer |
|---|---|---|
| `CachedBomRepository` | `BomRepository` | 5 min MemoryCache |
| `CachedSettingRepository` | `AppSettingRepository` | MemoryCache |
| `CachedHolidayRepository` | `HolidayRepository` | MemoryCache |
| `CachedOseonOperationConfigRepository` | `OseonOperationConfigRepository` | MemoryCache |

`BomRepository` liefert `BomQueryResult(Items, DataSource)` — die Quelle (Sage-View oder
OSEON-Fallback) wird mitgegeben, damit die UI sie anzeigen kann (siehe [[0007-bom-quelle-sage-view-mit-oseon-fallback]]).

## Konsequenzen

**Positiv**
- Controller und Services sind gegen Fakes/Moq testbar; das Testprojekt nutzt durchgaengig
  InMemory-Kontexte plus Moq auf den Interfaces.
- Cache-Strategie ist eine DI-Registrierung, kein Eingriff in Abfragelogik.
- Fremdsystem-Zugriffe (Sage/OSEON/enaio) sehen fuer Aufrufer aus wie WMS-Zugriffe.

**Negativ / Risiken**
- Mehr Dateien pro Aggregat (Interface + Impl + ggf. Decorator).
- **Raw-SQL-Pfade sind nicht InMemory-testbar.** Wo ein Repository raw SQL nutzt, gibt es
  entweder einen unit-testbaren Entscheidungs-Helper daneben oder der Pfad bleibt Manual-UAT.
  Fallstrick dazu: „BomCache: raw-SQL ist Produktion, EF-Methode ist Deko" in [[fallstricke]].
- Ein Decorator kann zu Stale-Reads fuehren (5 min BOM-Cache) — bewusst akzeptiert.
