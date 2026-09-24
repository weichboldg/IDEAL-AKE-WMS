---
typ: notiz
spec: "[[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: FA-Struktur — erkannte BDE-Arbeitsgänge je (Sub-)FA im Modal

Spec [[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]] · **bestehender Bündel-Worktree** (kein neuer).
`depends_on` [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] (v1.44.0, Testbereit im selben Worktree).

## Pre-Flight-Verdikt (2026-09-24)

- status Freigegeben, open_questions [], freigabe_entscheidung/_von/_am gesetzt.
- Kritische Prüfung: B1, B2, S1 in den Rumpf eingearbeitet, O1 vom Menschen bestätigt — kein offener Blocker.
- Worktree-Stand: AppVersion 1.44.0 → dieser Lauf **1.45.0**; keine Migration.

## Stand

- [x] Repository-Methoden (WorkOperation ×2, FaHierarchyNode GetByHauptFa falls fehlt) + Tests inkl. ToQueryString (AK 10)
- [x] Controller Index (HatArbeitsgaenge) + Action Arbeitsgaenge + ViewModel
- [x] Views (Knopf, Modal-Shell, Partial) + JS
- [x] Version, Changelog, TS-78, Brain

## QA (2026-09-24)

**Testbereit.** Build gruen, Tests gruen (Web 1398/1399 + 1 bestehender Skip, Service 268/268),
AK 1–12 gegen den Code geprueft (keine Abweichung), TS-78.1–78.9 vollstaendig, Index-Zeile 78 korrekt.
Deploy: nur Web-Publish, keine Migration, kein Service. Details/Evidenz im QA-Nachweis-Abschnitt der
Spec [[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]]. Naechster Schritt: Schranke 2
(Mensch: Publish aus dem Worktree, Manual-UAT laut TS-78, dann Merge).
