---
type: adr
id: 0024
title: Geheimnisse (SData-Kennwort) liegen verschluesselt in den Service-Einstellungen, nicht mehr in appsettings
status: accepted
date: 2026-11-13
supersedes: "[[0008-servicesettings-db-first-mit-typisiertem-katalog]] (teilweise: Ausnahme Verbindungsgeheimnisse, nur fuer die SData-Zugangsdaten)"
superseded_by: ""
---

> Spec [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]] (v1.99.14), F6, Entscheidung 5 und Freigabe-Antworten 1 und 5 (Gerald 2026-11-13).
> Ergaenzt [[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]] um einen Einstellungstyp „Geheim“.
> ADR 0008 bleibt unveraendert gueltig fuer `ConnectionStrings:*`, `MailSettings:*` und `Security:AdDomain`.

## Kontext und Problem

[[0008-servicesettings-db-first-mit-typisiertem-katalog]] nimmt Verbindungsgeheimnisse ausdruecklich aus der per Oberflaeche pflegbaren
Tabelle `ServiceSettings` aus („gehoeren nicht in eine per UI editierbare Tabelle“). Die SData-Zugangsdaten (`SageLagerbuchung:Username/Password`)
standen deshalb im Klartext an **zwei** Stellen: Dienst-`appsettings.json` und Web-`appsettings.Production.json` ([[sage-sdata-uebersicht]]
„Zugang“). Folgen: jeder Kennwortwechsel ist ein Dateieingriff an zwei Orten mit App-Pool-Recycle, IDEAL laesst sich nicht ohne Dateizugriff
einrichten, und das Kennwort liegt im Klartext neben dem Programm.

Gerald will den IDEAL-Endpunkt samt Zugang nur ueber die Oberflaeche einrichten (Entscheidung 5): ein Ort, Kennwort verschluesselt, nie angezeigt.

## Betrachtete Optionen

1. **appsettings bleibt** (ADR 0008 unveraendert) – zwei Dateien, Klartext, Dateieingriff je Wechsel.
2. **Klartext in `ServiceSettings`** – ein Ort, aber das Kennwort steht im Klartext in DB, Backups, DEV-Kopien und (ueber die generische Maske,
   die Orphan-Liste und die Einstellungspruefung) in Oberflaeche und Protokoll.
3. **Verschluesselt in `ServiceSettings` mit der vorhandenen ASP.NET-Core-DataProtection** (Schluesselring der Web-App, vom Dienst nur lesend
   genutzt) – gewaehlt.
4. Windows-Anmeldeinformationsspeicher / externer Tresor – neue Abhaengigkeit, pro Konto und Maschine verschieden; verworfen.

## Entscheidung

- Neue Katalog-Keys `SData:Username` (String, max. 100) und `SData:Password` (neuer `SettingType.Geheim`) in `ServiceSettingDefinitions`,
  Gruppe „Sage-Lagerbuchung“.
- **Speicherform:** `dp1:` + Schutztext der DataProtection, Purpose `IdealAkeWms.ServiceSettings.Geheim.v1`, ApplicationName `IdealAkeWms`,
  Schluesselring `DataProtection:KeysPath` bzw. `%ProgramData%\IdealAkeWms\DataProtection-Keys`. Kennwort 1–128 Zeichen (Schutztext passt in
  `ServiceSettings.Value` nvarchar(500), Test belegt die Laenge). Keine Migration.
- **Eine Stelle** fuer Schuetzen, Status und Aufloesen: `IdealAkeWms/Services/SageSdata/SdataZugangsdaten.cs`, genutzt von Web (Verbindungstest,
  Register Sage-Test, FA-Struktur live) und Dienst (`SdataLeseZielLeser.ZugangAsync` → alle SData-Abgleiche, Sage-Buchungs-Worker fuer
  Lagerbuchung und Sage-Wareneingang). Pfadermittlung des Schluesselrings ebenfalls dort (`SchluesselPfad`), Web-`Program.cs` nutzt sie.
- **Dienst nur lesend:** `DataProtectionProvider.Create(Ordner, SetApplicationName, DisableAutomaticKeyGeneration)`, einmal je Prozess.
  Der Dienst legt nie Schluessel an; nur die Web-App rotiert.
- **Reihenfolge:** `SData:Username` gesetzt → DB gewinnt. Nicht entschluesselbar (fremder Schluesselring, Wert ohne `dp1:`) → Fehler
  „SData-Kennwort nicht entschluesselbar – in den Service-Einstellungen neu eingeben“, **kein** Aufruf an Sage, **kein** Rueckfall.
  `SData:Username` leer → Rueckfall auf `SageLagerbuchung:*` aus appsettings mit Warnung je Lauf (Uebergang, Freigabe-Antwort 1).
- **Nie im Klartext:** keine Seite (Feld ohne `value`, nur Status „gesetzt | nicht gesetzt | nicht entschluesselbar“), keine JSON-Antwort,
  kein Protokoll (Einstellungspruefung ueberspringt Geheim-Keys, Validator zitiert den Wert nie), keine Orphan-Liste (Katalog-Key);
  der Freitext-Weg Create/Edit/Delete lehnt Geheim-Keys ab. Aenderung/Entfernen schreibt eine Serilog-Zeile mit Windows-Benutzer, ohne Wert.
  `SdataZugang`, `SdataLeseZiel` und `SageBookingEndpoint` ueberschreiben `ToString` ohne Kennwort.

## Konsequenzen

**Positiv**
- Ein Ort, Pflege ueber `/ServiceSettings`, kein Dateieingriff und kein Recycle beim Kennwortwechsel; IDEAL ohne Dateizugriff einrichtbar.
- DB, Backups und DEV-Kopien enthalten das Kennwort nicht im Klartext.

**Negativ / Risiken**
- **Schutzziel bewusst begrenzt (Freigabe-Antwort 5):** Der Schluesselring liegt unverschluesselt im Dateisystem (`Program.cs` ohne
  `ProtectKeysWith*`). Wer Lesezugriff auf Schluesselordner **und** DB hat, kann entschluesseln. DPAPI-Schutz der Schluessel ist eine eigene
  Aufgabe, weil er Anmeldung und Antiforgery mitbetrifft.
- **Schluesselring-Verlust oder DB-Kopie** auf ein anderes System = Kennwort „nicht entschluesselbar“, Dienst sendet nichts an Sage, bis es
  neu eingegeben ist ([[fallstricke]] nachziehen). DEV mit kopierter Prod-DB braucht deshalb immer eine Neueingabe.
- **Deploy-relevant:** Der Dienst braucht Leserecht auf den Schluesselordner (Manifest `v1.99.14-DataProtectionDienst`, Platzhalter
  `{DataProtection}`), und ist im Web `DataProtection:KeysPath` gesetzt, muss die Dienst-`appsettings.json` denselben Wert haben.
- **Uebergangszustand:** Der appsettings-Rueckfall bleibt, bis `Pruefung/SDataKennwortAppsettings` ueberall 0 zeigt; Entfernen = Folgeaufgabe.
- Die Ausnahme gilt **nur** fuer die SData-Zugangsdaten. Weitere Geheimnisse (ConnectionStrings, SMTP) bleiben nach ADR 0008 appsettings-only;
  ein Umzug braeuchte einen eigenen ADR (der Typ `Geheim` waere wiederverwendbar).
