---
type: changelog
version: 1.99.16
date: 2026-11-13
---
# v1.99.16 — Update-Skript: zweiter Trockenlauf nach Altbestand, keine Endlosschleife bei leerer Eingabe, DEV-Ziele

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commit `cb204c96`. **Testbereit** (QA 2026-11-13, DEV AKE+IDEAL, Volllauf mit synthetischer Sperre). Spec [[2026-11-13-updater-zweiter-trockenlauf-altbestand-spec]],
Bug [[2026-11-13-updater-zweiter-trockenlauf-altbestand-bug]]. Nicht gemergt, nicht gepusht; /goal-Lauf [[2026-11-13-buendel-v1-99-16-goal-lauf]].

## Kernpunkte

- **Altbestand-Skript (v1.88.0):** Die Wiederholungssperre gibt im Trockenlauf „bereits überführt“ + `TREFFER: 0` aus; der Scharflauf bricht
  weiter mit `RAISERROR` ab. Fehlwarnung „Trockenlauf danach TREFFER: “ nach jedem erfolgreichen Scharflauf entfällt.
- **Update-Skript:** Warnung des zweiten Trockenlaufs nennt fehlende `TREFFER`-Zeile und Exit-Code („Scharflauf ist übernommen“).
  Spur-Abfrage scheitert → „entfällt“ statt Abbruch am Haltepunkt (Freigabe-Antwort 1).
- **Leere/geschlossene Eingabe:** Firma-/System-Auswahl (Update- und Install-Skript) bricht ab („Nichts gestoppt, nichts geaendert.“, Exit 1)
  statt endlos „Ungueltig.“ zu schreiben. `Read-Ja` mit Leer-Default; bei `-NurPruefen` gilt leer bei den zwei rein lesenden Fragen als ja.
- **`ziele.psd1`:** DEV-Ziele AKE-DEV (`D:\Dev\AKE\…`, Port 8081) und IDEAL-DEV (`D:\Dev\IDEAL\…`, Port 8082), `Prod = $false`, belegt aus IIS
  und Diensten.
- **Drift-Tests:** Sperr-Regel (`SperrBloecke`) und Auswahl-Schleifen-Regel, je mit Rotprobe; Regel zusätzlich in `spec-agent.md` und
  der Spec-Vorlage (Freigabe-Antwort 3).

## Migration / SQL

Keine Migration. Geändert nur `SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql` (Trockenlauf-Zweig); bestehende
Marker bleiben gültig (`Get-Marker` liest nur `ManifestId`).

## Deploy-Risiken

Web + Dienst nur wegen der Version (das Update-Skript vergleicht die Fußzeile mit der Paketversion). Scharfer Hand-Lauf (TS-133.9) nur
Gerald (ADR 0023). Altes `publish\deploy.zip` vom 12.11. nicht weitergeben.
