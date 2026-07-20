# Windows-Auth: User-Agent-Gate + manueller SSO-Button — Design

**Status:** Approved (Design), wartet auf Plan
**Version:** in v1.25.0 gefaltet (kein AppVersion-Bump), Web-seitig
**Scope:** `WindowsAutoLoginMiddleware` + Login-Formular, hinter `WindowsAuthAktiv`

## Problem

Die App unterstützt Windows-SSO (per `WindowsAutoLoginMiddleware`) **und** ein
Formular-Login. Für Windows-Domänen-Geräte funktioniert das SSO transparent. Aber:
die Middleware schickt an **jeden** anonymen Browser einmalig eine Negotiate-Challenge
(`WindowsAutoLoginMiddleware.cs:92-99`). Auf einem Android-/iOS-Gerät (kein Windows-User,
nicht domänen-joined) zeigt der Browser daraufhin einen **unerfüllbaren** Windows-Anmelde-
Dialog, statt direkt das Formular anzubieten.

## Ziel

1. **UA-Gate:** Die Negotiate-Challenge nur noch an **Windows-Desktop-Browser** senden.
   Android/iOS/Mac/Linux/unbekannt fallen sofort aufs Formular durch — kein Prompt.
2. **Manueller Fallback-Button:** Ein „Mit Windows anmelden"-Button auf dem Login-Formular
   erzwingt das SSO auch für per UA nicht erkannte Windows-Clients (exotischer Browser,
   Mac/Linux im Domänennetz). Macht das UA-Gate verlustfrei.

Kein IIS-Umbau (die App löst die Challenge selbst aus — nicht IIS). Kein neues
AppSetting. Alles hinter dem bestehenden `WindowsAuthAktiv` (Default false).

## Nicht-Ziel

- Keine `/sso`-Virtual-Directory, kein IIS-Reconfig (beide IIS-Auth-Modi + `IISServerDefaults`
  bleiben wie in v1.23.0).
- Kein Passwort-Login für AD-User (die haben `PasswordHash = NULL`) — mobile Nutzer müssen
  lokale Konten sein. Unverändert.

## Design

### 1. UA-Erkennung (reiner Helfer)

Neuer statischer Helfer `UserAgentHelper.IsWindowsDesktop(string? userAgent)` (unit-testbar):
- null/leer → `false`.
- enthält (case-insensitiv) einen Mobile-Marker (`Android`, `iPhone`, `iPad`, `iPod`,
  `Windows Phone`, `Mobile`) → `false`.
- enthält `Windows NT` → `true`.
- sonst → `false` (Mac/Linux/unbekannt → Formular).

Konservativ: nur eindeutige Windows-Desktops bekommen die Auto-Challenge; im Zweifel
Formular (sicher, weil der Button + Formular immer als Fallback bleiben).

### 2. Middleware-Gate

In `WindowsAutoLoginMiddleware.TryAutoLoginOrChallengeAsync`, **nur** die anonym→Challenge-
Verzweigung (Zeile 92-99) wird UA-abhängig:

```
challenge nur wenn: kein AutoLoginTried-Cookie
                    UND ( UserAgentHelper.IsWindowsDesktop(UA)  ODER  ForceSso-Cookie gesetzt )
```

- Der „schon authentifiziert"-Zweig (SAM-Match, Zeile 70-90) bleibt **unverändert** — wenn IIS
  bereits eine Windows-Identität liefert, wird sie immer gematcht (UA egal).
- Wird nicht gechallenged (Nicht-Windows-UA, kein ForceSso) → `return false` → `next()` →
  LoginRedirect zeigt das Formular. **Kein `AutoLoginTried`-Cookie setzen** (damit ein späterer
  Button-Klick nicht blockiert wird).
- User-Agent lesen via `context.Request.Headers.UserAgent.ToString()`.

### 3. Force-SSO-Flow (manueller Button)

Neue Cookie-Konstante `ForceSsoCookie` (z. B. `IdealAkeWms.ForceSso`, HttpOnly, kurzlebig).

- **Login-View** (`Views/Account/Login.cshtml`): Button/Link „Mit Windows anmelden", **nur**
  gerendert wenn `WindowsAuthAktiv` true ist → GET `/Account/WindowsLogin`. Der `AccountController`
  gibt `WindowsAuthAktiv` an die View (ViewBag/Model).
- **`AccountController.WindowsLogin` (GET, neu):** löscht `NoAutoLogin`- + `AutoLoginTried`-Cookies,
  **setzt** den `ForceSso`-Cookie, `RedirectToAction("Index","Home")` (→ `/`).
  (`/Account/*` ist von der Middleware ausgeschlossen — deshalb den Redirect auf `/` machen,
  wo die Middleware greift.)
- **Middleware:** Auf `/` mit `ForceSso`-Cookie → Challenge trotz Nicht-Windows-UA (siehe §2).
  Nach erfolgreichem SAM-Match: `ForceSso`-Cookie **löschen** (zusätzlich zu den bestehenden
  AutoLoginTried/NoAutoLogin-Deletes). Bei SAM-Fail / anonym-bereits-gechallenged → `ForceSso`
  ebenfalls löschen (kein Loop — der bestehende `AutoLoginTried`-Gate verhindert Re-Challenge).

### Ablauf (Beispiele)

- **Windows-Desktop, WindowsAuthAktiv:** `/` anonym → UA=Windows → Challenge → Browser
  antwortet still → SAM-Match → Session → Home. (wie heute, nur jetzt UA-gegated.)
- **Android:** `/` anonym → UA=Android → **keine** Challenge → Formular. Kein Prompt.
- **Mac/Linux/exotisch, will SSO:** Formular → „Mit Windows anmelden" → `/Account/WindowsLogin`
  (Cookies löschen + ForceSso setzen) → `/` → ForceSso → Challenge → SAM-Match → Home.

## Betroffene Dateien

- `IdealAkeWms/Services/UserAgentHelper.cs` (**neu**, reiner Helfer)
- `IdealAkeWms/Middleware/WindowsAutoLoginMiddleware.cs` (UA-Gate + ForceSso-Cookie-Logik)
- `IdealAkeWms/Controllers/AccountController.cs` (`WindowsLogin`-Action + `WindowsAuthAktiv` an Login-View)
- `IdealAkeWms/Views/Account/Login.cshtml` (Button, gated by WindowsAuthAktiv)
- Tests: `UserAgentHelperTests` (Theory) + `WindowsAutoLoginMiddlewareTests` erweitern (UA-Fälle,
  ForceSso) + `AccountControllerTests` (`WindowsLogin`)
- Doku: Changelog v1.25.0, CLAUDE.md (Session & Auth + Fallstrick), TESTSZENARIEN (Windows-Auth-
  Kapitel erweitern), PROJECT_STATUS
- **Kein** Schema-Change / **keine** Migration / **kein** AppVersion-Bump / **kein** neues AppSetting

## Tests (TDD)

- **`UserAgentHelper.IsWindowsDesktop` (Theory):** Windows-Chrome/Edge/Firefox-UAs → true;
  Android-Chrome / iPhone-Safari / iPad / Windows Phone / generic Mobile → false; Mac/Linux → false;
  null/leer → false.
- **Middleware:** (a) anonym + Windows-UA + kein Cookie → Challenge ausgelöst; (b) anonym +
  Android-UA → **keine** Challenge (`ChallengeAsync` nie), fällt durch; (c) anonym + Android-UA +
  ForceSso-Cookie → Challenge ausgelöst; (d) bereits authentifiziert (SAM-Treffer) → Session gesetzt,
  ForceSso gelöscht, egal welche UA; (e) WindowsAuthAktiv=false → gar nichts (unverändert).
- **`AccountController.WindowsLogin`:** löscht NoAutoLogin/AutoLoginTried, setzt ForceSso, Redirect auf Home.
- **Manual-UAT** (nicht InMemory): echter Negotiate-Handshake am IIS-Zielsystem — Android zeigt
  Formular (kein Prompt), Windows-Desktop SSO, Button erzwingt SSO auf Nicht-Windows.

## Fallstricke

1. **UA-Gate NUR um die Challenge-Ausgabe**, nicht um den SAM-Match — sonst würde ein bereits
   von IIS authentifizierter Request (z. B. nach Button-Force) nicht gematcht.
2. **Kein `AutoLoginTried`-Cookie setzen, wenn nicht gechallenged wird** — sonst blockiert ein
   späterer Button-Klick die Force-Challenge (der Gate `if(!AutoLoginTried)`).
3. **`ForceSso` bei jedem Verlassen des SSO-Pfads löschen** (Erfolg, SAM-Fail, bereits-gechallenged),
   damit kein verwaister Force-Cookie hängt. Der bestehende `AutoLoginTried`-Gate verhindert
   ohnehin einen Challenge-Loop.
4. **Login-Button nur bei `WindowsAuthAktiv`** — sonst zeigt ein Nicht-SSO-System einen toten Button.
5. **UA ist spoofbar/unzuverlässig** — bewusst akzeptiert: Fehldetektion führt IMMER nur zum
   sicheren Formular (+ Button-Fallback), nie zu einem Zugriff ohne gültige Windows-Credentials.
6. **Negotiate-Handshake + LDAP bleiben Manual-UAT** (wie in v1.23.0 dokumentiert) — nur die
   Entscheidungslogik (UA-Gate/ForceSso/SAM) ist unit-testbar via `IChallengeIssuer` + Fake-Repo.
