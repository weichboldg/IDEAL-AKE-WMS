---
type: changelog
version: 1.99.28
date: 2026-11-16
---
# v1.99.28 — Sicherheit: Anmeldung nur mit Kennwort, Erstkennwort, Erstinstallation, Installer

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `6447db0a` (Anmeldung, Erstkennwort, FreshInstall) und `4559e0a2` (Doku, Installer,
Manifest, Version). Spec [[2026-11-14-sicherheit-anmeldung-und-installer-spec]], ADR [[0027-formular-anmeldung-nur-mit-kennwort-erstkennwort-mit-pflichtwechsel]],
Bug [[2026-11-14-sicherheit-anmeldung-und-installer-bug]]. Nicht gemergt. Version 1.99.27 bleibt für das Arbeitsgang-Mapping reserviert.

## Kernpunkte

- **Regel R:** Formular-Anmeldung nur mit gespeichertem Kennwort; Benutzer ohne Kennwort nur per Windows. Ausnahme nur `IsDevelopment()`
  mit sichtbarem Hinweis.
- **Regel P:** Erstkennwort durch den Admin (eingeben oder erzeugen, 12 Zeichen, einmal im Banner) mit Haken `MussKennwortAendern`;
  Pflichtwechsel vor der Sitzung, zustandslos. `/Users` zeigt Benutzer ohne Kennwort und offene Erstkennwörter.
- **admin:** ohne Kennwort setzt der Web-Start `wms-admin` + Haken (`AdminSeed`, auch Rückweg über `PasswordHash = NULL`).
- Cookies Sitzung/Antiforgery `SecurePolicy = SameAsRequest`.
- `00_FreshInstall.sql`: zwei Indizes auf nicht existierende Spalten entfernt (Meldung 207 verwarf den ganzen Abschnitt 15), Guards für
  WarehouseRequisitions/-Items, Wächter-Test `FreshInstallIndexSpaltenTests`.
- Installer: `-SageLagerbuchungCredential` entfernt; FA-Abgleich nach dem Aufwärmen in der DB aus (`Sync:ProductionOrdersEnabled=false`);
  SMTP-Warnung. Dienst: Warnung bei SMTP-Benutzer ohne `SmtpUseSsl`; tote Blöcke in `appsettings.json` entfernt (außer `Feiertag*`).

## Migration / SQL

- `SQL/152_AddUserMussKennwortAendern.sql` (additiv, `COL_LENGTH`-Guard, History separat). EF-Migration `20261116165705_AddUserMussKennwortAendern`.
  FreshInstall an beiden Stellen nachgezogen. Kein SQL/151 (Rückfrage 4: P-2b zuerst).

## Deploy-Risiken

- **Prod-Sperre:** `Pruefung/BenutzerOhneKennwort` (Vorher) – vorher Kennwörter setzen oder Benutzer deaktivieren (admin zählt nicht).
- Terminals/Handgeräte: Kennwörter nötig; Gerätekonten ohne Haken.
- Nach dem Deploy sofort als admin mit `wms-admin` anmelden, falls admin kein Kennwort hatte.
- **P-2b auf IDEAL-Prod vor dem Deploy** – Abschnitt-15-Indizes fehlen dort sehr wahrscheinlich ganz (Folgeaufgabe
  [[2026-11-16-anmeldung-zusatzfunde-folgeaufgabe]]).
- Web und Dienst gemeinsam deployen; die Server-Dienstdatei wird nicht überschrieben.
