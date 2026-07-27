# Windows-Authentifizierung + AD-User-Rollen — Design

> Status: Entwurf zur Review
> Datum: 2026-06-18
> Branch: `feature/windows-auth-ad-users` (Worktree `.claude/worktrees/missingparts-include-pd`)
> Ziel-Version: v1.23.0

## 1. Kontext & Problem

Der App-Login ist ein **separates Formular** (`AccountController`), das
`AppUserId`/`AppUserName` in die Session schreibt; die `LoginRedirect`-Middleware lässt
nur durch, wer eine **App-Session** hat. Ergebnis (bestätigtes Symptom): ein
windows-authentifizierter Benutzer landet **trotzdem auf dem Login-Formular**, weil es
keine Überführung „Windows-Identität → App-Session" gibt.

### 1.1 Kritische Korrektur — Hosting-Modell (Code-Verifikation 2026-06-18)

`web.config` deployt **IIS in-process** (`hostingModel="inprocess"`) mit **beiden**
Auth-Modi aktiv: `anonymousAuthentication=true` UND `windowsAuthentication=true`.
`Program.cs` registriert aber `AddNegotiate()` — und **`AddNegotiate()` ist für
Kestrel/HTTP.sys gedacht, NICHT für IIS in-process**. Das ist die wahrscheinliche Ursache
der bisherigen Windows-Auth-Probleme. Zwei daraus folgende Korrekturen am Design:

1. **Auth-Registrierung umstellen:** Unter IIS in-process wird Windows-Auth über die
   **IIS-Integration** geführt: `AddAuthentication(IISServerDefaults.AuthenticationScheme)`
   (statt `AddNegotiate()`). Die Windows-Identität fließt dann von IIS in `HttpContext.User`
   (`IISServerOptions.AutomaticAuthentication` = true, Default). `AddNegotiate()` +
   `Microsoft.AspNetCore.Authentication.Negotiate` werden entfernt.
2. **Challenge ist nötig:** Weil IIS-Anonymous **aktiv bleiben muss** (sonst kämen lokale/
   Nicht-Domänen-Benutzer gar nicht erst zum Formular-Fallback) und die App kein globales
   `[Authorize]` nutzt, sendet der Browser **von sich aus keine** Windows-Credentials →
   `HttpContext.User` bleibt anonym. Die Middleware muss daher **aktiv einmalig challengen**
   (`ChallengeAsync(IISServerDefaults.AuthenticationScheme)` → 401 Negotiate). Ein
   Domänen-Browser im Intranet-Zone antwortet **still** (kein Popup) → Identität ist da →
   Auto-Login. Ein Nicht-Domänen-Browser zeigt einmal einen Windows-Dialog → Abbruch →
   **Formular-Fallback**. Eine Cookie-Markierung verhindert eine Challenge-Schleife.
3. **Dev (Kestrel via `dotnet run`):** keine IIS-Integration → Windows-Auth inaktiv. Da
   `WindowsAuthAktiv` per Default **false** ist, ist das Verhalten in Dev unverändert
   (nur Formular). Die echte SSO-Strecke wird im IIS-Zielsystem getestet.

Heutige Rollenvergabe: lokale `UserRole`-Zuordnung **plus** automatische
`Role.AdGroup`-Zuordnung (AD-**Gruppe** → Rolle, via `User.IsInRole(adGroup)`, gegated
hinter einer App-Session — Zirkellogik). Eine Zuordnung **einzelner AD-User** zu Rollen
existiert nicht; `User` hat kein Feld für den Windows-Benutzernamen.

## 2. Ziele

1. **Auto-Login:** Ein Domänen-Benutzer mit hinterlegtem Benutzer-Datensatz wird ohne
   Formular automatisch angemeldet (Windows-Identität → App-Session).
2. **Fallback:** Schlägt der Auto-Login fehl (kein/inaktiver Datensatz, AD-/Laufzeitfehler),
   erscheint die **normale Login-Maske** (deckt lokale Passwort-User + geteilte/Nicht-
   Domänen-Geräte ab).
3. **AD-User anlegen:** Eigener Button im Benutzer-Stamm, der die Mitglieder einer
   konfigurierten **Berechtigungsgruppe** live aus dem AD vorschlägt; der Admin wählt
   einen aus, legt ihn als Windows-Benutzer (kein Passwort) an und vergibt **explizit
   Rollen** — wie bei lokalen Benutzern.
4. **Rollenmodell vereinfachen:** Rollen ausschließlich explizit pro Benutzer; die
   automatische `Role.AdGroup`-Zuordnung wird entfernt.
5. **Sicherer Rollout:** Das Auto-Login-Verhalten hängt an einem Feature-Flag
   (`WindowsAuthAktiv`, Default false) — solange aus, verhält sich die App exakt wie heute.

## 3. Nicht-Ziele (YAGNI)

- **Keine** Live-Prüfung der Gruppenmitgliedschaft beim Login (Login bleibt
  LDAP-unabhängig). Eligibility wird beim **Anlegen** durchgesetzt (Admin fügt nur
  Gruppenmitglieder hinzu). Zugriff entziehen = Admin deaktiviert/löscht den Datensatz.
  (Möglicher späterer Ausbau: periodischer Sync, der aus der Gruppe entfernte User
  deaktiviert — bewusst NICHT in diesem Scope.)
- **Keine** automatische User-Provisionierung beim ersten Login (unbekannter Windows-User
  → Formular-Fallback, kein Auto-Anlegen).
- **Kein** Umbau der bestehenden `[RequireXxxAccess]`-Filter / des Session-Modells auf
  ASP.NET-Claims (Ansatz ③ verworfen).
- **Keine** AD-Schreiboperationen (nur lesend: Gruppenmitglieder auflisten).

## 4. Architektur-Überblick

### Komponenten (neu/geändert)

| Komponente | Art | Zweck |
|-----------|-----|-------|
| `User.WindowsUserName` | Feld (neu) | Domänen-Account (SAM, ohne Domäne) als Login-Schlüssel |
| `WindowsAutoLoginMiddleware` | Middleware (neu) | Windows-Identität → App-Session, vor LoginRedirect, hinter Flag |
| `IActiveDirectoryService` / `ActiveDirectoryService` | Service (neu) | Liest Mitglieder der Berechtigungsgruppe via LDAP (nur für Picker) |
| `WindowsAccountHelper` | Helper (neu) | SAM-Extraktion aus `DOMAIN\sam` (testbar) |
| `UsersController.CreateAdUser` (GET/POST) | Action (neu) | AD-User-Picker + Anlage mit Rollen |
| `CurrentUserService` | geändert | AD-Gruppen-Rollen-Logik entfernt; Rollen nur aus `UserRoles` |
| `Role.AdGroup` + zugehörige UI | entfernt | Automatik fällt weg |
| `AccountController.Logout/Login` | geändert | Logout setzt „kein-Auto-Login"-Marker; Login löscht ihn |
| `Program.cs` Auth-Registrierung | geändert | `AddNegotiate()` → `AddAuthentication(IISServerDefaults.AuthenticationScheme)` |

### Datenfluss — Login

```
Browser → IIS (anonymous ODER windows-auth) → HttpContext.User (anonym oder DOMAIN\sam)
  → [Session-Middleware]
  → [WindowsAutoLoginMiddleware]   (nur wenn WindowsAuthAktiv && keine AppUserId
                                     && kein NoAutoLogin-Cookie && Nicht-Ausnahmepfad)
        Fall A — Identität bereits da (IsAuthenticated):
           sam = WindowsAccountHelper.ExtractSam(User.Identity.Name)
           user = UserRepository.GetActiveByWindowsUserNameAsync(sam)
           Treffer → Session.AppUserId/AppUserName setzen → weiter
           kein Treffer/inaktiv → AutoLoginTried-Cookie setzen → durchfallen (Formular)
        Fall B — anonym && AutoLoginTried-Cookie NICHT gesetzt:
           AutoLoginTried-Cookie setzen → ChallengeAsync(IISServerDefaults) (401 Negotiate)
           → Domänen-Browser antwortet still → Re-Request landet in Fall A
           → Nicht-Domänen-Browser bricht ab → Re-Request bleibt anonym → Fall C
        Fall C — anonym && AutoLoginTried-Cookie gesetzt:
           nichts tun → durchfallen (Formular)
        (alles in try/catch: jeder Fehler → Log → durchfallen)
  → [LoginRedirect]  → bei Session: weiter zur Zielseite
                      → ohne Session: Redirect /Account/Login (Formular-Fallback)
```

Der `AutoLoginTried`-Cookie ist ein kurzlebiger Session-Cookie, der genau **eine**
Challenge je Browser-Session erzwingt (keine Endlosschleife für Nicht-Domänen-Browser).
Nach erfolgreichem Auto-Login bzw. Formular-Login wird er gelöscht.

### Datenfluss — AD-User anlegen

```
Admin → Users/CreateAdUser (GET)
  → IActiveDirectoryService.GetAuthorizationGroupMembersAsync()
        (LDAP: Mitglieder der Berechtigungsgruppe, bereits importierte ausgeblendet)
  → Picker: Admin wählt 1 Mitglied + Rollen
Admin → Users/CreateAdUser (POST)
  → User { Name, WindowsUserName=SAM, PasswordHash=null, IsActive=true } + UserRoles
```

## 5. Datenmodell-Änderungen (Migration 73)

- **`Users.WindowsUserName`** `NVARCHAR(200) NULL`. Gefilterter Unique-Index
  `UQ_Users_WindowsUserName` (`WHERE WindowsUserName IS NOT NULL`) gegen Doppelanlage.
  Speicherung: **SAM-Name ohne Domäne**, case-insensitiv gematcht (D1).
- **`Roles.AdGroup`** Spalte **droppen** (D2). Vorher prüfen, dass keine Logik mehr darauf
  zugreift.
- App-Layer-Guard für Duplikat-Erkennung zusätzlich zum Index (InMemory-Tests enforcen
  Unique-Indizes nicht — bestehende Projekt-Konvention).
- `SQL/73_AddWindowsUserNameDropAdGroup.sql` (idempotent, OBJECT_ID/COL_LENGTH-Guards) +
  `SQL/00_FreshInstall.sql` (Spalte + Index ergänzen, `Roles.AdGroup` aus konsolidiertem
  Schema entfernen, History-Insert `..._AddWindowsUserNameDropAdGroup`).

**Semantik:** AD-User = `WindowsUserName` gesetzt + `PasswordHash` NULL. Lokaler User =
`PasswordHash` (oder Freipasswort) + `WindowsUserName` NULL. Kein zusätzlicher Enum/
Discriminator — die Anwesenheit von `WindowsUserName` ist der Indikator.

## 6. Konfiguration

| Key | Ort | Default | Beschreibung |
|-----|-----|---------|-------------|
| `WindowsAuthAktiv` | AppSettings (DB) | `false` | Master-Schalter Auto-Login-Middleware |
| `WindowsAuthBerechtigungsgruppe` | AppSettings (DB) | (leer) | SAM-Name der AD-Berechtigungsgruppe für den Picker |
| `Security:AdDomain` | appsettings.json | (leer) | Optional: Domäne/LDAP-Container für die Abfrage; leer = aktuelle Maschinen-Domäne |

`Security:AdGroupCacheMinutes` entfällt (AD-Gruppen-Rollen-Logik wird entfernt).

Schalter + Gruppe liegen in der admin-pflegbaren AppSettings-DB (Settings-Seite),
die Domäne als Infrastruktur-Wert in `appsettings.json`.

## 7. Komponenten-Details

### 7.1 `WindowsAccountHelper.ExtractSam(string? identityName)`
- `"AKE\\jmuster"` → `"jmuster"`; `"jmuster"` → `"jmuster"`; `null`/leer → `null`.
- `"jmuster@ake.at"` (UPN) → `"jmuster"` (Teil vor `@`), falls kein `\` enthalten.
- Reine Stringlogik, Theory-getestet.

### 7.2 `IActiveDirectoryService`
```csharp
public interface IActiveDirectoryService
{
    Task<IReadOnlyList<AdUserCandidate>> GetAuthorizationGroupMembersAsync(CancellationToken ct = default);
}
public record AdUserCandidate(string SamAccountName, string? DisplayName, string? Email, bool Enabled);
```
- Impl `ActiveDirectoryService` (`[SupportedOSPlatform("windows")]`): liest
  `WindowsAuthBerechtigungsgruppe` aus AppSettings; `new PrincipalContext(ContextType.Domain,
  domainOrNull)`; `GroupPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, group)`;
  `.GetMembers()` → `UserPrincipal` → `AdUserCandidate`.
- **Robustheit:** wenn nicht Windows (`!OperatingSystem.IsWindows()`), Gruppe leer/nicht
  gefunden, oder LDAP-Fehler → **leere Liste** + Log-Warnung (kein Throw).
- DI: immer real registriert (Impl prüft selbst auf Windows); Tests injizieren ein Fake.
- **Nur** vom Picker genutzt — NICHT vom Login-Pfad.

### 7.3 `WindowsAutoLoginMiddleware`
- Implementiert als `IMiddleware` (factory-basiert, DI-fähig) — so lassen sich
  `IUserRepository` und der AppSettings-Accessor sauber als scoped Dependencies injizieren
  (Registrierung als scoped Service + `app.UseMiddleware<WindowsAutoLoginMiddleware>()`).
- Reihenfolge: nach `UseSession()`, **vor** der `LoginRedirect`-Middleware.
- Skippt dieselben Pfade wie LoginRedirect (`/account/*`, `/api/*`, statische Dateien).
- **Vorbedingungen** (sonst sofort `await next()`): `WindowsAuthAktiv`=true; keine
  `AppUserId` in Session; kein `NoAutoLogin`-Cookie.
- **Entscheidungslogik** (vgl. Datenfluss §4):
  - *Fall A — `User.Identity.IsAuthenticated`:* `sam` extrahieren →
    `GetActiveByWindowsUserNameAsync(sam)` → Treffer → Session setzen + `AutoLoginTried`/
    `NoAutoLogin`-Cookies löschen → `next()`. Kein Treffer → `AutoLoginTried`-Cookie setzen
    → `next()` (LoginRedirect schickt zum Formular).
  - *Fall B — anonym && kein `AutoLoginTried`-Cookie:* Cookie setzen, dann
    `return ChallengeAsync(IISServerDefaults.AuthenticationScheme)` (401 Negotiate; **kein**
    `next()`). Domänen-Browser antwortet still → Re-Request → Fall A.
  - *Fall C — anonym && `AutoLoginTried`-Cookie gesetzt:* `next()` (Formular-Fallback;
    keine erneute Challenge → keine Schleife).
- **Alles in try/catch**: jeder Fehler → Log → `next()` (Formular-Fallback).
- `AutoLoginTried` ist ein Session-Cookie (HttpOnly), das nur den einmaligen Challenge-
  Versuch je Browser-Session markiert; wird bei erfolgreichem Login (Auto oder Formular)
  gelöscht.

### 7.4 `UserRepository.GetActiveByWindowsUserNameAsync(string sam)`
- `Users` WHERE `WindowsUserName != null` AND `LOWER(WindowsUserName) == LOWER(sam)` AND
  `IsActive`. In Code lower-case normalisieren (test-sicher gegen InMemory-Ordinal).
- Inklusive `UserRoles` (für spätere Rollenauflösung nicht nötig — die läuft über
  `CurrentUserService`; Methode liefert nur den User für die Session).

### 7.5 `CurrentUserService` (Vereinfachung)
- `GetAdGroupRolesAsync()` + AD-Gruppen-Cache **entfernen**.
- `LoadRoleKeysAsync()` liefert nur noch `GetRoleKeysByUserIdAsync(userId)`.
- `GetWindowsUserName()`/`GetDisplayName()` bleiben (Audit-Felder + Anzeige).

### 7.6 `UsersController.CreateAdUser`
- **GET:** `IActiveDirectoryService.GetAuthorizationGroupMembersAsync()`; bereits
  importierte (per `WindowsUserName`) ausblenden; ViewModel mit Kandidatenliste
  (SAM, Anzeigename, Email, Enabled) + Rollen-Checkboxen. Bei leerer/fehlgeschlagener
  AD-Abfrage: Info-Banner („keine Mitglieder gefunden / AD nicht erreichbar"), Button zur
  manuellen Anlage über die normale `Create`-Maske bleibt verfügbar.
- **POST:** validiert SAM (nicht leer, noch nicht vorhanden); legt `User` mit
  `WindowsUserName`, `Name` (Anzeigename oder SAM), `IsActive=true`, `PasswordHash=null` an;
  setzt Rollen via bestehende `SetUserRolesAsync`; Audit-Felder.
- Zugriff: `[RequireAdminAccess]` (wie restlicher `UsersController`).

### 7.7 `Role` / `RolesController` / Views
- `Role.AdGroup`-Property + `RoleEditViewModel.AdGroup` + Form-Feld (Roles/Create,
  Roles/Edit) + Spalte (Roles/Index) **entfernen**.
- `RoleOverview.cshtml` + CLAUDE.md (AdGroup-Erwähnungen) anpassen.

### 7.8 `AccountController` Logout/Login (D3)
- **Logout:** zusätzlich zum Session-Clear ein kurzlebiges Cookie
  `IdealAkeWms.NoAutoLogin` setzen (Session-Cookie). Die Middleware respektiert es →
  AD-User landet auf dem Formular und kann sich z. B. als lokaler User anmelden.
- **Login (POST erfolgreich):** Cookie löschen (Auto-Login wieder erlaubt).

### 7.9 Users/Index + Edit (UI)
- Index: Button „AD-User anlegen" neben „Benutzer anlegen"; Badge/Spalte „AD"/„Lokal"
  (Spalten-filterbar gemäß Universal-Filter-Pattern).
- Edit: `WindowsUserName` anzeigen (für AD-User read-only sinnvoll); Rollenpflege wie heute.

## 8. Fehlerbehandlung

| Fall | Verhalten |
|------|-----------|
| Auto-Login: kein/inaktiver Datensatz | `AutoLoginTried`-Cookie setzen → durchfallen → Login-Formular |
| Auto-Login: Laufzeitfehler | try/catch → Log → durchfallen → Formular |
| Nicht-Domänen-Browser (Challenge abgebrochen) | 1× Windows-Dialog → Abbruch → `AutoLoginTried` gesetzt → Formular (keine erneute Challenge) |
| `NoAutoLogin`-Cookie gesetzt (nach Logout) | Auto-Login + Challenge übersprungen → Formular |
| Picker: AD nicht erreichbar / Gruppe leer | leere Liste + Info-Banner; Login unberührt |
| `WindowsAuthAktiv=false` | Middleware inaktiv → exakt heutiges Verhalten |
| Nicht-Windows-Host (Dev/Test) | AD-Service liefert leer; Auto-Login matcht nur lokale DB-Datensätze (kein LDAP nötig) |

## 9. Sicherheit

- Die Negotiate-Identität ist vertrauenswürdig (von IIS/Kerberos gesetzt); für AD-User ist
  kein Passwort nötig (Windows hat bereits authentifiziert).
- Kein Datensatz / inaktiv → kein Auto-Login (und ohne lokale Credentials auch kein
  Formular-Login). Zugriffsentzug = Datensatz deaktivieren/löschen.
- Berechtigungsgruppe wird **nur beim Anlegen** ausgewertet (Picker), nicht beim Login —
  bewusste Entscheidung (Login LDAP-unabhängig). Risiko dokumentiert (Nicht-Ziele §3).
- Audit-Felder (`CreatedByWindows` etc.) bleiben über `GetWindowsUserName()` befüllt.

## 10. Tests

- `WindowsAccountHelper.ExtractSam` — Theory (`DOMAIN\\u`, `u`, `u@d`, null/leer).
- `WindowsAutoLoginMiddleware` — Unit über konstruierten `HttpContext` + Fake-`IUserRepository`
  + Fake-AppSettings:
  - Flag aus → `next()` aufgerufen, nichts gesetzt.
  - Session vorhanden → `next()`, kein DB-Zugriff.
  - `NoAutoLogin`-Cookie → `next()`, keine Challenge.
  - Fall A Identität vorhanden + Treffer → Session gesetzt, Cookies gelöscht.
  - Fall A Identität vorhanden + kein Treffer/inaktiv → keine Session, `AutoLoginTried` gesetzt.
  - Fall B anonym + kein `AutoLoginTried` → Challenge ausgelöst (401), `next()` NICHT aufgerufen,
    Cookie gesetzt. (Challenge über injizierbare Abstraktion, damit kein echter IIS-Handler nötig.)
  - Fall C anonym + `AutoLoginTried` gesetzt → `next()`, keine Challenge (keine Schleife).
  - Exception im Pfad → `next()`, kein Throw.
- `UserRepository.GetActiveByWindowsUserNameAsync` — Treffer case-insensitiv, inaktiv
  ausgeschlossen, null-WindowsUserName ignoriert.
- `UsersController.CreateAdUser` — GET filtert importierte raus (Fake-AD-Service); POST legt
  User + Rollen an; Duplikat abgelehnt.
- `CurrentUserService` — Rollen kommen nur aus `UserRoles` (kein AD-Gruppen-Pfad mehr).
- Die **echte** `ActiveDirectoryService`-Impl ist Windows-/AD-gebunden → **nicht
  InMemory-testbar** (als Fallstrick dokumentieren, analog raw-SQL BomCache).

## 11. Migration / Deploy

- **Auth-Registrierung umstellen** (§1.1): in `Program.cs` `AddAuthentication(NegotiateDefaults…)
  .AddNegotiate()` ersetzen durch `AddAuthentication(IISServerDefaults.AuthenticationScheme)`;
  PackageReference `Microsoft.AspNetCore.Authentication.Negotiate` entfernen. `IISServerOptions`
  ggf. konfigurieren (`AutomaticAuthentication` bleibt Default true).
- NuGet `System.DirectoryServices.AccountManagement` ins Web-Projekt; TFM bleibt `net10.0`,
  Windows-Gating via `OperatingSystem.IsWindows()` + `[SupportedOSPlatform("windows")]`
  (CA1416-sauber).
- Migration 73 + `SQL/73` + FreshInstall + History.
- Version-Bump **v1.23.0** (`AppVersion.cs` Web + Service) + Changelog + Hilfe.
- TESTSZENARIEN: neues Kapitel.
- **IIS-Voraussetzungen (unverändert lassen!):** `web.config` hat bereits
  `windowsAuthentication=true` UND `anonymousAuthentication=true` — **beide bleiben aktiv**.
  Windows liefert die SSO-Identität (auf Challenge), Anonymous hält den Formular-Fallback
  für lokale/Nicht-Domänen-Benutzer offen. Webserver domänen-gebunden; App-Pool-Identität
  darf AD lesen (Standard-Domänen-User genügt).
- **Reihenfolge beim Aktivieren:** `WindowsAuthAktiv` erst NACH Anlage der ersten AD-User
  einschalten (sonst landen Domänen-User ohne Datensatz nach der Challenge auf dem Formular —
  funktioniert via Fallback, ist aber Verwirrungsquelle).

## 12. Offene Punkte / Risiken

- **Auto-Login-Strecke ist nur im echten IIS-Zielsystem voll testbar** (Negotiate-Challenge +
  Domänen-Browser). Unit-Tests decken die Middleware-**Entscheidungslogik** ab (Fälle A/B/C
  über ein gefaketes `AuthenticationService`/`IISServerDefaults`-Ergebnis bzw. gesetzte
  Identität); der echte 401-Negotiate-Handshake wird manuell im Zielsystem verifiziert
  (TESTSZENARIEN). Analog zur raw-SQL beim BomCache als Fallstrick dokumentieren.
- **Nicht-Domänen-Browser sehen einmalig einen Windows-Dialog** vor dem Formular-Fallback
  (unvermeidbar bei „Anonymous aktiv + Auto-SSO"). Für ein Intranet-WMS akzeptiert; per
  `AutoLoginTried`-Cookie auf genau eine Challenge je Browser-Session begrenzt.
- **CA1416 / Windows-only Package:** Build muss auf dem CI/Dev-Rechner sauber bleiben; ggf.
  `<NoWarn>` gezielt für CA1416 in der Impl-Datei oder Attribut-Gating.
- **`Role.AdGroup`-Drop ist datenverändernd** (Spalte weg) — falls jemand Werte gepflegt
  hat, gehen sie verloren. Da die Automatik ohnehin abgelöst wird, akzeptiert; DB-Backup
  vor Deploy empfohlen.
- **Hosting-Annahme:** Korrektur §1.1 gilt für IIS in-process (laut `web.config`). Würde je
  auf Kestrel/HTTP.sys gewechselt, müsste die Auth-Registrierung erneut angepasst werden.
