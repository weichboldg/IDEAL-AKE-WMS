---
type: adr
id: 0023
title: Daten- und Einmal-Skripte fuehrt das Update-Skript mit Bestaetigung am Zielserver aus; Marker-Tabelle
status: accepted
date: 2026-11-11
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung-spec]] (v1.99.0), Auftrag und Freigabe Gerald 2026-11-11.
> Loest in Teilen [[0015-einmal-datenskripte-ausserhalb-der-nummerierten-sql-reihe]] und
> [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]] ab (siehe „Abgeloest“).

## Kontext und Problem

Der erste Lauf am Zielsystem IDEAL-Test (11.11.2026, [[2026-11-11-buendel-deploy-update-skript]]) hat gezeigt:
- Daten- und Einmal-Skripte startete der Mensch in SSMS, ohne Nachweis, ob sie auf dem System schon liefen (ADR 0015: nur das
  Deploy-Protokoll weiss es).
- SQL-pruefbare Zustaende (Kommissionierbereiche fehlten, RCSI aus) meldete das Skript nur als Hand-Schritt oder gar nicht.

## Betrachtete Optionen

1. Weiter von Hand (ADR 0015/0022 unveraendert).
2. Das Update-Skript fuehrt aus, aber nur nach einer Eingabe des Menschen am Zielserver; ein Marker in derselben DB haelt fest, was lief.
3. Ein unbeaufsichtigter Modus (verworfen: der Mensch soll jede Datenaenderung sehen).

## Entscheidung

Option 2.
- **Mit Trockenlauf-Schalter:** Trockenlauf automatisch, Zeile `TREFFER: n`; 0 = entfaellt; sonst Eingabe `SCHARF` und ein Backup
  aus **demselben** Lauf. Das Skript ersetzt das Literal `DECLARE @Trockenlauf BIT = 1;` nur im Arbeitsspeicher.
- **Ohne Schalter:** die ganze Datei in **einer** SqlClient-Transaktion (GO-Aufteilung), Ausgabe zeigen, dann `uebernehmen`/`verwerfen`.
  `sqlcmd` kann keine Transaktion ueber eine Eingabe offen halten.
- **Marker `dbo.DeployScriptRuns`** (SQL/142, UNIQUE `ManifestId`): Eintrag in derselben Transaktion wie die Datenaenderung; ein Eintrag
  mit Marker wird nie wieder angeboten. Altstand ueber Spur-Abfragen oder einmalige Erfassung („lief schon“, Mode `VonHand`/`Spur`).
- **Pruefungen** (Manifest-Typ `Pruefung`): lesende Abfrage bei jedem Lauf, Reparatur nur nach Bestaetigung und bei gestopptem Web und
  Dienst. Reparaturen bekommen keinen Marker – eine Pruefung bewertet den Zustand jedes Mal neu.

Unveraendert gelten: Ordner `SQL/Einmalig/`, Warnkopf, Zaehlung, Audit-Felder, Reihenfolge im Kopf (ADR 0015); **Agenten fuehren Daten- und
Einmal-Skripte nie scharf aus** (auf keinem System; Tests nur mit synthetischen Skripten gegen eine DEV-Kopie); kein unbeaufsichtigter
Modus, kein Schalter, der Bestaetigungen ueberspringt.

## Abgeloest

- ADR 0015, Entscheidung: „Der Dev-Lauf legt das Skript an, fuehrt es **nie** aus; die Ausfuehrung ist ein Deploy-Schritt des Menschen.“ –
  Der Dev-Lauf fuehrt weiter nie aus; am Zielserver fuehrt das Update-Skript aus, nach der Eingabe des Menschen dort.
- ADR 0015, Konsequenzen: „kein technischer Marker … bei haeufigerem Bedarf neu entscheiden“ – ersetzt durch `DeployScriptRuns`.
- ADR 0022, Entscheidung: Daten-/Einmal-Skripte „nie scharf durch das Skript“ und Konsequenz „Marker-Tabelle ist Folge-Spec“.

## Konsequenzen

- Trockenlauf-Skripte tragen genau ein Literal `DECLARE @Trockenlauf BIT = 1;` und eine Zeile `TREFFER: n` im Trockenlauf-Zweig;
  Skripte fuer die Transaktion enthalten kein `ROLLBACK` und keine `sqlcmd`-Direktiven (Drift-Test `NachlaufManifestDriftTests`).
- Soll ein Skript bewusst ein zweites Mal laufen, bekommt es einen neuen Manifest-Eintrag (z. B. `SQL/110#2`).
- Waehrend der Entscheidung `uebernehmen`/`verwerfen` haelt die Transaktion Sperren; deshalb laufen diese Eintraege vor dem Start bei
  gestopptem Web und Dienst (Phase `VorStart`, Freigabe-Antwort 1).
- `sys.dm_os_file_exists` (Backup-Ordner-Pruefung) braucht SQL Server 2017+; AKESQL20 prueft Gerald vorab (P-B1).
