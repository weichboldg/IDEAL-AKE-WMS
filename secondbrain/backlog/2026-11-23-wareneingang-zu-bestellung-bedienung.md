---
type: backlog
typ: feature
created: 2026-11-23
status: erledigt
spec: "[[2026-11-23-wareneingang-zu-bestellung-bedienung-spec]]"
quelle: Gerald 2026-11-23 (Sammelauftrag „bitte folgende features planen“, Punkte 2–4)
modul: Wareneingang zu Bestellung (PurchaseOrders/Receive)
---
# Wareneingang zu Bestellung: Bedienung nachschärfen

Drei kleine Punkte auf derselben Seite (`Views/PurchaseOrders/Receive.cshtml` im Bündel-Worktree
`2026-08-07-ideal-teile-1-5`) – eine Spec, drei Abschnitte.

1. **Bestellnummer ohne Jahr eingeben.** Eingabe `167578` → automatisch `2027-167578` (Feld `belegnummer`,
   Platzhalter „z. B. 2027-168975“). Für die Spec klären/belegen: Welches Jahr – laufendes Kalenderjahr oder
   Sage-Belegjahr (Beispiel nennt 2027, heute ist 2026 → Belegjahr ≠ Kalenderjahr? Prüfabfrage gegen Sage
   formulieren, nicht annehmen). Was passiert bei Treffern in mehreren Jahren / keinem Treffer im Ergänzungsjahr?
   Gilt das auch für Scans und den Filter „Bestellung“ der Liste (`PurchaseOrdersController`, Filter `bestellung`)?
2. **Positionstabelle („das Zahnrad“) – aktive Zeile erkennbar.**
   - Hover über die Zeile und Hervorhebung der Zeile, in die der Benutzer geklickt hat (Fokus in Menge/Lagerplatz),
     damit die „aktivierte“ Zeile erkennbar ist.
   - Zeilen abwechselnd einfärben wie in `Views/PurchaseProposals/Index.cshtml` (`table-striped`).
   - Achtung: Zeilen mit Fehler sind schon `table-danger` – Hervorhebung darf das nicht überdecken; Kontrast WCAG AA,
     Farbe nicht alleiniger Bedeutungsträger. Was „das Zahnrad“ genau meint (Spaltenauswahl im Toolbar-Host?), als
     Rückfrage klären.
3. **Lagerplatz: Hauptlagerplatz vorschlagen.** Spalte „Ziel-Lagerplatz“ ist heute „— wählen —“, wenn kein Sage-Platz
   zur Position passt. Vorschlag = Hauptlagerplatz des Artikels. Klären/belegen: Woher kommt der Hauptlagerplatz
   (vorhandenes Feld `Hauptlagerplatz` in BOM/Kommissionierung, `PickingController` ~Z. 670 „Vorgeschlagener
   Quell-Lagerplatz“ wiederverwenden?), Vorrang Sage-Platz der Bestellposition vs. Hauptlagerplatz, Hinweis wenn
   Hauptlagerplatz im WMS nicht buchbar.
