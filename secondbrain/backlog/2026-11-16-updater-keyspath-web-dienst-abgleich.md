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
- **Ursache belegt** (2026-11-26, Freigaben `\\AKENET01\wwwroot` und `\\AKENET01\WMSService$`, nur gelesen):
  Web `IdealAkeWMS-TEST\appsettings.Production.json` `DataProtection:KeysPath = C:\AKE\keys\test`, Dienst
  `IDEALAKEWMSService-Test\appsettings.Production.json` `= C:\AKE\keys\dev`. Schlüssel `keys\test\key-6f06c00f-….xml` entstand
  24.11. 15:51:58, zwei Minuten vor der Kennwort-Eingabe. Der Dienst sucht ihn in `keys\dev`, wo nur `key-4dacb266` vom 13.11. liegt.
  Prod 4443 (Web und Dienst `keys\prod`) und 4445 (beide `keys\dev`) stimmen jeweils überein.
- Weitere Abweichungen 4444-Dienst `appsettings.Production.json`: `MailSettings:PickupDirectory` zeigt auf
  `C:\AKE\IDEALAKEWMSService-TestIDEAL\mail-pickup` (Ordner von 4445); `SenderName`/`FromAddress` „DEV“/`wms-dev@` überschreiben „TEST“;
  `ErrorNotification:Enabled = false` und alle `Sync:*Enabled = false` überschreiben `appsettings.json` (wirken nur, wo kein
  `ServiceSettings`-Wert existiert); `SageLagerbuchung:Password` noch im Klartext (Übergangsrückfall, seit DB-Wert ungenutzt).
- **Behoben 2026-11-26:** Dienst-KeysPath von Gerald um 11:58 auf `keys\test` gestellt, Dienst läuft seit 11:58 wieder ohne Fehler.
  Web `IdealAkeWMS-TEST\appsettings.Production.json` Absender auf „IDEAL AKE WMS TEST“/`wms-test@ake.at` (Sicherung `.bak-20261126-125853`).
  Dienst-Datei: Schreibrecht über die Freigabe verweigert → Absender, PickupDirectory (`…-Test\mail-pickup`, Ordner angelegt) und
  `SageLagerbuchung`-Block macht Gerald von Hand. `ErrorNotification`/`Sync:*` in appsettings wirkungslos, weil `ServiceSettings` sie überschreibt.
- 2026-11-26 13:0x: Web `appsettings.Development.json` → `xxx_appsettings.Development.json` (nicht geladen, Umgebung Production).
  Dienst-Ordner: Umbenennen ebenfalls verweigert → Gerald. Auskommentieren verworfen, siehe Fallstrick 72.
