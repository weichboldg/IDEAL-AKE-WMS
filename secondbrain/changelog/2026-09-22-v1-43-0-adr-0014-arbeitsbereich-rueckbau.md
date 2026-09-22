---
type: changelog
version: 1.43.0
date: 2026-09-22
---
# v1.43.0 — Rückbau: Werkbank-aus-Arbeitsbereich-Ableitung (ADR 0014 auf falsches Feld)

Umsetzung der freigegebenen Spec [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]] im
**Bündel-**Worktree. Umsetzungsnotiz [[2026-09-22-adr-0014-arbeitsbereich-ist-zielort-umsetzung]].
**Keine Migration.** Reiner **Rückbau** (keine Feature). **Vor** der BDE-Spec umgesetzt.

**Warum:** Der `Arbeitsbereich` (`K-02`/`S-01`; Sage `USER_OSAbteilung`) ist ein **Zielort**, keine
Werkbank. Die im August auf der vorläufigen Annahme „Werkbank = Arbeitsbereich" gebaute Ableitung
(v1.37.0, Testbereit, nie produktiv) hätte, sobald IDEAL-Werkbänke angelegt sind, entweder Dauerrauschen
(„jeder Arbeitsbereich = unbekannte Werkbank") oder stille Fehlzuweisungen (Zielort im Werkbank-Feld)
erzeugt. Billigster Zeitpunkt: jetzt, im ungemergten Zweig.

## Zurückgebaut

- **`FaMaterializationSyncService`**: kompletter Werkbank-Ableitungsblock entfernt — Lookup,
  `ApplyWorkplace` (beide Pfade), `SendUnknownWorkplaceDigestAsync`, Unbekannt-/Mehrdeutig-Meldungen, die
  vier Counts-Keys an allen 3 `FinishSuccessAsync` + Log, Ctor-Param `IUnknownWorkplaceState`,
  Klassenkommentar 2→1 Z1-Ausnahme (nur Coating bleibt).
- **`FaMaterializationPlanner.MaterializationSourceOrder`**: Feld `Arbeitsbereich` entfernt (ungenutzt).
- **Gelöscht:** `Common/IUnknownWorkplaceState.cs`, `FaMaterializationWorkplaceTests.cs`,
  `UnknownWorkplaceStateTests.cs`. `Program.cs`: DI-Registrierung entfernt (`IUnknownWorkStepTokenState`
  der BDE-Spec bleibt).
- **Nicht angetastet:** `FaHierarchyNode.Arbeitsbereich` (Struktur-Cache, Kommissionierlisten-Filter),
  `ProductionOrder.ProductionWorkplaceId` (Spalte bleibt), Coating-Pfad (Regressionsanker), AKE-Pfad
  (Master-Gate), die geteilte „Werkbank"-Spalte in den Listen.

## Gewollte Verhaltensänderung (AK 10)

Die entfernte Z1-Ausnahme überschrieb `ProductionWorkplaceId` bei jedem Lauf. Nach dem Rückbau fasst der
Sync das Feld nicht mehr an → eine **von Hand** per `FaCompletion.SetWorkplace` gesetzte Werkbank
**überlebt** jeden Materialisierungslauf. AKE unberührt (Ableitung lief dort nie).

## Entscheidungen (Freigabe 2026-09-22)

- **Grundsatzfrage:** Werkbank vorerst **nur je Arbeitsgang**, keine automatische Werkbank am Auftrag
  (Nachrüstung mit dann bekanntem Zweck offen). Manueller Weg bleibt.
- **Arbeitsbereich/Zielort-Spalte:** eigener Umfang, nicht diese Spec; **nicht** mit Kommissionierziel
  gleichsetzen (getrennte Struktur-Felder).
- **ADR 0014:** **additiver Nachtrag** (kein Supersede) — Muster gilt weiter, war nur am falschen Feld;
  korrekte Anwendung = Werkbank aus `KHKPpsArbeitsplaetze` (BDE-Spec Baustein a).

## Deploy

- **Web: ja** (nur Versions-Bump/Changelog liegen in `IdealAkeWms`) · **Service: ja** · **Migration: nein**.
- **Merge-Commit:** offen (Schranke 2, Mensch).

## Nachweis

Build 0 Fehler; **Web 1391 grün +1 skip, Service 263 grün** (−14 = gelöschte Workplace-Tests; Coating-/
Planner-/Sync-Tests regressionsgrün). TS-71 angepasst (71.6–71.9 als zurückgebaut markiert, neu
71.14 Rückbau-Verifikation + 71.15 manuelle Werkbank überlebt). Dauerwissen: **ADR-0014-Nachtrag** +
[[fallstricke]] §15.

## Zugehörig

Spec [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]] ·
Umsetzung [[2026-09-22-adr-0014-arbeitsbereich-ist-zielort-umsetzung]] ·
korrigiert v1.37.0 [[2026-08-20-materialisierung-fachliche-felder-spec]] (AK 9/10 dort überholt) ·
ADR [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]] (Nachtrag) ·
Folge [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] · Bündel [[2026-08-07-ideal-teile-1-5]].
