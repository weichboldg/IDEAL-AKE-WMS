---
type: backlog
title: FA-Struktur, Stueckliste und Rueckmeldungen per SData ins WMS speichern (Kommissionierung + Verbrauchshistorie)
slug: 2026-10-30-sage-sdata-fa-persistenz
status: Idee
created: 2026-10-30
updated: 2026-10-30
prioritaet: hoch
quelle: Gerald, 2026-10-30
bezug: "[[2026-10-30-sage-sdata-fa-ansicht]], [[2026-10-30-fa-rueckmeldung-an-sage]], [[0007-bom-quelle-sage-view-mit-oseon-fallback]], [[0012-fa-hierarchie-einweg-migrationstor]], [[0013-bom-bridge-repository-schnittstelle-statt-cache-kopie]], [[sage-sdata-realtimedata]]"
---

# FA-Struktur, Stueckliste und Rueckmeldungen per SData ins WMS speichern

## Was Gerald will (2026-10-30, wortgetreu sinngemaess)

- Die per SData gelesene FA-Struktur **inkl. Stueckliste im WMS speichern**, damit wir
  (a) **Verbraeuche in der Zukunft sehen** und (b) sie **fuer die Kommissionierung verwenden** koennen.
- Die neue Ansicht "FA-Struktur (Sage live)" gefaellt im ersten Schritt besser als die FA-Hierarchie.
  View-Informationen wie **PPS-Termin, Beschichter** sollen dazugejoint werden.

## Einordnung der Koordination

Das ist "Weg 3" aus der Analyse vom 2026-10-30: SData wird **Quelle eines Sync**, nicht Live-Ersatz. Live je
Seitenaufbau scheidet aus (Sage-Application-Server vertraegt eine Anfrage nach der anderen; mehrere
Kommissionierer = Last + 20-s-Timeouts im Lager). In den AKE-BOM-Cache kippen scheidet nach ADR 0013 aus
(Artikel-Schluessel, Menge x Stueck -> stille Doppelmultiplikation).

### Vorschlag: eigene Tabellen, SData-Sync im Dienst, Bridge an der BOM-Schnittstelle

| Baustein | Vorschlag | Begruendung |
|---|---|---|
| Speicher | Neue Tabellen `SageFa` (Kopf je BelID), `SageFaPosition` (je BelID + Ressource), `SageFaRueckmeldung` (append-only) | Nicht `FaHierarchyNodes`: dessen Zeile ist "Struktur UND Stueckliste in einer Zeile, Full-Refresh, roh aus der IDEAL-View" - andere Granularitaet, kein Verlauf. Eigene Tabellen tragen SData-Feldnamen 1:1 (keine Umdeutung). |
| Verlauf (Verbraeuche) | `SageFaPosition` mit `GueltigVon/GueltigBis` (Aenderung = alte Zeile schliessen, neue oeffnen); `SageFaRueckmeldung` nur anhaengen; Kopf mit Statuswechsel-Zeitpunkten | "Verbraeuche in der Zukunft sehen" braucht Historie; ein Full-Refresh wie bei FaHierarchy loescht sie. |
| Sync | Dienst, serielle Aufrufe, Fenster: offene FAs (`dtsFaUebersicht`/`dtsFaNachStatus`) + alles mit `GeaendertAm` > letzter Lauf (`dtsFaBelegsuche` liefert `GeaendertAm`) | Inkrementell statt alles; schont Sage; `GeaendertAm` ist belegt (Fixture 835). Ob `GeaendertAm` auch bei Positionsaenderungen hochgesetzt wird: **ungeprueft**. |
| Kommissionierung | Dritte Implementierung `SageFaBomRepository : IBomRepository, IBomCacheRepository` hinter der bestehenden Weiche (`BomRepositoryMasterSwitch`) | Exakt das Muster von ADR 0013; Picking-Workflow, Bedarfe, Artikelinfo laufen ohne Umbau. `MengeIstAuftragsmenge = false`, Menge je Einheit x `Sollmenge` des FA ueber `BomQuantityResolver` (einzige Multiplikationsstelle). |
| Join Artikelstamm | Lokale `Articles`-Tabelle (Artikel-Sync) statt Sage | Artikelgruppe, Hauptlagerplatz, Artikeltyp, Masse sind Stammdaten, nicht FA-Daten. |
| Join PPS | Weiterhin `vw_IDEAL-AKE_Kommissionierung_FAInfos` -> `FaHierarchyOrderInfo` (KO_Termin, Dienstleister = Beschichter, RAL, Neuer_PT_PPS) | Gerald: "View-Informationen dazujoinen". Bleibt die einzige View-Abhaengigkeit, bis SData das abdeckt. |
| Anzeige | "FA-Struktur (Sage live)" bekommt einen zweiten Modus "gespeichert" (aus den Tabellen, mit PPS-Join); Live bleibt als Diagnose | Gleiche Ansicht, die Gerald gefaellt; Live-Knopf zeigt Abweichung Sage vs. gespeichert. |
| FaHierarchy | Bleibt vorerst parallel; Abloesung ist eine eigene Entscheidung (ADR 0012 Einweg-Tor) nach Feldparitaet | Nichts abschalten, bevor die Luecken unten geschlossen sind. |

### Feldabdeckung SData gegen `FaHierarchyNode` (Stand Fixtures 835/836, AKE-Test)

Belegt vorhanden in SData:
- Struktur: `dtsFaBelegsuche` - BelID, StrukturID, Artikelnummer, Matchcode, Sollmenge, Fertigungmenge, Istmenge,
  IstEingelastet/Begonnen/Beendet, VerursacherTyp, Soll-Start/-Ende, GeaendertAm, Fertigungsart, Zeichnungsnummer;
  ohne `select` 53 Felder **+ 35 USER-Felder** (IDEAL-Benutzerfelder wie BemerkungPN/FertigungsInfoPN liegen
  vermutlich dort - **ungeprueft**, Feldliste ziehen).
- Positionen: `dtsTeileverwendungsnachweis` - KopfArtikelnummer, Ressourcenummer, Matchcode, Menge (je Einheit),
  Ressourcetyp MA/AG/VS, Arbeitsgang-/Arbeitsplatznummer, Materialteileart, IstBeendet, Dimensionstext,
  Fremdfertiger, MengeNKS, Umrechnungsfaktor (37 Felder).
- Rueckmeldungen: `dtsRueckKorrektur` (MengeGut je BelID), `dtsFertigmeldungen`.

Luecken (aus der IDEAL-View, nicht in SData gesehen):
| Feld FaHierarchyNode | Ersatz |
|---|---|
| `Position` (Sage-Position in der Vater-Stueckliste) | **kein Positionsfeld** in den 37 TVN-Feldern -> Reihenfolge = Antwortreihenfolge; Pfad "3.7.2" (ADR 0013) muss aus laufender Nummer gebaut werden. Kollisionsregel noetig. |
| `VaterFA` | nicht direkt; Ableitung Ressourcenummer = Artikelnummer eines Sub-FA derselben Struktur (P-2: im Testmandanten eindeutig, produktiv **ungeprueft**) |
| Artikelgruppe, Hauptlagerplatz, Artikeltyp, Material, Breite/Hoehe/Tiefe, Beschaffungsartikel, EKBedarf | lokaler Artikelstamm (`Articles`) |
| `Beschichtet`, `Arbeitsbereich`, `Kommissionieren`, `VMBedarf`, `BemerkungPN`, `FertigungsInfoPN` | vermutlich USER-Felder am FA-Kopf oder Artikel-Benutzerfelder - **pruefen** (Feldliste `dtsFaBelegsuche` ohne select, 1 FA) |
| `Arbeitsschritte` | ableitbar aus TVN-Zeilen `Ressourcetyp = AG` (Arbeitsgangnummer/Arbeitsplatz) |
| `Bezeichnung1/2` | `KopfBezeichnung1/2` nur fuer den Kopf; Positions-Bezeichnung ueber Artikelstamm |

### Was Verbrauch hier heisst (zu klaeren mit Gerald)
- Variante a: **Soll-Verbrauch** je Artikel aus den FA-Positionen ueber die Zeit (was die FAs an Material verlangten).
- Variante b: **Ist-Verbrauch** = Rueckmeldungen (`dtsRueckKorrektur` MengeGut, Istmenge) bzw. WMS-eigene
  Kommissionier-/BDE-Buchungen.
- Sage-Materialentnahmen je Position liefert keine der gefundenen dts-Quellen -> Partner-Frage (Block D).

## Pruefabfragen vor der Spec (Gerald, Postman, nur GET)
1. `dtsFaBelegsuche.Sage.PPS?where=BelID eq 835&format=json` **ohne select** -> Liste der 53 + 35 USER-Felder;
   welche davon tragen Beschichtet / Kommissionieren / BemerkungPN / FertigungsInfoPN / Arbeitsbereich?
2. `dtsTeileverwendungsnachweis.Sage.PPS?where=BelID eq 835&format=json` ohne select -> gibt es doch ein
   Positions-/Reihenfolgefeld (z. B. `PosID`, `Zeile`)?
3. Einen FA mit nachtraeglich geaenderter Stueckliste im Testmandanten: aendert sich `GeaendertAm` des Kopfes?
4. Mandant IDEAL: gibt es einen SData-Host fuer die IDEAL-Sage? (offen seit [[sage-sdata-uebersicht]])

## Abhaengigkeiten
- Partner-Antwort Block D (Schreibweg) entscheidet, ob der Sync spaeter zum Rueckmeldekanal wird; fuer das Lesen
  und Speichern ist sie **nicht** Voraussetzung.
- Bundle-Worktree IDEAL Teile 1-5 muss gemergt sein (ADR 0013-Weiche, `FaHierarchyOrderInfo` liegen dort).

## Naechster Schritt
Pruefabfragen 1-2 (zehn Minuten Postman), dann `/spec` fuer Etappe 1: Tabellen + Dienst-Sync + Ansicht
"gespeichert" (ohne Kommissionierung). Etappe 2: `SageFaBomRepository` hinter der Weiche. Etappe 3: Verbrauchsauswertung.
Kandidat fuer `epic: true` (ein Worktree, drei Etappen).
