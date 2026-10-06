---
type: uebersicht
title: "Beschichtungsauftrag-Ausbau — Uebersicht (Split in 4 Teile)"
slug: 2026-10-06-beschichtungsauftrag-ausbau-uebersicht
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
source_backlog: "[[2026-10-06-beschichtungsauftrag-ausbau]]"
teile:
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec]]"
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]]"
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]]"
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec]]"
open_questions: []
---

> **Umsetzungsort (Hinweis, keine Entscheidung):** jeder Teil nach Schranke 2 des IDEAL-Buendels
> (`feature/2026-08-07-ideal-teile-1-5`, v1.47.1), aus main in einem eigenen neuen Worktree. SQL-, TS-Nummer
> und Version jeweils naechste freie zum Umsetzungszeitpunkt (SQL in beiden Zweigen pruefen: heute Buendel
> `SQL/93`, main `SQL/88`).

Quelle: [[2026-10-06-beschichtungsauftrag-ausbau]] (`split: true`). Standort aller Teile: **IDEAL**
(Datenbasis `FaHierarchyNode`/`FaHierarchyOrderInfo`). Baut auf [[2026-07-29-standort-ideal-teil-4-spec]]
und [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]] auf; uebernimmt
[[2026-09-17-druck-briefkopf-logo-adresse]].

## Teile in Reihenfolge

| # | Spec | Zweck (eine Zeile) | depends_on | Migration |
|---|------|--------------------|------------|-----------|
| 1 | [[2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec]] | Summierte Sicht der heutigen Beschichtungs-Ansicht (Muster Kommissionierliste summiert) | — | nein |
| 2 | [[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]] | Gemeinsamer Briefkopf-Partial (Logo, Absender, Empfaenger) fuer Druck = PDF | — | nein (ja nur bei Logo-Upload in die DB) |
| 3 | [[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]] | Gespeichertes Objekt: Nummernkreis, Kopf, Positionen (Schluessel + Snapshot), Status-Automat mit Audit — ohne Oberflaeche | — | ja |
| 4 | [[2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec]] | Oberflaeche: „Aus Vorschlagsliste erstellen", „Reklamation", Auftragsliste, Druck/PDF, Dienstleister aus Kreditoren | Teil 3, [[2026-10-06-kreditoren-stammdaten-spec]] | ja |

## Abhaengigkeiten (begruendet)

- **Teil 1 → keine.** Reine Lese-Sicht auf bestehende Daten.
- **Teil 2 → keine Pflicht.** Der Partial haengt an der bestehenden `Print.cshtml`. Ist Teil 1 schon gemergt,
  bindet Teil 2 auch dessen Summiert-Druck ein, sonst holt Teil 1 das nach.
- **Teil 3 → keine (insbesondere nicht Teil 1).** Teil 3 speichert Positionen je Strukturknoten — das braucht
  der Doppelvergabe-Guard. Summierung ist Darstellung und betrifft erst den Auftragsdruck in Teil 4.
- **Teil 4 → Teil 3 + Kreditoren-Spec (hart).** Weich: Teil 2 (Briefkopf im Aussendokument, Teil-4-Rueckfrage 5)
  und Teil 1 (falls der Auftragsdruck summiert, Teil-4-Rueckfrage 6). Die Kreditoren-Spec entsteht parallel;
  solange sie nicht existiert, kann der Status-Automat die Abhaengigkeit nicht aufloesen.

Empfohlene Merge-Reihenfolge: 1 → 2 → 3 → (Kreditoren) → 4. Teil 1 bis 3 sind untereinander tauschbar.

## Verteilung der Klaerungspunkte der Notiz

| Notiz-Punkt | Thema | Teil (Rueckfrage) |
|---|---|---|
| 1 | Summierschluessel | Teil 1 (1) |
| 2 | Anpassbare HTML-Vorlage, Logo-Quelle | Teil 2 (1) |
| 3 | Nummernformat | Teil 3 (1) |
| 4 | Status-Automat, Reklamation, wer darf | Teil 3 (2, 3) |
| 5 | Doppelvergabe | Teil 3 (4) Regel, Teil 4 (1) Darstellung |
| 6 | Freigabe-Schritt — **out of scope**, nur nicht verbauen | Teil 3 (6) |
| 7 | Vorgemerktes — **out of scope**, nur nicht verbauen | Teil 3 (7) |
| 8 | Kombigeraete, mehrere Dienstleister/RAL | Teil 3 (5) |

## Querschnitts-Befunde (am Code belegt, gelten fuer mehrere Teile)

- **Cache-Ids sind nicht stabil.** Der Sync loescht und befuellt `FaHierarchyNodes`/`FaHierarchyOrderInfos`
  bei jedem Lauf neu (`FaHierarchySyncService.cs:131-136`). Alles Gespeicherte (Teil 3/4) referenziert per
  Herkunftsschluessel + Snapshot, nie per FK auf den Cache.
- **RAL, Dienstleister, Termine** liegen nur am Auftragskopf (`FaHierarchyOrderInfo.cs:27-34`), werden nur vom
  Sync aus der Sage-View FAInfos geschrieben (`FaHierarchySyncService.cs:282-288`) und im WMS nur gelesen.
- **Firmenname/Firmenanschrift** werden gepflegt, aber von keiner Druckansicht gelesen; `AppSettings.Value` ist
  auf 500 Zeichen begrenzt (`ApplicationDbContext.cs:702`) — kein Platz fuer ein Logo als Data-URI.
- **Rolle `beschichtungsauftrag` ist reine Lese-Ebene** (`RequireBeschichtungsauftragAccessAttribute.cs:7-12`);
  schreibende Actions brauchen einen Edit-Split (Teil 3 entscheidet, Teil 4 setzt um).
- **Ungepruefte Sage-Fakten** (Pruefabfragen in den Teilen): Eindeutigkeit des Positionsschluessels
  (Teil 3), Positionseinheit hinter `Sollmenge` (Teil 1), Filterkriterium der FAInfos-View (Teil 3),
  Nebenbefund „Dienstleister? · RAL 0" (Teil 2).

## Abweichungen vom Schnitt der Notiz

1. **Teil 3 ohne Oberflaeche.** Die Notiz nennt fuer Teil 3 nur Objekt, Nummernkreis, Status und Audit; Teil 4
   ist „Oberflaeche". Teil 3 ist deshalb ein dunkler Backend-Merge (Modell, Migration, Workflow, Tests). Auch
   Liste/Detail/Statusbuttons der gespeicherten Auftraege liegen in Teil 4 — sonst waere Teil 4 nur die
   halbe Oberflaeche.
2. **Kreditor-Fremdschluessel in Teil 4, nicht in Teil 3.** Die Kreditor-Tabelle existiert erst mit der
   Kreditoren-Spec; Teil 3 haelt den Dienstleister als Text-Snapshot, damit Teil 3 nicht von ihr abhaengt.
3. **Zusaetzliche Rueckfragen** ueber die 8 Notiz-Punkte hinaus, weil der Code sie nicht entscheiden kann:
   Teil 1 (Standardansicht/Druck), Teil 2 (offene Fragen 1, 3, 4, 5 aus dem uebernommenen Briefkopf-Backlog
   plus Feldtyp Anschrift), Teil 3 (Snapshot/Menge, Termine), Teil 4 (Auswahl, Reklamations-Details, Kreditor-
   Pflicht, Briefkopf-Pflicht, Druckdarstellung, Bearbeitbarkeit, Entwurfsdruck).
