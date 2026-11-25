---
type: aufgabe
title: Folge-Spec – Fräsen-Pfad in den übrigen Artikel-Listen
status: NEU
spec: "[[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]]"
assignee: agent
blocked_by: []
created: 2026-11-25
updated: 2026-11-25
---
# Folge-Spec: Fräsen-Pfad in den übrigen Artikel-Listen

v1.99.72 zeigt den Fräsen-Pfad nur in den Masken der Stufe 1 (Artikel-Liste, Artikelinfo, Artikel bearbeiten, BDE-Teileliste, Bestandsübersicht,
Lager-Detailseite, Besteller-Seite). Offen für eine eigene Spec (Stufe 2 der Spec [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]]):

- Weitere Listen mit Artikelbezug (z. B. Kommissionierung, Stückliste, Wareneingang, Bestellvorschläge, BF-Aufträge) – je Liste prüfen, ob der Pfad
  dort gebraucht wird; Baustein `_FraesenPfad` + Spaltenauswahl, Standard ausgeblendet.
- Druck der Lager-Detailseite (heute bewusst ohne Pfad).
- Offene Punkte aus der Umsetzung: Lagerbestell-Zeilen nach `Type` filtern (Glas ausschließen?), `current-stock` in Bestände `Locked`.
