---
typ: bug
created: 2026-11-08
quelle: "[[2026-11-08-buendel-2026-11-08-goal-lauf]]"
---
# Seiten-Querscrollen bei 1520 px durch die Navigationsleiste; Kontrast Kartenlink „Meine Fehlteile“

Zusatzfunde der qa-agent-Läufe im Bündel 2026-11-08 (DEV v1.79.0–v1.81.0, nicht aus deren Diffs):
- Bei 1520 px Fensterbreite scrollt die ganze Seite waagrecht (IDEAL-DEV `/ServiceSettings`, `/StorageLocations`, Artikelinfo). Ursache laut
  QA: die gemeinsame Navigationsleiste ist zu breit, der Benutzername rechts wird abgeschnitten. Widerspricht v1.64.1 („genau ein Scrollbalken“).
- Kartenlink „… Fehlteile (wird nachgeliefert)“ auf `/WarehouseRequisitions` in `text-warning` auf Weiß ≈ 1,6:1 (WCAG AA verlangt 4,5:1).
