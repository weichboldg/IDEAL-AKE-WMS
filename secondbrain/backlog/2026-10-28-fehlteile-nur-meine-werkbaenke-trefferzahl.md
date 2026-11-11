---
typ: bug
---
# Fehlteile: „nur meine Werkbänke“ filtert nur die geladene Seite, Trefferzahl falsch

**Aufgenommen 2026-10-28** (Gerald, Antwort 4 der Spec [[2026-10-16-aktivitaetsprotokoll-filter-und-namen-spec]]): eigener
Backlog-Eintrag, nicht im Aktivitätsprotokoll-Lauf.

## Befund (Bündel-Worktree v1.69.0, nur gelesen)

- `MissingPartsController.cs:104-112`: Bei „nur meine Werkbänke“ mit mehreren Werkbänken (`effectiveWorkplaceId == null`) wird die
  bereits geladene Seite (`rawRows`) im Speicher auf die Werkbank-Namen gefiltert und `total` auf die Zeilen dieser Seite gesetzt.
  Folge: Treffer auf anderen Seiten fehlen, Trefferzahl und Seitennavigation stimmen nicht. Gleiches Fehlerbild wie im
  Aktivitätsprotokoll (`SyncLogController.cs:36-39`).
- Ursache der Umgehung: Die Repository-Methode nimmt nur eine Werkbank an (`WarehouseRequisitionRepository.cs:392`,
  `int? workplaceFilter`, Filter `:406-407`).

## Offen

- Filter in SQL über eine Werkbank-Liste (Signatur ändern) – Fundstellen und Fehlteile-Testszenarien beim /spec belegen.
