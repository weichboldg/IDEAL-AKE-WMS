---
typ: feature
---
# Wareneingang Teil 2: Sage-Wareneingangsbeleg (E5) und Rahmenabrufe (EBZ)

**Aufgenommen 2026-11-08** (Gerald). Folge-Spec zu [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] (Antwort 13: E5 als Folge-Spec;
Entscheidung E0-Blocker B-1: EBZ später).

## Umfang
1. **E5 Sage-Wareneingangsbeleg:** Wareneingang zu Bestellung legt in Sage per `StandardBelegService` (Erfassungsart 4100,
   Belegübernahme Bestellung → Wareneingang, `BelegPositionLagermengen` platzgenau) einen Wareneingangsbeleg an, hinter
   `SageWareneingangBelegAktiv` (Default aus); dann **keine** zusätzliche Sage-Lagerbuchung aus der Queue (Ausschluss in
   `SageBookingEnqueueDecision` + Nacherfassung), Status/Fehler je Receipt (SQL/114 laut Teil 1). Ersetzt das manuelle Schließen der
   Position durch den Einkauf (Freigabe-Antwort 1 Teil 1).
   - Voraussetzungen: **P-W7** Belegkennzeichen Sage-Wareneingang (lesend AKE-Test), **Schreibtest am AKE-Test nur mit ausdrücklicher Freigabe
     Gerald** (Test-Bestellung, Ergebnis + Rückbau dokumentieren).
   - DPS-MDE-Modul existiert, ist bei uns nicht im Einsatz (Gerald 2026-11-08) → eigene Umsetzung.
2. **Rahmenabrufe (EBZ):** offen je Abruf = Abrufmenge `Menge` − im WMS zu diesem Abruf erfasste Menge (Sage liefert kein „geliefert“ je
   Abruf; Vorgangsmengen gelten für den ganzen Rahmen, Befund B-1). Anzeige in „Offene Bestellungen“ und im Wareneingang, Kennzeichnung
   „Rahmenabruf“.
## Nicht im Umfang
Etikettendruck beim Eingang, Chargen-/Seriennummernerfassung, Rechnungsprüfung.
