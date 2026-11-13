---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: neu
modul: Offene Bestellungen (Cache SagePurchaseOrderLine, EinkaufBestellungenSyncService, PurchaseOrders/Index)
stand: "Bündel v1.99.22, 90bbbbd7, feature/2026-08-07-ideal-teile-1-5"
---
# Review-Befunde Offene Bestellungen (Teil A2, lesend)

Spec: [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] (E1/E2), [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]] (EBZ).

## OB-1 · niedrig · Cache-Spaltenlängen sind nicht belegt und werden im Abgleich nicht geprüft

- **Fundstelle:** `IdealAkeWms/Data/ApplicationDbContext.cs:1400-1410` (`LieferantKto` 20, `LieferantName` 50, `Bezeichnung1/2` 50,
  `Mengeneinheit`/`MengeBasiseinheit`/`Lagermengeneinheit` 5, `SagePlatzKurzbezeichnung` 50, Kommentar „Längen laut Sage-SQL (E0, P-W1)“),
  `IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs:155-165` (ohne Längenprüfung), Vollersatz in einer Transaktion `:186-193`.
- **Befund:** Die Belegnummer stand mit derselben Begründung auf 20 und war falsch; v1.99.22 hat sie auf 45 korrigiert. Für die übrigen Felder gibt
  es keinen Beleg im Code. Ist ein einziger Wert zu lang, scheitert `SaveChanges`, und damit fällt der ganze Abgleich in jedem Zyklus aus
  ([[fallstricke]] „Sage-SData-Lesen“: Sage-Spalten teils länger, Muster `KontokorrentSyncService.AdressFelder`). Die Folge: Der Cache bleibt alt,
  nach 24 h setzt die Lieferverzug-Mail die Kreditor-Mails aus, und „Wareneingang zu Bestellung“ findet neue Bestellungen nicht.
- **Prüfabfrage (Mensch, Sage):** Längen von `A0Empfaenger`, `A0Name1`, `Bezeichnung1`, `Bezeichnung2`, `Mengeneinheit`, `MengeBasiseinheit`,
  `Lagermengeneinheit`, `Kurzbezeichnung` und `Artikelnummer` in der View hinter `EinkaufPositionenInfo` (bzw. `$schema`), gegen die Spalten oben.
- **Vorschlag:** Spalten nach dem Befund anpassen (Migration). Alternativ je Feld wie `AdressFelder` prüfen: zu lang = Zeile übernehmen, Feld leer
  lassen und eine Warnung mit Beispielen ausgeben. Nicht still kürzen.

## Geprüft ohne Befund

- SData nur mit `where` (je Belegart `eq`, kein `ne`/`or`) und `orderBy BelPosId`. Empty-Guard je Belegart. `SyncedAt` wird vor dem Lesen gesetzt.
  Fehlende Mengen, unlesbare Termine und fehlende Artikel/Kreditoren/Plätze werden als Warnung gemeldet. Kein Sage-Schreibweg.
- EBZ: Menge = `MengeBasis`, geliefert 0, Anzeige „–“ (fallstricke §34). Voll erfasste Abrufe werden gezählt ausgeblendet.
- Liste: Gate + `RequireStockReadAccess`, Server-Spaltenfilter (alle `<th>` mit `data-col-key`), Pagination, Filterkarte. Hinweise als Text.
  Überfällige Positionen und solche ohne Termin bleiben immer sichtbar.
- Migration 112 und 149: Guards, FreshInstall an beiden Stellen. `Sync:EinkaufBestellungenEnabled` im Katalog. Tests `EinkaufBestellungenSyncTests`,
  `PurchaseOrdersControllerTests`.
- Tippfehler: keine gefunden.
