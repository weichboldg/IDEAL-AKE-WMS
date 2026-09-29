---
type: changelog
version: 1.30.1
date: 2026-09-29
---
# v1.30.1 — AKE-Hotfix: Stuecklisten-Druck 404.15 + Lagerbestellung Ist-Menge bestaetigen

Umsetzung von [[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec]] in einem **eigenen kleinen
Worktree aus `main`** (`feature/2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist`), Umsetzungsnotiz
[[2026-09-29-ake-hotfix-stueckliste-druck-lagerbestellung-ist-umsetzung]]. **Keine Migration.** Patch-Version,
weil v1.31.0 im IDEAL-Buendel bereits vergeben ist.

## Teil 1 — Stuecklisten-Druck

- **Ursache:** `visiblePositions` (alle sichtbaren Positionsnummern) im GET-Query → IIS `maxQueryString`
  (2048) → HTTP 404.15 bei grossen Stuecklisten.
- `PickingController.PrintBom`: `[HttpGet]` (Lesezeichen/Alt-Links, druckt **immer alles**) + neue
  `[HttpPost, ValidateAntiForgeryToken, ActionName("PrintBom")] PrintBomPost` (Positionsliste im
  Formular-Body). `Bom.cshtml`: Formular `target=_blank` synchron im Klick statt `window.open`; alle
  sichtbar = keine Liste.

## Teil 2 — Lagerbestellung: nicht Geliefertes muss bestaetigt werden

- **Befund (am Code):** Der Autosave schrieb leere Ist-Felder als `0` in die DB; der Abschluss setzte vor
  jedem Submit alle leeren Felder auf `0` (auch bei „Nein" im Dialog „Soll = Ist buchen?"). Grund war ein
  Parallel-Array-Workaround fuer `int[]`.
- **Fix:** `Close`/`PrintAndClose` binden `int?[]` (Bindung `""` → `null` am selben Index **belegt** durch
  `NullableIntArrayBindingTests`, echter ASP.NET-`ParameterBinder`); Autosave sendet `""`;
  `CloseAsync` prueft **vor** jeder Aenderung: Ist-Menge (auch 0) **oder** Fehlteil — sonst nichts
  gespeichert, Rueckgabe der offenen Positionen; kein `?? 0m` mehr. Sammel-Dialog, `fillSollAsIst`,
  `normalizeEmptyQuantitiesToZero` und Bestellt-Placeholder entfernt; sichtbare Pflichtmarkierung im
  Client (Bedienungshilfe, massgeblich ist der Server).
- **Einmal-Skript** `SQL/Einmalig/2026-09-28_Hotfix-1.30.1_ResetAutosaveZeroQuantityPicked.sql` —
  neue Konvention [[0015-einmal-datenskripte-ausserhalb-der-nummerierten-sql-reihe]].

## Deploy-Risiken

- **Einmal-Skript: genau einmal je System**, NACH dem Web-Deploy, DB-Backup vorher; Ausfuehrung im
  Deploy-Protokoll der Spec vermerken. AKE jetzt, IDEAL mit dem Buendel-Deploy.
- Verhaltensaenderung fuer das Lager: Abschliessen ohne Ist-Menge/Fehlteil ist nicht mehr moeglich
  (gewollt) — Anwender vorab informieren.

## Commits / Merge

Worktree-Commits `3fc809c2` `f653f8a0` `9a725fd5` `d9705b75` `9becb394` `8cc23829` (Code-Review-Fixes in `8cc23829`; QA-Nachweis in der Spec). Merge-Commit: folgt (Schranke 2). Danach
Vorwaerts-Merge `main` → `feature/2026-08-07-ideal-teile-1-5` (eigener Schritt).
