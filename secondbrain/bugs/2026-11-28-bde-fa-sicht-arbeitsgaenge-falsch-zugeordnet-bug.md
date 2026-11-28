---
type: bug
title: BDE FA-Sicht zeigt Arbeitsgänge, die nicht in den Arbeitsschritten der Zeile stehen
status: offen
severity: mittel
created: 2026-11-28
jira: WMS-10
standort: IDEAL
affected_code:
  - IdealAkeWms/Services/BdeFaAngebot.cs:107-130 (AG-Status = alle WorkOperations der Sub-FA)
  - IdealAkeWms/Services/BdeFaAngebot.cs:116-119 (Strukturzeile = Kopfknoten der Sub-FA)
  - IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:100-147 (DirectChildren-Scope)
  - SQL/Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql
  - scripts/deploy/nachlauf.psd1:338-340
source_backlog: "[[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet]]"
spec: "[[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet-spec]]"
---

## Symptom
BDE-Seite, Reiter FA, Werkbank KA Kanten, Haupt-FA 1069617: Sub-FA 1069721 zeigt im AG-Status VM·ST·EG·KA, die Spalte
Arbeitsschritte nur „VM“; 1069724 zeigt M31·ST·EG·KA bei Arbeitsschritten „M31“. Gleiches Muster bei 1069719, 1069722, 1069725–1069727,
1069729, 1069733. Stimmig nur, wo die Baugruppe selbst KA traegt (1069728, 1069730). In Teile Einzeln/Summiert passt es (Mail
Benjamin Andl-Braun, Punkt 1, Screenshot im Backlog-Anhang).

## Reproduktion
1. IDEAL, BDE-Seite, Werkbank mit Kuerzel KA (BDE-Teileliste an), Werker anmelden, Reiter FA.
2. Zahnrad: Spalte Arbeitsschritte einblenden. Spaltenfilter Haupt-FA = 1069617.
3. Zeile Sub-FA 1069721: AG-Status enthaelt Kuerzel, die in Arbeitsschritte fehlen.

## Root Cause
Zwei Teile, Details mit Fundstellen in der Spec:
1. **Belegt (Code):** Der AG-Status der FA-Sicht listet alle `WorkOperations` der Sub-FA (`BdeFaAngebot.cs:108,124,130`). Diese
   entstehen am Standort IDEAL aus dem DirectChildren-Scope = Kopfzeile der Sub-FA **plus** Arbeitsschritte ihrer direkten Blechteile
   (`WorkOperationStructureDetectionService.cs:113-114`). Die Spalte Arbeitsschritte zeigt nur das Feld der Kopfzeile
   (`BdeFaAngebot.cs:117-119,174`). ST/EG an 1069721 kommen also von Blechteilen der Baugruppe. Die Teile-Sichten leiten die Symbole je
   Zeile aus deren eigenem Feld ab (`FaHierarchyTreeViewModel.cs:291`), deshalb passen sie dort.
2. **Vermutet (Daten):** KA ist eine Teileliste-Werkbank. Seit v1.99.32 legt die Erkennung KA aus Blechteilen nicht mehr an der Vater-Sub-FA an
   (`WorkOperationStructureDetectionService.cs:96-98,125-129`). Die KA-Arbeitsgaenge an 1069721/1069724 sind dann Altbestand, den das
   Einmal-Skript v1.99.32 entfernen sollte. Lief dessen Trockenlauf mit TREFFER 0 (Option noch nicht gesetzt), bietet das Update-Skript es nie
   wieder an (`nachlauf.psd1:20`, `Update-IdealAkeWms.ps1:572-573,824-825`). Prueffragen P-1 bis P-5 in der Spec.

## Fix / Verweis
[[2026-11-28-bde-fa-sicht-arbeitsgaenge-falsch-zugeordnet-spec]]
