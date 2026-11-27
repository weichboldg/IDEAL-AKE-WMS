---
type: bug
title: "Sage-AG-Anlage: Sage lehnt den JSON-Rumpf ab („Ungültige Elementdefinition“)"
status: offen
severity: mittel
created: 2026-11-20
standort: IDEAL
affected_code:
  - "IDEALAKEWMSService/Services/SageFaArbeitsgangClient.cs:27-43 (Feldkonstanten, TODO P-1..P-4)"
  - "IDEALAKEWMSService/Services/SageFaArbeitsgangClient.cs:57-73 (Serialize)"
  - "IDEALAKEWMSService/Services/SageFaArbeitsgangService.cs:157-177 (Senden, Fehlertext ins Protokoll)"
source_backlog: "[[2026-11-20-bugfix-sage-ag-anlage-json-rumpf]]"
spec: "[[2026-11-20-bugfix-sage-ag-anlage-json-rumpf-spec]]"
---

## Symptom

Schreibtest E1b am 2026-11-20 (Koordination, :88 v1.99.44, Ziel Prod-Sage `IDEAL;1`, Freigabe Gerald 08:18): POST
`$service/FertigungsauftragService` für FA **1065144** / Sage-AG **162** (Kürzel SM) → `HTTP 500 Internal Server Error`,
`Sagede.Shared.SData.Serialization.SDataSerializationException: Ungültige Elementdefinition` in `DataContainerJsonReader.Read()` →
`SDataRequest.GetContentAsDataContainer` → `DPSbsSDataServiceHandler.Execute()`. Sage liest den Rumpf nicht ein. In Sage wurde nichts
angelegt („AG in Sage“ = 0). Die Warteschlangen-Zeile steht auf `Fehler` (Status 5, Versuche 1).
Quelle: [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]], Abschnitt „Befund E1b“.

## Reproduktion

1. :88, `SageFaArbeitsgaengeAnlegenAktiv` an, `Sync:SageFaArbeitsgaengeBearbeiter` = `WMS`, SData-Ziel `IDEAL;1`.
2. *Stammdaten → Arbeitsgänge → Sage*, Haupt-FA 1059463, „Sage-Stand prüfen“, an FA 1065144 „FA … anlegen“ bestätigen.
3. Dienst läuft (`SageBookingWorker`) → POST mit dem Rumpf aus `SageFaArbeitsgangClient.Serialize` → HTTP 500 wie oben.

Nicht erneut ausführen ohne Freigabe Gerald (schreibt in Prod-Sage).

## Root Cause

**Teilweise belegt, Rest ungeprüft.** Der Rumpf (`SageFaArbeitsgangClient.cs:57-73`) ist ein Dictionary, dessen Feldnamen nur aus einem
`$schema`-Auszug abgeleitet und mit `TODO(P-1..P-4)` markiert sind (`:27-43`). Der Lesebefund `$template` (Backlog, 2026-11-20 08:37)
zeigt unter `request` **kein** Feld `Handle`; das WMS sendet `Handle` auf oberster Ebene (`:60`). Der Fehler entsteht beim Einlesen
(`DataContainerJsonReader`), also bei Aufbau/Namen, nicht bei der fachlichen Prüfung. Welches Element Sage genau ablehnt, nennt die
Meldung nicht; die vollständige Feldliste von `FaBelegArbeitsgangPosition` fehlt noch (Anzeige abgeschnitten). Analyse und
Soll-Rumpf: [[2026-11-20-bugfix-sage-ag-anlage-json-rumpf-spec]].

## Fix / Verweis

[[2026-11-20-bugfix-sage-ag-anlage-json-rumpf-spec]] (Entwurf). Schalter `SageFaArbeitsgaengeAnlegenAktiv` bleibt bis zum
Wiederholtest aus.
