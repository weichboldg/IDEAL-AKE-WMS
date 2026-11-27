---
type: changelog
version: 1.99.34
date: 2026-11-17
---
# v1.99.34 — Beschichtung an der AKE: Vorschlagsliste und Beschichtungsauftrag aus der WA-Stückliste

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commit `eff0c1a4` (Code, Tests, SQL/157, Manifest, Version, Doku, TS-145). Spec
[[2026-11-16-beschichtungslogik-je-standort-spec]] Etappe 5 + Nachtrag 2/3. Nicht gemergt. Lauf-Notiz [[2026-11-17-goal-lauf-beschichtung-e5]].

## Kernpunkte

- **Flache Positionsquelle** `Services/Beschichtung/BeschichtungFlacheQuelle.cs`: aktiv, wenn die Kennzeichen-Quelle aufgelöst
  `Artikelkategorie` ist (`BeschichtungResolver.ResolveQuellen`, Master `ProduktionsauftragHierarchisch` aus). Offene WA = Haupt-FA,
  `!IsDone && !IsCancelled && PickingStatus.HasCoatingParts` (kein `IsDonePicking`-Filter); Zeilen = Stücklisten-Cache des WA-Artikels mit
  Artikelkategorie = `LackierteilKategorieName` (`IBeschichtungsauftragRepository.GetFlacheLackierteileAsync`). Projektion als
  `FaHierarchyNode` im Speicher: HauptFA = WA-Nummer, VaterFA null, SubFA 0, Position null, `PositionText` getrimmt (nie null), Maße 0,
  Sollmenge über `BomQuantityResolver` (Menge × WA-Menge wie Kommissionierliste).
- **Verbraucher:** `BeschichtungsauftragService` (Liste/Summiert, keine Kopfzeilen an der AKE), `BeschichtungsauftragVergabeAnzeige` (nur die WA
  der Seite), `BeschichtungsauftragWorkflow` (Anlegen, Hinzufügen, eigener Fehlertext „Für WA … keine offenen Lackierteil-Positionen“).
  IDEAL-Wege unverändert.
- **Positionsschlüssel** `PositionsSchluessel(VaterFA, SubFA, Position, Artnr, PositionText)` über `Bilden` (normiert wie
  `PickingOrderRestmengen.Schluessel`: Artnr trim+upper, Position trim). `PositionText` null = IDEAL-Schlüssel dreiteilig wie bisher.
  Textform im Formular fünfteilig `Vater|SubFA|Pos|Artnr|PositionText`, Teile 4/5 URL-kodiert.
- **Sortierung:** `ThenBy(PositionText, NaturalPositionComparer)` in Liste, Summiert, Details, Reklamation.
- **Sammelmeldung:** offene WA mit Kennzeichen ohne Lackierteil-Zeile → `alert-warning` auf Index/Summiert (höchstens 20 Nummern) + Log-Warnung.
- **AKE-Kopf:** Start = `StartBeschichtungAsync` (Fertigungsdatum − KommissionierTage − Vorkommissioniertage inkl. Werkbank-Override, dann
  `BeschichtungResolver.Bestimmen`); Dienstleister/RAL/Retour leer.
- **Namen:** „(IDEAL)“ aus Seitentiteln, Druck-Titeln, Spaltenkatalog, Hilfe-Karte, Rollenübersicht. Gate `FaHierarchyBeschichtungAktiv`:
  Anzeigename „Beschichtungsauftrag aktiv“, Gruppe Termine und Beschichtung, Standort beide.

## Migration / SQL

- **SQL/157** `157_AddBeschichtungsauftragPositionText.sql`, MigrationId `20261117141308_AddBeschichtungsauftragPositionText`: Spalte
  `BeschichtungsauftragPositionen.PositionText nvarchar(50) NULL`, additiv, kein Backfill, `COL_LENGTH`-Guard, History separat, FreshInstall an
  beiden Stellen. AgentJobs nicht betroffen.
- Manifest: `SQL/157` (Braucht SQL/94), `Pruefung/BeschichtungPositionOhneTextFlach`, `v1.99.34-Beschichtungsauftrag-AKE` (Einstellung, AKE).

## Deploy-Risiken

- Gering: additive Spalte; an der AKE ohne Gate keine Wirkung. Einschalten an der AKE nur nach Abstimmung (Rollen zuweisen).
- Altzeilen ohne `PositionText` an einem flachen System würden nicht als vergeben erkannt — nur möglich, wenn vor v1.99.34 an der AKE Aufträge
  entstanden; die Prüfung meldet es.
