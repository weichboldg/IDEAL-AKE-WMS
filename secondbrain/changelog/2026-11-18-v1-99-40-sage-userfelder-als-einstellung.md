---
type: changelog
version: 1.99.40
date: 2026-11-18
---
# v1.99.40 — Sage-Benutzerfelder als Einstellung

Bündel `feature/2026-08-07-ideal-teile-1-5`, Basis `2ae5d1da`, Commit `127f7e3f`. Goal-Lauf [[2026-11-18-goal-lauf-userfelder-einstellung]],
Spec [[2026-11-18-sage-userfelder-als-einstellung-spec]], ADR [[0033-sage-benutzerfelder-als-einstellung]]. Nicht gemergt.

## Kernpunkte

- **27 Service-Einstellungen** `Sync:Userfeld:<Abgleich>:<Standardname>` für Kreditoren, Debitoren, Artikel, Offene Bestellungen, Arbeitsplätze,
  Blechfertigung und FA-Struktur.
  - Default = bisheriger Name → AKE liest zeichengleich (per Test belegt).
  - Leer = nicht angefragt, Anzeige „nicht konfiguriert“.
- **Fehlermeldungen:** „Ungültiger Spaltenname“ nennt die Einstellung. HTTP 404 einer SData-Resource: eine Meldung, danach ist der Schritt 24 h pausiert.
- **Anzeige:** Bestellvorschläge-Spalte heißt „Mindestbestand (Sage)“. BF-Aufträge zeigen einen Hinweis-Kasten bei nicht konfigurierten Feldern.
- Fallstrick 51.

## Deploy-Risiken

- Keine Migration. Web + Dienst. Einstellungszeilen entstehen erst beim Speichern; ohne Zeile gilt der Default.
- **IDEAL:** Laut Manifest `v1.99.40-Userfeld-EKMindestbestellwert-IDEAL` den Kreditoren-Mindestbestellwert leer setzen, sonst bricht der
  Kreditoren-Abgleich weiter ab. Den Artikel-Lieferanten-Abgleich aus lassen (die Resource fehlt am Server).
- Die 404-Sperre liegt im Speicher: Ein Dienst-Neustart setzt sie zurück.

Tests bestanden: Web 3254 (1 übersprungen), Dienst 778 (davon 9 neue `SageUserfelderTests`). Testszenario TS-161.
