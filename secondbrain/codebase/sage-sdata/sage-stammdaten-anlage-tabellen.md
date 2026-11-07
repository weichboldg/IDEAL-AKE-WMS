---
type: codebase-karte
updated: 2026-11-06
quelle: "Recherche (Claude Research) 2026-11-06 – Quellen: Anhang Datenbankschema Office Line Evolution 2010 (Auszüge), Datenbankdokumentation Sage 100 9.0.7.2 (desk-firm), Sage-Onlinehilfe, IAS-Leitfaden Lagerplätze, sage-forum.de, erp100.rocks, TerraCom. Sage-Version laut Gerald: 9.0.10. NICHT am Server geprüft."
---
# Sage-Tabellen für Lagerstamm und PPS-Mitarbeiter (Anlage aus dem WMS)

Bezug: Spec [[2026-11-06-sage-sdata-stammdaten-sync-spec]] F11/G5 (Outbox). Alles unten ist **Recherche**, nicht am Server belegt; die
Prüfabfragen am Ende entscheiden.

## Lagerstamm

- `KHKLagerorte` – Schlüssel vermutlich (Mandant, Lagerkennung varchar 10); Lagertechnik (→ `KHKLagertechnikenArten`), AnzahlDimension1–3,
  AnzahlLagerplaetze, IstDispoLager, IstFremdlager, IstKommissionslager.
- `KHKLagerplaetze` – Schlüssel (Mandant, PlatzID long); Lagerkennung, Dimension1–3, Dimensionsebene (0 = Basisplatz = das Lager selbst),
  Kurzbezeichnung `Lagerkennung;D1;D2;D3` (z. B. `LL;2;28;0`), Platzbezeichnung; Maße/Sperren seit 9.0.5 („Stammdatenbereich Lagerplätze“).
- Plätze entstehen in Sage als **Matrix** je Lagertechnik (Hochregal 4 Dim., strukturiertes/unstrukturiertes Flächenlager, Blocklager);
  **nachträgliches Verkleinern nicht möglich**, Sperren statt Löschen (IAS).
- Abhängig: `KHKLagerplatzbestaende`, `KHKLagerplatzbuchungen(+Jobs)`, `KHKArtikel.PlatzID` (Hauptlagerplatz), `KHKArtikelVariantenLagerplatz`,
  `KHKLagerplaetzePermanenteIV`, Chargen/SerienNr- und BDE-Stempel-Tabellen.
- **PlatzID-Vergabe offen:** IDENTITY oder Tan (`KHKTan`, `spSysTan`)?

## PPS-Mitarbeiter

- Keine eigene `KHKPpsMitarbeiter` gefunden – Daten in **`KHKMitarbeiter`** (Schlüssel Mandant + Nummer varchar 20, frei vergeben, keine Tan belegt);
  Produktionsfelder Kartennummer, Schichtplannummer, Pausenabzug, Kostenstelle, Kostenträger; Gruppe → `KHKGruppen` Typ 40046; UserName → Sage-Benutzer.
  DPS-Ressource `PpsMitarbeiterInfo` ist vermutlich eine Sicht darauf (am AKE-Test 0 Einträge).
- Wawi- und PPS-Mitarbeiter teilen den Stamm.

## Anlagewege

| Weg | Lagerplätze | Mitarbeiter |
|---|---|---|
| Offizielle REST-API (Kunden, Lieferanten, Artikel, Belege) | nein | nein |
| DPS-Service (CommonWawiServices erweitern) | ja, wenn beauftragt | ja, wenn beauftragt |
| Business Objects / eigener AppServer-Dienst | Anlage-Methode nicht belegt | nicht belegt |
| Importer (Desk, System AG) | nicht gelistet | nicht gelistet |
| Direkter SQL-INSERT | **abgeraten** (Matrix, Zähler, Tan) | **vertretbare Ausnahme** unter Bedingungen (Recherche §5: Prüfabfragen ohne Identity/Tan/Trigger, alle NOT-NULL gefüllt, Referenzen validiert, eigener SQL-Login nur KHKMitarbeiter, Test im AKE-Test, Schema-Vergleich nach jedem Update, DPS informiert) |

**Empfehlung der Recherche:** Lagerplätze in Sage anlegen, WMS liest (Sage führend); DPS-Services für „Mitarbeiter anlegen/ändern“ und „Lagerplätze
erweitern“ anfragen; Mitarbeiter übergangsweise per kontrolliertem SQL nur nach Prüfung.

## Vor Ort prüfen (nur lesend, Gerald)

Vollständige Abfragen im Recherchebericht §6 (Chat 2026-11-06). Kern: `INFORMATION_SCHEMA.COLUMNS` (NOT NULL/Defaults), Schlüssel/Indizes,
`sys.foreign_keys`, `sys.identity_columns`, `KHKTan WHERE Tabelle IN ('KHKLagerplaetze','KHKLagerorte','KHKMitarbeiter')`, `OBJECT_DEFINITION('spSysTan')`,
`sys.triggers`, `KHKLagertechnikenArten`, Muster an `LL`; dazu ein SQL-Trace, während in Sage ein Lagerplatz und ein Mitarbeiter angelegt werden.

## Befund am AKE-Test 2026-11-06 (SData `LagerplaetzeInfo`, alle 3.299 Plätze, nur GET)

- PlatzID 2 … 4.378, aber nur 3.299 Plätze → rund 1.080 IDs nicht (mehr) vergeben; **eine fortlaufende Nummer über alle Lager**, Lücken werden
  nicht wiederverwendet (spricht für Tan/Identity, nicht für „max+1 je Lager“). Kurzbezeichnung eindeutig (0 Dubletten).
- Je Lager genau **ein Basisplatz** (Dimensionsebene 0); Einzellager (MAS, CHE, ELE …) bestehen nur aus dem Basisplatz.
- Lager mit Struktur: HR 1 + 1.996 Plätze (Ebene 3, max 10×10×31 = 3.100 → **Matrix nicht voll**), GL 1 + 1.088 (16×4×17 = 1.088, voll),
  LL 1 + 68 (Ebene 2, 2×34), SL 1 + 100 (Ebene 2, 10×10).
- HR, GL und SL haben **PlatzID-Blöcke mit Lücken, verschränkt mit anderen Lagern** → Sage hat diese Lager nachträglich erweitert und dabei neue IDs
  aus dem gemeinsamen Zähler vergeben. HR zeigt außerdem, dass Einzelplätze einer Matrix fehlen dürfen.
- Folgerung (Deutung): Ein einzelner zusätzlicher Platz innerhalb der Lagertechnik ist datenmäßig nichts Ungewöhnliches. Entscheidend für einen
  SQL-INSERT sind (1) der ID-Zähler, (2) Format `Lagerkennung;D1;D2;D3` + Dimensionsebene passend zur Lagertechnik, (3) die Zähler in
  `KHKLagerorte` (AnzahlDimension1–3, AnzahlLagerplaetze), (4) Trigger. (1), (3), (4) nur per SQL prüfbar.
- Quellenlage: Keine öffentliche Quelle beschreibt INSERTs in `KHKLagerplaetze`; Forum (2023) rät nur, PlatzIDs vor der ersten Buchung „in eine Logik
  zu bringen“ ([sage-forum: Lagertechniken](https://www.sage-forum.de/threads/funktionale-unterschiede-bei-lagertechniken.4110/)).
