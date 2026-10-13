---
type: changelog
version: 1.57.0
date: 2026-10-13
---
# v1.57.0 — BDE-Terminal: offene Aufträge mit Arbeitsgang-Status (FA-Struktur Teil 3)

[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec]] (Antworten 1–9). **Direkt im Bündel**
`feature/2026-08-07-ideal-teile-1-5`, Start `d3746373` (v1.56.0), Commits `4c5d4e66`, `d4f326ef`. **Keine Migration.**
Umsetzung [[2026-10-13-fa-struktur-arbeitsgaenge-symbole-teil-3-umsetzung]].

**Warum:** Der Werker sah am Terminal eine flache Knopfreihe in Auftragsnummern-Reihenfolge; er soll sehen, welcher Auftrag als
nächster fällig ist und welcher Arbeitsgang überfällig ist oder gerade läuft.

## Kernpunkte

- `GetAvailableOperations` (Normalmodus): Arbeitsgänge, die ein **anderer** Werker bucht, bleiben sichtbar als „läuft“ (Antwort 6);
  Antwort angereichert (Auftrag, Termin, Status, Soll, aria-label). NurFA unverändert.
- Termin: IDEAL frühester FE_Termin der Kopfzeilen, sonst/AKE `ProductionDate` (Antworten 7, 9); Soll = Termin + Offset aus den
  Arbeitsgang-Stammdaten (ohne Eintrag 0), Rechnung über `OseonDueDateCalculator`.
- Status-Regel aus Teil 2 an einen Ort gezogen (`ArbeitsgangStatusRegel`), Darstellung (Klasse + SVG) in `FaAgStatusDarstellung`;
  Teil-2-Palette als CSS-Variablen, die Terminal-Knöpfe übernehmen sie.
- TS-90 (inkl. P-T1/P-T2 als Vorab-Schritte); Tests Web 1845 (+14), Service 272.

## Deploy-Risiken

Nur Web. Wirkt erst mit `BdeAktiv = true` (produktiv derzeit aus). `SQL/95` (v1.54.0) muss auf beiden Site-DBs gelaufen sein.
Verhaltensänderung für Werker: fremd gebuchte Arbeitsgänge erscheinen jetzt in der Auswahl (Start prüft wie bisher). Vor der
AKE-Abnahme P-T1 inkl. Kollisionsabfrage.

Merge-Commit: — (Bündel, nicht gemergt)
