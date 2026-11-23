---
type: aufgabe
status: offen
created: 2026-11-23
quelle: Gerald, Chat 2026-11-23 (/goal-run Symbole, Favoriten, Wareneingang, Lagerbestellung)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-23: Dashboard-Symbole, Favoriten, Wareneingang, Lagerbestellung (v1.99.50–1.99.53)

Auftrag Gerald 2026-11-23: vier freigegebene Specs im Bündel-Worktree, Basis `0b6fff36` (v1.99.47), Reihenfolge wie unten.
Je Spec eigene Version, Build + alle Tests grün, eigene Commits. Danach Doku (`docs/anwender` + `docs/betrieb`, Playwright-Fotos gegen DEV,
nur ansehen), qa-agent synchron, DEV AKE + IDEAL, **Paket 13** + ZIP auf beide Shares (Größe + SHA-256).
Grenzen: nichts an Sage schreiben, keine Einstellungen auf Prod, kein Merge, kein Push.

Hinweis Versionen: 1.99.48 (Sage-AG-Bugfix) und 1.99.49 (BDE-Liste) sind in Entwürfen reserviert und noch nicht gebaut – dieser Lauf
springt bewusst von 1.99.47 auf 1.99.50. SQL: höchste Datei `163`; 164 (BDE-Liste) und 165 sind reserviert → Lagerbestellung nimmt
wie in der Spec `166`.

## Übersicht

| Spec | Typ | SQL | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-23-bugfix-dashboard-symbole-nachschaerfen-spec]] | Bug (Var. B) | – | 1.99.50 | TS-175 | Testbereit | `af6eadd7` | |
| [[2026-11-23-dashboard-favoriten-reiter-spec]] | Spec | – | 1.99.51 | TS-176 | Testbereit | `4802462c`, `563adecd` | |
| [[2026-11-23-wareneingang-zu-bestellung-bedienung-spec]] | Spec | – | 1.99.52 | TS-177 | Testbereit | `e51e80c9`, `9937d357` | |
| [[2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung-spec]] | Spec | 166 | 1.99.53 | TS-178 | Testbereit | `f7b26ebc`, QA `a93bd910` | |

## Protokoll

- 2026-11-23: Lauf gestartet, Basis `0b6fff36`, Worktree sauber. Vorprüfung: alle vier `Freigegeben`, worktree = Bündel, Freigabe-Antworten
  vollständig (Symbole `freigabe_entscheidung: B`), keine BLOCKER. Status → InUmsetzung.
- 2026-11-23: Spec 1 Symbole `af6eadd7` (frontend-design geladen): 21 neue Kachel-Symbole, Wächter `KachelSymbole_SindEindeutig`, v1.99.50,
  TS-175. Build 0 Fehler, Tests Web 3430 + 1 übersprungen, Service 790. Sichtprüfung aller 46 Symbole (Headless-Render 64/32 px): keine
  Vollflächen. Fotos 18-01/18-02/18-05 stehen aus (Doku-Phase).
- 2026-11-23: Spec 2 Favoriten `4802462c` + `563adecd` (frontend-design geladen): Reiter Favoriten/Alle, Stern je Kachel (stretched-link +
  Formular als Geschwister), `UserViewPreferences` Key `Startseite.Favoriten`, Manifest-Info `v1.99.51-StartseiteFavoriten`. Build 0 Fehler,
  Tests Web 3445 + 1 übersprungen, Service 790.
  Brain-Nachzug offen: fallstricke §60 (Stern in Link-Karte / stretched-link / Fokusring ::after / nur `nav.Favoriten`; Razor-Funktion mit
  TagHelper = async Task, MVC1006), controller.md Home (FavoritUmschalten/FavoritenBereinigen, Antiforgery, Key bewusst nicht in
  ColumnDefinitions), feature-map, Changelog, Testindex TS-176.
- 2026-11-23: Spec 3 Wareneingang `e51e80c9` + `9937d357` (frontend-design geladen): Endungssuche Bestellnummer (Mehrdeutigkeit = Meldung mit
  Liste), table-striped/hover + Fokus-Zeile (`--bs-table-bg-state` + Balken `--bs-primary-text-emphasis`, da `--bs-primary` auf
  table-danger-Aktiv nur ~2,7:1), Zahnrad `ColumnDefinitions.PurchaseOrderReceive` (8 Spalten gesperrt), Hauptlagerplatz-Vorschlag
  (`GetPrimaryStorageLocationIdsAsync`, Sage-Platz-Vorrang, bei Sage-WE nur `IstSageWePlatz`). Build 0 Fehler, Tests Web 3452 + 1, Service 790.
  Abweichungen (in Spec übernehmen): Meldung bei exakt gleicher Belegnummer an zwei Belegen „über Offene Bestellungen wählen“; Balkenfarbe
  text-emphasis; Hinweise nicht-buchbar/ohne Sage-Buchung auch nach POST-Fehler; Fall (a) = SagePlatzId null und StorageLocationId null.
  Brain-Nachzug offen: fallstricke §61 (box-shadow/--bs-table-bg-state, Kontrast), controller.md (PurchaseOrdersController + IArticleRepository,
  ZielVorschlaegeAsync), feature-map, Changelog, Testindex TS-177. Doku: Fotos Kapitel 05 offen.
- 2026-11-23: Spec 4 Lagerbestellung `f7b26ebc` (frontend-design geladen): Migration `20261123124036_AddStockMovementWarehouseRequisitionItem`
  + `SQL/166` + FreshInstall (2 Stellen), Manifest SQL/166 + Einstellung + Info; `QuellLagerplatzVorschlag.Waehle` (aus PickingController
  verschoben), `AusbuchungBestandsregel.PruefeAsync` (auch Warenausgang), Close/PrintAndClose → `AbschliessenAsync` (Status-Guard F4, eine
  Transaktion). Build 0 Fehler, Tests Web 3472 + 1, Service 790. Manual-UAT TS-178.8 (Transaktion/RowVersion nicht InMemory-testbar).
  Abweichungen 1–7 (Prüfreihenfolge, PrintAndClose speichert Eingaben, Hinweistext, Glas-Notiz, Ist int?, data-filter-text in table-filter.js,
  Status-Guard auch bei Schalter aus) → in Spec übernehmen.
  Brain-Nachzug offen: fallstricke §62 (Sage-Platz-Ausbuchung ohne Sage-Übergabe wird vom Abgleich zurückgebucht), controller.md
  (WarehousePickingController Ctor + AbschliessenAsync), services.md (QuellLagerplatzVorschlag, AusbuchungBestandsregel), datenmodell.md
  (StockMovements.WarehouseRequisitionItemId, SQL/166), feature-map, Changelog, Testindex TS-178.
- 2026-11-23: qa-agent: Build 0 Fehler, Tests Web 3473 + 1 übersprungen, Service 790. QA-Fix `a93bd910` (F-QA-1: die Bestandsprüfung zählt die geplanten Mengen
  gleicher Artikel/Platz mit). DEV AKE + IDEAL v1.99.53 (Web + Dienst, SQL/166), TS-175–178 auf DEV soweit möglich. Publish `<worktree>\publish` v1.99.53
  (Commit `a93bd910`). Alle vier Specs **Testbereit**, QA-Nachweis und Checkliste stehen jeweils in der Spec. Offen für den Koordinator: Doku-Fotos (18-01/02/05, Favoriten,
  Kap. 05), Brain-Nachzug (fallstricke §60–62, controller/services/datenmodell, feature-map, Changelog), Paket 13 + Shares.
- 2026-11-23: Koordinator Brain-Nachzug erledigt: fallstricke §60–62, codebase/controller.md + services.md + datenmodell.md, feature-map
  (4 Zeilen Testbereit), Testindex TS-175–178, Changelogs `2026-11-23-v1-99-50…53-*`, Backlogs → erledigt. Doku-Fotos laufen.
- 2026-11-23: Doku `1cfb92d7` (Worktree): Fotos 18-01/18-02/18-05 ersetzt, neu 18-06/18-07 (Favoriten), 05-06/05-07/05-08 (Fokus-Zeile, Zahnrad,
  Hauptlagerplatz-Hinweis); Kapitel 03 ohne Foto (Spalte „Entnahme aus“ nur mit Schalter, DEV bleibt aus → Vermerk „Foto folgt nach Einschalten“).
  Auf DEV nichts gebucht; Test-Favoriten wieder entfernt (ggf. leere Zeile `Startseite.Favoriten` übrig).
  Zusatzfund (nur gemeldet): DEV AKE – 371 von 439 offenen Bestellungen zeigen „Hauptlagerplatz … im WMS nicht buchbar“ (DEV-Plätze nur teilweise
  aktiv); den Vorschlagsfall mit echten Plätzen zeigt erst TS-177 im Manual-UAT gegen das Testsystem.

## Paket 13

- [x] Publish (Commit `a93bd910`, Doku-Commit `1cfb92d7` ändert nur `docs/`), ZIP `paket13-v1.99.53.zip`, 237 027 105 Byte,
  SHA-256 `CBDCBE684DFECE1A5F7C774B2F56311B9A4D3EF8E434D34917B039ED5F4F29E1`
  - `\AKENET01.ake.at\Installsourcen`: Größe + SHA-256 gleich ✔
  - `\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen`: Größe + SHA-256 gleich ✔

## Manual-UAT (aus QA)

- TS-175: Handy-Ansicht, Terminal 32 px. TS-176: Rechte-/Modulentzug mit zweitem Benutzer, Admin-Zurücksetzen, manipulierter POST, Fokusring,
  Screenreader, 390 px. TS-177: Optik Fokus-Zeile, Spalten aus/ein + Breiten, Buchen mit ausgeblendeten Spalten (AK 25), Hardware-Scanner,
  Prüfabfragen P-1–P-3, Hauptlagerplatz-Vorschlag mit echten Plätzen. TS-178: 178.6 Sage-Entnahme, 178.7 manipulierter POST, 178.8 gleichzeitiges
  Abschließen, 178.9 Drucken + Abschließen, 178.10 Glas, `NegativeBuchungErlaubt`, Regression Bestellvorschläge + Rollen.
- DEV-Testdaten bleiben: Zubuchungen „QA TS-178 Testbestand“, AKE Lagerbestellungen #57/#53 abgeschlossen (#53 Pos 1 bestellt 5), IDEAL Testliste #1.

## Confluence (für spätere Übernahme)

- Anwender Kap. 18 Startseite und Menü (Symbole, Favoriten, Fotos 18-01/02/05/06/07), Kap. 05 Wareneingang zu Bestellung (Bestellnummer ohne Jahr,
  Fokus-Zeile, Zahnrad, Hauptlagerplatz; Fotos 05-06/07/08), Kap. 03 Lagerbestellung Abschnitt 5a (Entnahme/Verbrauchsbuchung), Anwender-README
- Betrieb 00-index (Stand, Änderungen 1.99.50–53), 03 Installation/Update (SQL/166), 04 Konfiguration (`LagerbestellungVerbrauchsbuchungAktiv`)
