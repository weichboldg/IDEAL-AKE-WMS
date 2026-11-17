---
type: adr
id: 0030
title: BDE-Teileliste – Teilmeldung mit fachlichem Schlüssel neben WorkOperation
status: accepted
date: 2026-11-17
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]] (v1.99.32, Freigabe-Antworten 1–8 = Empfehlung, Erkennung V1). Commits `6704fe2a` … `c7c968fe`.

## Kontext und Problem

- Die Blechfertigung (IDEAL) meldet Teile (Blätter der FA-Struktur) und Baugruppen, nicht ganze Sub-FA-Arbeitsgänge. Blätter haben keine
  `WorkOperation`.
- `FaHierarchyNode.Id` ist kein stabiler Schlüssel: der Struktur-Sync erneuert die Knoten.
- Die Struktur-Erkennung legte für Blatt-Kürzel einen Vater-AG an (P-TL3 auf IDEAL TEST: IF 4, KA 120, SM 6, ST 120, davon 0 gebucht).

## Entscheidung

- **Eigene Tabelle `BdeTeilMeldungen`** mit fachlichem Schlüssel (HauptFA, VaterFA, SubFA, Position, Artnr) + Kürzel statt Node-Id;
  Doppelschutz durch gefilterten UNIQUE `WHERE IsCancelled=0` plus `sp_getapplock` (Ressource = SHA256 des Schlüssels, Längengrenze 255).
- **Wirksam** nur bei `BdeAktiv && BdeTeileliste && !BdeNurFaMeldung` und gesetztem Kürzel an der Werkbank (`ProductionWorkplaces.BdeTeileliste`).
- **Baugruppe** meldet zusätzlich den eigenen Sub-FA-AG über `BdeBookingService.ReportCompletionCoreAsync`; **Blatt** ist nur Teilmeldung.
  Nichts geht an Sage.
- **Erkennung V1:** Für Kürzel von Teileliste-Werkbänken erzeugen Blatt-Kürzel keinen Vater-AG mehr (Count `teileliste_blatt_uebersprungen`);
  Altbestand per Einmal-Skript (Trockenlauf-Default, nur ungebuchte, nur `CreatedBy='WorkOperationStructureDetection'`).
- **Rücknahme** bis Schichtende, ohne Schichtkalender (IDEAL, P-TL5) bis Ende des Meldetags; Admin-Storno mit Pflichtgrund.
- Keine Kürzel-Konstanten im Code (Wächtertest AK 19).
- **Bewusste Ausnahme von ADR [[0001-repository-pattern-mit-decorator-fuer-caching]]:** `BdeTeilelisteService` nutzt `ApplicationDbContext`
  direkt (neue Tabelle + Struktur-Lesungen in einer Transaktion mit applock).

## Folgen

- Doppelschutz (N1) nur im Manual-UAT prüfbar (InMemory kennt weder gefilterten UNIQUE noch applock).
- Waisen: Meldungen, deren Schlüssel nach einem Struktur-Sync nicht mehr existiert, werden als Hinweis gezeigt, nicht gelöscht.
- Eine spätere Rückmeldung an Sage ([[2026-10-30-fa-rueckmeldung-an-sage]]) kann auf demselben Schlüssel aufsetzen.
