---
type: spec
title: "Tabellen: Spalten umsortieren an sieben Listen einschalten, Raster ohne Zahnrad festhalten"
slug: 2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-2-spec
status: Gemerged
created: 2026-11-23
updated: 2026-11-28
source_backlog: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung]]"
uebersicht: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-uebersicht]]"
depends_on: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-1-spec]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.55 (Dev-Lauf prüft)"
testszenario_vorschlag: "TS-180 (Dev-Lauf prüft)"
affected_code:
  - "IdealAkeWms/Views/{Articles/Index,StorageLocations/Index,Creditors/Index,Debitors/Index,PurchaseOrders/Index,PurchaseOrders/Receive,PurchaseProposals/Index}.cshtml (#view-config supportsReorder)"
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (SupportsReorder derselben sieben ViewConfigs, gleichziehen)
  - IdealAkeWms.Tests/Views/ViewKeyDriftGuardTests.cs:63 (Erwartung Receive) + neuer Gleichlauf-Test C# ↔ #view-config
  - docs/TESTSZENARIEN.md (TS-180), Views/Help/Changelog.cshtml, beide AppVersion.cs
open_questions:
  - "„WE zu Bestellung“: Umsortieren einschalten, obwohl [[2026-11-23-wareneingang-zu-bestellung-bedienung-spec]] es ausgeschlossen hat?"
  - "Die sechs übrigen Listen (Artikel, Lagerplätze, Kreditoren, Debitoren, Offene Bestellungen, Bestellvorschläge) ebenfalls umsortierbar machen?"
  - "Klassifizierung bestätigen: die 16 Tabellen ohne Zahnrad sind Eingabe-, Detail- oder Vorschauraster und bleiben ohne Zahnrad – oder welche davon soll eines bekommen?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "freigegeben"
freigabe_von: "Gerald"
freigabe_am: 2026-11-23
---

# Teil 2: Tabellen – Zahnrad und Umsortieren

Übersicht: [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-uebersicht]] · Backlog-Punkt 2.
Überschneidung: [[2026-11-23-wareneingang-zu-bestellung-bedienung-spec]] (v1.99.52, dieselbe Receive-Tabelle).

## Ziel / Nutzen (das Warum)

Das Zahnrad soll sich überall gleich verhalten. Wer in einer Liste Spalten ziehen kann und in der nächsten nicht, hält das für einen
Fehler – so kam die Meldung zu „WE zu Bestellung“.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Umsortieren (Ziehen im Zahnrad) für sieben registrierte Listen einschalten; C#-Katalog und View gleichziehen; Gleichlauf-Test.
Festhalten, welche Tabellen ohne Zahnrad bleiben.

**Out-of-Scope:** OSEON-Teileverfolgung (Baum, bleibt aus), Standardsortierung (`supportsSortDefault`), neue Zahnräder an Rastern,
Änderungen an `column-preferences.js`.

## Fachliche Anforderungen

**Belegt – wo der Schalter wirklich sitzt:**
- Wirksam ist allein das JSON `#view-config` der View: `column-preferences.js:134` (Reihenfolge anwenden) und `:493` (Ziehgriff im
  Zahnrad) lesen `_viewConfig.supportsReorder`.
- `ColumnDefinitions.SupportsReorder` (C#) liest kein Produktivcode: Grep `\.SupportsReorder` über `*.cs`/`*.cshtml` findet nur
  `ViewKeyDriftGuardTests.cs:63`; `GetByViewKey` dient nur der viewKey-Prüfung der Prefs-API (`UserViewPreferencesApiController.cs:29,45,67`)
  und dem Anzeigenamen (`UsersController.cs:389`). Schon festgestellt in [[2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec]] Z. 715.
  Der Backlog-Verweis `ColumnDefinitions.cs:804-806` ist also der Katalog, die Wirkung steht in `Receive.cshtml:269`.
- Acht Listen haben `false` (View + Katalog gleich): OseonTracking (`OseonIndex.cshtml:198`, `ColumnDefinitions.cs:213`), Articles
  (`:142` / `:679`), StorageLocations (`:200` / `:702`), Creditors (`:181` / `:727`), Debitors (`:127` / `:752`), PurchaseOrders (`:183` /
  `:777`), PurchaseOrderReceive (`:269` / `:806`), PurchaseProposals (`:254` / `:828`).

**Belegt – Begründungen:**
| Liste | Grund für `false` | Beleg |
|---|---|---|
| OseonTracking | Baum mit drei Ebenen, eigene Sortierung | `ColumnDefinitions.cs:208-212`, [[2026-09-10-fa-liste-ausbau-matchcode-umsetzung]] Z. 49 |
| PurchaseOrderReceive | „Kein Umsortieren (Formular-Zellen)“ | `ColumnDefinitions.cs:802`, Receive-Spec Z. 158 „Umsortieren würde Zellen tauschen“ |
| Articles, StorageLocations, Creditors, Debitors, PurchaseOrders, PurchaseProposals | **kein Grund dokumentiert** | Kommentare `ColumnDefinitions.cs:672-828` nennen keinen; Grep `SupportsReorder|umsortier` über `secondbrain/` ohne Treffer zu diesen Listen |

**Belegt – Umsortieren ist technisch unkritisch:**
- `reorderDomColumns` (`column-preferences.js:220-247`) hängt `<th>`/`<td>` je Zeile per `appendChild` in neuer Reihenfolge um. Eingabefelder
  wandern mit; Namen und Werte bleiben. Zeilen mit einer `colspan`-Zelle (Leerzustand) bleiben stehen (eine Zelle, Index 0).
- Vorbild mit Formular-Zellen und Umsortieren: `WarehousePicking/Details.cshtml:98` (`quantitiesPicked`) mit `supportsReorder: true` (`:230`).
  Der Server ordnet die Receive-Eingaben über `BelPosId` zu, nicht über die Spalte (Receive-Spec Z. 151-153).
- Kein Index-Zugriff auf Zellen in den sieben Views (Grep `cells\[|children\[\d|nth-child|cellIndex|\.eq\(\d`: kein Treffer).
- Die Fokus-Markierung der Receive-Tabelle (`site.css` `.we-positionen`, Balken `inset 4px 0 0`) hängt an der Zeile; nach Umsortieren sitzt
  der Balken an der dann ersten Zelle. QA prüft das optisch.

**Belegt – Tabellen ohne Zahnrad (Backlog-Liste, je `<table>` gelesen):**

| View | Art | Begründung |
|---|---|---|
| `Articles/Info` (`:151, 189, 236`) | Detail | Bedarfe/FA-Bezug eines Artikels in Karten |
| `BdeBookings/Edit` (`:73`) | Eingabe | Mengen einer Buchung |
| `BdeTerminal/Index` (`:158`) | Dialog | Modal-Tabelle am Terminal |
| `Beschichtungsauftraege/Reklamation` (`:19`) | Eingabe | Teileauswahl eines Auftrags |
| `FaCompletion/Edit` (`:258`) | Eingabe | Spec-Zeilen inline |
| `FaHierarchy/_FaWorkOperationsModal` (`:20`) | Dialog | Arbeitsgänge eines FA |
| `OrderRecipients/Edit` (`:42`) | Eingabe | Empfänger einer Regel |
| `ProductionWorkplaces/Edit` (`:165`) | Eingabe | Zuordnungen einer Werkbank |
| `SageSdataFa/Index` (`:105, 192`) | Diagnose | Testseite, Aufruf-Diagnose |
| `Shared/_ArtikelSageKarten` (`:124`) | Detail | Sage-Karte eines Artikels |
| `Shared/_BeschichtungFreiePositionen` (`:5`) | Eingabe | freie Positionen (per JS) |
| `StockMovements/Inbound` (`:89`) | Detail | offene Bedarfe des gewählten Artikels |
| `StockMovements/InboundBulk` (`:58`) | Eingabe | Erfassungszeilen |
| `StockMovements/LocationTransfer` (`:51`) | Vorschau | Bestand eines Platzes vor dem Umbuchen |
| `StockMovements/OutboundAll` (`:62`) | Vorschau | Bestand eines Platzes vor dem Ausbuchen |
| `WarehouseRequisitions/Edit` (`:35`) | Eingabe | Positionen einer Lagerbestellung |

Alle sind auf einen Datensatz vorgefiltert und unpaginiert; ADR [[0005-listen-view-pattern-mit-server-side-spaltenfilter]] verlangt das Muster
für Listen-Views. Empfehlung: bleiben ohne Zahnrad (Rückfrage 3).

**Anforderung (Empfehlung, Rückfragen 1 und 2):**
1. Die sieben Listen Articles, StorageLocations, Creditors, Debitors, PurchaseOrders, PurchaseOrderReceive, PurchaseProposals bekommen
   `"supportsReorder": true` im `#view-config` und `SupportsReorder: true` im Katalog. `locked` bleibt unverändert (sperrt das Ausblenden);
   ob gesperrte Spalten ziehbar sind, folgt dem heutigen Verhalten von `column-preferences.js` an den übrigen Listen – keine Sonderregel.
2. OseonTracking bleibt aus.
3. Ein Test sichert, dass `#view-config.supportsReorder` jeder View mit dem Katalog übereinstimmt – damit C# nicht wieder lügt.

## Technischer Loesungsentwurf

Leiter Sprosse 2: vorhandenes `column-preferences.js`, nur Schalter. Je View eine Zeile JSON, je ViewConfig ein `false` → `true`.
`ViewKeyDriftGuardTests.ViewKey_Registriert` (`:27-37`) um den Vergleich `supportsReorder` erweitern (Regex auf `#view-config`, Vergleich mit
`GetByViewKey(key).SupportsReorder`); `:63` erwartet für Receive dann `(true, false)`. Kommentar `ColumnDefinitions.cs:802` („Kein Umsortieren“)
anpassen.

## Migrations-/SQL-Auswirkungen

Keine. Gespeicherte Benutzer-Einstellungen bleiben gültig: `order` je Spalte wird schon heute gespeichert und erst mit `supportsReorder`
angewendet (`column-preferences.js:113-121, 134`).

## Einstellungen

keine

## Audit-Feld-Auswirkungen

Keine.

## Akzeptanzkriterien

1. In jeder der sieben Listen zeigt das Zahnrad Ziehgriffe; eine gezogene Spalte steht danach an der neuen Stelle, auch nach Neuladen.
2. „WE zu Bestellung“: Spalte „Ist-Menge“ vor „Artikel“ gezogen, Menge und Platz eingeben, Buchen → gebucht wird je Position das
   Eingegebene (Kontrolle: Buchungen bzw. Sage-WE-Beleg je Position, wie in TS-177), keine Verwechslung.
3. Receive: Fokus-Markierung und Fehlerzeile (`table-danger`) bleiben nach Umsortieren sichtbar.
4. Spaltenfilter, CSV-Export und Druck (wo vorhanden) arbeiten nach dem Umsortieren mit den richtigen Spalten.
5. OSEON-Teileverfolgung zeigt weiter keine Ziehgriffe.
6. Test: weicht `#view-config.supportsReorder` einer View vom Katalog ab, schlägt er fehl.
7. Die 16 Raster haben weiter kein Zahnrad.

## Test-Szenarien

Neues Kapitel **TS-180 „Tabellen: Umsortieren an Stammdaten- und Einkaufslisten (v1.99.55)“**, Verweis aus TS-177 (Receive).
- 180.1 Je Liste ziehen + Neuladen (AK 1) · 180.2 Receive buchen nach Umsortieren, DB-Kontrolle (AK 2, 3) · 180.3 Filter/CSV/Druck (AK 4)
- 180.4 Negativ OSEON (AK 5) · 180.5 Automatisch: Gleichlauf-Test (AK 6)
- DEV AKE (Einkauf/Wareneingang nur dort aktiv), DEV IDEAL für Artikel/Lagerplätze/Kreditoren/Debitoren.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Vorläufig, QA bestätigt am echten Diff.*
- **Web-App:** ja
- **Service:** nein
- **Migration:** nein
- **Manifest-Einträge:** keine
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. „WE zu Bestellung“: Umsortieren einschalten, obwohl [[2026-11-23-wareneingang-zu-bestellung-bedienung-spec]] es ausgeschlossen hat
   (Out-of-Scope Z. 69-70)? Empfehlung: ja – der dort genannte Grund („Zellen tauschen“) ist technisch kein Hindernis (siehe Beleg), und
   genau diese Tabelle wurde gemeldet.
2. Die sechs übrigen Listen (Artikel, Lagerplätze, Kreditoren, Debitoren, Offene Bestellungen, Bestellvorschläge) ebenfalls umsortierbar
   machen? Empfehlung: ja, ein Grund für `false` ist nirgends dokumentiert.
3. Klassifizierung bestätigen: die 16 Tabellen ohne Zahnrad sind Eingabe-, Detail- oder Vorschauraster und bleiben ohne Zahnrad – oder
   welche davon soll eines bekommen?

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → ja (Empfehlung): Umsortieren auch in „WE zu Bestellung“ einschalten.
2. → ja (Empfehlung): die sechs übrigen Listen ebenfalls umsortierbar.
3. → bestätigt: die 16 Tabellen bleiben ohne Zahnrad.

## QA-Nachweis 2026-11-23 (qa-agent)

**Gemeinsamer Nachweis (QA-Lauf 2026-11-23 abends, qa-agent, Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Diff `1cfb92d7..b79a4b1a`):**
- **Build:** `dotnet build IdealAkeWms.slnx` 0 Fehler (30 Warnungen, vorbestehend).
- **Tests:** `IdealAkeWms.Tests` 3480 bestanden / 1 übersprungen / 0 Fehler; `IDEALAKEWMSService.Tests` 790 / 0 / 0. Nach dem QA-Fix `b79a4b1a` Web erneut 3480/1/0.
- **DEV-Deploy** (`D:\Dev\scripts\dev-deploy.ps1`, Quelle Worktree, Web + Dienst, erhöhte Shell): AKE und IDEAL je zweimal (vor und nach QA-Fix), Exit 0,
  „HTTP 200, Fusszeile v1.99.58“, „Fertig: AKE v1.99.58 auf http://localhost:8081/“ bzw. „Fertig: IDEAL v1.99.58 auf http://localhost:8082/“. Footer = AppVersion 1.99.58.
- **Publish:** `<worktree>\publish\IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` (Ordner vorher geleert, beide Exit 0), `SQL\` → `publish\SQL`, `scripts\deploy\` → `publish\deploy`,
  `publish\publish-info.txt` Version 1.99.58, Commit `b79a4b1a0fef42d220265ad5d81983816dce50aa`. Paket-ZIP/Shares: Sache des Orchestrators (DoD).
- **QA-Fix `b79a4b1a`** (`qa(...teil-5)`): Leerzeilen-`colspan` der Stückliste zählt die neue Spalte „Menge IST“ mit (`Picking/Bom.cshtml`, `emptyColspan` + 1 bei `Model.Auftrag != null`).
- **Geänderte bestehende Tests:** `WarehousePickingVerbrauchsbuchungTests` (Zeile 227: `vm.EntnahmePlaetze` → `vm.Items[0].EntnahmeOptionen`, Typ-/Ortswechsel durch Nachbesserung `b7e2d5a4`, Aussage gleich);
  `ViewKeyDriftGuardTests.ViewKey_Registriert` (verschärft: `supportsReorder` Gleichlauf, Teil 2 AK 6); `ViewKeyDriftGuardTests.WareneingangPositionen_*` `(false,false)`→`(true,false)` (Verhalten absichtlich geändert, Teil 2 Antwort 1). Kein „Sonstiges“.
- **Checkliste CLAUDE.md:** keine Migration/kein SQL/kein FreshInstall/keine Einstellung (ADR 0020 entfällt), keine Audit-Änderung; Version 1.99.54–1.99.58 in beiden `AppVersion.cs` (Dienst nur Nummer), Anwender-Changelog je Version vorhanden;
  TS-178.12, TS-179–183 in `docs/TESTSZENARIEN.md` und im Testindex. **Offen beim Orchestrator:** Brain-Changelog `secondbrain/changelog/` und `feature-map.md` für 1.99.54–1.99.58 fehlen noch; Doku-Nachzug `docs/anwender`/`docs/betrieb` (DoD). Kein Manifest-Eintrag nötig (kein Deploy-Schritt).

**Code-Review (qa-agent, selbst, Diff `1cfb92d7..HEAD`, 24 Dateien):** Befunde gesamt: (B1) `Bom.cshtml` Leerzeilen-`colspan` ohne neue Spalte → behoben `b79a4b1a`.
(H1, Hinweis, nicht behoben) `site.js` Teil 1: Bekommt ein Zahlenfeld `pointerdown`, aber nie den Fokus (z. B. `disabled`), bleibt `step="any"` bis zum nächsten Fokus/Blur – nur Browser-Schrittprüfung betroffen, Server prüft ohnehin; kein Handlungsbedarf.
(H2, Hinweis) Nachbesserung `EntnahmeOptionen`: Rückfall „volle Liste“ greift nur, wenn weder Bestand noch NAN wählbar – dann bleibt die frühere Laufzeit (seltener Fall, bewusst).
Geprüfte Fehlerklassen: kein `@Html.Raw` in `<script>` (neue `#column-config`-Zeile ist statisches JSON in `<text>`); kein `hidden` mit `d-*`; keine neuen stillen Fallbacks (`r.Auftrag?.RowVersion ?? rv` führt höchstens zu „veraltet“-Warnung, nicht still);
Parallel-Arrays: Receive nutzt indizierte Namen `zeilen[i].*`, Umsortieren der Spalten ändert keine Bindung; Spaltenzugriffe in `Bom.cshtml`-JS über `bomColKeyToIndex` (dynamisch), nicht über feste Indizes; `column-preferences.js` ignoriert gespeicherte Schlüssel ohne DOM-Spalte (Stückliste ohne Auftrag);
Serverregel Statuswechsel bleibt in `BeschichtungsauftragWorkflow.StatusWechselnAsync` (eine Stelle), Ist-Menge-Prüfung weiter nur serverseitig in `PickingTransferService`.

**Deploy (QA-bestätigt am echten Diff):** Web ja (`IdealAkeWms/` geändert), Dienst: nur Versionsnummer (`IDEALAKEWMSService/AppVersion.cs`) – fachlich unverändert, mitliefern für gleiche Version; Migration nein; Manifest nein.
Paket: `<worktree>\publish` v1.99.58 (Web + Dienst). Ablauf Mensch: Paket auf Testsystem → testen → Merge. Nach dem Merge nur dann aus `main` neu publizieren, wenn der Merge geprüfte Dateien mit parallelen main-Änderungen zusammengeführt hat:
`dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`.

**Beweisart je AK:** AK 6 automatisiert (`ViewKeyDriftGuardTests.ViewKey_Registriert`, grün). AK 1–5, 7 Manual-UAT (Browser-JS, Bindung im echten Formular).

**DEV-Testlauf TS-180:**
| Nr | gemacht | Ergebnis | Urteil |
|---|---|---|---|
| 180.1 | DEV IDEAL `/Articles`: Zahnrad, „Matchcode“ per Ziehgriff vor „Bezeichnung“, Seite neu geladen, danach „Auf Standard zurücksetzen“ | Ziehgriffe sichtbar; Kopf und Zellen wandern gemeinsam; nach Neuladen bleibt die Reihenfolge | OK (Artikel); übrige 6 Listen nicht einzeln |
| 180.2 | WE zu Bestellung umsortiert buchen | nicht ausgeführt (Sage-Bestellung/WE-Daten auf DEV nötig) | NICHT MOEGLICH (Mensch) |
| 180.3 | Filter/CSV nach Umsortieren | nicht ausgeführt (CSV = Datei-Download) | NICHT MOEGLICH (Mensch) |
| 180.4 | OSEON/Raster negativ | nicht ausgeführt | offen (Mensch) |

**Manuelle Checkliste (offen):**
1. TS-180.2 Wareneingang zu Bestellung: „Ist-Menge“ vor „Artikel“ ziehen, je Zeile andere Menge/Platz, buchen, Bewegungen je Position prüfen (wichtigster Punkt: Bindung).
2. TS-180.1 für Lagerplätze, Kreditoren, Debitoren, Offene Bestellungen, Bestellvorschläge.
3. TS-180.3 Filter + CSV nach Umsortieren; TS-180.4 OSEON ohne Ziehgriffe, Raster ohne Zahnrad.
4. Durchlauf am Testsystem.
