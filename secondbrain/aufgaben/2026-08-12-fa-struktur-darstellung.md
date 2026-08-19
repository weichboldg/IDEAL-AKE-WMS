---
type: aufgabe
title: "FA-Struktur: Darstellung, Spaltenfilter, Icons, Spaltenauswahl (Nachtrag Teil 2)"
status: Testbereit
spec: "[[2026-08-12-fa-struktur-darstellung-spec]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
created: 2026-08-19
updated: 2026-08-19
---

# FA-Struktur Darstellung (Nachtrag Teil 2) — BEREITS UMGESETZT, /dev = Verifikation

**Befund (2026-08-19):** Diese freigegebene Spec ist im Bündel-Worktree **bereits vollständig
umgesetzt** — die Spec-Analyse (Karten-`<div>`, Kontrast-Bug bei Z. 174–176) ist **stale** gegen einen
älteren Commit; seither wurde der ganze Umbau als „Etappe 7" geliefert:
- `961749f wip: fa-struktur - Kontrast-Fix WCAG AA` (Block A)
- `767f06f epic … Etappe 7 - FA-Struktur seitenweite Tree-Table (OSEON) + Kontrast + Icons +
  Baum-Spaltenfilter + column-prefs` (Block B)

Der /dev-Lauf **baut daher nichts neu** (das wäre Doppelarbeit/Rückschritt), sondern **verifiziert**
den vorhandenen Stand gegen die 16 AK und lässt den qa-agent Build+beide Suiten fahren.

## Vorhandener Stand (verifiziert gegen die Spec)
- **AC 1 Kontrast:** `site.css:127` `.fa-structure-header { background: var(--ake-light-gray); color:
  var(--ake-text) }` — rechnerisch #4A494A auf #F5F5F5 = **8,2:1** (dokumentiert im CSS-Kommentar).
- **AC 2/8 Tree-Table:** `Views/FaHierarchy/Index.cshtml` — EINE `<table id="faTree"
  data-view-key="FaHierarchyStructure" class="fa-tree-table">`, `<tbody class="fa-structure-group">`
  je Struktur, colspan-Kopfzeile + Struktur-Chevron; `_FaHierarchyNode.cshtml` rendert flache
  `<tr class="fa-node-row">` mit `data-node-id/parent-id/depth`, Einrückung nur Struktur-Spalte.
- **AC 3 Schmal:** `.fa-tree-table`-Responsive-Regeln (`lg`/992px). Reale Terminal-Auflösung =
  Manual-UAT-Bestätigung (offene Rückfrage der Spec).
- **AC 4/5/6 Spaltenfilter:** `wwwroot/js/fa-hierarchy-tree.js` (344 Z.), Filter-Zeile mit `<select>`
  für Arbeitsbereich/Komm.-Ziel/Status + Freitext sonst; `#faNodeCounter` „X von Y"; zwei klar
  beschriftete Mechanismen (Hervorheben vs. Filtern), `.fa-node-context` (gedimmter Vorfahrenpfad).
- **AC 7 Icons:** `_FaHierarchyNode.cshtml` `switch(Model.IconType)` (`FaNodeIconType`
  Root→Assembly→Purchased→Material), Form+Farbe+`title`; ausklappbare Legende (`<details>`).
- **AC 9 nicht sortierbar:** kein `data-sortable`; `#view-config` `supportsSortDefault: false`.
- **AC 10–14 column-prefs:** `ColumnDefinitions.FaHierarchyStructure` (Z. 385) + `GetByViewKey`-case
  (Z. 417), `SupportsReorder: true`, `SupportsSortDefault: false`, structure+matchcode `locked`;
  `#view-config`/`#column-config`; `column-preferences.js` VOR `fa-hierarchy-tree.js`; jedes `<th>`
  `data-col-key`. Tests `UserViewPreferencesApiControllerTests.{Get,Put}_FaHierarchyStructureViewKey_IsAccepted`.
- **AC 15 keine Regression:** Controller/TreeBuilder/Filterkarte/Zugriffsschutz unverändert.
- **AC 16 keine neue Lib:** Icons als Inline-SVG (`currentColor`), kein neues Paket.
- **Testszenarien:** TS-62 „IDEAL Teil 2: FA-Struktur Darstellung (Nachtrag)" existiert bereits.

## Fortschritt
- Befund + Verifikation der 16 AK gegen den Code — **erledigt**.
- **qa-agent (2026-08-19): Build + beide Suiten grün** (Web 1219/1 skip, Service 231), alle 16 AK
  einzeln gegen den Code geprüft (inkl. eigenständig nachgerechnetem Kontrast 8,22:1),
  `TS-62` (16 Szenarien) deckt alle AK ab, Deploy-Abschnitt aus dem echten Diff finalisiert
  (Web ja / Service nein / Migration nein). **Status → Testbereit.** QA-Nachweis + manuelle
  Test-Checkliste im Spec-Rumpf. Wartet jetzt auf Schranke 2 (Manual-UAT + Merge des ganzen
  Epic-Bündels durch den Menschen).
