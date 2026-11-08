---
type: changelog
version: 1.90.0
date: 2026-11-08
---
# v1.90.0 — Lager-Tools: FA-Prüfkarte je Pflicht

Bündel `feature/2026-08-07-ideal-teile-1-5`, Lauf [[2026-11-08-nachbesserungen-vor-buendel-merge-goal-lauf]]. Spec [[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]].
Commits `5d4df4be`. Nicht gemergt (Schranke 2).

## Kernpunkte

- Q-1 `api/stock/fa-lookup?art=` liefert `pflicht` (Regel `FaBuchungsText.IstPflicht`); Karte: mit Pflicht „FA nicht eindeutig – Buchen erst mit gültigem FA möglich.“, sonst der bisherige Text.
- Q-2 Link-Kontrast (`alert-link`, seit `d7180d63`).

## Deploy-Risiken

Nur Web, keine Migration. Strg+F5 (geändertes `fa-feld.js`, per `asp-append-version` versioniert).
