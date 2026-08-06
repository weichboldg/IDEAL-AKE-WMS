---
type: spec
title: "IDEAL-Standort Teil 5 — Vormontage-Listen"
slug: 2026-07-29-standort-ideal-teil-5-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/FaHierarchyVormontageController.cs (neu, Name provisorisch)
  - IdealAkeWms/Services/VormontageService.cs (neu)
  - IdealAkeWms/Views/FaHierarchyVormontage/Index.cshtml (neu)
  - IdealAkeWms/Views/FaHierarchyVormontage/Summiert.cshtml (neu)
  - IdealAkeWms/wwwroot/js/ideal-vormontage-export.js (neu, Isolierfraesen-Export)
  - IdealAkeWms/Models/AppSettingKeys.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Isolierfraesen-Software-Import-Format: genaue Spalten-/Trennzeichen-Spezifikation fehlt noch (Anhang referenziert nur 'kompatibel zum Import', kein Format-Dokument)"
  - "Referenz-Screenshot Konzept_Uebertrag_MDE_System.docx (Vormontage-Ansicht mit Reitern pro Arbeitsbereich) — Datei nicht Teil dieser Spec-Runde, vor Feinspezifikation zu beschaffen"
  - "Rollen/Zugriff: bestehende Rolle vorbau wiederverwenden oder neue IDEAL-Rolle?"
  - "Wochenbezug ('kommende Woche') von VMBedarf-Terminen: welches Datumsfeld ist massgeblich (FE_Termin, Neuer_PT_PPS, Verladetermin_Vsl)?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Jeder Vormontage-Arbeitsbereich (Feld `VMBedarf`) bekommt eine Liste seiner in der kommenden
Woche vorzubereitenden Teile — analog zur bestehenden AKE-Vorbau-Abarbeitungsliste, aber auf
IDEAL-Struktur-Basis (Teil 1) und mit eigener Aggregations-/Export-Logik.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** drei Sichten — (1) Einzelne Teile (flache Liste), (2) Summierte Liste (aggregiert
nach Artikel), (3) Export in Zwischenablage im Isolierfraesen-Import-Format.

**Out-of-Scope:** keine Rueckmeldefunktion (Teil 8); `ProductionOrders`/AKE unveraendert; keine
Integration in die bestehende `FaWorklist` (separate Domain laut B5).

## Fachliche Anforderungen

- Filter: `VMBedarf` gefuellt.
- FAListe-Spalten: `HauptFA`, `HauptArtnr`, `Artnr`, `Matchcode`, `Sollmenge`, `Fertigungmenge`,
  `BemerkungPN`, `VMBedarf`.
- FAInfos-Spalten: `FE_Termin`, `MontageAbteilung`, `Prio`, `Neuer_PT_PPS`, `Verladetermin_Vsl`.
- Gruppierung/Reiter je Arbeitsbereich (`VMBedarf`-Wert), analog zur Referenz-Excel-Ansicht.

## Technischer Loesungsentwurf

`VormontageService` liest ueber die Teil-1-Repositories, baut die drei Sichten. Export-Sicht
generiert Zwischenablage-Text im (noch zu klaerenden) Isolierfraesen-Format ueber JS
(`navigator.clipboard`), analog zu bestehenden Clipboard-Mustern im Projekt.

## Migrations-/SQL-Auswirkungen

Keine — reine Lesefunktion.

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten.

## Akzeptanzkriterien

1. Alle drei Sichten (Einzelteile, Summiert, Export) zeigen ausschliesslich Positionen mit
   gesetztem `VMBedarf`.
2. Summierte Sicht aggregiert korrekt nach Artikel (keine Doppelzaehlung ueber mehrere Strukturen
   hinweg).
3. Export liefert eine Zwischenablage-Ausgabe, die sich unveraendert in die
   Isolierfraesen-Software importieren laesst (sobald Format-Spezifikation vorliegt).
4. AKE-Verhalten unveraendert.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 5 — Vormontage-Listen": Testfall mit mehreren Strukturen und
uebereinstimmenden Artikeln in der Summierten Sicht; Export-Zwischenablage gegen die
Isolierfraesen-Software abnehmen (sobald Format vorliegt); Reiter-Wechsel zwischen
Arbeitsbereichen.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch).

## Offene Rueckfragen

1. Isolierfraesen-Import-Format (Spalten/Trennzeichen) fehlt.
2. Referenz-Screenshot `Konzept_Uebertrag_MDE_System.docx` nicht Teil dieser Spec-Runde — vor
   Feinspezifikation zu beschaffen.
3. Rollen/Zugriff fuer diese Listen.
4. Massgebliches Datumsfeld fuer „kommende Woche".

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →
3. →
4. →
