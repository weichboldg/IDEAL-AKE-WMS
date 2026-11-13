---
type: bug
title: "Update-Skript: zweiter Trockenlauf des Altbestand-Skripts meldet „TREFFER: (leer)“"
status: offen
severity: niedrig
created: 2026-11-13
affected_code:
  - "scripts/deploy/Update-IdealAkeWms.ps1:582-583 (Fluss A, zweiter Trockenlauf; Exit-Code wird nicht ausgewertet)"
  - "SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql:47-51 (Wiederholungssperre bricht auch im Trockenlauf mit RAISERROR 16 ab)"
  - "IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs:132 (prüft nur, ob irgendwo 'TREFFER: ' steht)"
source_backlog: "[[2026-11-13-updater-zweiter-trockenlauf-altbestand]]"
spec: "[[2026-11-13-updater-zweiter-trockenlauf-altbestand-spec]]"
---

## Symptom

Deploy AKE Test-IDEAL 13.11.2026 (Update-Skript v1.99.12): Nach dem Scharflauf des Altbestand-Skripts erscheint die Warnung
„Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand: zweiter Trockenlauf TREFFER:  (erwartet 0).“ Der Wert ist leer.
Der Scharflauf selbst war erfolgreich. Die Warnung führt also in die Irre.

## Reproduktion

1. System mit Altbestand (PickingItems mit `IsPicked = 1` oder `IsTransferred = 1`), ohne Marker und ohne Spur `CreatedBy = N'Migration-KO-Altbestand'`.
2. Update-Skript bis Schritt 4a: Der Trockenlauf meldet `TREFFER: n > 0`. Backup aus diesem Lauf anlegen, `SCHARF` eintippen.
3. Der Scharflauf committet mit Marker `Scharf` (`Update-IdealAkeWms.ps1:579`, Marker in derselben Transaktion `:382-387`).
4. Der zweite Trockenlauf (`:582`) trifft auf die Wiederholungssperre (`…Altbestand.sql:47-51`), die jetzt greift:
   - `RAISERROR … 16` → `sqlcmd -b` endet mit Exit ≠ 0 (`Get-SqlcmdArgs` `:246`);
   - eine Zeile `TREFFER:` kommt nie (die steht erst bei `:96`);
   - `Get-Treffer` (`:355`) liefert `$null`;
   - `$null -ne 0` ist wahr, deshalb kommt die Warnung mit leerem Wert (`:583`).

## Root Cause

Das Altbestand-Skript unterscheidet in seiner Wiederholungssperre nicht zwischen Trockenlauf und Scharflauf. Fluss A (ADR 0023) erwartet
aber, dass jeder Trockenlauf eine Zeile `TREFFER: n` liefert, auch nach einem erfolgreichen Scharflauf.

Warum Tests und Abnahme das nicht gefunden haben:
- Der Drift-Test prüft nur, ob irgendwo `TREFFER: ` vorkommt (`NachlaufManifestDriftTests.cs:132`).
- TS-126.7 nutzte ein synthetisches Skript ohne Sperre.

Zusätzlich wertet `:583` den Exit-Code nicht aus und schreibt bei fehlender Zeile einen leeren Wert in die Warnung.

## Fix / Verweis

[[2026-11-13-updater-zweiter-trockenlauf-altbestand-spec]]: Sperre im Trockenlauf mit `TREFFER: 0` beenden, Meldung in `:583` ehrlich machen,
Drift-Wächter für Sperren in Trockenlauf-Dateien.
