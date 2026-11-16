---
type: adr
id: 0027
title: Formular-Anmeldung nur mit gespeichertem Kennwort; Erstkennwort durch Admin mit Pflichtwechsel; AD-Benutzer ohne Kennwort nur per Windows
status: accepted
date: 2026-11-16
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-14-sicherheit-anmeldung-und-installer-spec]] (v1.99.28, Freigabe-Antworten 1–16, Nachtrag 2026-11-16).
> Bug [[2026-11-14-sicherheit-anmeldung-und-installer-bug]]. Ergänzt (ersetzt nicht) ADR [[0002-dual-auth-session-login-plus-windows-sso]].

## Kontext und Problem

- BH-1: Benutzer **ohne** gespeichertes Kennwort meldeten sich im Formular mit leerem Feld an (`AccountController.Login`, Stand v1.99.26).
  AD-Benutzer werden ohne Kennwort angelegt – damit war das Formular für sie aus dem ganzen Netz offen (Anmeldename = WMS-Benutzername).
- BH-5a: `admin` wurde beim ersten Start mit leerem Kennwort (Leer-Hash) angelegt.
- Optionen: A streng (Formular nur mit Kennwort), B nur AD-Benutzer sperren, C = A + Übergangsschalter; für Benutzer ohne Kennwort zusätzlich
  „Selbstvergabe beim ersten Login“.

## Entscheidung

- **Regel R:** Das Formular meldet nur an, wenn ein gespeicherter Hash existiert und das eingegebene, nicht leere Kennwort passt. Ohne Hash
  Meldung „Für diesen Benutzer ist kein Kennwort hinterlegt …“ (mit Windows-Hinweis, wenn `WindowsAuthAktiv`). Variante A, **kein Schalter**.
- **Ausnahme nur `IsDevelopment()`** (lokale QA unter `dotnet run`): leeres Feld bei Benutzer ohne Hash meldet an; die Login-Seite zeigt
  dann sichtbar „Entwicklungsmodus …“.
- **Regel P:** Admin vergibt ein Erstkennwort (eingeben oder `GenerateInitialPassword`, 12 Zeichen, einmal im Erfolgs-Banner);
  `User.MussKennwortAendern` (Haken, vorbelegt bei neuem Kennwort, abwählbar für Gerätekonten). Mit Haken zeigt das Login vor der Sitzung den
  Schritt „Neues Kennwort festlegen“. **Zustandslos:** jeder POST des Schritts prüft Benutzer, bisheriges Kennwort und Haken neu; ein
  manipulierter POST ohne Haken ändert nichts. Regeln nur: nicht leer, neu ≠ bisherig, Wiederholung gleich. Keine Ablaufzeit.
- **admin-Seed (S1):** fehlt admin oder hat er keinen bzw. einen Leer-Hash, setzt der Web-Start das feste Standard-Erstkennwort `wms-admin`
  mit Haken (`AdminSeed.Ensure`) und warnt im Log (ohne Kennwort). Gilt auch als **Rückweg:** `PasswordHash = NULL` + Neustart.
- **Windows-SSO** prüft weder Kennwort noch Haken (unverändert).
- **Übergang** über `Pruefung/BenutzerOhneKennwort` (Phase Vorher, sperrt Prod; admin zählt nicht, weil der Seed ihn umstellt).

## Verworfen

- **Selbstvergabe beim ersten Login:** wer den Benutzernamen kennt und zuerst kommt, übernimmt das Konto; der echte Benutzer ist ausgesperrt.
- **B (nur AD sperren):** lokale Benutzer ohne Kennwort blieben offen.
- **C (Schalter):** ein Schalter „Formular ohne Kennwort erlauben“ bleibt erfahrungsgemäß an.
- **S2 (Zufalls-Erstkennwort für admin in Datei):** mehr Teile (Datei, Rechte, Installer-Ausgabe) für dasselbe Restrisiko-Fenster.

## Folgen

- Terminals und Handgeräte brauchen Kennwörter; Gerätekonten ohne Haken (Freigabe-Antwort 16).
- Restrisiko S1: bis zur ersten admin-Anmeldung kennt jeder mit Betriebshandbuch das Kennwort – Installer-Abschluss, Start-Warnung und
  `/Users`-Hinweis „offene Erstkennwörter“ verlangen sofortige Anmeldung.
- Lokale QA ohne Kennwort nur unter Kestrel/Development; auf DEV-IIS gelten R und P.
- Zusatzfunde (keine Sperre nach Fehlversuchen, keine Mindestlänge, Meldungen verraten vorhandene Benutzernamen) bewusst nicht hier –
  Folgeaufgabe [[2026-11-16-anmeldung-zusatzfunde-folgeaufgabe]].
