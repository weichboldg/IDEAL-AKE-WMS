---
type: spec
title: Einkauf – Bestellvorschläge für Nachbestellartikel (Epic)
slug: 2026-11-08-einkauf-bestellvorschlaege-spec
status: InUmsetzung
created: 2026-11-07
updated: 2026-11-13
source_backlog: "[[2026-11-07-einkauf-bestellvorschlaege]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
depends_on: "[[2026-11-08-wareneingang-aus-sage-bestellungen-spec]]"
task: "[[2026-11-13-buendel-v1-99-19-goal-lauf]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
version: "1.99.20 (Vorschlag: 1.99.16–1.99.18 vergibt der goal-Lauf [[2026-11-13-buendel-v1-99-16-goal-lauf]], 1.99.19 schlägt [[2026-11-13-lieferverzug-nachbesserung-keyuser-spec]] vor; Dev-Lauf prüft)"
sql_block: "148–150 (reserviert 2026-11-13; 148 E3 ArticleSuppliers letzte Bestellung, 149 E5 PurchaseProposalTransfers, 150 Reserve; letzte belegte Nummer im Bündel 147, Dev-Lauf prüft)"
testszenario: "TS-136"
standort: beide
affected_code:
  - "IdealAkeWms/Models/RoleKeys.cs (neu: Einkauf = \"einkauf\"; heute keine Einkauf-Rolle, RoleKeys.cs:3-27)"
  - "IdealAkeWms/Services/ICurrentUserService.cs + CurrentUserService.cs (neu: CanAccessEinkaufAsync, Muster CurrentUserService.cs:107-108)"
  - "IdealAkeWms/Filters/RequireEinkaufAccessAttribute.cs (neu, Muster RequireStockReadAccessAttribute.cs:13-18) + RequireEinkaufAktivAttribute.cs (neu, Gate, Muster RequireFaHierarchyKommissionierlistenAktivAttribute.cs:28-34)"
  - "IdealAkeWms/Models/AppSettingKeys.cs + AppSettingDefinitions.cs (4 neue Keys, Gruppe „Einkauf“; MultiChoice-Muster AppSettingDefinitions.cs:62/95)"
  - "IdealAkeWms/Controllers/PurchaseProposalsController.cs (neu) + Views/PurchaseProposals/Index.cshtml (neu) + ColumnDefinitions (viewKey)"
  - "IdealAkeWms/Services/PurchaseProposalCalculator.cs (neu, reine Rechenregel an EINEM Ort)"
  - "IdealAkeWms/Data/Repositories/IPurchaseProposalRepository.cs + PurchaseProposalRepository.cs (neu, Datensammlung; Lieferanten als ArticleSupplierRow, ArticleSupplierRow.cs:7-9)"
  - "IdealAkeWms/Data/Repositories/StockMovementRepository.cs (nur genutzt: GetStockByArticleNumbersAsync :395-459, Regel der Meldebestand-Mail StockCheckService.cs:20-43)"
  - "IdealAkeWms/Data/Repositories/SagePurchaseOrderLineRepository.cs (nur genutzt: GetAllAsync :28, GetStandAsync :39-40) + PurchaseOrderReceiptRepository.cs (nur genutzt: GetErfasstJeBelPosAsync :74, ErfasstJeBelPos.Rest :22-30) – E2"
  - "IdealAkeWms/Models/ArticleSupplier.cs (+ LetzteBestellmenge, LetzteBestellungAm, LetzteBestellungBeleg – E3) + IDEALAKEWMSService/Services/Sdata/ArtikelLieferantSyncService.cs (zweiter Leseschritt letzte Bestellung – E3, Lesepfad nach P-L1)"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml (neues Dropdown „Einkauf“ nach „Bestellungen“ :248-272, Variablen-Block :50-68)"
  - "IdealAkeWms/Views/Users/RoleOverview.cshtml (Rolle einkauf)"
  - "E5 BLOCKIERT: IdealAkeWms/Models/PurchaseProposalTransfer.cs (neu) + Sage-Schreibclient DispositionStapel*Service (neu) – erst nach Schreibtest P5"
  - "Migrationen + SQL/148_AddArticleSupplierLetzteBestellung.sql (E3), SQL/149_AddPurchaseProposalTransfers.sql (E5, blockiert), SQL/00_FreshInstall.sql, scripts/deploy/nachlauf.psd1"
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs, Views/Help/Changelog.cshtml, Views/Help/Index.cshtml, README.md, docs/TESTSZENARIEN.md (TS-136)"
open_questions:
  - "11. Erhöhung bei außerordentlichem Bedarf: Reicht eine letzte Bestellmenge L nicht (Fehlmenge ≥ L), Vorschlag = (⌊Fehlmenge / L⌋ + 1) × L mit Grund „n × letzte Bestellmenge“? (Empfehlung: ja – Alternative „Fehlmenge, aufgerundet auf Mindestbestellmenge“ lässt verfügbar genau auf dem Meldebestand und erzeugt sofort wieder einen Vorschlag)"
  - "12. Zählt ein Rahmenabruf (Sage-Belegart „Rahmenabruf“, EBZ) als letzte Bestellung, oder nur die Belegart „Bestellung“ (EBB)? (Empfehlung: beide, der jüngste Beleg gilt – sonst haben reine Rahmenartikel keine letzte Bestellmenge)"
  - "13. Ohne letzte Bestellung beim gewählten Lieferanten: (a) letzte Bestellung des Artikels bei einem anderen Lieferanten nehmen (mit Hinweis), sonst (b) Fehlmenge, bei Fehlmenge 0 der Meldebestand, Hinweis „keine letzte Bestellung – Menge prüfen“? (Empfehlung: a, dann b)"
epic: true
etappen:
  - "E0 Prüfabfragen lesend (P-L1 SData AKE-Test, P-L2 Sage-SQL AKE-Test, P-L3 WMS-DB, P2, P4) – Ergebnisse eintragen, Abweichung = Blocker"
  - "E1 Grundgerüst: Rolle einkauf, Gate EinkaufAktiv, Menü „Einkauf“, Rechenregel, Liste nur lesen, CSV – keine Migration"
  - "E2 Offene Sage-Bestellungen aus dem vorhandenen Cache (Teil B) mit Rest-Regel – keine Migration"
  - "E3 Letzte Bestellmenge je Artikel und Lieferant – SQL/148; BLOCKER bis P-L1 (SData-Lesepfad) und Rückfragen 11–13"
  - "E4 Aufrunden auf Mindestbestellmenge + Einheiten-Wächter – keine Migration (Einheiten liegen seit v1.85.0 vor); Rundung unverifiziert bis P2"
  - "E5 Übergabe an den Sage-Dispo-Stapel – SQL/149; BLOCKIERT bis Lesetest P4 und Schreibtest P5 (schreibt an Sage)"
  - "E6 Abschluss: TS-136, Hilfe, README, Changelog, Brain"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-13"
---

> [!warning] (überholt 2026-11-13 16:27 – Freigabe erneuert, siehe Freigabe-Nachtrag 2026-11-13) Zurückgestellt (Gerald 2026-11-07 19:19): „brauchen wir noch nicht“ – NICHT umsetzen. Die Freigabe-Antworten bleiben stehen; bei Wiederaufnahme nur Freigabe neu setzen.

> [!note] Überarbeitung 2026-11-13 (Rework nach den Keyuser-Antworten)
> Eingearbeitet: F3 = letzte Bestellmenge (Einstellung `EinkaufVorschlagsverfahren` entfällt), VPE/P3 und Kennzeichen (alte F6/E3,
> `ArticlePurchaseSettings`, alte SQL/128) entfallen, `standort: beide`, Teil B wird wiederverwendet (Cache `SagePurchaseOrderLine`),
> Einheiten-Spalten liegen seit v1.85.0 vor (alte SQL/129 und P1 entfallen), Nummern gegen den Bündel-Stand (HEAD `cb204c96`) neu vergeben.
> Überholte Aussagen des alten Rumpfs sind als *ueberholt* markiert; die Abschnitte Prüfabfragen, Freigabe-Nachtrag, Freigabe-Antworten und
> Keyuser-Antworten sind unverändert.

# Einkauf – Bestellvorschläge für Nachbestellartikel (Epic)

Bündel [[2026-11-08-buendel-uebersicht]], Teil C. Teil B ([[2026-11-08-wareneingang-aus-sage-bestellungen-spec]]) ist umgesetzt
(v1.84.0–v1.99.15: Cache `SagePurchaseOrderLine`, Dienst `EinkaufBestellungenSyncService`, Rahmenabrufe seit v1.99.15); die Lieferantenanzeige
aus [[2026-11-08-artikeldetails-lieferanten-anzeigen-spec]] (Teil A) liefert das Zeilenformat `ArticleSupplierRow`. Datenbasis:
[[2026-11-06-sage-sdata-stammdaten-sync-spec]]. Keyuser-Antworten: [[2026-11-12-keyuser-fragen-einkauf]].

## Ziel / Nutzen (das Warum)

Sage rechnet bei AKE keine Bestellvorschläge; die „Erweiterte Disposition“ ist nicht lizenziert, die Sage-Disposition wird kaum genutzt
(Entscheidung Gerald 2026-11-07 16:26). Der Einkauf erkennt Nachbestellbedarf heute nur über die Meldebestand-Mail und die Farbcodierung der
Bestandsübersicht. Das Epic gibt dem Einkauf **beider Standorte** (sieben Personen laut Keyuser-Antworten) eine Arbeitsliste „Einkauf →
Bestellvorschläge“: je Artikel mit Sage-Meldebestand eine Projektion aus WMS-Bestand, offenen Sage-Bestellungen und offenen WMS-Bedarfen und
als Vorschlag die **letzte Bestellmenge** beim Lieferanten, aufgerundet auf dessen Mindestbestellmenge. Die Übergabe in den Sage-Dispositionsstapel
bleibt als letzte fachliche Etappe im Plan, aber blockiert bis zu ihrem Schreibtest. **Die Vorschlagslogik liegt im WMS, Sage bekommt nur das
Ergebnis** (verbindlich).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neuer Hauptmenüpunkt **„Einkauf“**, erster Unterpunkt **„Bestellvorschläge“** (verbindlich).
- Rechenregel (eine Klasse) und Liste nach Listen-View-Pattern, CSV über den vorhandenen CSV-Knopf.
- Offene Sage-Bestellungen aus dem vorhandenen Cache als Zugang (E2), offene Lagerbestellungen/Bedarfsmeldungen als Abgang (Einstellung).
- Letzte Bestellmenge je Artikel und Lieferant aus Sage (E3, Lesepfad SData – Blocker bis P-L1).
- Einheiten-Wächter und Aufrunden auf die Mindestbestellmenge (E4).
- Übergabe ausgewählter Zeilen an den Sage-**Dispositionsstapel** (E5) – **blockiert** bis Lesetest P4 und Schreibtest P5; Schalter Default aus.

**Standort:** Der Code ist standortneutral (`standort: beide`). Alle Sage-Daten der Liste kommen über SData-Abgleiche (Artikel-Zusatzfelder,
Artikel-Lieferanten, offene Bestellungen, letzte Bestellung). An IDEAL gibt es noch keinen SData-Endpunkt: Artikelquelle SData dort gesperrt bis
P-R1 (`ArtikelQuelleSwitch.cs:11/41-47`), `Sync:EinkaufBestellungenEnabled` „IDEAL ohne SData-Endpunkt aus lassen“
(`ServiceSettingDefinitions.cs:117`). Deshalb bleibt `EinkaufAktiv` an IDEAL aus, bis dort die SData-Schnittstelle läuft; der IDEAL-Teil hängt
an ihr (Keyuser-Antworten, Rückfrage 8/Standort).

**Out-of-Scope**
- Direkte Sage-Bestellung (`StandardBelegService`, Erfassungsart 4100) – eigene Folge-Spec, Priorität höher (Keyuser 2026-11-13, Rückfrage 7).
- Sonderbedarfe anderer Abteilungen → [[2026-11-13-sonderbedarfe-erfassen]].
- Kennzeichen „nicht disponieren“ / „ignorieren bis“ – entfällt (Keyuser: am Anfang nicht nötig).
- VPE / Bestellvielfaches – entfällt (Keyuser: in Sage keine Verpackungseinheiten, nur Mindestbestellmengen).
- Stufe 2 der Notiz: verbrauchsbasierte Meldebestände, Reichweite, Sicherheitsbestand nach ABC/XYZ, FA-Stücklisten-Bedarfe.
- „Auffüllen auf Höchstbestand“: im belegten Sage-Schema gibt es kein Höchstbestand-Feld ([[sage-sdata-artikel-felder]] Z. 12-49).
- Vorschau-Horizont / Zeitraster: offene Bestellungen zählen unabhängig vom Liefertermin (Bestellpunktverfahren).
- Änderungen an Lagerbestellungen, Bedarfsmeldungen und Fehlteilen (Menü „Bestellungen“ bleibt unverändert).
- Änderung des Meldebestands in Sage (`apiArtikel` PUT, ungeprüft laut [[sage-sdata-wms-schnittstellenkatalog]] Z. 34).

**Abgrenzung Menü „Bestellungen“:** Bedarfsmeldungen (`PartRequisition`) und Lagerbestellungen (`WarehouseRequisition`, Typ Lager/Glas)
bleiben dort. Dieses Epic **liest** sie nur als offene Bedarfe; es wird nicht nach Typ gefiltert, weil nur Artikel mit Meldebestand > 0 in
die Liste kommen.

## Fachliche Anforderungen

### F1 Nachbestellartikel (verbindlich, Entscheidung 2026-11-07 16:30)
Nachbestellartikel = WMS-Artikel mit **Meldebestand > 0 in Sage**. *(ueberholt: „und ohne Kennzeichen nicht disponieren (ab E3)“ – Kennzeichen
entfallen.)*
Beleg Feld: Meldebestand steht im WMS als `Article.ReorderLevel` (`Article.cs:43`). Geschrieben **nur** vom Artikel-Abgleich: View-Weg
`SageArticleSql.cs:18` (`v.Meldebestand AS ReorderLevel`) → `SageImportService.cs:449/474/516`; SData-Weg `SdataArtikelReader.cs:260`
(0 → null). Sage-„0“ wird auf beiden Wegen zu `null` (`SageImportHelpers.cs:14`, `SdataArtikelReader.cs:260`). Also gilt:
`ReorderLevel > 0` ⇔ Sage-Meldebestand > 0.
Einschränkung (belegt, AKE-SData-Weg): Im WMS stehen nur Ressourcenummern aus Stücklisten und `IstBestellartikel AND Aktiv`
(`SdataArtikelReader.cs:35`, `WhereBestellartikel` :63). Ein Sage-Artikel mit Meldebestand außerhalb dieser Menge erscheint nicht.

> [!warning] Brain-Korrektur
> Backlog und [[feature-map]] Z. 25 nennen `Article.MinimumStock` – dieses Feld gibt es nicht (kein Treffer im Code); das Feld heißt
> `Article.ReorderLevel`.

### F2 Projektion je Artikel
```
verfügbar = WMS-Bestand
          + offen bestellt (Sage-Cache, Rest nach der Rest-Regel, ab E2)
          + im Dispo-Stapel (WMS-Stapel in Sage, ab E5 – blockiert)
          − offene Bedarfe (gemäß Einstellung EinkaufOffeneBedarfe)
Vorschlag, wenn verfügbar ≤ Meldebestand
```
- **WMS-Bestand** (verbindlich „Bestand = WMS-Bestand“; Rückfrage 4: alle Plätze außer Kommissionierwagen): über
  `IStockMovementRepository.GetStockByArticleNumbersAsync` (`StockMovementRepository.cs:395-459`) – Ein-/Sage-Einbuchung und Umbuchungs-Ziel
  plus, Aus-/Sage-Ausbuchung minus (:417-424), Umbuchungs-Quelle minus (:429-449), Kommissionierwagen auf beiden Seiten ausgenommen
  (:405, :435), kein Filter auf aktiv/buchbar. Es ist dieselbe Regel wie in der Meldebestand-Mail seit v1.93.0 (`StockCheckService.cs:20-23`,
  Aufruf :41-43, Summe je Artikel und „ohne Bewegung = 0“ :58-59). Damit zeigen Liste und Mail denselben Bestand.
  *(ueberholt: „Summe aus `GetCurrentStockAsync` und Gruppierung in C#“ sowie der Befund zum `StockCheckService`-SQL – seit v1.93.0 behoben,
  das eigene SQL gibt es nicht mehr, `StockCheckService.cs:28-29` liest nur noch den Artikelstamm.)*
- **Sage verfügbar** (Vergleichsspalte, verbindlich): `ArticleSageDetail.LagerbestandVerfuegbar` (`ArticleSageDetail.cs:68`), Stand = letzter
  Artikel-Abgleich (`ModifiedAt`, sonst `CreatedAt`). Der Satellit wird **nur** bei `Sync:Quelle:Artikel = SData` geschrieben
  (`SageImportService.cs:534-539`, `ArticleSageDetail.cs:7-9`; Default `View`, `ServiceSettingDefinitions.cs:260`). Fehlt die Zeile: Zelle „–“
  und Banner mit Anzahl.
- **Offen bestellt** (E2, Teil B wiederverwendet, keine eigene Sage-Abfrage im Web):
  - Quelle: Cache `SagePurchaseOrderLine` über `ISagePurchaseOrderLineRepository.GetAllAsync()` (`SagePurchaseOrderLineRepository.cs:14/28`;
    laut Klassenkommentar :7-9 die einzige Lese-Schnittstelle für offene Sage-Bestellungen, „auch für das spätere Einkaufs-Epic“). Geschrieben
    nur vom Dienst (`EinkaufBestellungenSyncService.cs:186-193`, Vollersatz je Lauf), Gate `Sync:EinkaufBestellungenEnabled` (Default false,
    `ServiceSettingDefinitions.cs:270`). Enthalten sind Bestellungen `EBB` und Rahmenabrufe `EBZ` (`EinkaufBestellungenSyncService.cs:40-42`).
  - Menge je Position = **Rest** nach der Rest-Regel `ErfasstJeBelPos.Rest(l)` (`PurchaseOrderReceiptRepository.cs:22-30`) mit
    `GetErfasstJeBelPosAsync` (:38/:74) – dieselbe Funktion wie die Maske „Wareneingang zu Bestellung“ und die Lieferverzug-Mail (:25-26).
    Grund: Ein im WMS erfasster Wareneingang steht sofort im WMS-Bestand, verringert die offene Menge im Cache aber erst nach Sage-Übergabe und
    nächstem Abgleich ([[fallstricke]] Z. 1765-1767). Mit `MengeOffen` direkt würde er doppelt zählen.
  - Zuordnung über `SagePurchaseOrderLine.ArticleId` (beim Lauf aufgelöst, :32; ohne FK). Positionen ohne `ArticleId` gehören zu keinem
    WMS-Artikel und erscheinen nicht. Mengen stehen in der Basiseinheit (:9, :35-40). Positionen mit `EinheitAbweichend = true` (:44-46, gesetzt
    `EinkaufBestellungenSyncService.cs:149-151`) werden **nicht** eingerechnet und im Sammelbanner gezählt.
  - „Nächster Liefertermin“ = frühester `Liefertermin` (:51) der Positionen mit Rest > 0.
  - Stand = `GetStandAsync()` (`SagePurchaseOrderLineRepository.cs:19/39-40`). Bei `null` (Cache leer) zeigt die Liste einen Banner „Offene
    Bestellungen aus Sage nicht gelesen (Dienst-Abgleich Sync:EinkaufBestellungenEnabled)“, die Spalte „–“ statt 0, und die Projektion ist als
    „ohne offene Bestellungen“ gekennzeichnet. *(ueberholt: Live-Lesen von `EinkaufPositionenInfo` im Web und der Sage-Lesefehler-Banner – das
    Web liest nur den Cache; Lesefehler protokolliert der Dienst, `EinkaufBestellungenSyncService.cs:83-89`.)*
- **Offene Bedarfe** (Einstellung `EinkaufOffeneBedarfe`, Rückfrage 1 beantwortet: beide):
  - Lagerbestellungen: Kopf `Status = Submitted` → `QuantityRequested`; `Status = PartiallyDelivered` → je Position mit
    `ShortageStatus = WillBeRestocked` der Rest `QuantityRequested − (QuantityPicked ?? 0)`. Belege: `WarehouseRequisition.cs:11`,
    `WarehouseRequisitionItem.cs:20-21/44`, Status-Ableitung `WarehouseRequisitionRepository.cs:240-250`, Rest-Formel wie :439. `Draft`,
    `Closed`, `Cancelled` zählen nicht. Belegt: Abschluss bucht **keinen** Bestand (`CloseAsync` :193-238 setzt nur Mengen, Notizen, Status,
    Audit-Felder), daher ist eine offene Lagerbestellung noch nicht im WMS-Bestand abgezogen.
  - Bedarfsmeldungen: `Status = Offen` → `Quantity` (`PartRequisition.cs:22/29`). Erfüllt wird sie bei der Einbuchung
    (`StockMovementsController.cs:169` → `PartRequisitionRepository.cs:111-113`).
  - Zuordnung über `ArticleNumber` (kein Artikel-FK: `PartRequisition.cs:11`, `WarehouseRequisitionItem.cs:12`), getrimmt, Groß/Klein egal.
    Weicht `Unit` (`PartRequisition.cs:25`, `WarehouseRequisitionItem.cs:18`) von `Article.Unit` (`Article.cs:36`) ab (beide nicht leer), wird
    die Zeile **nicht** eingerechnet und im Sammelbanner gezählt.
- **Im Dispo-Stapel** (E5, blockiert): `MengeBasis` der Positionen des WMS-Stapels, live aus `DispositionStapelPositionenInfo` (Feldnamen: P4),
  nur bei `EinkaufDispoUebergabeAktiv = true`.

### F3 Vorschlagsmenge = letzte Bestellmenge (Keyuser 2026-11-13, ersetzt Rückfrage 2)
```
F  = Meldebestand − verfügbar                    (≥ 0, da Vorschlag nur bei verfügbar ≤ Meldebestand)
L  = letzte Bestellmenge des Artikels beim gewählten Lieferanten (E3)
V  = L                                            Regelfall
     Erhöhung bei außerordentlichem Bedarf        Rückfrage 11 (Vorschlag: V = (⌊F / L⌋ + 1) × L, Grund „n × letzte Bestellmenge“)
     ohne L                                       Rückfrage 13 (Vorschlag: L eines anderen Lieferanten, sonst F bzw. Meldebestand)
V  = max(V, Mindestbestellmenge)                  E4, nur bei passender Einheit (F5)
```
*(ueberholt: Einstellung `EinkaufVorschlagsverfahren` mit `FesteMenge`/`Auffuellen` auf Basis der Sage-Bestellmenge. `ArticleSageDetail.Bestellmenge`
(:70) bleibt reine Anzeigespalte „Sage-Bestellmenge“.)*

- **Definition L:** Menge der jüngsten Sage-Bestellposition des Artikels beim Lieferanten, Reihenfolge Belegdatum absteigend, dann BelID
  absteigend; Belegarten laut Rückfrage 12. Menge in der **Basiseinheit** (`MengeBasis`). L gilt nur, wenn `ArticleSageDetail.Basismengeneinheit`
  = `Lagermengeneinheit` (`ArticleSageDetail.cs:30/32`); sonst bleibt L leer mit Hinweis „Einheit Basis ≠ Lager“ (gleiche Sperrregel wie der
  Cache, `EinkaufBestellungenSyncService.cs:147-151`). Dass `MengeBasis` die Basiseinheit ist und `Menge` die Bestelleinheit, ist für die
  Belegtabellen **unverifiziert** (P-L2 c).
- **Beleg AKE (Sage-SQL, nur lesend, Gerald 2026-11-13, `AKESQL20.ake.at` / `ake_TEST2026` = SageConnection von `D:\Dev\AKE\Web`, Testkopie):**
  Tabellen `KHKEKBelege` (Mandant smallint, BelID int, Belegart, Belegdatum, A0Empfaenger) und `KHKEKBelegePositionen` (Mandant smallint,
  BelID int, Artikelnummer varchar, Menge money, MengeBasis money, Liefertermin datetime), Join über Mandant + BelID. `Belegart` enthält
  **Text**: Wareneingang 98.226 (2014-04-11 … 2026-11-13), Bestellung 64.002 (2008-01-10 … 2026-07-08), Rahmenabruf 3.455, Preisanfrage 1.356,
  Rahmenvertrag EK 522, Retour 428, RMA-Anmeldung 293, RMA-Retour 3, Liefererinnerung 1, Mahnung 1. Abfrage (sinngemäß):
  ```sql
  WITH x AS (
    SELECT p.Mandant, p.Artikelnummer, p.Menge, p.MengeBasis, b.Belegdatum, b.BelID, b.A0Empfaenger,
           ROW_NUMBER() OVER (PARTITION BY p.Mandant, p.Artikelnummer ORDER BY b.Belegdatum DESC, b.BelID DESC) AS rn
    FROM KHKEKBelegePositionen p
    JOIN KHKEKBelege b ON b.Mandant = p.Mandant AND b.BelID = p.BelID
    WHERE b.Belegart = 'Bestellung' AND ISNULL(p.Artikelnummer, '') <> '')
  SELECT COUNT(*) FROM x WHERE rn = 1;   -- 24.686 Artikel mit letzter Bestellung
  ```
  Damit ist für AKE belegt, **dass** es die Daten gibt. **Nicht** geprüft: Wert des Mandanten-Filters, ob `A0Empfaenger` die Lieferanten-Kto ist
  (Hinweis, kein Beleg: in der SData-Ressource `EinkaufPositionenInfo` liest der Dienst `A0Empfaenger` als Lieferanten-Kto,
  `EinkaufBestellungenSyncService.cs:132`), Rahmenabrufe, `Menge` vs `MengeBasis`, IDEAL. Prüfschritt P-L2.
- **Lesepfad im WMS: nur SData**, für beide Standorte gleich (IDEAL ist nur über SData erreichbar; Teil B ist ebenso SData-only,
  `EinkaufBestellungenSyncService.cs:30`). Kandidaten, Entscheidung nach P-L1:
  1. `EinkaufPositionenAllInfo` (Vertrag `CommonWawiServicesResources`, Schwester der schon genutzten `EinkaufPositionenInfo`). Ohne `where`
     Timeout > 20 s ([[sage-sdata-wms-schnittstellenkatalog]] Z. 18); im Lese-Client nur mit `where` zugelassen (`SageSdataReadClient.cs:150`).
     Feldnamen (Datumsfeld, `Menge`, `MengeBasis`) für diese Ressource unbelegt.
  2. `eptEinkaufsbelege` im Partner-Vertrag `apiDMSConnector.100036671.DPSBS_DMS_Connector` (Felder VorID, Bestellung, Bestelldatum,
     Lieferantennummer, VorPosID, Artikelnummer, Menge, MengeBasis …, [[sage-sdata-api-vertraege]] Z. 91-97). Leserecht des SData-Benutzers
     ungeprüft (Z. 103), Lesetests der API-Verträge stehen aus (Z. 12-13). *Korrektur:* Die Ressource liegt **nicht** in
     `CommonWawiServicesResourcesSpecific` (dort nur Kommissionier-, Sammelmappen- und Lizenz-Ressourcen, Z. 105-113).
  Bleibt P-L1 ohne bestätigten Pfad, ist **E3 Blocker** (kein Raten, kein Direkt-SQL als Ersatz).
- **Ablage:** drei Spalten an `ArticleSuppliers` (Artikel + Kreditor = „beim Lieferanten“): `LetzteBestellmenge`, `LetzteBestellungAm`,
  `LetzteBestellungBeleg` (SQL/148). Schreiber: ein zweiter Leseschritt im Dienst `ArtikelLieferantSyncService` nach dessen Hauptschritt (gleiches
  Gate `Sync:ArtikelLieferantEnabled`, `ServiceSettingDefinitions.cs:269`, Default false; Audit wie dort). Zuordnung Lieferant über Kto →
  `Creditor.SageKto`, Artikel über Artikelnummer (Muster `EinkaufBestellungenSyncService.cs:103-108`). Letzte Bestellung zu einem Lieferanten
  ohne `ArticleSuppliers`-Zeile: nicht abgelegt, im Protokoll als Warnung mit Beispielen gezählt. Lesefehler des Schritts: Warnung im
  Protokoll, gespeicherte Werte bleiben, der Artikel-Lieferanten-Lauf selbst bleibt erfolgreich. Ob in einem Lauf (mit Datumsfenster) oder
  je Artikel gelesen wird, legt das Ergebnis von P-L1/P-L3 fest (Laufzeit); Datumsfilter nur als `@yyyy-MM-dd@` ([[fallstricke]] Z. 1774-1776).
- Spalten: „letzte Bestellmenge“, „letzte Bestellung am“ (mit Beleg), „Vorschlag“, „Grund“ (z. B. „letzte Bestellmenge“, „2 × letzte
  Bestellmenge“, „auf Mindestbestellmenge aufgerundet“, „keine letzte Bestellung – Menge prüfen“).
- Alle Mengen in Lagermengeneinheit (`Article.Unit` = Sage `Lagermengeneinheit`, Kommentar `ArticleSageDetail.cs:28-29`).

### F4 Lieferant, WBZ, Liefertermin
- **Gewählter Lieferant = Hauptlieferant:** `ArticleSupplier` mit `IstHauptlieferant = true` (`ArticleSupplier.cs:32`) → Kreditor (Name,
  `SageKto`, Projektion wie `ArticleRepository.cs:66-71`); Bestellnummer `SageBestellnummer` (:18), Mindestbestellmenge (:21), Einkaufsmengeneinheit
  (:22), WBZ (:28, Tage). Geschrieben nur vom Lese-Abgleich `ArtikelLieferantSyncService` (u. a. :205-206; Gate `Sync:ArtikelLieferantEnabled`).
- Fehlt eine `ArticleSupplier`-Zeile, aber `ArticleSageDetail.Hauptlieferant` (:73, Kto-Text) ist gesetzt: Kto anzeigen, Hinweis „Kreditor fehlt
  im WMS“, keine letzte Bestellmenge, Zeile nicht übergebbar.
- **Letzter Lieferant** `ArticleSageDetail.LetzterLieferant` (:74), **ABC** `ABCKlasse` (:65), **USER_Mindestbestand** `UserMindestbestand` (:80,
  Rückfrage 3: nur Anzeige) – Anzeige.
- **WBZ** = WBZ des gewählten Lieferanten, sonst `ArticleSageDetail.Wiederbeschaffungszeit` (:76). Einheit Tage, belegt am AKE-Test
  ([[sdata-befund-ake-test-2026-10-30]] §14 B1/B2). **Wunschtermin** = heute + WBZ (Kalendertage), nur Anzeige.
- Lieferant änderbar auf einen anderen `ArticleSupplier` desselben Artikels nur in E5 (blockiert).

### F5 Mindestbestellmenge und Einheiten-Wächter (E4)
- **Quelle Mindestbestellmenge:** `ArticleSupplier.Mindestbestellmenge` (`ArticleSupplier.cs:21`), aus SData `ArtikelLieferantInfo`
  (`ArtikelLieferantSyncService.cs:29` select, :206 geschrieben, auf 3 Stellen gerundet). *Nicht* `ArticleSageDetail.MinimaleLosgroesse`
  (:57, Ressource Artikel – Losgröße, kein Lieferantenwert).
- Aufrunden `V = max(V, Mindestbestellmenge)` nur, wenn `Einkaufsmengeneinheit` (:22) leer oder gleich `Article.Unit` ist **und** P2 die Einheit
  der Mindestbestellmenge bestätigt hat. Sonst keine Rundung, Hinweis in der Zeile. Bis P2 beantwortet ist, gilt die Rundung als
  **unverifiziert** (Code-Kommentar `TODO(P2)`).
- Übergabe an Sage (E5, `MengeBasis`) nur, wenn `Basismengeneinheit = Lagermengeneinheit`; sonst Zeile gesperrt mit Hinweis „Einheit Lager ≠ Basis
  – in Sage bestellen“. Die Felder liegen seit v1.85.0 vor: `ArticleSageDetail.cs:28-33`, Migration `20261107182445_AddArtikelMengeneinheiten`
  = `SQL/140_AddArtikelMengeneinheiten.sql`, gelesen `SdataArtikelReader.cs:53`, gemappt :275-276. Umrechnung über `UmrechnungsFaktorLME`
  bleibt Folgearbeit. *(ueberholt: neue Spalten in E4, SQL/129, Erweiterung des `SdataArtikelReader`, P1 – alles schon vorhanden.)*

### F6 Kennzeichen je Artikel – *ueberholt, entfällt* (Keyuser 2026-11-13)
Keine Kennzeichen „nicht disponieren“ / „ignorieren bis“, kein Satellit `ArticlePurchaseSettings`, keine Aktion `SetFlags`, kein Filter
„alle Artikel mit Meldebestand“ für Kennzeichen (der Umfang-Filter in F8 bleibt).

### F7 Übergabe an den Sage-Dispositionsstapel (E5) – **BLOCKIERT**
> [!danger] Schreibt an Sage – blockiert
> Vorbedingung zum Entsperren: Lesetest P4 und Schreibtest P5 am AKE-Test mit Freigabe Gerald; Freigabe-Nachtrag 2026-11-07: „Etappen, die an
> Sage schreiben, bleiben blockiert und aus“. Beim Entsperren die seitherigen Schreib-Lehren übernehmen: `ResultNumber 200` = Ok, ohne
> `format=json` Atom-XML ([[fallstricke]] Z. 1770-1771), vor erneutem Senden Lookup statt blindem Wiederholen (Z. 1768-1769), Memo-/Kennungs-
> Eindeutigkeit je WMS (Z. 1784-1789). Alle Fundstellen dieses Abschnitts dann neu prüfen.

Bedeutung belegt: Der Dispositionsstapel ist in Sage das Ergebnis der Disposition, aus dem der Einkauf Bestellungen erzeugt
([[sage-sdata-commonwawiservices]] Z. 99-105). Am AKE-Test geprüft (§14): Stapel anlegen (B1, Typ 0 Manuell), Position anlegen (B2, `MengeBasis 200`,
Lieferant K31013 fix → `PosHandle 383`; Sage ergänzt Lieferanten-Bestellnummer, Einzelpreis, Liefertermin), Position löschen (B3) wirkt,
**Stapel löschen (B4) wirkt nicht** → Stapel wiederverwenden.
- Nur sichtbar und serverseitig erlaubt bei `EinkaufDispoUebergabeAktiv = true` (Default false) – zusätzlich zu Gate und Rolle.
- Zeilen auswählen, Menge (> 0, Dezimal kulturfest) und Lieferant (aus `ArticleSupplier` des Artikels) anpassen, „An Sage übergeben“ mit
  Bestätigungsdialog (Warntext wie Einstellung).
- Server prüft je Zeile neu: Rolle, Gate, Schalter, Menge > 0, Lieferant gehört zum Artikel und hat `SageKto`, Einheiten-Wächter (F5),
  keine Protokollzeile `Gesendet` für diesen Artikel, Artikel nicht schon im WMS-Stapel (P4). Abgewiesene Zeilen → Sammelmeldung mit Grund
  (`TempData["WarningMessage"]`), übergebene → `SuccessMessage` mit Anzahl.
- Ablauf je Zeile (Rückfragen 5/6 beantwortet: ein fester Stapel, synchron aus der Web-App): Stapel sicherstellen → Protokollzeile **vor** dem
  Aufruf auf `Gesendet` → `DispositionStapelPositionenService` (`OperationMode` Neu = 0, `Artikelnummer`, `AuspraegungHandle = 0`, `MengeBasis`,
  `MengeBasisFix`, `Lieferant = SageKto`, `IsLieferantFix = true`) → `Bestaetigt` mit `PosHandle` oder `Fehler` mit Text. Timeout/unklare
  Antwort: Status bleibt `Gesendet`, Zeile „unklar – im Sage-Stapel prüfen“, kein automatischer Neuversuch; das nächste Laden liest den Stapel
  und setzt ggf. `Bestaetigt`.
- Zugang: *(ueberholt: `SData:*`-ServiceSettings + `SageLagerbuchung:Username/Password` aus der Konfiguration.)* Seit v1.99.14 liegt der
  SData-Zugang verschlüsselt in den ServiceSettings (ADR [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]], [[fallstricke]]
  §33); Ziel-Objekt `SdataLeseZiel` (`SageSdataFaModels.cs:59`). Keine Zugangsdaten in Log oder Meldung.

### F8 Liste „Einkauf → Bestellvorschläge“
- Spalten: Lieferant (Name + Kto), Artikel, Bezeichnung, Bestand WMS, Sage verfügbar, offen bestellt (E2), nächster Liefertermin (E2),
  im Dispo-Stapel (E5), offene Bedarfe, verfügbar, Meldebestand, letzte Bestellmenge + am/Beleg (E3), Vorschlag, Grund, Einheit,
  Mindestbestellmenge (E4), Sage-Bestellmenge, Bestellnummer Lieferant, WBZ, Wunschtermin, letzter Lieferant, ABC, USER_Mindestbestand,
  Status/Hinweis.
- Standardsortierung Lieferant, dann Artikelnummer (ohne eigene Gruppenzeilen).
- Filterkarte: Lieferant (Auswahl aus Kreditoren mit Artikel-Lieferant-Zeilen), Artikelgruppe, Umfang „nur Vorschläge“ (Default) / „alle Artikel
  mit Meldebestand“, Dringlichkeit „kritisch (verfügbar < 0)“ / „nachbestellen (≤ Meldebestand)“ / alle.
- Dringlichkeit als Text-Badge, Farbe nur zusätzlich (WCAG AA, Farbe nie alleiniger Bedeutungsträger).
- Banner (melden statt still): Sage-Zusatzfelder fehlen (Anzahl ohne `ArticleSageDetail`), Artikel-Lieferanten fehlen, offene Bestellungen nicht
  gelesen bzw. Stand des Caches, Positionen/Bedarfe mit abweichender Einheit nicht eingerechnet, Artikel ohne letzte Bestellmenge (E3).

## Technischer Lösungsentwurf

- **Rechenregel an einem Ort:** `PurchaseProposalCalculator` (statisch, rein, keine DB) nimmt je Artikel ein Eingabe-Record (Bestand, offen
  bestellt, im Stapel, Bedarfe, Meldebestand, letzte Bestellmenge, Mindestbestellmenge, Einheiten, heute) und liefert Projektion, Vorschlag,
  Grund, Dringlichkeit, Hinweise. Unit-Tests decken F2/F3/F5 ab.
- **Datensammlung:** `PurchaseProposalRepository` lädt: Artikel mit `ReorderLevel > 0` + `SageDetail` (`Article.cs:58`); Lieferanten aller
  dieser Artikel in **einer** Abfrage als `ArticleSupplierRow` (Teil A hat nur `GetSuppliersAsync(int articleId)` je Artikel,
  `ArticleRepository.cs:66` – für die Liste wäre das N+1; die Projektion von :71 als Muster); offene Bedarfe (zwei gruppierte Abfragen);
  Bestand über `GetStockByArticleNumbersAsync` (s. F2); offene Bestellungen über `ISagePurchaseOrderLineRepository` +
  `IPurchaseOrderReceiptRepository` (E2).
- **Listen-View-Pattern (ADR 0005):** berechnete Liste → Spaltenfilter **in C# nach der Berechnung** über `ColumnFilterHelper.ReadFromQuery` +
  `ColumnFilterSyntax.Matches` (`ColumnFilterSyntax.cs:373`), dann `PageSize.Resolve` + `PaginationState` + `_Pagination`.
  `data-server-column-filter="true"`, `data-col-key` auf jedem `<th>`, viewKey in `ColumnDefinitions.GetByViewKey` registrieren
  ([[fallstricke]] §3, Drift-Guard `ViewKeyDriftGuardTests`). CSV über `csv-export.js` ohne Zusatzcode. Referenz `ProductionOrdersController.Index`.
- **Controller** `PurchaseProposalsController` (MVC): Class-Level `[RequireEinkaufAccess]` + `[RequireEinkaufAktiv]` (kumulativ, ADR 0006/0011);
  `Index` (GET); `Transfer` (POST, E5, blockiert) mit `[ValidateAntiForgeryToken]`. *(ueberholt: `SetFlags` – Kennzeichen entfallen.)*
- **Gate-Attribut** nach Invert-Default-Muster (nur exakt `"true"` aktiviert, MVC → Redirect Home + WarningMessage), wie
  `RequireFaHierarchyKommissionierlistenAktivAttribute.cs:28-34`.
- **Menü** in `_Layout.cshtml` als eigenes Dropdown „Einkauf“ direkt nach „Bestellungen“ (:248-272), Sichtbarkeits-Variablen im Block :50-68,
  sichtbar bei `EinkaufAktiv && CanAccessEinkaufAsync`.
- **Dienst (E3):** zweiter Leseschritt in `ArtikelLieferantSyncService` (F3 „Ablage“); Feldnamen ausschließlich aus P-L1 ins `select`
  (falscher Name = HTTP 500, [[fallstricke]] Z. 1795-1797); Lookups eng filtern (Z. 1777-1780).
- **E5 (blockiert):** Schreibclient und Protokolltabelle erst nach P4/P5; Bauform dann gegen die Schreib-Lehren aus F7 prüfen.

## Migrations-/SQL-Auswirkungen

| Nr. | Etappe | Inhalt | Guard |
|---|---|---|---|
| — | E1, E2, E4 | keine (E4: Einheiten-Spalten seit `SQL/140_AddArtikelMengeneinheiten.sql`) | – |
| **148** | E3 | `SQL/148_AddArticleSupplierLetzteBestellung.sql`: `ArticleSuppliers` + `LetzteBestellmenge` decimal(18,3) NULL, `LetzteBestellungAm` date NULL, `LetzteBestellungBeleg` nvarchar(n) NULL (n = Länge laut P-L1-Schema; mindestens wie `SagePurchaseOrderLines.Belegnummer`) | `IF COL_LENGTH(N'dbo.ArticleSuppliers', N'LetzteBestellmenge') IS NULL` je Spalte |
| **149** | E5 (blockiert) | `SQL/149_AddPurchaseProposalTransfers.sql`: Tabelle `PurchaseProposalTransfers` (`Id`, `ArticleId` FK, `CreditorId` FK, `Quantity` decimal(18,3), `Unit` nvarchar(20), `Stapelkennung` nvarchar(n nach P5), `Status` int (Offen/Gesendet/Bestaetigt/Fehler), `SagePosHandle` int NULL, `SentAt` datetime2 NULL, `LastError` nvarchar(1000) NULL, `ResponseRaw` nvarchar(4000) NULL, Audit-Spalten); Index (`ArticleId`, `Status`) | `IF OBJECT_ID(N'dbo.PurchaseProposalTransfers', N'U') IS NULL` |
| 150 | – | Reserve | – |

*(ueberholt: SQL/128 `ArticlePurchaseSettings` – entfällt; SQL/129 Einheiten – seit SQL/140 vorhanden; SQL/130 → jetzt 149.)*
Je Nummer: Model → `dotnet ef migrations add` → SQL-Skript mit Guard (DDL eigener Batch) → `__EFMigrationsHistory`-Insert in separatem Batch →
`SQL/00_FreshInstall.sql` an **beiden** Stellen (Schema + `MigrationId`). Status-Spalten ohne EF-`HasDefaultValue` (Sentinel-Fallstrick,
[[fallstricke]] Z. 1801). Nicht daten-destruktiv (nur neue Spalten/Tabellen). `SQL/AgentJobs/` nicht betroffen (kein Pflichtfeld, kein
Folge-MERGE). Nummern prüft der Dev-Lauf gegen den Bündel-Stand (letzte belegte 147: `SQL/147_RemoveAppSettingSageSdataFaAnsichtAktiv.sql`;
der goal-Lauf v1.99.16–18 und die Lieferverzug-Nachbesserung belegen keine SQL-Nummer).

## Einstellungen

Alle in `AppSettingDefinitions.All` (neue Gruppe „Einkauf“, Drift-Guard `AppSettingDefinitionsTests`), Keys in `AppSettingKeys`. Kein Seed
(fehlende Zeile = Default). Jede Instanz (AKE, IDEAL) hat ihre eigene Datenbank, die Werte gelten also je Standort.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `EinkaufAktiv` | Bool (nur exakt „true“ aktiviert) | an / aus | **aus** | Schaltet Hauptmenü „Einkauf“ und alle Seiten unter `/PurchaseProposals` frei (kumulativ zur Rolle `einkauf`). | Menü, `PurchaseProposalsController` | beide (IDEAL erst einschalten, wenn die SData-Abgleiche dort laufen) | – |
| `EinkaufOffeneBedarfe` | MultiChoice (Auswahl, leer erlaubt) | `Lagerbestellungen`, `Bedarfsmeldungen` | beide (Rückfrage 1) | Welche offenen WMS-Bedarfe die verfügbare Menge mindern; leer = keine (wirkt anders als „nicht gesetzt“ = beide). | Bestellvorschläge | beide | – |
| `EinkaufDispoUebergabeAktiv` (E5, blockiert) | Bool (nur exakt „true“) | an / aus | aus | Zeigt „An Sage übergeben“ und erlaubt den POST; liest den WMS-Dispo-Stapel live. Wirkt nur mit `EinkaufAktiv`. | Bestellvorschläge (E5) | AKE; IDEAL erst nach eigenem Schreibtest | „Schreibt Bestellvorschläge in den Sage-Dispositionsstapel. Positionen lassen sich nur in Sage entfernen; das Löschen eines Stapels über die Schnittstelle wirkt nicht.“ |
| `EinkaufDispoStapelkennung` (E5, blockiert) | String (Server prüft Länge 1…n nach P5 und Zeichen `[A-Za-z0-9_-]`) | Sage-Stapelkennung | `WMS` | Kennung des einen WMS-Stapels (Rückfrage 5); angelegt, wenn nicht vorhanden, und wiederverwendet. | Bestellvorschläge (E5) | wie oben | „Eine neue Kennung legt beim nächsten Übergeben einen weiteren Stapel in Sage an; der alte bleibt bestehen.“ |

*(ueberholt: `EinkaufVorschlagsverfahren` – entfällt, F3 ist fest „letzte Bestellmenge“.)*
Unverändert gelesen: ServiceSettings `Sync:Quelle:Artikel`, `Sync:ArtikelLieferantEnabled` (E3 schreibt im selben Lauf, keine neue
ServiceSetting), `Sync:EinkaufBestellungenEnabled`, `Sync:KreditorenEnabled`, SData-Zugang.

## Audit-Feld-Auswirkungen

- `ArticleSupplier` erbt bereits `AuditableEntity` (`ArticleSupplier.cs:9`): Der E3-Schritt setzt bei jeder Änderung der drei Spalten
  `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` mit dem Dienst-Schreibernamen wie der vorhandene Abgleich.
- `PurchaseProposalTransfer` (E5, blockiert) erbt `AuditableEntity`; Statuswechsel setzen die Modified-Felder aus `ICurrentUserService`.
- Keine Änderung an `Articles`, `ArticleSageDetails`, `PartRequisitions`, `WarehouseRequisition*`, `SagePurchaseOrderLines` (nur gelesen; der
  Cache ist ohnehin kein `AuditableEntity`, `SagePurchaseOrderLine.cs:7-8`).

## Rollen und Zugriff

- **Neue Rolle `einkauf`** (Rückfrage 8 beantwortet: eine Rolle). Belegt, dass es sie noch nicht gibt: `RoleKeys.cs:3-27` (vollständig, kein
  Einkauf-Key), kein `CanAccessEinkauf*` in `CurrentUserService.cs`, kein Filter mit „Einkauf“ in `IdealAkeWms/Filters/`. Offene Bestellungen und
  Lieferverzug nutzen andere Rechte (`PurchaseOrdersController.cs:15-16`: `stock_read` + Gate; Lieferverzug nur Mail-Empfänger-Settings,
  `ServiceSettingDefinitions.cs:144-145`). `RoleKeys.Einkauf`, `CurrentUserService.CanAccessEinkaufAsync()` = admin oder einkauf (Muster
  :107-108). **Drei Pflichtstellen:** Attribut `RequireEinkaufAccessAttribute`, [[controller]] (Filter-Tabelle + Zugriffsmatrix),
  `Views/Users/RoleOverview.cshtml`.
- Anlage der Rolle über das Update-Skript (Prüfung mit Reparatur, Muster `Pruefung/RollenBeschichtungsauftrag`, `scripts/deploy/nachlauf.psd1:109-118`);
  Zuweisen an die sieben Einkäufer AKE + IDEAL (Keyuser-Antworten) bleibt Hand-Schritt unter `/Roles`.
- Gate `[RequireEinkaufAktiv]` kumulativ (Class-Level). Übergabe (E5) zusätzlich an `EinkaufDispoUebergabeAktiv` gebunden.
- Wächter-Test: jede POST-Action hat Antiforgery und beide Filter (Muster `BeschichtungsauftraegeControllerTests.JedeSchreibendeAction_…`).

## Akzeptanzkriterien

1. Ohne `EinkaufAktiv = true` ist kein Menü „Einkauf“ sichtbar und `/PurchaseProposals` leitet auf Home mit Warnung um – auch für admin.
2. Mit Gate an sieht nur admin oder Rolle `einkauf` den Menüpunkt „Einkauf → Bestellvorschläge“; andere erhalten beim Direktaufruf den
   Zugriffsfehler des Rollenfilters.
3. Die Liste zeigt im Umfang „nur Vorschläge“ genau die Artikel mit `ReorderLevel > 0`, deren `verfügbar ≤ Meldebestand` ist (F2).
4. „Bestand WMS“ entspricht für jeden Artikel dem Bestand, den die Meldebestand-Mail für ihn rechnet (`GetStockByArticleNumbersAsync`): ohne
   Kommissionierwagen, Artikel ohne Bewegung 0, eine Umbuchung zwischen zwei regulären Plätzen ändert den Wert nicht.
5. Offene Lagerbestellungen (Submitted; PartiallyDelivered nur Rest „wird nachgeliefert“) und offene Bedarfsmeldungen mindern „verfügbar“ gemäß
   `EinkaufOffeneBedarfe`; Draft/Closed/Cancelled bzw. Erfüllt/Storniert nicht. Abweichende Einheiten werden nicht gerechnet und im Banner gezählt.
6. (E2) „offen bestellt“ = Summe der Reste (`ErfasstJeBelPos.Rest`) aller Cache-Positionen (EBB und EBZ) des Artikels; ein im WMS erfasster,
   noch nicht nach Sage übergebener Wareneingang mindert „offen bestellt“ um seine Menge (keine Doppelzählung mit dem WMS-Bestand).
   Positionen mit abweichender Einheit zählen nicht und erscheinen im Banner. Bei leerem Cache: Banner, Spalte „–“, Kennzeichnung „ohne
   offene Bestellungen“.
7. (E3) Vorschlag = letzte Bestellmenge beim gewählten Lieferanten; Spalten „letzte Bestellmenge“ und „letzte Bestellung am“ (mit Beleg) zeigen
   die Werte, die P-L1/P-L2 für den Referenzartikel liefern. Erhöhung bei außerordentlichem Bedarf und Verhalten ohne letzte Bestellung laut
   Antworten 11–13, jeweils mit sichtbarem Grund.
8. (E3) Bei `Basismengeneinheit ≠ Lagermengeneinheit` bleibt die letzte Bestellmenge leer, Hinweis „Einheit Basis ≠ Lager“.
9. (E3) Ein Lesefehler beim Schritt „letzte Bestellung“ erzeugt eine Warnung im Aktivitäts-Protokoll, die gespeicherten Werte bleiben, der
   Artikel-Lieferanten-Lauf endet erfolgreich.
10. „Sage verfügbar“ zeigt `LagerbestandVerfuegbar` mit Stand-Datum; fehlt der Satellit, „–“ und Banner mit Anzahl.
11. Lieferant = Hauptlieferant aus `ArticleSuppliers`; fehlt der Kreditor, erscheint der Kto aus `ArticleSageDetail.Hauptlieferant` mit Hinweis.
12. (E4) Vorschlag kleiner als die Mindestbestellmenge wird auf sie angehoben – nur bei gleicher Einheit; sonst Hinweis, keine Rundung.
13. (E5, blockiert) Mit `EinkaufDispoUebergabeAktiv = false` kein Übergabe-Knopf, POST serverseitig abgewiesen; mit Schalter an legt
    „An Sage übergeben“ je Zeile eine Position im Stapel an (Protokoll `Bestaetigt` mit PosHandle); doppelte Übergabe abgewiesen; Sage-Fehler
    → `Fehler` mit Text ohne Zugangsdaten; Artikel mit Basis ≠ Lager gesperrt.
14. Spaltenfilter (Server-Mode), Pagination und CSV funktionieren; CSV enthält nur sichtbare Spalten in Bildschirmreihenfolge.
15. Dringlichkeit ist als Text erkennbar (nicht nur Farbe); Kontrast WCAG AA.
16. IDEAL-DEV mit `EinkaufAktiv` aus: kein Menü, keine Verhaltensänderung an bestehenden Seiten (Regression).
17. Build und alle Tests grün; Unit-Tests für `PurchaseProposalCalculator` (F2/F3/F5), Drift-Guard für die neuen Keys und den viewKey,
    Wächter-Test für Filter/Antiforgery, Dienst-Test für den E3-Schritt (Zuordnung, Einheiten-Sperre, Fehlerfall Werte bleiben).

## Test-Szenarien

Neues Kapitel **TS-136 „Einkauf – Bestellvorschläge“** in `docs/TESTSZENARIEN.md`, Index [[testszenarien-index]] nachziehen. Umgebung AKE-DEV mit
`Sync:Quelle:Artikel = SData`, `Sync:ArtikelLieferantEnabled = true`, `Sync:KreditorenEnabled = true`, `Sync:EinkaufBestellungenEnabled = true`,
Sage AKE-Test. Referenzartikel `00722001` (Sage: Bestand 200, Meldebestand 85, Hauptlieferant K31013, WBZ 14 –
[[sage-sdata-wms-schnittstellenkatalog]] Z. 34). IDEAL-DEV nur Regression (TS-136.10).

- **TS-136.1 Gate/Rolle:** Gate aus → kein Menü, Direktaufruf Redirect (auch admin). Gate an, Benutzer ohne Rolle → kein Menü, Direktaufruf
  verweigert. Rolle `einkauf` → Menü „Einkauf → Bestellvorschläge“. Update-Skript-Prüfung legt die fehlende Rolle an.
- **TS-136.2 Projektion:** Testartikel mit Meldebestand > 0; WMS-Bestand über Einbuchung knapp über Meldebestand → kein Vorschlag; Ausbuchung
  unter Meldebestand → Vorschlag „nachbestellen“; ins Minus (falls erlaubt) → „kritisch“.
- **TS-136.3 Bestand wie Mail:** Wert „Bestand WMS“ mit der Meldebestand-Mail vergleichen; Umbuchung regulär → regulär ändert nichts, Umbuchung
  auf Kommissionierwagen mindert.
- **TS-136.4 Bedarfe:** Lagerbestellung absenden → „offene Bedarfe“ steigt; mit Fehlteil „wird nachgeliefert“ abschließen → nur Rest; Bedarfsmeldung
  offen → zählt, nach Einbuchung mit Erfüllung → nicht. Einstellung leer → keine Bedarfe. Negativ: andere Einheit → nicht gerechnet, Banner.
- **TS-136.5 (E2) Offene Bestellungen:** Artikel mit offener Sage-Bestellung (Liste „Offene Bestellungen“ als Gegenprobe) → „offen bestellt“ =
  Rest dort, Liefertermin; Teilmenge im WMS über „Wareneingang zu Bestellung“ erfassen → „offen bestellt“ sinkt um diese Menge, „Bestand WMS“
  steigt, „verfügbar“ bleibt gleich. Rahmenabruf (EBZ) zählt. Negativ: Cache leeren (DEV) → Banner, Spalte „–“.
- **TS-136.6 (E3) Letzte Bestellmenge:** Artikel-Lieferanten-Lauf → Werte für 00722001 wie P-L1/P-L2 (Menge, Datum, Beleg); Vorschlag = letzte
  Bestellmenge, Grund sichtbar. Außerordentlicher Bedarf (Bestand per Ausbuchung weit unter Meldebestand) → Erhöhung laut Antwort 11. Artikel ohne
  letzte Bestellung → Verhalten laut Antwort 13. Artikel mit Basis ≠ Lager (z. B. EK003604) → leer mit Hinweis. Negativ: SData-Ziel falsch →
  Warnung im Aktivitäts-Protokoll, Werte bleiben.
- **TS-136.7 (E4) Mindestbestellmenge:** Lieferant mit Mindestbestellmenge > Vorschlag → angehoben (nur gleiche Einheit), Grund „auf
  Mindestbestellmenge aufgerundet“; andere Einkaufseinheit → keine Rundung, Hinweis.
- **TS-136.8 Lieferant/Satellit fehlt:** Artikel ohne `ArticleSageDetail` → „–“ und Banner; Kreditor fehlt → Kto mit Hinweis.
- **TS-136.9 Liste:** Spaltenfilter (Text, Zahl), Pagination, CSV (nur sichtbare Spalten), Sortierung Lieferant/Artikel, Kontrast/Text-Badge.
- **TS-136.10 IDEAL-Regression:** IDEAL-DEV, `EinkaufAktiv` aus → kein Menü, Bestellungen-/Lager-Menü unverändert.
- **TS-136.11 (E5, erst nach Entsperren):** Schalter aus → kein Knopf, POST per Werkzeug abgewiesen; Schalter an: 00722001 mit K31013 übergeben →
  `Bestaetigt`, in Sage Stapel `WMS` mit Position; erneut → abgewiesen; falsches Passwort → `Fehler` ohne Zugangsdaten. **Danach die Position in
  Sage löschen** (Stapel bleibt, B4).

## Etappen (nur bei epic: true)

Ein Worktree, je Etappe ein Commit, ein Merge am Ende. Branch während der Arbeit mit `scripts/sync-worktree.ps1 -Slug <slug>` aktuell halten.
Jede Etappe baut und testet grün für sich; eine offene Rückfrage oder Prüfabfrage blockiert nur ihre Etappe, die übrigen laufen weiter (Blocker
in der Tabelle vermerken). Die alte E3 (Kennzeichen) entfällt; die Nummer E3 trägt jetzt die letzte Bestellmenge.

| # | Etappe | Abhängigkeit | Status | Commit |
|---|--------|--------------|--------|--------|
| 0 | Prüfabfragen lesend ausführen und eintragen: P-L1 (a–c) SData AKE-Test, P-L2 Sage-SQL AKE-Test, P-L3 WMS-DB, P2, P4 (Freigabe-Nachtrag „E0“). Abweichung von der Spec = Blocker melden | SData-/SQL-Zugang AKE-Test; IDEAL-Teile (P-L1 d) bleiben offen | **erledigt** 2026-11-13 (Ergebnisse E0, A1–A4) | – (nur lesend) |
| 1 | Grundgerüst: Rolle `einkauf` (3 Stellen + Update-Skript-Prüfung), Gate `EinkaufAktiv`, Menü „Einkauf“, `PurchaseProposalCalculator` + Tests (letzte Bestellmenge als Eingabe, noch leer), Repository, Liste nur lesen (Bestand, Sage verfügbar, Bedarfe, Meldebestand, Vorschlag, Lieferant), Einstellungen `EinkaufAktiv`/`EinkaufOffeneBedarfe`, Version in beiden `AppVersion.cs` – keine Migration | Rückfrage 13 (Verhalten ohne letzte Bestellung gilt bis E3 für alle Zeilen) | **erledigt** 2026-11-13 | `f5209692` |
| 2 | Offene Sage-Bestellungen aus dem Cache mit Rest-Regel, Spalten „offen bestellt“/„nächster Liefertermin“, Banner Stand/leer/Einheit – keine Migration | – (Teil B ist im Branch) | **erledigt** 2026-11-13 | `31ae0d69` |
| 3 | Letzte Bestellmenge: **SQL/148**, Dienst-Schritt in `ArtikelLieferantSyncService`, Spalten + Grund, Rückfragen 11/12 in der Rechenregel | **BLOCKER bis P-L1** (SData-Lesepfad bestätigt) und P-L2 (b, c); Rückfragen 11, 12 | **blockiert** (E0 A3: Leseweise – Entscheidung Gerald) | |
| 4 | Aufrunden auf Mindestbestellmenge + Einheiten-Wächter (Felder vorhanden) – keine Migration | P2 (ohne Antwort: `TODO(P2)`, Wächter strikt) | **erledigt** 2026-11-13 (Wächter konservativ nach E0-A2, `TODO(P2)`) | `9b63dfc6` |
| 5 | Übergabe an den Sage-Dispo-Stapel: Schalter, Auswahl/Menge/Lieferant, Schreibclient, Stapel-Lesen, Protokoll – **SQL/149** | **BLOCKIERT:** Lesetest P4 + Schreibtest P5 (Freigabe Gerald), Freigabe-Nachtrag „Sage-Schreiben aus“ | blockiert | |
| 6 | Abschluss: TS-136 + Index, Hilfeseite (Bedienschritte, Formel, Schalter-Warnung), README (AppSettings), Anwender- + Brain-Changelog, feature-map, controller.md, codebase/services/datenmodell, ggf. fallstricke | – | **erledigt** 2026-11-13 | `f5f6bd5b` |

## Prüfabfragen für den Menschen (nur lesend, AKE-Test; P5 ist ein Schreibtest mit Rücksetzen)

Abhängige Aussagen sind bis zur Antwort **unverifiziert**.
- **P1 Einheiten Artikel** (für E4): `GET {Basis}/sdata/ol/CommonWawiServicesResources/ake_TEST2026;1/Artikel?select=Artikelnummer,Basismengeneinheit,Lagermengeneinheit,UmrechnungsFaktorLME&where=IstBestellartikel eq -1 and Aktiv eq -1&orderBy=Artikelnummer&count=500&format=json`
  (Header `Accept: application/json`). Frage: Liefert Sage die drei Felder (kein HTTP 500)? Wie viele Artikel haben Basis ≠ Lager? Für
  einen solchen Artikel: ist `UmrechnungsFaktorLME` „Basis je Lager-ME“ oder umgekehrt (Abgleich mit der Sage-Maske)?
- **P2 Einheit Mindestbestellmenge** (für E4): `GET …/ArtikelLieferantInfo?select=Lieferant,Artikelnummer,Mindestbestellmenge,Einkaufsmengeneinheit,UmrechnungsfaktorEK&where=Artikelnummer eq '00722001'&orderBy=Artikelnummer,Lieferant&format=json`
  und Vergleich mit der Sage-Lieferantenmaske: Ist die Mindestbestellmenge in Einkaufs- oder Lagermengeneinheit?
- **P3 VPE** (Folgearbeit, Rückfrage 9): `GET …/ArtikelLieferantInfo/$schema` – gibt es ein Verpackungseinheiten-/Bestellvielfaches-Feld (in
  RealTimeData heißt es `UmrechnungsfaktorEK/VPEK`, [[sage-sdata-realtimedata]] Z. 124)?
- **P4 Dispo-Stapel lesen** (für E5): `GET …/DispositionStapelPositionenInfo/$schema` und `GET …/DispositionStapelPositionenInfo?where=Stapelkennung eq 'WMSTEST01'&orderBy=…&format=json`
  – Feldnamen für Stapelkennung, Artikelnummer, MengeBasis, Lieferant, Handle; Filter auf Stapelkennung möglich? Zusätzlich in der
  Sage-Oberfläche: Was geschieht mit einer Stapelposition, wenn daraus eine Bestellung erzeugt wird (verschwindet sie aus dem Stapel?).
- **P5 Schreibtest Stapel** (für E5, Freigabe Gerald, Muster §14): Maximale Länge `Stapelkennung` ($schema `DispositionStapelService`);
  zweite Position in einen bestehenden Stapel mit `OperationMode` Neu (ohne erneutes Stapel-Anlegen) – geht das? Wirkung von
  `MengeBasisFix = true` (bleibt die Menge bei einem Sage-Dispo-Lauf erhalten?). Danach Position löschen (B3-Muster).
- **P6** (gehört Teil B): Einheit und Offen-Definition der Menge aus `EinkaufPositionenInfo` – hier nur übernommen.

### Prüfabfragen Wiederaufnahme 2026-11-13 (nur lesend)

Stand zu den Punkten oben: **P1 entfällt** (Felder werden seit v1.85.0 gelesen, `SdataArtikelReader.cs:53`; Umrechnungsrichtung erst für die
Folgearbeit Umrechnung), **P3 entfällt** (Keyuser: keine VPE in Sage), **P6 erledigt** durch Teil B (`SagePurchaseOrderLine.cs:9-11`, E0 B-2).
P2, P4, P5 bleiben.

- **P-L1 SData-Lesepfad letzte Bestellung** (für E3; Blocker, solange offen). Header `Accept: application/json`, immer mit `orderBy`.
  - (a) Vertrags-`$schema` (das Ressourcen-`$schema` liefert am AKE-Test 401, [[fallstricke]] Z. 1855):
    `GET {Basis}/sdata/ol/CommonWawiServicesResources/ake_TEST2026;1/$schema` → in `EinkaufPositionenAllInfo` die Namen für Belegkennzeichen,
    Belegdatum, Lieferanten-Kto (`A0Empfaenger`?), Artikelnummer, `Menge`, `MengeBasis`, `Mengeneinheit`, `MengeBasiseinheit`, Belegnummer
    notieren (mit Länge).
  - (b) Mit den Namen aus (a), eng gefiltert (nie ohne `where`):
    `GET {Basis}/sdata/ol/CommonWawiServicesResources/ake_TEST2026;1/EinkaufPositionenAllInfo?select=<Namen aus a>&where=Belegkennzeichen eq 'EBB' and Artikelnummer eq '00722001'&orderBy=BelPosId&format=json`
    → jüngste Position mit P-L2 (b) vergleichen; Antwortzeit notieren. Danach Laufzeit einer Sammelabfrage messen:
    `where=Belegkennzeichen eq 'EBB' and <Datumsfeld> ge @2025-11-13@` (Abbruch nach 60 s melden) – entscheidet „ein Lauf“ vs „je Artikel“.
  - (c) Partner-Vertrag (Aufrufform laut [[sage-sdata-api-vertraege]] Z. 12, noch nie gelesen):
    `GET {Basis}/sdata/ol/apiDMSConnector.100036671.DPSBS_DMS_Connector/ake_TEST2026;1/eptEinkaufsbelege?where=Artikelnummer eq '00722001'&orderBy=VorPosID&count=20&format=json`
    → HTTP-Status (200/401/403), Felder `Bestelldatum`, `Lieferantennummer`, `Menge`, `MengeBasis` wie im Schema?
  - (d) **IDEAL:** (a)–(c) gegen den IDEAL-SData-Endpunkt, sobald es ihn gibt – bis dahin offen; der IDEAL-Teil bleibt aus.
- **P-L2 Sage-SQL AKE-Test** (`AKESQL20.ake.at` / `ake_TEST2026`, nur SELECT; ergänzt den Beleg vom 2026-11-13):
  ```sql
  -- (a) Mandant
  SELECT b.Mandant, COUNT(*) AS Belege FROM KHKEKBelege b WHERE b.Belegart = 'Bestellung' GROUP BY b.Mandant;
  -- (b) Lieferant = A0Empfaenger? (00722001: erwartet K31013, fallstricke Z. 1806)
  SELECT TOP 5 b.Mandant, b.Belegdatum, b.BelID, b.A0Empfaenger, p.Menge, p.MengeBasis
  FROM KHKEKBelegePositionen p JOIN KHKEKBelege b ON b.Mandant = p.Mandant AND b.BelID = p.BelID
  WHERE b.Belegart IN ('Bestellung', 'Rahmenabruf') AND p.Artikelnummer = '00722001'
  ORDER BY b.Belegdatum DESC, b.BelID DESC;
  -- (c) Menge vs MengeBasis (EK003604: Bestellung PKG, Basis Stk, EinkaufBestellungenSyncService.cs:148)
  SELECT COUNT(*) AS Positionen, SUM(CASE WHEN p.Menge <> p.MengeBasis THEN 1 ELSE 0 END) AS MengeUngleichBasis
  FROM KHKEKBelegePositionen p JOIN KHKEKBelege b ON b.Mandant = p.Mandant AND b.BelID = p.BelID
  WHERE b.Belegart = 'Bestellung';
  SELECT TOP 5 b.Belegdatum, p.Menge, p.MengeBasis
  FROM KHKEKBelegePositionen p JOIN KHKEKBelege b ON b.Mandant = p.Mandant AND b.BelID = p.BelID
  WHERE b.Belegart = 'Bestellung' AND p.Artikelnummer = 'EK003604' ORDER BY b.Belegdatum DESC;
  -- (d) Rahmenabrufe als jüngster Beleg (für Rückfrage 12)
  WITH x AS (SELECT b.Belegart, ROW_NUMBER() OVER (PARTITION BY p.Mandant, p.Artikelnummer ORDER BY b.Belegdatum DESC, b.BelID DESC) AS rn
             FROM KHKEKBelegePositionen p JOIN KHKEKBelege b ON b.Mandant = p.Mandant AND b.BelID = p.BelID
             WHERE b.Belegart IN ('Bestellung', 'Rahmenabruf') AND ISNULL(p.Artikelnummer, '') <> '')
  SELECT Belegart, COUNT(*) AS Artikel FROM x WHERE rn = 1 GROUP BY Belegart;
  ```
- **P-L3 WMS-DB** (AKE-DEV und IDEAL-DEV, nur SELECT) – Mengengerüst für E3 und die Liste:
  ```sql
  SELECT COUNT(*) AS MitMeldebestand FROM dbo.Articles WHERE ReorderLevel > 0;
  SELECT COUNT(*) AS MitHauptlieferant FROM dbo.Articles a
  WHERE a.ReorderLevel > 0 AND EXISTS (SELECT 1 FROM dbo.ArticleSuppliers s WHERE s.ArticleId = a.Id AND s.IstHauptlieferant = 1);
  ```

### Ergebnisse E0 (2026-11-13, /goal-Lauf [[2026-11-13-buendel-v1-99-19-goal-lauf]], nur lesend: SData GET mit `orderBy`/`where`, SQL nur SELECT)

Zugang: SData `https://sagetest01.ake.at:5493/sdata/ol/…`, Dataset `ake_TEST2026;1`, Basic-Auth über den appsettings-Rückfall der DEV-Dienst-Konfiguration
(`SData:Username`/`Password` in `AKE_WMS_DEV.ServiceSettings` leer, [[fallstricke]] §33). Sage-SQL `AKESQL20.ake.at`/`ake_TEST2026`; WMS `.`/`AKE_WMS_DEV`, `IDEAL_WMS_DEV`.
P5 (Schreibtest) und IDEAL (P-L1 d) nicht ausgeführt.

- **P-L1 (a)** Vertrags-`$schema` 200 (325 ms). `EinkaufPositionenAllInfo` (134 Felder, alle filterbar, nur GET): `Mandant`, `BelId`, `BelPosId`, `VorPosId`,
  `Belegkennzeichen` string(3), `Belegart`, `Belegdatum` dateTime, `BelegnummerFormatiert` **string(45)**, `Belegnummer` int, `A0Empfaenger` string(20),
  `A0Matchcode`, `A0Name1`, `Artikelnummer` string(50), `Menge`, `Mengeneinheit`(5), `MengeBasis`, `MengeBasiseinheit`(5), `MengeEKEinheit`,
  `MengeEKUmrechnung`, `Lagermengeneinheit`, `UmrechnungsFaktorLME`, `PositionGGErfuellt`. Schreibweise `BelId`/`BelPosId` (nicht `BelID`).
- **P-L1 (b)** EBB + `00722001`: 200, 376 ms, 55 Zeilen, alle K31013; jüngste BelId 360630, 2026-04-09, 2027-168142, 400 lfm = Basis 400 (= P-L2 b).
  EBZ: 0 Zeilen. Sammelabfrage 1 Jahr (`Belegdatum ge @2025-11-13@`, `count=1000`): EBB 7.760 Zeilen / 8 Seiten / **27,4 s**, EBZ 420 / 1,4 s.
  Nur gezählt: EBB ab 2023-11-13 36.288 (9,0 s), EBB gesamt 173.030 (19,6 s für `count=1`), EBZ gesamt 3.995. Drei Jahre geschätzt 5–6 min (nicht gemessen).
  **Paging:** Sortierung nach `BelPosId`, nicht nach Datum – „jüngster Beleg“ nur über alle Seiten bestimmbar.
- **P-L1 (c)** Partner-Vertrag: `$schema` 200 (4,4 s); `…/eptEinkaufsbelege?…` **404** `ResourceKindNotFound`; richtiger Name
  `eptEinkaufsbelege.100036671.DPSBS_DMS_Connector` → 200 (611 ms), Felder wie Spec, aber ohne Einheiten, ohne Belegkennzeichen, ohne `$totalResults`.
- **P-L2** (a) nur Mandant 1, 64.002 Bestellungen. (b) 00722001: 5 Bestellungen, alle K31013 (jüngste 360630, 2026-04-09, 400). (c) 173.030 Positionen,
  37 mit Menge ≠ MengeBasis; belegt: `MengeBasis = Menge × MengeEKUmrechnung` (Abweichung > 0,01 nur 6 Positionen), `Menge` = Bestell-Einheit;
  EK003604 jüngste 2026-05-22 Menge 2 = Basis 2 (Einheit PKG / Basis Stk / Lager PKG). (d) jüngster Beleg je Artikel: Bestellung 24.518, **Rahmenabruf 190**.
- **P-L3** AKE_WMS_DEV: 2.814 mit Meldebestand, davon 2.784 mit Hauptlieferant (ArticleSuppliers 33.158, letzte Änderung 2026-11-08). IDEAL_WMS_DEV: 1.511 / **0**
  (keine Artikel-Lieferanten). Abgleich der 2.814 gegen Sage-SQL: 2.778 mit Bestellhistorie, letzte im letzten Jahr nur bei **1.537**, in 3 Jahren bei 2.291.
- **P2** `ArtikelLieferantInfo` 00722001: 200, K31013, Mindestbestellmenge 200, Einkaufsmengeneinheit lfm, Faktor 1 – an diesem Artikel nicht entscheidbar.
  SQL-Hinweis (kein Maskenbeleg): bei den 13 von 34.802 `KHKArtikelLieferant`-Zeilen mit Faktor ≠ 1 passt die Mindestmenge zur **Einkaufseinheit**
  (EK002400 24 PKG Faktor 3; EK007623 5 Stk Faktor 5). **Maskenprüfung offen → Gerald.**
- **P4** Ressourcen-`$schema` 401 (bekannt); aus dem Vertrags-Schema: `DispositionStapelPositionenInfo` (73 Felder; Schlüssel `Mandant`+`ID`, `DispoPosID`;
  `Stapelkennung` string(10), `Lieferant`, `Artikelnummer` string(31), `Menge`, `MengeBasis`, `MengeBasisEinheit`, `MengeEKEinheit`, `MengeEKMindest`,
  `Bestelltermin`, `Liefertermin`, `Bestellnummer`, `RahmenvertragsID`; kein `MengeBasisFix`). Filter `Stapelkennung eq 'WMSTEST01'` → 200, 0 Zeilen
  (kalt 24,8 s, warm 0,76 s, mit `Mandant eq 1` 0,05 s). Ein Stapel WMSTEST01, `KHKDispostapelPositionen` leer → positiver Filterbeleg erst mit P5.

**Abweichungen (laut Freigabe-Nachtrag „Abweichung = Blocker, nicht raten“):**
- **A1** Partner-Ressource heißt `eptEinkaufsbelege.100036671.DPSBS_DMS_Connector` (ohne Suffix 404). Für E3 ohne Folgen – Lesepfad ist `EinkaufPositionenAllInfo`.
- **A2 (E4)** Einheiten-Wächter F5 („Einkaufsmengeneinheit leer oder = `Article.Unit`“) reicht nicht: gleiche Einheit mit Faktor ≠ 1 kommt vor
  (EK002042 Stk/Stk ×2, EK007623 Stk/Stk ×5, EK015491 ×2; Faktor 0 bei EK003375, EK016975). Umsetzung **konservativ**: Aufrunden zusätzlich nur bei
  `ArticleSupplier.UmrechnungsfaktorEK = 1`, sonst kein Aufrunden mit Hinweis (Wächter strikter, keine Umrechnung geraten); Entscheidung über eine
  Umrechnung bleibt bei Gerald zusammen mit P2.
- **A3 (E3) – BLOCKER:** Ein Lauf mit 1-Jahres-Fenster (~29 s) findet die letzte Bestellung nur für 1.537 von 2.778 AKE-Meldebestandsartikeln mit
  Historie (55 %), 3 Jahre ~82 % bei geschätzt 5–6 min; je Artikel ~0,35–0,5 s → 16–23 min. **Zu entscheiden (Gerald): Fenster + Nachlesen je Artikel
  für den Rest, langes Fenster, oder nur je Artikel.** Bis dahin bleibt E3 (und damit SQL/148) blockiert; die Liste rechnet für alle Zeilen nach
  Rückfrage 13 (b).
- **A4** Länge Belegnummer für SQL/148: `BelegnummerFormatiert` string(45) → n = 45. Nebenfund: `SagePurchaseOrderLines.Belegnummer` (Teil B) ist nur
  nvarchar(20) – heute reichen 11 Zeichen.

Bestätigt ohne Abweichung: Lieferant = `A0Empfaenger` (K31013), Bedeutung `Menge`/`MengeBasis`, Rahmenabruf als jüngster Beleg selten (190 Artikel,
EBZ-Abfrage billig), Datumsfilter `@…@` wirkt, nur Mandant 1.

## Deploy

*Vorläufig – der qa-agent bestätigt gegen den echten Diff.*
- **Web-App:** ja (Controller, View, Menü, Einstellungen, Rolle; E3 Spalten; E5 nur nach Entsperren).
- **Service:** ja ab E3 (`ArtikelLieferantSyncService` liest die letzte Bestellung). E1/E2/E4 allein: nein.
- **Migration:** ja ab E3 – `SQL/148`; `SQL/149` nur mit E5. DB vor Web und Dienst; nicht daten-destruktiv.
- **Voraussetzungen AKE** (Rückfrage 10 beantwortet): `Sync:Quelle:Artikel = SData`, `Sync:ArtikelLieferantEnabled = true`,
  `Sync:KreditorenEnabled = true`, `Sync:EinkaufBestellungenEnabled = true`, je mindestens ein erfolgreicher Lauf. Rolle `einkauf` zuweisen.
  Danach `EinkaufAktiv = true`.
- **IDEAL:** Deploy unschädlich, `EinkaufAktiv` bleibt aus bis zur IDEAL-SData-Schnittstelle (dann dieselben Voraussetzungen wie AKE).
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023, ASCII; Version = vergebene Version, Vorschlag 1.99.20):
  - `@{ Id='SQL/148'; Version='1.99.20'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Datei='148_AddArticleSupplierLetzteBestellung.sql'; MigrationId='<aus dem Dev-Lauf>'; Voraussetzung='additiv' }`
  - `@{ Id='Pruefung/RolleEinkauf'; Version='1.99.20'; Typ='Pruefung'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='Reparatur'; Pruefung='SELECT COUNT(*) FROM (VALUES (N''einkauf'')) v(k) WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.[Key] = v.k)'; Reparatur=<INSERT wie Pruefung/RollenBeschichtungsauftrag, nachlauf.psd1:111-116>; Text='Rolle einkauf fehlt (kein Seed). Zuweisen an den Einkauf bleibt Hand-Schritt unter /Roles.' }`
  - `@{ Id='v1.99.20-EinkaufAktivieren'; Version='1.99.20'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Bestellvorschlaege: EinkaufAktiv bleibt aus. AKE: erst nach erfolgreichen Laeufen Artikel (SData), Kreditoren, Artikel-Lieferanten und Offene Bestellungen einschalten und Rolle einkauf zuweisen. IDEAL: aus lassen bis zur SData-Schnittstelle.' }`
  - `SQL/149` und ein Einstellung-Eintrag zu `EinkaufDispoUebergabeAktiv` erst mit E5 (blockiert).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Zusatzfunde (gemeldet, nicht Teil dieses Epics)

- *ueberholt:* `StockCheckService` zählte Umbuchungen regulär → regulär als Zugang – seit v1.93.0 behoben (`StockCheckService.cs:20-23`, TS-122).
- Backlog/[[feature-map]] nennen `Article.MinimumStock`; das Feld heißt `Article.ReorderLevel`.
- *ueberholt:* „Backlog nennt eine VPE in `ArticleSuppliers`; die Tabelle hat keine“ – seit v1.85.0 gibt es `UmrechnungsfaktorVPEK`
  (`ArticleSupplier.cs:26`); fachlich ohne Belang (Keyuser: keine VPE).
- Die Aufgabe dieses Reworks nannte `eptEinkaufsbelege` als Ressource in `CommonWawiServicesResourcesSpecific`; laut
  [[sage-sdata-api-vertraege]] Z. 91-97 liegt sie im Partner-Vertrag `apiDMSConnector…` (siehe F3).

## Offene Rueckfragen

11. **Erhöhung bei außerordentlichem Bedarf:** Reicht die letzte Bestellmenge L nicht (Fehlmenge F ≥ L), wie viel wird vorgeschlagen?
    **Empfehlung:** `V = (⌊F / L⌋ + 1) × L` (ein Vielfaches der gewohnten Menge, so dass nach Lieferung verfügbar > Meldebestand), Grund
    „n × letzte Bestellmenge“ in der Liste; danach Mindestbestellmenge. Die Alternative aus den Keyuser-Antworten („Fehlmenge > L → Fehlmenge,
    aufgerundet auf Mindestbestellmenge“) hebt verfügbar nur genau auf den Meldebestand; weil der Vorschlag bei verfügbar ≤ Meldebestand
    greift, stünde der Artikel nach der Bestellung sofort wieder auf der Liste.
12. **Rahmenabrufe:** Zählt ein Rahmenabruf (Belegart „Rahmenabruf“, 3.455 Belege am AKE-Test; im Cache `EBZ`) als letzte Bestellung?
    **Empfehlung:** ja, Bestellung und Rahmenabruf, der jüngste Beleg gilt – sonst haben Artikel, die nur über Rahmen bezogen werden, keine
    letzte Bestellmenge. P-L2 (d) zeigt, wie viele Artikel das betrifft.
13. **Ohne letzte Bestellung beim gewählten Lieferanten:** **Empfehlung:** (a) die jüngste Bestellung des Artikels bei einem anderen Lieferanten
    nehmen, Hinweis „letzte Bestellung bei <Lieferant>“; gibt es keine, (b) Fehlmenge, bei Fehlmenge 0 der Meldebestand, Hinweis „keine letzte
    Bestellung – Menge prüfen“; danach Mindestbestellmenge.

### Beantwortet (Rückfragen 1–10, Freigabe 2026-11-07; Abweichungen siehe Keyuser-Antworten 2026-11-13)

1. **Offene Bedarfe:** Zählen offene Lagerbestellungen (Submitted voll, PartiallyDelivered nur Rest „wird nachgeliefert“) und offene
   Bedarfsmeldungen (Offen) beide als Bedarf? Risiko: Hat der Einkauf für eine Bedarfsmeldung schon bestellt, steht sie zusätzlich in
   „offen bestellt“ und gleicht sich aus; doppelt mindernd wirkt sie nicht. **Empfehlung:** ja, beide; Default der Einstellung = beide.
2. **Vorschlagsverfahren-Default:** „Feste Bestellmenge“ oder „Auffüllen auf Meldebestand + Bestellmenge“? **Empfehlung:** Feste
   Bestellmenge – die Sage-Bestellmenge ist gepflegt (00722001) und entspricht der klassischen Sage-Logik Meldebestand + Bestellmenge.
   *(ueberholt durch Keyuser 2026-11-13: letzte Bestellmenge, F3.)*
3. **USER_Mindestbestand:** Fachliche Bedeutung (Sicherheitsbestand? Altfeld?) – rechnen oder nur anzeigen? **Empfehlung:** in diesem Epic nur
   Anzeigespalte; Bedeutung klärt Gerald, Rechnen ggf. in Stufe 2.
4. **Bestand-Umfang:** Alle WMS-Plätze außer Kommissionierwagen, auch inaktive und nicht buchbare? **Empfehlung:** ja, wie Bestandsaggregation
   ([[fallstricke]] §7 „IsActive vs IstBuchbar“) – sonst fehlt vorhandener Bestand und es wird zu viel bestellt.
5. **Dispo-Stapel:** ein fester WMS-Stapel oder je Lieferant? **Empfehlung:** ein fester Stapel (Kennung `WMS` aus Einstellung) – Lieferant
   steht je Position, und weil Stapel-Löschen nicht wirkt (B4), würden sich Stapel je Lieferant ansammeln.
6. **Übergabe-Weg:** synchron aus der Web-App mit Protokolltabelle oder Outbox + neuer Dienst-Worker? **Empfehlung:** synchron – der Einkauf
   braucht das Ergebnis je Zeile sofort, die Mengen sind klein (Dutzende), die Outbox (`SageStammdatenOutboxItem.cs:5-11`) ist für
   Stammdaten gebaut und hat keinen Worker; Status `Gesendet` vor dem Aufruf + Stapel-Lesen ersetzen den automatischen Neuversuch.
7. **Direktbestellung** (`StandardBelegService`, 4100) in diesem Epic? **Empfehlung:** nein, eigene Folge-Spec hinter eigenem Schalter;
   Gerald hat „Sage bekommt nur das Ergebnis (Dispo-Stapel)“ entschieden.
8. **Rolle:** eine Rolle `einkauf` für Liste, Kennzeichen und Übergabe? **Empfehlung:** ja; der Übergabe-Schalter begrenzt die Außenwirkung,
   eine getrennte Leserolle ist derzeit ohne Bedarf.
9. **Rundung:** nur auf die Mindestbestellmenge aufrunden (VPE/Bestellvielfaches liegt nicht im WMS)? **Empfehlung:** ja; VPE nach P3 als
   Folgearbeit. *(Keyuser 2026-11-13: keine VPE in Sage – P3 entfällt.)*
10. **Betrieb AKE:** Werden vor der Abnahme `Sync:Quelle:Artikel = SData` und `Sync:ArtikelLieferantEnabled = true` gesetzt (Default View
    bzw. false)? Ohne beides fehlen Bestellmenge, Lieferant, WBZ und Sage-verfügbar, die Liste zeigt nur Banner. **Empfehlung:** ja, als
    Deploy-Voraussetzung.

## Freigabe-Nachtrag 2026-11-07 19:17 (Gerald, geht dem Rumpf vor)
- Alle Rückfragen **wie empfohlen** (Antworten unten), Abweichungen nur wo dort vermerkt.
- **Etappe E0 vor E1:** Alle Prüfabfragen dieser Spec, die lesend am AKE-Test möglich sind (SData nur GET mit `orderBy`; Sage-SQL bzw.
  WMS-DB nur SELECT), führt der Dev selbst aus und trägt die Ergebnisse ein. **Weicht ein Ergebnis von der Spec-Annahme ab: Blocker melden,
  nicht raten** (Muster B1 vom 07.11.). Prüfabfragen, die IDEAL-Systeme oder Menschen brauchen, bleiben offen; betroffene Teile hinter
  Schalter/Standort aus.
- **Etappen, die an Sage schreiben, bleiben blockiert und aus** (laut Bündel-Übersicht). Keine Schreibtests an Sage in diesem Lauf.
- Worktree laut Lauf-Auftrag (Empfehlung Ü1: nach Merge des Bündels frisch von `main`).

## Freigabe-Nachtrag 2026-11-13 16:27 (Gerald, geht dem Rumpf und dem Nachtrag vom 07.11. vor)
- **Freigabe erneuert:** Der Zurückstellungs-Vermerk vom 07.11. ist überholt. Umsetzen im gemeinsamen /goal-Lauf mit
  [[2026-11-13-lieferverzug-nachbesserung-keyuser-spec]] **im Bündel-Worktree** `.claude/worktrees/2026-08-07-ideal-teile-1-5`
  (nicht „frisch von main“ – der entsprechende Punkt im Nachtrag 07.11. gilt nicht mehr). Nummern in Lauf-Reihenfolge nach der Lieferverzug-Spec.
- **Feature-Schalter Default aus**, an beiden Standorten; IDEAL-Teil hinter der IDEAL-SData-Schnittstelle. Rolle `einkauf`.
- **Keyuser-Antworten 2026-11-13** (Abschnitt oben) sind verbindlich, wo sie vom Rumpf abweichen; Rückfragen 11–13 wie unten beantwortet.
- **Etappen, die an Sage schreiben (Übergabe Dispo-Stapel), bleiben blockiert und aus** – kein Schreibtest in diesem Lauf; Code darf hinter dem
  Schalter entstehen, wenn die Spec das vorsieht, aber kein POST.
- E0: lesende Prüfabfragen über DEV selbst ausführen; Abweichung von der Spec-Annahme (z. B. Quelle „letzte Bestellmenge“) = Blocker, nicht raten.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Offene Bedarfe (Lagerbestellungen + Bedarfsmeldungen) → **wie empfohlen (Gerald 2026-11-07 19:17)**
2. Vorschlagsverfahren-Default → **wie empfohlen (Gerald 2026-11-07 19:17)**
3. USER_Mindestbestand → **wie empfohlen (Gerald 2026-11-07 19:17)**
4. Bestand-Umfang → **wie empfohlen (Gerald 2026-11-07 19:17)**
5. Dispo-Stapel fest oder je Lieferant → **wie empfohlen (Gerald 2026-11-07 19:17)**
6. Übergabe-Weg → **wie empfohlen (Gerald 2026-11-07 19:17)**
7. Direktbestellung → **wie empfohlen (Gerald 2026-11-07 19:17)**
8. Rolle einkauf → **wie empfohlen (Gerald 2026-11-07 19:17)**
9. Rundung → **wie empfohlen (Gerald 2026-11-07 19:17)**
10. Betrieb AKE (SData-Artikel + Artikel-Lieferanten) → **wie empfohlen (Gerald 2026-11-07 19:17)**

## Keyuser-Antworten 2026-11-13 (Till Zöhrer, Einkauf) – vor Wiederaufnahme einarbeiten
Quelle: [[2026-11-12-keyuser-fragen-einkauf]]. Spec bleibt zurückgestellt (Gerald 2026-11-12: wird benötigt, sobald die Sage-Schnittstelle am
Produktivsystem verfügbar ist). Abweichungen gegenüber den Freigabe-Antworten vom 07.11.:
- **Rückfrage 2 / F3 Vorschlagsmenge – abweichend:** Vorschlag = **letzte Bestellmenge** des Artikels (beim Lieferanten), erhöht bei
  außerordentlichem Bedarf; danach Aufrunden auf Mindestbestellmenge. Datenquelle der letzten Bestellmenge ist **unbelegt** (Prüfabfrage
  nötig, z. B. letzte EBB-Position je Artikel aus `EinkaufPositionenInfo`). „Außerordentlicher Bedarf“ = Fehlmenge > letzte Bestellmenge?
  → Rückfrage an Gerald/Till bei Wiederaufnahme.
- **Rückfrage 3:** USER_Mindestbestand vermutlich Altfeld, nur anzeigen (unverändert); Till prüft final.
- **Rückfrage 7:** direkte Bestellerzeugung aus dem WMS ausdrücklich gewünscht → Folge-Spec bleibt, Priorität höher.
- **Rückfrage 9:** keine Verpackungseinheiten in Sage, nur Mindestbestellmengen → P3/VPE entfällt.
- **F6 Kennzeichen:** am Anfang nicht nötig (Etappe E3 kann entfallen oder später kommen).
- **Rückfrage 8 / Standort:** Nutzer = gesamter Einkauf IDEAL + AKE (Bianca, Melisa, Andrea, Janes, Marko, Andreas, Till) → `standort: beide`,
  IDEAL-Teil hängt an der IDEAL-SData-Schnittstelle.
- **Neu:** Sonderbedarfe anderer Abteilungen in die Liste → [[2026-11-13-sonderbedarfe-erfassen]].

## Freigabe-Antworten Wiederaufnahme (Rückfragen 11–13, Mensch füllt aus)

11. Erhöhung bei außerordentlichem Bedarf → **Wie empfohlen: V = (⌊F/L⌋ + 1) × L, Grund „n × letzte Bestellmenge“, danach Mindestbestellmenge (Gerald 2026-11-13 16:27).**
12. Rahmenabruf als letzte Bestellung → **Ja, Bestellung und Rahmenabruf, der jüngste Beleg gilt (Gerald 2026-11-13 16:27).**
13. Ohne letzte Bestellung beim gewählten Lieferanten → **Wie empfohlen: (a) jüngste Bestellung bei anderem Lieferanten mit Hinweis, sonst (b) Fehlmenge bzw. Meldebestand mit Hinweis; danach Mindestbestellmenge (Gerald 2026-11-13 16:27).**
