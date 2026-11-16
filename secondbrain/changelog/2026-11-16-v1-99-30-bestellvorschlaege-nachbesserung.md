---
type: changelog
version: 1.99.30
date: 2026-11-16
---
# v1.99.30 — Bestellvorschläge Nachbesserung: FA-Bedarfe, Dringlichkeit nach Termin, Mengenregel A, Einheiten, Matchcode/Link, Artikelinfo

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `223d7015` (E1), `92e6fcc3` (E2), `6a3e7502` (E3), `6c2e039c` (E4), `addd7c0b` (E5),
`34c38ce8` (E6), `4a580fc5` (E7), `fd9f49db` (E9, wip feature-complete). Spec [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] (Epic),
Grundspec [[2026-11-08-einkauf-bestellvorschlaege-spec]]. Nicht gemergt. Spec nannte 1.99.31 – vergeben 1.99.30 (nächste freie Nummer).

## Kernpunkte

- **E1 Laufzeit:** eigener Dienst-Lauf `LetzteBestellungSyncService` (SyncLog-Service `LetzteBestellung`) nur zu `Sync:LetzteBestellungUhrzeitNacht`
  (3) / `-Mittag` (12), Gate `Sync:ArtikelLieferantEnabled`; vorher ~287 s je Zyklus. `MengeBasis <= 0` zählt nicht (EK023626).
- **E2:** letzte Bestellung bei Lieferanten ohne Artikel-Lieferant-Zeile → `dbo.ArticleLastOrdersUnassigned`; gilt, wenn jünger als alle
  zugeordneten (Antwort 4). Hinweise nach Ursache, Lieferant-Kaskade `WaehleLieferant` (F8).
- **E3/E4 eine Bedarfsregel:** `PlannedConsumptionService` (Web) für Artikelinfo **und** Bestellvorschläge: offener FA-Rest (Soll − Ausbuchungen
  zum FA, `StockMovementRepository.GetAusbuchungenJeFaAsync`), je FA max(FA-Rest, Bedarfsmeldung), dann max(FA-Teil, Lagerbestellungen).
  BOM-Weg `GetComponentMengePerDeviceAsync` (eine Abfrage für alle Artikel). Spalten FA-Bedarf, Matchcode; Artikelnummer → Artikelinfo nur mit
  `HasMasterDataReadAccessAsync` (R1), `returnUrl` lokal geprüft in der View, Knopf „Zurück zu Bestellvorschlägen“.
- **E5:** „kritisch“ über Fenster heute + WBZ (`PurchaseProposalCalculator.VerfuegbarBis`, `ArtikelBedarf.AngerechnetBis`), Bestellungen nur mit
  Liefertermin ≤ Grenze; Spalte „nächster Bedarf“.
- **E6 Mengenregel A:** V = L; v + L ≤ M → M − v + 1 (aufgerundet auf `DezimalstellenLager`); gleiche Regel für Sage-Bestellmenge, ohne Quelle
  M − v + 1. Kein Vielfaches mehr.
- **E7 Ersatzlösung Einheiten:** offene Position mit Basis = Lager zählt trotz `EinheitAbweichend`; letzte Bestellmenge und Sage-Bestellmenge nur
  bei Basis = Lager und `UmrechnungsFaktorLME` leer/1 (BV-1). Nie Faktor 1 geraten.
- **E8 übersprungen** (Blocker P-N6/P-N9, Rahmenverträge), SQL/156 unverbraucht.

## Migration / SQL

- SQL/155 `155_AddArticleLastOrderUnassigned.sql`, Migration `20261116190709_AddArticleLastOrderUnassigned` (Tabelle
  `ArticleLastOrdersUnassigned`, additiv). FreshInstall an beiden Stellen. Manifest-Eintrag `SQL/155` in `scripts/deploy/nachlauf.psd1`.
- Migrationstest 2026-11-16 auf `.` (Wegwerf-DB): FreshInstall (mit `sqlcmd -I`) + SQL/155 zweimal grün, 101 Migrationen = 101 im Projekt, DB gelöscht.

## Deploy-Risiken

- SQL/155 vor dem Dienststart; Web und Dienst aus demselben Stand.
- **Artikelinfo rechnet anders** (sichtbar für Lager/Produktion an beiden Standorten): Lager/Produktion vorab informieren.
- Mengen und Dringlichkeiten der Liste ändern sich (Regel A, Fenster) – Till bestätigt Regel A am Bildschirm (TS-154.6).
- Ladezeit der Liste mit FA-Bedarfen auf AKE-DEV nicht gemessen (eine BOM-Abfrage per OPENJSON statt Blöcken) → QA.
- Die zwei Info-Einträge aus dem Spec-Deploy-Abschnitt (`v1.99.31-LetzteBestellungUhrzeiten`, `-ArtikelinfoBedarfe`) wurden **nicht** ins Manifest
  übernommen – offen für QA/Gerald.

## Tests

Web 2997 grün (1 übersprungen), Service 675 grün. TS-154 (154.1–154.13).
