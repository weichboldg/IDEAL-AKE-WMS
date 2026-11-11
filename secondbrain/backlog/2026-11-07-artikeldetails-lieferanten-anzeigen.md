---
typ: feature
---
# Artikeldetails: Hauptlieferant, letzter Lieferant und Artikel-Lieferanten anzeigen

**Aufgenommen 2026-11-07** (Gerald, über die Koordination): „Ich sehe ihn [den letzten Lieferanten] nämlich nicht angezeigt.“

Ist laut [[2026-11-06-sage-sdata-stammdaten-sync-spec]]:
- `ArticleSageDetails.Hauptlieferant` / `LetzterLieferant` (Sage-Konto, z. B. `K31013`) aus `ArtikelVariantenExtended`, nur über SData befüllt (E3a, ADR 0019).
- `ArticleSuppliers` (alle Lieferanten je Artikel aus `ArtikelLieferantInfo`, FK `CreditorId`, `IstHauptlieferant`) – laut Spec „keine Oberfläche“ (E8c).
- Die Spec legt nicht fest, welche Felder die Artikeldetails zeigen („Anzeige in Artikeldetails“, Etappe 3a). Vom Dev zu belegen: Welche
  `ArticleSageDetails`-Felder zeigt die Seite heute, und wird die Karte bei fehlendem Satelliten ausgeblendet?

Gewünscht:
- In den Artikeldetails (Karte „Sage“ bzw. „Einkauf“): Hauptlieferant und letzter Lieferant mit Konto **und Name** aus `Creditors`, verlinkt auf
  die Kreditor-Detailseite; ohne Treffer nur das Konto.
- Liste der Artikel-Lieferanten aus `ArticleSuppliers` (Lieferant, Bestellnummer, Mindestbestellmenge, EK-Einheit, Hauptlieferant-Kennzeichen).
- Ist der Satellit leer (Quelle View oder noch kein SData-Lauf): Hinweis statt leerer Karte, wodurch die Felder gefüllt werden
  (`Sync:Quelle:Artikel = SData`, `Sync:ArtikelLieferantEnabled`).
- Umgekehrt in den Kreditor-Details: Anzahl/Liste der Artikel dieses Lieferanten (optional, Empfehlung Spec-Agent).

Grenzen: nur Anzeige, keine Pflege; Sage bleibt führend (G6).

## Befund Koordination 2026-11-07 11:45 (AKE-DEV `localhost:8081`, v1.78.0, nur angesehen)
- Daten sind da: Artikel-Sync 11:39–11:41 (Quelle SData) „Sage-Zusatzfelder (ArticleSageDetails): 56736 neu“, Benutzerfeld-Merkmale
  391.669 Werte; Artikel-Lieferanten-Abgleich 11:41: `ArtikelVariantenExtended` 23.509 Zeilen, `hauptlieferant=15424` (Filter-Fix wirkt).
- **Angezeigt wird davon nichts außer den Benutzerfeld-Merkmalen:** Es gibt keine Artikel-Detailseite (`/Articles/Details/2963` liefert keine
  Seite), die Bearbeiten-Seite `/Articles/Edit/2963` (00722001) zeigt nur die Karte „Stammdaten“ mit den Merkmalen „aus Sage“ – keine
  `ArticleSageDetails`-Felder (Hauptlieferant, letzter Lieferant, WBZ, ABC, EAN …), keine Spalten in der Artikelliste.
  Die SData-Spec nannte für E3a „Anzeige in Artikeldetails“ – nicht umgesetzt bzw. nicht auffindbar. Diese Notiz deckt deshalb die
  **gesamte** Anzeige der `ArticleSageDetails` ab (Karte „Sage“ in der Bearbeiten-Seite oder eigene Detailseite, optional Listenspalten),
  nicht nur die Lieferanten.
