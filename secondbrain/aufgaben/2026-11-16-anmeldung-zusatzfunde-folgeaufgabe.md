---
type: aufgabe
status: offen
created: 2026-11-16
quelle: "[[2026-11-14-sicherheit-anmeldung-und-installer-spec]]"
---
# Folgearbeit Sicherheit v1.99.28 (Anmeldung, Erstinstallation)

Aus [[2026-11-14-sicherheit-anmeldung-und-installer-spec]] Freigabe-Antwort 10 (Zusatzfunde = Folgeaufgabe) und dem Dev-Lauf 2026-11-16.
ADR [[0027-formular-anmeldung-nur-mit-kennwort-erstkennwort-mit-pflichtwechsel]].

## Zusatzfunde (nicht in v1.99.28)

- [ ] Keine Sperre/Verzögerung nach Fehlversuchen im Anmeldeformular.
- [ ] Keine Kennwort-Mindestlänge (Pflichtwechsel, `/Users`, `/Account/Profile` – an einer Stelle regeln).
- [ ] Unterschiedliche Meldungen verraten vorhandene Benutzernamen („Benutzer nicht gefunden oder inaktiv.“ / „Falsches Passwort.“ /
  „kein Kennwort hinterlegt“) – `AccountController.Login`.
- [ ] BH-7: Installer prüft fest „mindestens 75 Migrationen“ (`scripts/deploy/Install-IdealAkeWms.ps1`, Schritt 6) – Zahl veraltet.

## Vor Deploy (Gerald, Prüfabfragen der Spec)

- [ ] **P-2b auf IDEAL-Prod.** Beleg aus dem Dev-Lauf: Meldung 207 bricht den **ganzen** Batch „15. Indexes“ beim Übersetzen ab – auf einer
  lokalen Prüf-DB mit dem alten Skript fehlten **alle** Indizes des Abschnitts samt FK. IDEAL-Prod (Erstinstallation 04.11.) fehlen sie
  daher sehr wahrscheinlich. `Pruefung/FreshInstallIndizes` (seit `4f3bf15d`, IDEAL, Vorher) sperrt IDEAL-Prod bei Treffern; dann SQL/151 bauen – nicht Teil von v1.99.28.
- [ ] P-1 je System (Benutzer ohne Kennwort) – `Pruefung/BenutzerOhneKennwort` sperrt sonst den Prod-Deploy.
- [ ] MailSettings je Dienst-Datei (Host, Port, SmtpUseSsl, Benutzer gesetzt ja/nein).

## Gemeldet, nicht still geändert

- [ ] Manifest-Eintrag `v1.71.0-AppsettingsProd` („appsettings.Production.json mit Abschnitt SageLagerbuchung“) ist seit ADR
  [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] überholt – Text anpassen oder Eintrag streichen (Entscheidung Gerald).
