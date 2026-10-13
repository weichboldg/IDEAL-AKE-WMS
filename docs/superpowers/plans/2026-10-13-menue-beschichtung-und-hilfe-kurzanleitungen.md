# Menü „Beschichtung“ + Kurzanleitungen in der Hilfe — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans (native, ein Lauf). Steps use checkbox syntax.

**Goal:** Zwei Top-Level-Menüpunkte der Beschichtung → ein Dropdown „Beschichtung“ (Vorschlagsliste, Aufträge); Titel der
Vorschlagslisten „Vorschlagsliste Beschichtung (IDEAL)“; Hilfe-Karte „Beschichtung (IDEAL)“ mit Kurzanleitung; Kurzanleitungen
für Kürzel im FA-Baum und CSV-Export.

**Architecture:** Nur Razor-Markup (`_Layout.cshtml`, `Help/Index.cshtml`, zwei Vorschlagslisten-Views) + Quelltext-Wächter in
`BeschichtungsauftraegeViewTests`. Keine Logik, kein Model, keine Migration.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen-spec.md`
(Rumpf + Antworten 1–4 + FREIGABE-NACHTRAG: zwei Einträge, keine Trennlinie).

## Global Constraints

- Direkt im Bündel, Start `627c992a` (v1.55.0) → Version **1.56.0**, **TS-89**; Web-App nicht lokal starten.
- Bedingung `hasBeschichtungsauftragAccess && faBeschichtungAktiv` und Variablen wortgleich; Rollen/Filter/Routen unverändert.
- Druck-/PDF-Views unverändert; Changelog-Altbestand unverändert.
- Hilfe: jede Aussage durch TS-81/82/84/85/86/88 oder Bestandshilfetext gedeckt; Rollen-Schlüssel/Schalter-Name nur im Absatz
  „Einrichtung (Admin)“; `@` als `@@`; Build „0 Fehler“ vor Commit.

## Review Focus

1. Dropdown-Active auch auf `FaHierarchyBeschichtung/Index` (Liste) und Auftragsdetail/Reklamation → Wächter prüft Controller-Namen.
2. Keine Rest-`nav-link` auf die beiden Controller (Wächter).
3. Kein Inhaltsverlust beim Zusammenführen v1.48/v1.49/v1.51 → Abgleich Satz für Satz.
4. Kürzel-Abschnitt hat seit v1.55.0 Status-/Soll-Punkte — zusammenführen, nicht doppeln.
5. Razor `@` im Hilfetext.

### Task 1: Layout-Dropdown + Wächter (TDD: Wächter zuerst rot)
### Task 2: Seitentitel der zwei Vorschlagslisten-Views
### Task 3: Hilfe — Karte „Beschichtung (IDEAL)“, Kürzel-, CSV-Kurzanleitung + Hilfe-Wächter
### Task 4: Version 1.56.0, Changelog, TS-89, TS-81.2/81.13/84.14, Brain, QA
