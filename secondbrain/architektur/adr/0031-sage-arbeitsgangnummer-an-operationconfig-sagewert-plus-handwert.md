---
type: adr
id: 0031
title: Sage-Arbeitsgangnummer an OperationConfig – Sage-Wert plus Handwert, Arbeitsgang ist kein Arbeitsplatz
status: accepted
date: 2026-11-17
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec]] (Changelog-Label v1.99.27, ausgeliefert mit v1.99.32; Freigabe-Antworten 1–7, Variante A). Commit `5960b6a5`.
> Prüfabfragen P-AG1..5: [[2026-11-17-pruefabfragen-gerald]].

## Kontext und Problem

- Ziel ist später die Rückmeldung der Arbeitsgänge in die Sage-FAs ([[2026-10-30-fa-rueckmeldung-an-sage]]). Dafür braucht jedes Kürzel
  seine Sage-Arbeitsgangnummer.
- Die View `vw_IDEAL_AKE_WMS_Arbeitsgaenge` liefert in Spalte 0 die **Arbeitsgangnummer** aus `KHKPpsArbeitsgaenge` (P-AG1), keine
  Arbeitsplätze (P-AG3: 0 Treffer in `KHKPpsArbeitsplaetze`). Die Einstellungs-Beschreibung behauptete das Gegenteil.
- Auf IDEAL TEST hat die Werkbank-Anlage 26 dreistellige Werkbänke mit Arbeitsgangnummern als `SageArbeitsplatznummer` angelegt (P-AG5).

## Entscheidung

- **Variante A:** `OperationConfigs.SageArbeitsgangnummer` (nur der Sync schreibt) + `SageArbeitsgangnummerManuell` (nur `/OperationConfigs`);
  wirksam ist der Handwert, sonst der Sage-Wert. Abweichung wird als Banner und Sync-Warnung gemeldet, nie still überschrieben.
- Nummern werden **nur aus der View-Quelle** gelesen; bei Tabelle `KHKPpsArbeitsplaetze` oder SData ist Spalte 0 der Arbeitsplatz – dann keine Nummer.
- **Prüfliste** (a) Kürzel ohne Sage-AG, (b) Werkbänke ohne Arbeitsplatz, (b2) Werkbank-Nummer gleicht einer Sage-AG, (c) Sage-AG ohne Kürzel
  (Link Aktivitäts-Protokoll). Nur Anzeige, nichts wird geändert.
- Nichts an Sage schreiben; der Rückmeldeweg ist eine eigene Spec.

## Folgen

- Die 26 Werkbänke auf IDEAL TEST bleiben, bis Gerald entscheidet (Aufgabe).
- PROD hat `OperationConfigSync` aus – Nummern bleiben leer, bis er eingeschaltet wird.
