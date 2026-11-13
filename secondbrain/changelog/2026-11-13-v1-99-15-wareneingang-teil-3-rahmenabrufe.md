---
type: changelog
version: 1.99.15
date: 2026-11-13
---
# v1.99.15 — Wareneingang Teil 3: Rahmenabrufe (EBZ) lesen, anzeigen, buchen

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `5c3538b3` (E1 Abgleich, Rest-Regel, Konstante), `47c95931` (E1 Views + E3),
`de4a85ac` (Abschluss, leer), QA `e92894c9` (TS-132.2.9 Text). **Testbereit** E0+E1+E3 (QA 2026-11-13). Spec [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]]. Nicht gemergt, nicht gepusht;
/goal-Lauf [[2026-11-13-buendel-v1-99-13-goal-lauf]]. **E2 (Sage-Schreibtest) offen – wartet auf Gerald.**

## Kernpunkte

- **E0 / P-W8** (nur GET, Sage-AKE-Test, 2026-11-13): 108 EBZ-Positionen (je Abruf-Beleg eine), `MengeBasis` nie leer, 0 × `Menge ≠ MengeBasis`
  bei gleicher Einheit; 2 Positionen lfm/Stk mit Menge = MengeBasis (im WMS über `EinheitAbweichend` gesperrt); 16 Rahmen, höchstens 18 Abrufe
  je Rahmen. Kein Blocker.
- **Abgleich** `EinkaufBestellungenSyncService`: zwei Abfragen EBB + EBZ, EBZ-Mengen aus `MengeBasis`, Empty-Guard je Belegart, Counts
  `positionen_ebb`/`positionen_ebz`.
- **Rest-Regel** an einer Stelle (`PurchaseOrderReceiptRepository` `Abzuziehen(l)`/`Rest(l)`): EBZ zieht alle WMS-Wareneingänge ab.
- **Anzeige:** Hinweis „Rahmenabruf“ (filterbar), „geliefert“ = „–“ (auch in den Mails), Rest-0-Abrufe ausgeblendet mit Zähler und Filter,
  Badge im Wareneingang, Grenze in der Hilfe.
- **Sage-WE für Abrufe:** `SageWareneingang.RahmenabrufMitSageWe = false` → `NichtNoetig`, Barcode gesperrt mit Abruf-Text. Abrufe gehen als
  Lagerbuchung, der Einkauf schließt in Sage von Hand – bis E2 erfolgreich war.
- **Lieferverzug:** Abrufe in Einkauf-Sammelliste UND Kreditor-Mail (Freigabe-Antwort 1); vor dem Echtbetrieb klärt die Koordination mit dem
  Keyuser, ob Abruf-Lieferungen direkt in Sage gebucht werden.

## Migration / SQL

Keine. Nächste freie SQL-Nummer bleibt 148.

## Deploy-Risiken

Web + Dienst gemeinsam (Dienst nutzt `ErfasstJeBelPos`/`SageWareneingang` aus dem Web-Projekt). Manifest-Info `v1.99.15-Rahmenabrufe`
(AKE, Nachher, anzeigen). Nach dem Deploy einen Abgleich „Offene Bestellungen (Sage)“ laufen lassen, Einkauf und Lager informieren.
IDEAL: Code ohne Wirkung (Abgleich dort aus).

## E2 – vorbereitet, nicht gesendet

Request (ein Abruf: 2027-168973, BelId 363218, BelPosId 406315) und Vorbedingungen stehen in der Spec, Abschnitt QA/E2. Vorbedingungen u. a.
`Sync:SageWareneingangMaxWiederholungen = 0` (auf DEV derzeit 3), Kürzel DEV, Lagerbuchung aus, Ziel-Platz GL;0;0;0 (einziger Sage-WE-Platz
auf DEV), Erfolg nur mit `ReferenzBelID = 363218`.
