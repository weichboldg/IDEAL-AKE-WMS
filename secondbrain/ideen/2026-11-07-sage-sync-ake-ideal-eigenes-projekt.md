---
typ: feature
---
# Eigenes Projekt: Sage-Abgleich zwischen AKE und IDEAL (z. B. Artikel, Aufträge)

**Aufgenommen 2026-11-07** (Gerald): „In einem eigenen Projekt eventuell Sage-Sync zwischen AKE und IDEAL, z. B. Artikel bzw. Aufträge.“

**Kein WMS-Thema** – bewusst als eigenes Projekt vorgesehen; hier nur festgehalten, weil die SData-Erkenntnisse aus dem WMS dafür
nutzbar sind.

## Worum geht es
Abgleich zwischen den beiden Sage-Mandanten/Installationen: Artikelstamm (z. B. gemeinsame Artikel, Zukaufteile) und Aufträge
(z. B. Bestellung IDEAL bei AKE ↔ Auftrag AKE, Intercompany).

## Was es dafür schon gibt (Koordination)
- Lesen über `CommonWawiServicesResources` (Artikel, Belege) und Schreiben über `CommonWawiServices` (`StandardBelegService`) –
  [[sage-sdata-uebersicht]], [[sage-sdata-wms-schnittstellenkatalog]]. IDEAL hat noch keinen SData-Endpunkt (Lizenz wird bestellt).
- Feldbefunde (`ne`-Filter, `startIndex` 0-basiert, Accept-Header) in [[fallstricke]] „Sage-SData-Lesen“.

## Offene Fragen / Unklarheiten
- Richtung und Führung je Datenart (wer ist Master für welche Artikel?), Nummernkreise, Konflikte.
- Intercompany: Bestellung IDEAL → Auftrag AKE automatisch? Rückmeldung Liefertermin/Lieferschein?
- Eigenes Repository/Dienst oder Teil eines bestehenden Integrationsdienstes? DPS-Lösung am Markt prüfen?

## Reif für den Backlog?
Nicht für das WMS-Backlog – eigenes Projekt aufsetzen.
