---
typ: feature
created: 2026-11-11
quelle: "Gerald 2026-11-11, Freigabe-Antwort 6 zur Spec Wareneingang Teil 2 (Rahmenabrufe herausgenommen)"
herkunft_spec: "[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]]"
vorgaenger_backlog: "[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe]]"
---
# Wareneingang Teil 3: Rahmenabrufe (EBZ) lesen, anzeigen, buchen

**Aufgenommen 2026-11-11** (Gerald, Freigabe-Antwort 6 zu [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]]): Rahmenabrufe
kommen aus Teil 2 heraus und werden ein eigener nächster Schritt. Diese Notiz enthält alles, was Teil 2 dazu erarbeitet hatte (Stand
2026-11-08, Code-Zeilen im Bündel-Worktree `2026-08-07-ideal-teile-1-5`). Die Spec prüft die Zeilennummern neu.

## Ursprüngliche Anforderung (Backlog [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe]], Punkt 2)
- Offen je Abruf = Abrufmenge `Menge` − im WMS zu diesem Abruf erfasste Menge. Sage liefert kein „geliefert“ je Abruf; die Vorgangsmengen
  gelten für den ganzen Rahmen (Teil 1, Befund B-1).
- Anzeige in „Offene Bestellungen“ und im Wareneingang, Kennzeichnung „Rahmenabruf“.
- Nicht im Umfang (wie Teil 2): Etikettendruck, Charge/Seriennummer, Rechnungsprüfung.

## Befunde aus Teil 2 (belegt)
- Datenbasis: dieselbe SData-Ressource `EinkaufPositionenInfo` wie Teil 1 (P-W1: 91 Felder, u. a. `Menge`, `MengeBasis`, `VorPosId`,
  `Belegkennzeichen`). EBZ = „Rahmenabruf“: 108 offene Positionen am AKE-Test (Teil 1, P-W4); 3455 EBZ-Belege in `KHKEKBelege` (P-W7,
  2026-11-08).
- Die Sage-View filtert auf **Vorgangsposition** (`VP.GGBestellt > VP.GGGeliefert AND VP.GGErfuellt = 0`, Teil 1 E0 P-W4). Ein Abruf bleibt
  sichtbar, solange die Rahmen-Vorgangsposition offen ist, auch wenn der Abruf selbst voll geliefert ist.
- `PositionGGBestellt`/`PositionGGGeliefert` sind bei EBZ Rahmen-Mengen und gelten für bis zu 18 Abrufe (Teil 1 B-1, Beispiel EK013128).
  Für EBZ nie als bestellt/geliefert je Zeile verwenden.
- Der Abgleich liest heute nur EBB: `IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs:20,32` (`where Belegkennzeichen eq 'EBB'`),
  Belegart-Prüfung `:112`, Mengen `:137-147`.
- Bezug Receipt → Abruf **besteht schon**, keine Migration nötig: `PurchaseOrderReceipt.BelPosId` (`IdealAkeWms/Models/PurchaseOrderReceipt.cs:22`)
  wird beim Buchen aus der Cache-Zeile gesetzt (`PurchaseOrdersController.Receive.cs:101`), Index `IX_PurchaseOrderReceipts_BelPosId`
  (`ApplicationDbContext.cs:1429`), Summe über `GetErfasstJeBelPosAsync` (`PurchaseOrderReceiptRepository.cs:19-25`).
- `Belegkennzeichen` liegt schon im Cache (`SagePurchaseOrderLine.cs:16`) – keine neue Cache-Spalte.
- SData-Filter: nur `eq` ist belegt; `or` im `where` ist am Server nicht belegt, `ne` liefert auf Textfeldern still 0 Zeilen
  ([[fallstricke]] Z. 1720-1724). Deshalb zwei Abfragen (`eq 'EBB'`, `eq 'EBZ'`).

## Entwurf aus Teil 2 (damals F1/E1, nicht freigegeben)
- Zwei `QueryAllAsync`-Aufrufe mit `eq 'EBB'` bzw. `eq 'EBZ'`; Empty-Guard über die Summe (`EinkaufBestellungenSyncService.cs:80-85`);
  Counts-Keys `positionen_ebb`, `positionen_ebz`; Belegart-Prüfung `:112` als Liste `{EBB, EBZ}`.
- EBZ: `MengeBestellt = MengeOffen = Abrufmenge`, `MengeGeliefert = 0`; Anzeige „–“ bei „geliefert (Sage)“ mit Tooltip „Sage liefert für
  Rahmenabrufe keine gelieferte Menge je Abruf“, nie „0“. Modell-Kommentar `SagePurchaseOrderLine.cs:4-9,28-33` je Belegart.
- Rest EBZ = Abrufmenge − Σ **alle** Receipts derselben `BelPosId` (Sage verkleinert offen je Abruf nicht). Die Rest-Regel liegt an einer
  Stelle (`PurchaseOrderReceiveRow.Rest`, `PurchaseOrderReceiveViewModel.cs:20`); Teil 2 erweitert sie dort für EBB (F5) – Teil 3 setzt
  darauf auf.
- Kennzeichnung: Hinweis-Spalte `PurchaseOrdersController.cs:55-62` mit Text „Rahmenabruf“ (filterbar), Text-Badge im Kopf der Maske,
  Farbe nie allein. Einheitensperre `EinheitAbweichend` und Artikel-Zuordnung unverändert (`EinkaufBestellungenSyncService.cs:131-132`).
- Meldung „nur offene normale Bestellungen“ (`PurchaseOrdersController.Receive.cs:21`) → „offene Bestellungen und Rahmenabrufe“.
- Bekannte Grenze: In Sage direkt gebuchte Abruf-Lieferungen zieht das WMS nicht ab; der Abruf bleibt bis zum Ende des Rahmens offen.

## Offene Punkte (aus Teil 2, damals Rückfragen 6-8, nie entschieden)
1. **Abrufmenge `MengeBasis` oder `Menge`?** Backlog und B-1 nennen `Menge` (Bestelleinheit); Entscheidung B-2 bucht in der Basiseinheit.
   Empfehlung damals: `MengeBasis` (buchbare Zeilen haben ohnehin gleiche Einheiten, Sperre `EinkaufBestellungenSyncService.cs:131-132`).
   Prüfschritt **P-W8** (SData, Feldnamen laut P-W1 belegt):
   `EinkaufPositionenInfo?format=json&orderBy=BelPosId&count=200&where=Belegkennzeichen eq 'EBZ'&select=BelPosId,VorPosId,Menge,MengeBasis,Mengeneinheit,MengeBasiseinheit,Lagermengeneinheit`
   → zählen, wie viele Zeilen `Menge ≠ MengeBasis` haben und bei wie vielen davon die Einheiten gleich sind; einen Abruf in Sage öffnen und
   bestätigen, dass `MengeBasis` die Abrufmenge in der Basiseinheit ist.
2. **Abrufe mit Rest 0 im WMS** standardmäßig ausblenden (Filter „auch im WMS vollständig erfasste Abrufe“)? Und ist die Grenze „in Sage
   direkt gebucht wird nicht abgezogen“ akzeptiert? Empfehlung damals: ausblenden + akzeptieren, in der Hilfe nennen.
3. **Sage-Wareneingangsbeleg auch für Abrufe (EBZ → ELL)?** Hängt am Sage-WE aus Teil 2. Empfehlung damals: nur, wenn ein Schreibtest
   einen Abruf erfolgreich zeigt; sonst EBZ-Wareneingänge mit Status `NichtNoetig` (Queue-Verhalten Teil 1). Mit Teil 2 gilt bei
   eingeschaltetem Sage-WE die Platzregel und der Queue-Ausschluss – die Teil-3-Spec muss festlegen, welchen Status ein EBZ-Receipt bekommt.

## Damalige Akzeptanzkriterien und Testszenarien (Vorlage)
- AK: Lauf übernimmt EBB und EBZ, Protokoll `positionen_ebb`/`positionen_ebz`, andere Kennzeichen nicht; EBZ-Zeile zeigt „Rahmenabruf“,
  „–“ bei geliefert, offen = Abrufmenge; 3 auf Abrufmenge 10 gebucht → Rest 7, nach erneutem Abgleich weiter 7; Rest 0 nach Entscheidung 2;
  Scan der Belegnummer eines Abrufs öffnet ihn, kein Treffer zeigt die angepasste Meldung.
- TS-116.10 Rahmenabrufe lesen/anzeigen (Abruf aus EK013128 mit Sage vergleichen; negativ: abweichende Einheit nicht buchbar).
- TS-116.11 Rahmenabruf buchen (Teilmenge, erneut abgleichen, bis 0 buchen, Ausblenden; negativ: Überlieferung ohne Häkchen abgewiesen).
  Die Nummern vergibt die Teil-3-Spec neu (Teil 2 belegt TS-116.10 ff. jetzt selbst).

## Brain-Pflichten (aus Teil 2 übernommen)
- [[fallstricke]]: „EBZ-Vorgangsmengen gelten je Rahmen, nicht je Abruf“ (mit Warum).
- [[glossar]]: „Rahmenabruf“.
