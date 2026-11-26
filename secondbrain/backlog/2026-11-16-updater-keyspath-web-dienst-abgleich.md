---
type: backlog
status: in-spec
created: 2026-11-16
quelle: Koordination, Befund akenet01:4445 (Gerald, Chat 2026-11-16 10:34)
---
# Update-Skript: DataProtection:KeysPath von Web und Dienst vergleichen

## Befund (4445, v1.99.23)
- Sage-Wareneingang 16.11. 10:00:17: „SData-Kennwort nicht entschlüsselbar“ (Aktivitäts-Protokoll), zwei WE bleiben „offen (wird gesendet)“.
- Ursache (von Gerald ausgelesen): Web `appsettings.Production.json:42` `KeysPath = C:\AKE\keys\dev`, Dienst
  `appsettings.Production.json:48` `KeysPath = D:\Dev\AKE\keys` (DEV-Pfad, offenbar kopiert). Dienst läuft als `svc_idealakewms@ake.at`.
- ADR 0024 „Deploy-relevant“ verlangt gleichen Wert in Web und Dienst; das Update-Skript prüft nur Ordner/Recht
  (`nachlauf.psd1:212-215`, `v1.99.14-DataProtectionDienst`), nicht die Gleichheit der Pfade.

## Vorschlag
- Neue Prüfung im Manifest: `DataProtection:KeysPath` aus Web- und Dienst-appsettings*.json lesen; verschieden → Abbruch/Hinweis mit beiden Werten.
- Prüfung „DEV-Pfad auf Nicht-DEV-System“ (`D:\Dev\`, `localhost`) in den appsettings.
- Betriebshandbuch 07 (Zeile 44) um diesen Fall ergänzen.
- Offen (Gerald): eigener Schlüsselordner je Instanz statt `C:\AKE\keys\dev` (4444/4445 teilen ihn ggf.)?
