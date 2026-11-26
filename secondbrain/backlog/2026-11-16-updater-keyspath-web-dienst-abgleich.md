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

## Zweiter Fall: akenet01:4444 (Befund 2026-11-26, Hauptsession, nur lesend per SQL auf `IDEAL_AKE_WMS_Test`)
- `SyncLogs`: „SData-Kennwort nicht entschlüsselbar“ seit **2026-11-24 15:54:23**, bis 2026-11-26 11:51 insgesamt 1758 Einträge
  (SageLagerbuchung, SageWareneingang, Article, Kreditoren, Debitoren, ArtikelLieferant, EinkaufBestellungen).
- Bis 15:54:03 lief der Dienst mit Warnung „Zugangsdaten aus appsettings“, also war `SData:Username` leer. Seitdem stehen
  `SData:Username = sage` und `SData:Password = dp1:…` in `ServiceSettings` (in der Web-Maske eingegeben), und der Dienst kann den
  Schutztext nicht lesen. Älter als v1.99.81, also nicht durch das Paket 21 verursacht.
- Dateien auf akenet01 nicht lesbar (`c$` verweigert) → Prüfschritte an Gerald: KeysPath Web vs. Dienst, Leserecht des Dienstkontos,
  Status in `/ServiceSettings` der Web-Site.
