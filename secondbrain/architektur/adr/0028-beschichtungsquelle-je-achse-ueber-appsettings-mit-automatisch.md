---
type: adr
id: 0028
title: Beschichtungsquelle je Achse (Kennzeichen, Termin) über AppSettings mit „Automatisch“, aufgelöst an einer Stelle
status: accepted
date: 2026-11-16
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-16-beschichtungslogik-je-standort-spec]] (v1.99.25, Freigabe-Antworten 1–18, Nachtrag 2). Gebaut: E1 (`c61b7a52`),
> E3 (`318abf1f`); E2/E4/E5 warten auf Haltepunkte H1–H3.

## Kontext und Problem

Woher „beschichtet“ und der Beschichtungstermin kommen, war implizit an den Master `ProduktionsauftragHierarchisch` gebunden: AKE (flach)
= Artikelkategorie `LackierteilKategorieName` über BomCache + berechneter Termin (`CoatingDateCalculator`, Abholtage); IDEAL
(hierarchisch) = FA-Struktur `FaHierarchyNode.Beschichtet` + `Start_Beschichtung` aus der Sage-View FAInfos. Jede Liste rechnete selbst,
die FA-Liste mit einer kopierten Formel. Features (Beschichtungsauftrag an der AKE, IDEAL-Termin in Leitstand/Arbeitsliste) hingen damit
am Standort statt an der Datenquelle.

## Betrachtete Optionen

1. **Ein Schlüssel „Beschichtungslogik“** (OSEON | View) – koppelt Kennzeichen und Termin, obwohl sie unabhängig wählbar sein sollen.
2. **Zwei Schlüssel je Achse, Default je Standort per Seed** – Verhaltenswechsel, wenn der Seed am falschen System landet.
3. **Zwei Choice-AppSettings mit Default „Automatisch“ = folgt dem Master**, aufgelöst durch eine reine Funktion für Web und Dienst.

## Entscheidung

Option 3. `BeschichtungKennzeichenQuelle` (Automatisch | Artikelkategorie | FaStruktur) und `BeschichtungTerminQuelle`
(Automatisch | Berechnet | View) in `/Settings` (AppSettings, weil Web und Dienst lesen). `BeschichtungResolver.ResolveQuellen`
(`IdealAkeWms/Services/Beschichtung/BeschichtungResolver.cs`) ist die einzige Auflösung; der Dienst liest die Rohwerte über
`IHierarchicalModeReader.GetBeschichtungQuellenAsync`.

- Kombination ohne Datenbasis (Artikelkategorie bei Master an, FaStruktur oder View bei Master aus) lehnt `/Settings` beim Speichern ab;
  wird sie zur Laufzeit gefunden, gilt sie als „Automatisch“ und wird als Warnung protokolliert (ADR 0020 Punkt 2).
- **Genau ein Dienstlauf schreibt `HasCoatingParts`:** Lackierteil-Erkennung bei Artikelkategorie, FA-Materialisierung bei FaStruktur.
- Kein Wert `SData`; `USER_OSBeschichtet` ist dokumentierter Erweiterungspunkt der **Kennzeichen**-Quelle (Freigabe-Antwort 17).

## Konsequenzen

- Defaults ändern nichts: Automatisch = heutiges Verhalten je Standort.
- Die Listen lesen die Termin-Quelle erst mit E2/E4 (blockiert durch H1); bis dahin ist `BeschichtungTerminQuelle` „noch ohne Wirkung“
  (Beschreibung sagt das).
- Manifest-Prüfung `Pruefung/BeschichtungQuelleKombination` meldet unpassende Bestandswerte beim Update (nur Hinweis).
- Der Sage-Import-Hook läuft seit v1.99.25 nur mit `Sync:CoatingDetectionEnabled` (Freigabe-Antwort 12) – siehe [[fallstricke]] §39.
