---
type: changelog
version: 1.99.43
date: 2026-11-18
---
# v1.99.43 — IDEAL-Kommissionierliste: Ziele mehrfach mit Gedächtnis, Abteilung, Lagerplätze, Leitstand-Gate

Bündel `feature/2026-08-07-ideal-teile-1-5`, Basis `b5463336` (v1.99.42). Epic [[2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand-spec]]:
E1 `77c7c57e`, E2 `cbd32566`, E3 `7d544402`, E4 `6c9d4a5e`. QA-Fix `3a6372a5` (nur Doku TS-164.1). Testbereit (qa-agent), Paket 8.
Goal-Lauf [[2026-11-18-goal-lauf-kommissionierliste-v1-99-43]]. Nicht gemergt.

## Kernpunkte

- **Ziele mehrwertig** (`target` wiederholt, Häkchen-Partial `_KommissionierZiele`): je Benutzer gemerkt (`UserViewPreference`
  `FaHierarchyKommissionier.Ziele`, beide Ansichten gemeinsam), „Ziele auf Standard“ löscht das Gedächtnis; ohne Gedächtnis gilt die neue
  Einstellung `FaHierarchyKommissionierlisteVorfilterZiele` (MultiChoice aus Stammdaten `Kommissionierziel`, Seed `K-01,K-02`, leer = alle).
  Druck/PDF lesen nur die Query (`ZielQuery`). MultiChoice kann jetzt Werte aus Stammdaten statt fester Liste haben (`SettingValidator`,
  `_SettingField`, `SettingsController.OptionenAsync`).
- **Spalte Abteilung** (`montage-abteilung`): `VormontageService.FormatTexte` über alle Kopfzeilen des HauptFA – kommagetrennt, kein „Kombigerät“;
  `FormatAbteilung`/Vormontage/BDE unverändert. Column-Map je Anfrage (`BuildColumnMap`), Filter vor dem Paging.
- **Spalte Lagerplätze** (`lagerplaetze`): `GetStockByArticleNumbersAsync` (ohne Kommissionierwagen), Menge > 0, „K-02 (12), H4-05 (3)“, sonst „—“;
  eine Batch-Abfrage je Seite, Druck die ganze Menge; mit Lagerplatz-Filter erst die Restmenge laden (eine Abfrage), dann filtern und pagen.
- **Leitstand-Gate** `FaHierarchyKommissionierlisteNurFreigegebene` (Bool, Seed an): `GetReleasedHauptFaOrderNumbersAsync` (HauptFA mit
  `IsReleasedForPicking`, ohne IsDone-Ausschluss), Gate in allen sechs Aktionen vor den Spaltenfiltern; Banner mit Zahl, Warnung bei
  `LeitstandAktiv` aus; PDF eines nicht freigegebenen HauptFA → WarningMessage.

## Deploy-Risiken

- Nur Web, keine Migration, kein Dienst. Beide Einstellungen werden beim Web-Start geseedet.
- **Gate an ab Deploy:** alle nicht freigegebenen HauptFA verschwinden aus der IDEAL-Kommissionierliste. Prüfungen im Manifest (Phase Nachher,
  nur Hinweis): `Pruefung/KommissionierlisteNichtFreigegeben` (Zahl), `…LeitstandAus`, `…VorfilterZiele`. Vor dem Deploy auf Test Q-A/Q-B/Q-C der
  Spec laufen lassen; Gerald entscheidet (Freigabe-Antwort 2): im Leitstand freigeben oder Schalter vorübergehend aus.
- Abweichung von der Spec: Prüfung NichtFreigegeben bewusst **Nachher** statt Vorher (Vorher > 0 bricht Prod bei jedem Update ab).
