---
typ: feature
status: offen
created: 2026-11-25
herkunft: "Gerald 2026-11-25 nach AKE-Prod-Update v1.99.63 (Leitstand-Knopf zeigt 0 angelegt / 43 FA ohne Restmenge)"
standort: beide
---
# Kommissionieraufträge: Nachtrag automatisch, Knopf „Fehlende … erzeugen“ nur für Admins

Bezug: [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]] (v1.88.0, E2), [[2026-11-24-v1-99-64-sdata-group-by-ake-prod]].

## Anlass
Nach dem AKE-Prod-Update legte der Leitstand-Knopf „Fehlende Kommissionieraufträge für alle freigegebenen FAs erzeugen“ einmalig 39 Aufträge /
6 180 Zeilen an (2026-11-24 17:16). Ein weiterer Klick meldet „0 … für 0 FA angelegt; 43 FA ohne offene Restmenge“. Der Knopf bleibt sichtbar
und verwirrt im Alltag.

## Befund (Code, Bündel-Worktree Stand 2026-11-25)
- Neue Freigaben erzeugen ihre Aufträge selbst: Einzelfreigabe `PickingLeitstandController.cs:451`, Sammelfreigabe `:515`; ohne Leitstand beim
  Öffnen der Stückliste `PickingController.cs:243`.
- Nicht automatisch: Stücklisten-Nachträge NACH der Freigabe (neue Position / höhere Menge in Sage). Dafür gibt es heute nur den Knopf
  (Hilfe `Views/Help/Index.cshtml:1742`). `PickingOrderWorkflow.ErzeugeOffeneAsync` ist idempotent und rechnet die Restmengen.
- Den Knopf je Seitenaufruf nur bei Bedarf zu zeigen, kostet je freigegebenem FA eine Stücklisten-Auflösung → verworfen.

## Wunsch (Gerald)
1. Nachträge automatisch: Dienst-Job (z. B. nachts / periodisch) ruft für alle freigegebenen, offenen HauptFAs die bestehende Erzeugung auf;
   Aktivitäts-Protokoll Pflicht (neue Aufträge, übersprungene „Altbestand prüfen“, Fehler).
2. Knopf im Leitstand nur noch für Admins sichtbar (Notfall / Altbestand nach „Altbestand geprüft“).

## Entscheidung (Gerald 2026-11-25)
Beides umsetzen: Dienst-Job läuft **nachts** (Zeitpunkt als Service-Einstellung, Default z. B. 02:00) **und** Knopf nur für Admins.

## Offene Fragen
- Erzeugung liegt heute im Web (`IdealAkeWms/Services`); der Dienst müsste sie mitnutzen oder einen Web-Endpunkt rufen.
- Verhalten bei Mengen-Reduktion nach Freigabe (heute keine Rücknahme offener Zeilen) – mit regeln oder bewusst ausklammern.
- „Admin“ = Rolle admin oder Leitstand-Recht + eigenes Recht?
