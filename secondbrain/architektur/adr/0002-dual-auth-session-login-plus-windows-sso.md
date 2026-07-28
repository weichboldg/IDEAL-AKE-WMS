---
type: adr
id: 0002
title: Dual-Auth — Session-basierter App-Login plus optionales Windows-SSO ueber die IIS-Integration
status: accepted
date: 2026-06-18
supersedes: ""
superseded_by: ""
---

> **Nachtraeglich erfasst** (2026-07-27). Bestandsdokumentation; die Entscheidung entstand in
> v1.23.0 und wurde in v1.25.0 mehrfach nachgeschaerft.

## Kontext und Problem

Das WMS wird von zwei Nutzergruppen bedient: Domaenen-Mitarbeiter an Windows-Arbeitsplaetzen
(die sich nicht noch einmal anmelden wollen) und Terminal-/Mobilgeraete im Lager, teils ohne
Domaenen-Konto (Android/iOS). Reines Windows-Auth schliesst die zweite Gruppe aus, reiner
Formular-Login zwingt die erste zu einer zweiten Anmeldung. Zusaetzlich muss die App eigene
Rollen fuehren, die nicht 1:1 auf AD-Gruppen abbildbar sind
(siehe [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]]).

## Betrachtete Optionen

- **Nur Windows-Auth** — kein Passwort-Handling, aber Nicht-Domaenen-Geraete und ein
  Benutzerwechsel am gemeinsam genutzten Terminal sind nicht moeglich.
- **Nur Formular-Login** — funktioniert ueberall, aber der Domaenen-Arbeitsplatz meldet sich
  doppelt an.
- **Beides parallel, Windows-Identitaet wird in eine App-Session ueberfuehrt** — eine einzige
  Autorisierungsquelle (die Session), SSO als Komfort obendrauf.

## Entscheidung

Die **App-Session ist die einzige Autorisierungsquelle.** Autorisiert wird ausschliesslich
ueber Session-Keys (`AppUserId`, `AppUserName`) und die `RequireXxxAccess`-Filter — nie ueber
`[Authorize]`, `User.Identity` oder `User.IsInRole`.

Windows-SSO ist ein *Zubringer* in diese Session:

- Hosting ist **IIS in-process** mit **beiden** Auth-Modi aktiv (`windowsAuthentication=true`
  UND `anonymousAuthentication=true`); anonymous haelt den Formular-Fallback offen.
- Authentifiziert wird ueber `AddAuthentication(IISServerDefaults.AuthenticationScheme)` —
  **nicht** `AddNegotiate()`, das ist fuer Kestrel/HTTP.sys und war unter IIS in-process falsch
  (Paket `Microsoft.AspNetCore.Authentication.Negotiate` wurde entfernt).
- `../../../IdealAkeWms/Middleware/WindowsAutoLoginMiddleware.cs` matcht die SAM-Identitaet
  case-insensitiv auf `User.WindowsUserName` und setzt die Session. Jeder Fehler und jeder
  Nicht-Treffer fallen zum Formular durch.
- Master-Schalter ist das AppSetting `WindowsAuthAktiv` (Default false).
- Die anonym→Negotiate-Challenge ist **UA-gegated** (`UserAgentHelper.IsWindowsDesktop`), damit
  Nicht-Windows-Clients prompt-frei aufs Formular fallen. Der Button „Mit Windows anmelden"
  setzt ein `ForceSso`-Cookie und erzwingt die Challenge trotzdem.
- `NormalizeUserForSession` setzt `HttpContext.User` auf **anonym**, sobald eine App-Session
  existiert und auf allen `/account/*`-Pfaden — damit sind alle Antiforgery-Token konsistent
  anonym gebunden (Wurzel-Fix gegen 400er bei POSTs unter SSO).

## Konsequenzen

**Positiv**
- Eine Autorisierungsquelle; Rollen sind unabhaengig von AD-Gruppen pflegbar.
- Mobile/Nicht-Domaenen-Geraete funktionieren ohne Sonderweg.
- Benutzerwechsel am gemeinsamen Terminal ist moeglich (`NoAutoLogin`-Cookie nach Logout).

**Negativ / Risiken**
- **Nicht InMemory-testbar** sind der echte Negotiate-Handshake und die LDAP-Strecke; nur die
  Entscheidungslogik der Middleware ist unit-getestet (via `IChallengeIssuer`-Abstraktion +
  Fake-`IUserRepository`). Rest ist Manual-UAT (../../../docs/TESTSZENARIEN.md Kap. 40).
- UA-Detektion ist spoofbar — bewusst akzeptiert, weil eine Fehldetektion immer nur zum
  sicheren Formular fuehrt, nie zu Zugriff ohne gueltige Credentials.
- Die Middleware haelt inzwischen **vier** ineinandergreifende Sperren (NoAutoLogin,
  AutoLoginTried, UA-Gate, LoginRedirect-Ausnahmeliste). Jede Aenderung an der
  Challenge-Bedingung muss alle vier mitdenken — Details in [[fallstricke]].
- Jede neue anonym erreichbare Account-Action muss in die **einzeln aufgezaehlte**
  LoginRedirect-Ausnahmeliste in `Program.cs` (kein pauschales `/account/*`).
