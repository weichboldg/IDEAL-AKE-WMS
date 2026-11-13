---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: neu
modul: Einkauf Bestellvorschläge (PurchaseProposalsController, PurchaseProposalCalculator, PurchaseProposalRepository, ArtikelLieferantSyncService.LetzteBestellungAsync; v1.99.20–v1.99.22)
stand: "Bündel v1.99.22, 90bbbbd7, feature/2026-08-07-ideal-teile-1-5"
---
# Review-Befunde Bestellvorschläge (Teil A2, lesend)

Spec: [[2026-11-08-einkauf-bestellvorschlaege-spec]].

## BV-1 · Hinweis · Fallback „Sage-Bestellmenge“ ohne Einheiten-Sperre

- **Fundstelle:** `IdealAkeWms/Controllers/PurchaseProposalsController.cs:217-223`.
- **Befund:** Die Sperre „Einheit Basis ≠ Lager – letzte Bestellmenge nicht verwendet“ greift nur, wenn eine letzte Bestellung vorliegt
  (`quelle != null`). Der Fallback `a.SageDetail.Bestellmenge` (`:218`) wird auch bei Artikeln mit Basis ≠ Lager unverändert als Menge in der
  Lagereinheit verwendet. In welcher Einheit `ArtikelVariantenExtended.Bestellmenge` steht, ist im Code nicht belegt.
- **Prüfabfrage (Mensch, Sage):** Für einen Artikel mit Basismengeneinheit ≠ Lagermengeneinheit prüfen, in welcher Einheit die Sage-„Bestellmenge“
  in der Artikelmaske steht und was SData dafür liefert.
- **Vorschlag:** Steht sie nicht in der Lagereinheit: den Fallback bei Basis ≠ Lager wie die letzte Bestellmenge sperren (Hinweis, keine Menge).

## Bereits bekannt (nicht neu gezählt)

- Q-3 der Spec: Der E3-Schritt `LetzteBestellungAsync` liest in **jedem** SyncWorker-Zyklus alle EBB/EBZ-Positionen der letzten 3 Jahre (DEV 287 s).
  Das ist offen für Gerald. Es bremst „Offene Bestellungen“ und die Lieferverzug-Mail jedes Mal aus.
- Q-5 (Fallback-Text bei Bestellung ohne Artikel-Lieferant-Zeile).

## Geprüft ohne Befund

- Nur lesend, keine POST-Action, kein Sage-Schreibweg (E5-Dispo-Stapel blockiert). Gate `EinkaufAktiv` (Default aus) + Rolle `einkauf`/admin
  kumulativ.
- Rechenregel an EINER Stelle (`PurchaseProposalCalculator.Berechne`) entspricht Spec F2/F3/F5. Rückfrage 11: `(⌊F/L⌋+1)×L`. Mindestbestellmenge
  nur bei gleicher Einheit **und** Faktor 1 (fallstricke §37). Meldebestand > 0 durch den Repository-Filter gesichert, also kein Vorschlag 0.
- Offen bestellt über die EINE Rest-Regel (`ErfasstJeBelPos.Rest`). Positionen mit abweichender Einheit und Bedarfe mit anderer Einheit werden
  gezählt und als Banner gemeldet.
- `LetzteBestellungAsync`: `EinkaufPositionenAllInfo` nur mit `where` (Client-Guard `NurMitWhere`), Datum `@yyyy-MM-dd@`. Bei Lesefehler oder 0 EBB
  bleiben die Werte unverändert, mit Warnung. Die jüngste Bestellung wird clientseitig über alle Seiten bestimmt (fallstricke §37). Audit
  `ArtikelLieferantSync`. Migration 148 mit Guard und FreshInstall. `ArtikelLieferantInfo` ohne `where` ist durch
  `MaxZeilenOhneWhere` abgesichert (Abbruch, kein stilles Kürzen).
- Liste: Filterkarte, Server-Spaltenfilter nach der Berechnung, alle `<th>` mit `data-col-key`, Pagination. Dringlichkeit als Text-Badge. Banner
  für fehlende Sage-Zusatzfelder, Lieferanten, Bestellungen und letzte Bestellmengen.
- Tests: `PurchaseProposalCalculatorTests`, `PurchaseProposalsControllerTests`, `PurchaseProposalsOffeneBestellungenTests`,
  `PurchaseProposalRepositoryTests`, `KontokorrentArtikelLieferantTests`.
- Lagerbestellung/Bedarfsmeldung (soweit im Bündel für diese Module geändert): nur die gemeinsamen Lese-Abfragen
  `GetOffeneLagerbestellungenAsync`/`GetOffeneBedarfsmeldungenAsync` (nur Anzeige, auch im Wareneingang-Hinweis). Ohne Befund, außer dem Link-Recht in
  [[2026-11-14-review-befunde-wareneingang]] WE-5.
- Tippfehler: keine gefunden.
