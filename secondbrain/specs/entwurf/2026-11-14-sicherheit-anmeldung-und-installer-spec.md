---
type: spec
title: "Bugfix Sicherheit: Anmeldung nur mit Kennwort oder Windows, Erstinstallation, Installer ohne Klartext-Zugangsdaten (BH-1 bis BH-5)"
slug: 2026-11-14-sicherheit-anmeldung-und-installer-spec
status: Testbereit
created: 2026-11-14
updated: 2026-11-16
source_backlog: "[[2026-11-14-sicherheit-anmeldung-und-installer]]"
weitere_quellen:
  - "[[2026-11-14-betriebshandbuch-goal-lauf]]"
  - "[[2026-11-04-ideal-produktiv-installation]]"
  - "[[2026-11-12-freshinstall-leere-db-fehler]]"
bug: "[[2026-11-14-sicherheit-anmeldung-und-installer-bug]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.28"
version: 1.99.28
sql: 152
sql_vorschlag: "152 fest (User.MussKennwortAendern, Nachtrag 2026-11-16); 151 nur wenn Rückfrage 4 die Index-Reparatur bestätigt"
ts_vorschlag: "TS-139"
adr_vorschlag: "0027"
affected_code:
  - "IdealAkeWms/Controllers/AccountController.cs (Login POST :64-116: Kennwortregel :82-101, Pflichtwechsel-Schritt; Profile POST :222-227 löscht MussKennwortAendern)"
  - "IdealAkeWms/Models/ViewModels/LoginViewModel.cs (neu NewPassword, NewPasswordRepeat, KennwortFestlegen)"
  - "IdealAkeWms/Views/Account/Login.cshtml (:75-80 Hinweistext, Felder Pflichtwechsel)"
  - "IdealAkeWms/Models/User.cs (neu bool MussKennwortAendern)"
  - "IdealAkeWms/Migrations/<Zeitstempel>_AddUserMussKennwortAendern.cs + .Designer.cs + ApplicationDbContextModelSnapshot.cs"
  - "SQL/152_AddUserMussKennwortAendern.sql (neu, COL_LENGTH-Guard, History separat, Muster SQL/141)"
  - "IdealAkeWms/Services/PasswordService.cs + IPasswordService (neu GenerateInitialPassword)"
  - "IdealAkeWms/Controllers/UsersController.cs (Index :70-94 Hinweis-Banner; Create POST :103-151 und Edit GET/POST :230-326 Erstkennwort + Haken)"
  - "IdealAkeWms/Models/ViewModels/UserEditViewModel.cs (neu MussKennwortAendern, ErstkennwortErzeugen)"
  - "IdealAkeWms/Views/Users/Index.cshtml (Banner), Views/Users/Create.cshtml (:89-93), Views/Users/Edit.cshtml (:173-177)"
  - "IdealAkeWms/Program.cs (:55-61 Cookie.SecurePolicy, AddAntiforgery; :259-279 admin-Seed mit Erstkennwort + Haken statt Leer-Hash, Start-Warnung)"
  - "IDEALAKEWMSService/Services/MailService.cs (:70-82, je nach Rückfrage 9)"
  - "IDEALAKEWMSService/appsettings.json (:17-20, :25-50 tote Blöcke, je nach Rückfrage 6)"
  - "SQL/00_FreshInstall.sql (:1260-1263 entfernen; :1803/:1838 Guards; Users :24-55 Spalte MussKennwortAendern; MigrationId-Block nach :3262-3264)"
  - "SQL/151_RepairFreshInstallIndexes.sql (nur nach Rückfrage 4)"
  - "scripts/deploy/Install-IdealAkeWms.ps1 (:29, :82, :269-274, :362-373, :420-449, :480-493; Erstkennwort-Ausgabe admin nach Rückfrage 11)"
  - "scripts/deploy/nachlauf.psd1 (SQL/152 Schema, Pruefung/BenutzerOhneKennwort auf Erstkennwort mit Pflichtwechsel umgestellt, Info-Einträge, ggf. Pruefung/FreshInstallIndizes, Pruefung/SmtpBenutzer)"
  - "Tests: IdealAkeWms.Tests/Controllers/AccountControllerTests.cs (Login- und Pflichtwechsel-Fälle neu), UsersControllerTests (Erstkennwort/Haken), PasswordService-Test (GenerateInitialPassword), IdealAkeWms.Tests/Repositories/FreshInstallIndexSpaltenTests.cs (neu), NachlaufManifestDriftTests"
  - "docs/TESTSZENARIEN.md (TS-139 neu; TS-1.6 :174, TS-40.4 :5250, TS-40.7 :5299 anpassen), docs/betrieb/03, docs/betrieb/07-troubleshooting.md (Abschnitt 2: Meldung R, Pflichtwechsel, Rückweg admin), docs/betrieb/09-sicherheit-konten-rechte.md (:21, R1/R2 :187-188), README.md :63, Views/Help/Index.cshtml, Views/Help/Changelog.cshtml, AppVersion.cs (Web + Service)"
  - "Brain (Hauptcheckout): ADR 0027 neu (Umsetzungs-Lieferobjekt, Inhalt siehe Lösungsentwurf 9), fallstricke §4, codebase/controller.md (Account, Users), codebase/datenmodell.md (User.MussKennwortAendern), feature-map, changelog"
  - "Bug: [[2026-11-14-sicherheit-anmeldung-und-installer-bug]]"
open_questions:
  - "BH-1 Regel: A streng (Formular nur mit gespeichertem Kennwort), B nur AD-Benutzer sperren (lokale ohne Kennwort weiter wie heute) oder C = A plus Übergangsschalter? Empfehlung A. [Neubewertung Nachtrag 2026-11-16: Regel R für Benutzer ohne Kennwort ist bestätigt (= Kern von A); weiter offen nur die formale Bestätigung A gegen C]"
  - "Übergang: Ergebnis der Prüfabfrage P-1 je System (IDEAL-Prod, IDEAL-Test, AKE-Prod, AKE-Test, AKE-Test-IDEAL) – und soll Pruefung/BenutzerOhneKennwort den Prod-Deploy sperren (Phase Vorher, Empfehlung) oder nur hinweisen (Phase Nachher)? [Neubewertung Nachtrag 2026-11-16: geändert – Abhilfe ist jetzt Erstkennwort mit Pflichtwechsel; P-1-Ergebnis und Sperre/Hinweis weiter offen]"
  - "Lokale QA unter Kestrel (Testbenutzer QA-Agent/QA-Picker ohne Kennwort): Ausnahme nur bei IsDevelopment() mit sichtbarem Hinweis (Empfehlung) oder keine Ausnahme (QA-Testbenutzer bekommen Kennwörter, Ablage außerhalb des Brains)?"
  - "BH-2: Ergebnis P-2 auf IDEAL-Prod (fehlen Indizes aus Abschnitt 15?) – bei Treffern SQL/151 + Pruefung/FreshInstallIndizes (Reparatur, nur IDEAL-Prod) oder Reparatur von Hand? Guards für WarehouseRequisitions/-Items und der Wächter-Test mit in diese Spec? Empfehlung: ja, ja, ja."
  - "BH-3: Parameter -SageLagerbuchungCredential ersatzlos entfernen (Empfehlung) oder behalten und mit Hinweis abbrechen? SMTP-Kennwort: weiter in der Dienst-Datei (ADR 0008/0024) mit Warnhinweis des Installers (Empfehlung) oder eigener ADR für den Umzug nach ServiceSettings (Typ Geheim)?"
  - "BH-4: Installer setzt Sync:ProductionOrdersEnabled nach dem ersten Web-Start in der DB auf false (Variante A, Empfehlung wegen Autostart des Dienstes nach Neustart) oder nur die wirkungslose Zeile streichen (Variante B)? Tote Blöcke Sync (außer Feiertag*), WorkerSettings, ErrorNotification aus IDEALAKEWMSService/appsettings.json entfernen (Empfehlung ja)?"
  - "BH-5a admin ohne Kennwort: Erstkennwort-Schritt bei der ersten Anmeldung (Empfehlung), Installer fragt Kennwort ab und schreibt den Hash (zweite PBKDF2-Implementierung in PowerShell) oder unverändert nur mit Warnung? [ERLEDIGT durch Nachtrag 2026-11-16: Erstkennwort + Pflichtwechsel (Haken), Leer-Hash-Ausnahme E entfällt; Folgefrage = Rückfrage 11]"
  - "BH-5b Cookie: SecurePolicy SameAsRequest für Sitzungs- und Antiforgery-Cookie (Empfehlung; HTTP-Systeme IDEAL-Test :88 und DEV laufen weiter), Always (bricht HTTP-Systeme) oder unverändert?"
  - "BH-5c SMTP: nur Warnung, wenn ein SMTP-Benutzer ohne SmtpUseSsl gesetzt ist, plus Pruefung/SmtpBenutzer (Empfehlung), oder StartTlsWhenAvailable (Risiko: Relay mit ungültigem Zertifikat -> keine Mails mehr), oder unverändert? Dazu Prüfschritt: MailSettings je Dienst-Datei (Host, Port, SmtpUseSsl, Benutzer gesetzt ja/nein)."
  - "Zusatzfunde (keine Sperre nach Fehlversuchen, keine Mindestlänge, unterschiedliche Meldungen verraten vorhandene Benutzernamen, Installer-Mindestzahl 75 Migrationen BH-7): Folgeaufgabe (Empfehlung) oder in diese Spec?"
  - "Erstkennwort für admin nach Installation/Rückweg: S1 festes dokumentiertes Erstkennwort, S2 Web-Start erzeugt ein Zufalls-Erstkennwort in eine geschützte Datei unter App_Data, Installer zeigt es einmal und löscht die Datei (Empfehlung), oder S3 anderer Weg?"
  - "Anzeige-Ort des vom WMS erzeugten Erstkennworts: einmal im Erfolgs-Banner nach dem Speichern (TempData, verschlüsseltes Cookie, Empfehlung) oder eigene Anzeigeseite/Druckzettel?"
  - "Gültigkeitsdauer des Erstkennworts: keine (Empfehlung; /Users zeigt offene Erstkennwörter) oder Ablauf nach n Tagen (braucht eigene Spalte ErstkennwortGesetztAm)?"
  - "Kennwortregeln beim Pflichtwechsel: nur nicht leer, neu ungleich alt, Wiederholung gleich (Empfehlung, Ist hat keine Regeln) oder zusätzlich Mindestlänge n (berührt Zusatzfund Rückfrage 10)?"
  - "Haken manuell löschen / Kennwort ohne Haken setzen (Gerätekonten BDE-Terminal, gemeinsame Handgeräte-Konten): erlaubt (Empfehlung, Checkbox in /Users/Edit maßgeblich) oder nie?"
  - "Handgeräte-/Terminal-Login mit Pflichtwechsel: Schritt auf Handgerät/Terminal akzeptiert (Bildschirmtastatur, Hardware-Scanner-Fokus) – Prüfschritt am Netum/Terminal – oder Gerätekonten grundsätzlich ohne Haken (siehe Rückfrage 15)?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: "A ohne Schalter; Erstkennwort + Pflichtwechsel, admin Standard-Erstkennwort (S1); P-1/P-2b/MailSettings vor Deploy; Version beim Dev-Start = nächste freie"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-16"
---

# Bugfix Sicherheit: Anmeldung, Erstinstallation, Installer (BH-1 bis BH-5)

Bug-Datensatz: [[2026-11-14-sicherheit-anmeldung-und-installer-bug]]. Befunde aus [[2026-11-14-betriebshandbuch-goal-lauf]]
(Tabelle „Befunde beim Erstellen“). Basis Bündel v1.99.23 (`IdealAkeWms/AppVersion.cs:5`), Zielversion **1.99.24**.

> [!important] Nachtrag 2026-11-16 16:27 eingearbeitet
> Erstkennwort durch Admin + Pflichtwechsel (neue Spalte `User.MussKennwortAendern`, SQL/152) ersetzt die Leer-Hash-Ausnahme E.
> Entscheidung und Neubewertung der Rückfragen 1, 2, 7: Abschnitt [[#Nachtrag 2026-11-16 16:27 – Erstkennwort durch Admin + Pflichtwechsel]].
> Code-Fundstellen gelten für den Bündel-Worktree `feature/2026-08-07-ideal-teile-1-5` (Stand v1.99.23, geprüft 2026-11-16).

## Ziel / Nutzen (das Warum)

- **BH-1:** Heute reicht der Benutzername, um sich als jeder Benutzer ohne gespeichertes Kennwort anzumelden – mit allen seinen
  Rollen, auch `admin`-Rollen eines AD-Benutzers. Das widerspricht der Annahme der Windows-Auth-Specs („kein Passwort-Login für
  AD-User“, `docs/superpowers/specs/2026-07-09-windows-auth-ua-gate-design.md:31`). Ziel: Formular nur mit gespeichertem Kennwort;
  AD-Benutzer ohne Kennwort nur per Windows-SSO. Dazu eine ADR-Entscheidung (0027), die es bisher nicht gibt
  ([[0002-dual-auth-session-login-plus-windows-sso]] regelt nur das Nebeneinander der Wege).
- **BH-2:** Jede Neuinstallation aus `00_FreshInstall.sql` bricht ab; IDEAL-Prod ist so installiert worden
  ([[2026-11-04-ideal-produktiv-installation]] Z. 53-57). Ob dort deshalb weitere Indizes fehlen, ist ungeklärt.
- **BH-3/BH-4:** Der Installer widerspricht ADR 0024 (Klartext) und täuscht einen Schutz vor, der nicht wirkt.
- **BH-5:** drei kleine Lücken, je mit Entscheidung.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Anmelderegel im Formular (BH-1) mit Übergang, Erstkennwort durch Admin + Pflichtwechsel für alle Benutzer mit
Kennwort-Formular-Anmeldung (Spalte `User.MussKennwortAendern`, `/Users/Create`, `/Users/Edit`, Login-Zwischenschritt) und
`admin`-Seed mit Erstkennwort + Haken (BH-5a, Nachtrag 2026-11-16), Korrektur
`00_FreshInstall.sql` mit Nachweis auf leerer DB (BH-2), Installer ohne Klartext-SData und mit wirksamem FA-Schutz (BH-3/BH-4),
Cookie-Flag (BH-5b), SMTP-Hinweis (BH-5c), Manifest, Doku `docs/betrieb/03`, `07`, `09`, Hilfe, Changelog, Testszenarien.

**Out-of-Scope:** Windows-SSO-Logik (`WindowsAutoLoginMiddleware`) bleibt unverändert; keine Sperre nach Fehlversuchen, keine
Kennwort-Richtlinie, kein „Kennwort vergessen“ (Zusatzfunde, Rückfrage 10); kein Umzug des SMTP-Kennworts (ADR 0024 letzter Absatz
verlangt dafür einen eigenen ADR); `SQL/00_FreshInstall.sql`-Kopf mit festem DB-Namen (`:8-17`) bleibt; BH-6/BH-7 (Doku-Drift,
fehlende `<FIRMA>-Prod-*.sql`) nicht.

## Fachliche Anforderungen

### Ist-Stand (belegt)

**Wer setzt eine Sitzung?** Genau zwei Stellen: Formular-Login `AccountController.cs:104-110` und SSO
`WindowsAutoLoginMiddleware.cs:114-118` (Suche nach `SetInt32(` im Web-Projekt, sonst nur diese beiden). API-Controller haben keinen
eigenen Anmeldeweg: `/api/*` ist von der Login-Umleitung ausgenommen (`LoginRedirectMiddleware.cs:23`), geschützt wird über die
Filter mit der Sitzung; im Controller-Code gibt es weder API-Schlüssel noch Bearer/Basic-Prüfung (Suche `apikey|bearer|basic`:
nur der ausgehende Sage-Verbindungstest `ServiceSettingsController.cs:202-203`).

**Formular-Login heute** (`AccountController.cs:74-101`): Benutzer per Name, aktiv (`:75`). Hash vorhanden → Kennwort prüfen
(`:83-91`). Kein Hash → angemeldet, sofern das Feld **leer** ist (`:92-101`). Die Seite sagt dazu „Leer lassen, wenn kein Passwort
vergeben.“ (`Login.cshtml:79`). `Password` ist nicht `[Required]` (`LoginViewModel.cs:11-13`).

**Wo entsteht ein Benutzer, wo ein Hash** (alle Schreibwege; `new User` und `PasswordHash =` im Web- und Dienst-Code):

| Weg | Hash | Beleg |
|---|---|---|
| Start-Seed `admin` | Hash des **leeren** Kennworts; ist der Hash `NULL`, wird bei **jedem** Start wieder der Leer-Hash gesetzt | `Program.cs:260-279` (`:267`, `:275-278`) |
| FreshInstall `admin` | `NULL`, danach Start-Seed | `SQL/00_FreshInstall.sql:1541-1549` |
| `/Users/Create` (lokal) | nur wenn das Feld nicht leer ist | `UsersController.cs:136-137` |
| `/Users/CreateAdUser` | **nie** | `UsersController.cs:200-209` |
| `/Users/Edit` | setzt neu, wenn nicht leer; **kein** Weg leert den Hash | `UsersController.cs:307-308` |
| `/Account/Profile` | setzt neu, wenn nicht leer; leert nie | `AccountController.cs:222-223` |
| Dienst / Sync | legt keine `User` an | Suche `new User` in `IDEALAKEWMSService` ohne Treffer |
| „Kennwort zurücksetzen“ | gibt es nicht | Suche `ResetPassword|ForgotPassword` ohne Treffer |

Folgerung (belegt): Ein Hash, der das **leere** Kennwort bestätigt, entsteht nur über den `admin`-Seed (`Program.cs:267`, `:277`),
denn alle anderen `HashPassword`-Aufrufe sind durch `!string.IsNullOrEmpty(newPassword)` geschützt (`UsersController.cs:136`, `:307`,
`AccountController.cs:222`).

**Wer meldet sich heute wie an** (Belege; Mengen nur per P-1 erhebbar):
- Domänen-PC: SSO, wenn `WindowsAuthAktiv = true` (`WindowsAutoLoginMiddleware.cs:93-94`) und ein aktiver Benutzer mit passendem
  `WindowsUserName` existiert (`:110-112`).
- Handy/Tablet/Nicht-Domäne: Formular (`UserAgentHelper`-Gate, TS-40.7) – oder der Knopf „Mit Windows anmelden“ (ForceSso, TS-40.9,
  TS-40.12 nennt ihn ausdrücklich für Mobilgeräte). Ohne `WindowsAuthAktiv` gibt es den Knopf nicht (`Login.cshtml:85-94`).
- **BDE-Terminal:** ein WMS-Benutzer ist am Gerät angemeldet; das Terminal wird über `BdeTerminal.UserId` dieses Benutzers gefunden
  (`BdeTerminal.cs:7-11`, `BdeTerminalController.cs:43-51`); die Werker melden sich danach per Ausweis-Scan als BDE-Operator an
  (TS-8, `fallstricke.md:439`). Ob die Terminal-Benutzer ein Kennwort haben: **unbekannt → P-1** (Spalte `IstTerminal`).
- **Kommissionierer am Handgerät:** Formular mit eigenem Benutzer; ob mit Kennwort: **unbekannt → P-1** (Spalte `IsPicker`).
- **Belegte Konten ohne Kennwort:** AKE-DEV `lager` (Formular-Login „ohne Passwort“, [[2026-11-10-vorwaerts-merge-main-ins-buendel]]
  Z. 107); in der QA-DB `AKE_WMS_QA_MAIN` `QA-Agent` (admin), `QA-Picker`, `QA-OhnePicking`, weil unter Kestrel kein SSO geht
  ([[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]] Z. 693, [[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-spec]] Z. 300).
- `admin` hat nach jeder Neuinstallation das leere Kennwort (Installer-Text `Install-IdealAkeWms.ps1:29`, `:481`).

### Soll (Empfehlung Variante A, Rückfrage 1)

**Regel R (ADR 0027):** Das Anmeldeformular meldet nur an, wenn das eingegebene Kennwort **nicht leer** ist und den gespeicherten Hash
bestätigt. Benutzer ohne Hash (lokal oder AD) werden im Formular abgewiesen:
„Für diesen Benutzer ist kein Kennwort hinterlegt. Bitte „Mit Windows anmelden“ verwenden oder beim Administrator ein Kennwort setzen
lassen.“ (Ist `WindowsAuthAktiv` aus, entfällt der erste Halbsatz.) Es entsteht keine Sitzung.

~~**Ausnahme E (Erstkennwort, BH-5a, Rückfrage 7):** Bestätigt der gespeicherte Hash das **leere** Kennwort (laut Ist-Stand nur der
`admin`-Seed) und wurde das Feld leer gelassen, zeigt die Anmeldeseite statt einer Sitzung den Schritt „Erstes Kennwort festlegen“
(neues Kennwort + Wiederholung). Der Server prüft beim Speichern erneut, dass der Hash noch das leere Kennwort bestätigt, dass das
neue Kennwort nicht leer ist und beide Eingaben gleich sind; erst dann Hash setzen, Audit-Felder, Sitzung. Danach gilt für `admin`
Regel R. Der Rückweg „admin-Kennwort vergessen“ bleibt der heutige: `PasswordHash` per SQL auf `NULL`, App-Pool neu starten →
Seed setzt den Leer-Hash (`Program.cs:275-278`) → Erstkennwort-Schritt (Doku `07-troubleshooting`).~~
*Ueberholt durch Nachtrag 2026-11-16 16:27: es gibt keinen Leer-Hash und keine Anmeldung mit leerem Feld mehr; an die Stelle tritt
Regel P.*

**Regel P (Erstkennwort durch Admin + Pflichtwechsel, Nachtrag 2026-11-16):**
- Neues Feld `User.MussKennwortAendern` (bool, NOT NULL, Default `false`). Bedeutung: das gespeicherte Kennwort hat ein Admin bzw.
  der Seed vergeben; der Benutzer muss es bei der nächsten Formular-Anmeldung durch ein eigenes ersetzen.
- **Setzen:** `/Users/Create` und `/Users/Edit` – wird ein Kennwort eingegeben **oder** vom WMS erzeugt (Option „Erstkennwort
  erzeugen“), setzt der Server den Haken automatisch. Der Haken ist zusätzlich als Checkbox „Muss Kennwort bei der nächsten Anmeldung
  ändern“ manuell setzbar (nur sinnvoll bei vorhandenem Kennwort; ohne Kennwort gilt Regel R, der Haken wirkt erst mit einem
  Kennwort). Ob der Admin den Haken auch **abwählen** bzw. ein Kennwort ohne Haken setzen darf (Gerätekonten), entscheidet
  Rückfrage 15: „nie“ → ein neues Kennwort setzt den Haken immer, die Checkbox kann ihn nur setzen; „erlaubt“ (Empfehlung) → die
  Checkbox ist maßgeblich und bei neu eingegebenem/erzeugtem Kennwort vorausgewählt.
- **Erzeugtes Erstkennwort:** `IPasswordService.GenerateInitialPassword()` (Zufall per `RandomNumberGenerator.GetItems`, 12 Zeichen
  aus einem Alphabet ohne verwechselbare Zeichen `0 O 1 l I`). Es wird **genau einmal** angezeigt (Ort: Rückfrage 12, Empfehlung
  Erfolgs-Banner `TempData["SuccessMessage"]` nach dem Redirect – TempData ist lesend einmalig, Default-Provider = verschlüsseltes
  Cookie, `Program.cs` registriert keinen anderen), nie im Klartext gespeichert, nie geloggt.
- **Login:** richtiges, nicht leeres Kennwort + Haken gesetzt → keine Sitzung, sondern Zwischenschritt „Neues Kennwort festlegen“
  auf derselben Seite (`/Account/Login`, Felder: bisheriges Kennwort erneut, neues Kennwort, Wiederholung). Der Server prüft bei
  **jedem** POST erneut: Benutzer aktiv, Hash vorhanden, bisheriges Kennwort bestätigt den Hash, Haken noch gesetzt, neues Kennwort
  nicht leer/nicht nur Leerzeichen, neu ≠ bisheriges (`VerifyPassword(hash, neu)` = false), Wiederholung gleich; weitere Regeln nur
  nach Rückfrage 14. Erst dann `PasswordHash = HashPassword(neu)`, `MussKennwortAendern = false`, Audit-Felder, Sitzung. Der Schritt ist
  zustandslos (kein Session-Merker, kein Klartext in versteckten Feldern): Abbruch, Zurück, Schließen oder Fehler → keine Sitzung,
  Hash und Haken unverändert.
- **Ohne gespeichertes Kennwort** gilt weiter Regel R (keine Formular-Anmeldung, auch nicht mit Haken).
- **Selbst gewähltes Kennwort** über `/Account/Profile` (`AccountController.cs:222-223`) löscht den Haken ebenfalls (der Benutzer hat
  dann ein eigenes Kennwort; erreichbar mit Haken nur über SSO oder die Entwicklungs-Ausnahme).
- **AD-Benutzer:** Windows-SSO unverändert (`WindowsAutoLoginMiddleware.cs:114-118` prüft weder Kennwort noch Haken). Ein Erstkennwort
  bekommen sie nur bei Bedarf (Handgerät ohne SSO); `/Users/Edit` zeigt das Kennwortfeld auch für AD-Benutzer
  (`Views/Users/Edit.cshtml:173-177`, keine Bedingung auf `WindowsUserName`).
- **Warum nicht Selbstvergabe ohne Kennwort:** Ein Benutzer ohne Kennwort, der sich beim ersten Login „selbst“ ein Kennwort gibt, ist
  von jedem übernehmbar, der den Benutzernamen kennt (Namen wie `lager` sind erratbar, Belege unter Ist-Stand) – der Erste am
  Formular besitzt danach das Konto, der echte Benutzer ist ausgesperrt. Das Erstkennwort durch den Admin bindet die erste Anmeldung
  an ein Geheimnis, das nur der Admin weitergibt (ADR 0027).

**`admin` nach Installation (BH-5a, Nachtrag 2026-11-16):** Der Start-Seed (`Program.cs:259-279`) legt `admin` nicht mehr mit dem
Leer-Hash an, sondern mit einem Erstkennwort + `MussKennwortAendern = true`. Welches Erstkennwort und wie es zum Installateur kommt,
entscheidet **Rückfrage 11** (Empfehlung S2: Zufalls-Erstkennwort in eine Datei unter `App_Data`, Installer gibt es nach dem Aufwärmen
einmal aus und löscht die Datei; S1 festes, dokumentiertes Erstkennwort). Der Seed greift, wenn
`string.IsNullOrEmpty(admin.PasswordHash)` **oder** der Hash das leere Kennwort bestätigt – so werden bestehende `admin`-Konten mit
Leer-Hash (Ist: `Program.cs:267`, `:277`) beim ersten Start von v1.99.24 automatisch auf Erstkennwort + Haken umgestellt, statt nach
Regel R ausgesperrt zu sein. Ein `admin` mit eigenem Kennwort bleibt unberührt.

**Rückweg „admin-Kennwort vergessen“ (umgedeutet):** (1) Ein anderer Admin setzt unter `/Users/Edit` ein Erstkennwort → Haken →
Pflichtwechsel. (2) Kein Admin mehr anmeldbar: `UPDATE dbo.Users SET PasswordHash = NULL WHERE Name = N'admin'`, App-Pool neu starten →
Seed setzt Erstkennwort + Haken (Weg nach Rückfrage 11) → Pflichtwechsel. Doku `docs/betrieb/07-troubleshooting.md`.

**Übergang:** Vor dem Deploy erhebt Gerald je System P-1. Jeder aktive **lokale** Benutzer ohne Kennwort, der weiter arbeiten soll
(Terminals, Handgeräte), bekommt vorher über `/Users/Edit` ein Kennwort (geht schon heute, `UsersController.cs:307-308`) oder wird
deaktiviert. *Nachtrag 2026-11-16:* ab v1.99.24 ist die Abhilfe „Erstkennwort mit Pflichtwechsel setzen“ (Regel P); ein im alten
Stand gesetztes Kennwort trägt noch keinen Haken (Spalte entsteht erst mit SQL/152, Default `false`) – wer den Pflichtwechsel auch
für diese Konten will, setzt nach dem Deploy den Haken unter `/Users/Edit`. AD-Benutzer ohne Kennwort brauchen nur dann eines, wenn sie ohne SSO arbeiten (Mobilgerät ohne Knopf, oder
`WindowsAuthAktiv` aus). Die Prüfung `Pruefung/BenutzerOhneKennwort` zählt genau die Benutzer, die nach dem Deploy **keinen**
Anmeldeweg mehr hätten, und sperrt den Prod-Deploy, solange sie > 0 ist (Rückfrage 2). Kein Schalter (Begründung unter „Einstellungen“).

**Sichtbarkeit (Melden statt still):** `/Users` zeigt einen Hinweis „n aktive Benutzer ohne Kennwort: … (lokal: keine Anmeldung
mehr möglich; AD: nur Windows-Anmeldung)“ und „n Benutzer mit offenem Erstkennwort (Pflichtwechsel ausstehend): …“ (alle aktiven
mit `MussKennwortAendern = true`, `admin` eingeschlossen; ersetzt den früheren Text „admin hat noch kein Kennwort“). Der Web-Start
schreibt eine Warnung ins Log, solange `admin` den Haken trägt („Benutzer admin hat ein Erstkennwort – bei der ersten Anmeldung
ändern.“; nie das Kennwort selbst).

**Lokale QA unter Kestrel (Rückfrage 3, Empfehlung):** Nur wenn `app.Environment.IsDevelopment()` gilt Regel R nicht für Benutzer
ohne Hash; die Anmeldeseite zeigt dann sichtbar „Entwicklungsmodus: Anmeldung ohne Kennwort erlaubt“. Belegt nur über
`launchSettings.json:9-10`/`:18-19` (`Development`); jede IIS-Site setzt `Production` per `web.config:17-19`, DEV-IIS-Sites `DEV`
([[2026-10-29-dev-umgebung-lokal]] Z. 57/161) – die Ausnahme greift dort nicht.

### Varianten BH-1 (Rückfrage 1)

| | A – streng (Empfehlung) | B – nur AD | C – A + Schalter |
|---|---|---|---|
| Regel | Formular nur mit Kennwort | AD-Benutzer ohne Hash abgewiesen, lokale ohne Hash wie heute | wie A, AppSetting `AnmeldungOhneKennwort` (`gesperrt`/`nur lokale Benutzer`) |
| Lücke danach | keine (bis auf Erstkennwort-Fenster `admin`) | bleibt für jeden lokalen Benutzer ohne Kennwort (Namen wie `lager` sind erratbar) | wie A, solange der Schalter auf `gesperrt` steht |
| Übergang | Kennwörter vorher setzen, Prüfung sperrt Prod | keiner | Schalter erst später umlegen |
| Kosten | klein | klein | Schalter nach ADR 0020 (Katalog, Wirkung, Warnung), zweiter Codeweg, Drift-Guard |

Empfehlung **A**: einfachste Regel, ein Codeweg; der Übergang wird durch die Prüfung erzwungen statt durch einen Schalter, den man
vergisst zurückzustellen. B lässt genau die Lücke offen, die BH-1 meldet. C lohnt nur, wenn P-1 viele lokale Konten zeigt, die nicht
vor dem Deploy ein Kennwort bekommen können.

### BH-2 – Erstinstallation

- `00_FreshInstall.sql:1260-1263` legt `IX_ProductionOrders_IsReleasedForPicking_IsDone` und `IX_ProductionOrders_AssignedPickerId`
  auf `ProductionOrders` an. Die Tabelle hat diese Spalten nicht (`:388-418`; auch `PickingPriority` aus dem INCLUDE nicht). Sie
  liegen in `ProductionOrderPickingStatus` (`:428-448`), mit eigenen Indizes `:456-460`, gleichnamig wie im EF-Modell
  (`ApplicationDbContext.cs:521`, `:524`). → **Zeilen 1260-1263 ersatzlos löschen.**
- **Bestehende DBs** können die falschen Indizes nicht haben: ohne Spalte kein Index; upgegradete DBs haben sie in SQL/60 gelöscht
  (`SQL/60_ProductionOrderSplit.sql:423-441`). P-2a belegt das je System. **Offen ist die Umkehrung:** Bei IDEAL-Prod brach
  Abschnitt 15 (ein Batch `:1206-1278`) ab; ob SQL Server dabei nur die eine Anweisung oder den ganzen Batch verwarf, ist nicht
  belegt – dann fehlten dort bis zu ~30 Indizes, darunter die eindeutigen `IX_ProductionOrders_SubOrderNumber` (`:1250-1251`) und
  `IX_SageBookingQueueItems_StockMovementId` (`:1224-1225`) sowie der FK `FK_StockMovements_ProductionOrders_ProductionOrderId`
  (`:1219-1221`). **Unverifiziert → P-2b auf IDEAL-Prod.**
- QA-Befund 2026-11-12 ([[2026-11-12-freshinstall-leere-db-fehler]]): zusätzlich Meldung 2714 für `WarehouseRequisitions`/
  `WarehouseRequisitionItems`. Belegt: beide `CREATE TABLE` und ihre Indizes stehen **ohne Guard** (`:1803-1836`, `:1838-1869`),
  2714 entsteht also bei einem zweiten Lauf gegen dieselbe DB. Da das Skript per `USE [IDEAL_AKE_WMS]` (`:16`) die per `sqlcmd -d`
  gewählte DB verlässt, lief der QA-Lauf vermutlich gegen eine schon vorhandene `IDEAL_AKE_WMS` auf `.` – unverifiziert; ebenso die
  dortige Meldung „`__EFMigrationsHistory` fehlt“ (die Tabelle entsteht in `:3011-3019`; vermutlich in der falschen DB gesucht).
  → Guards `IF OBJECT_ID(N'dbo.WarehouseRequisitions', N'U') IS NULL` bzw. `…Items` um beide Blöcke (Rückfrage 4).
- **Wächter-Test (billig, Rückfrage 4):** `FreshInstallIndexSpaltenTests` liest `00_FreshInstall.sql`, findet jede
  `CREATE [UNIQUE] [NONCLUSTERED] INDEX [x] ON [dbo].[T](…) [INCLUDE (…)] [WHERE …]`-Anweisung und prüft jede genannte Spalte gegen
  das relationale EF-Modell (Kontext mit `UseSqlServer(<Dummy-Verbindung>)`, nur Modell, keine Verbindung). Tabellen ohne EF-Abbild
  stehen mit Grund in einer Ausnahmeliste. Muster wie `DeployScriptRunsMigrationSqlTests` (Datei lesen, kein SQL Server).
  `ponytail:` prüft nur Index-Spalten, nicht die Spaltenlisten der `CREATE TABLE`; Ausbau, wenn dort Drift auffällt. Findet der Test
  weitere Abweichungen, werden sie gemeldet (Befundliste in der Spec) und nur korrigiert, wenn die Korrektur eindeutig ist.

### BH-3 – Installer ohne Klartext-SData

- Heute: Parameter `-SageLagerbuchungCredential` (`Install-IdealAkeWms.ps1:82`) schreibt Benutzer **und Kennwort** in
  `appsettings.Production.json` (`:269-274`, `:277`) und in die Dienst-`appsettings.json` (`:366-369`, `:372`). Seit ADR 0024 liest der
  Dienst die DB-Zugangsdaten zuerst; der Datei-Weg ist nur noch Übergang mit Warnung je Lauf
  ([[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] „Reihenfolge“).
- Soll (Empfehlung, Rückfrage 5): Parameter und beide Schreibblöcke **entfernen**; die Abschlussliste (`:480-493`) bekommt
  „SData-Zugang unter /ServiceSettings › Sage-Lagerbuchung eintragen (verschlüsselt gespeichert)“. Ein Aufruf mit dem alten Parameter
  scheitert laut an PowerShell („Parameter nicht gefunden“) – gewollt. Die Kommentarzeile `:82` und die `.EXAMPLE` (`:32-39`) nachziehen.
- **SMTP-Kennwort** liegt heute nur in der Dienst-Datei (`MailService.cs:72-73` liest `MailSettings:SmtpUsername/SmtpPassword` aus der
  Konfiguration; Web sendet keine Mails – Suche `SmtpClient` nur im Dienst). ADR 0024 hält SMTP ausdrücklich bei ADR 0008
  (appsettings-only). Ein Umzug bräuchte einen eigenen ADR. Empfehlung: Speicherort unverändert, der Installer schreibt es weiter
  (`:362-365`), gibt aber eine Warnung aus: „SMTP-Kennwort steht im Klartext in <Datei>; anonymes internes Relay bevorzugen,
  Dateirechte eng halten.“

### BH-4 – FA-Schutz bei der Installation

- Belegt wirkungslos: der Dienst liest `Sync:ProductionOrdersEnabled` nur aus `dbo.ServiceSettings`
  (`SyncWorker.cs:43` → `ServiceSettings.GetBoolSafeAsync` `Common/ServiceSettings.cs:50-55` → `GetValueAsync` `:19-29`, SQL auf die
  Tabelle; fehlt die Zeile oder ist die DB nicht erreichbar, gilt der Code-Default `true`, **nicht** die Datei). Die Zeile existiert
  nach der Installation immer: FreshInstall-Seed `true` (`00_FreshInstall.sql:1112`) bzw. Katalog-Seed beim Web-Start
  (`Program.cs:314-327`, Default `ServiceSettingDefinitions.cs:199`).
- Dasselbe gilt für die übrigen Blöcke der Dienst-Datei: `WorkerSettings:*` (`SyncWorker.cs:31-32`, `NotificationWorker.cs:25`,
  `CleanupWorker.cs:35`) und `ErrorNotification:*` (`SyncErrorNotifier.cs:32-33` u. a.) werden nur über `ServiceSettings.*` gelesen;
  eine direkte Konfigurationslesung `configuration["Sync:…"]`/`["WorkerSettings:…"]`/`["ErrorNotification:…"]` gibt es im Dienst nicht
  (Suche ohne Treffer). **Einzige Ausnahme:** `Sync:FeiertagSyncEnabled/-CountryCode/-Region/-JahreVoraus` werden per
  `Configure<HolidaySyncOptions>(GetSection("Sync"))` gebunden (`IDEALAKEWMSService/Program.cs:110`, `HolidaySyncService.cs:12-27`) und
  dienen als Rückfall, wenn die DB-Zeile fehlt (`HolidaySyncService.cs:60-80`).
- Lücke hinter dem gemeinten Schutz: Der Dienst wird mit Starttyp **Automatisch** registriert (`Install-IdealAkeWms.ps1:382`, `:405`);
  nach einem Server-Neustart vor Teil 2 läuft der FA-Abgleich mit `true`.
- Soll (Empfehlung Variante A, Rückfrage 6): Zeile `:371` und den Text in `:373` streichen; in Schritt 6 nach erfolgreichem Aufwärmen
  (`$script:WebGestartet`, `:442`/`:446`) per `System.Data.SqlClient` (Muster `Get-SchemaStand` `:421-429`)
  `UPDATE dbo.ServiceSettings SET [Value] = N'false' WHERE [Key] = N'Sync:ProductionOrdersEnabled'` ausführen und mit „OK FA-Abgleich
  aus (Sync:ProductionOrdersEnabled = false) – nach Teil 2 unter /ServiceSettings einschalten“ melden; startet das Web nicht, Warnung
  mit genau dieser Anweisung. Abschlussliste Punkt 3 ergänzen. Variante B: nur streichen und dokumentieren.
- Tote Blöcke (Empfehlung): aus `IDEALAKEWMSService/appsettings.json` `ErrorNotification` (`:17-20`), `WorkerSettings` (`:46-50`) und
  im Block `Sync` (`:25-45`) alles außer den vier `Feiertag*`-Schlüsseln entfernen. Bestehende Server-Dateien bleiben unberührt (harmlos).

### BH-5 – drei Entscheidungen

| Punkt | Ist (Beleg) | Empfehlung | Folgen |
|---|---|---|---|
| a) `admin` ohne Kennwort | Seed mit Leer-Hash `Program.cs:260-279`; Installer-Text `:29`, `:481` | **Entschieden (Nachtrag 2026-11-16):** Seed mit Erstkennwort + Haken, Pflichtwechsel (Regel P), Erstkennwort-Weg nach Rückfrage 11 + Start-Warnung + Hinweis `/Users`. *Ueberholt:* „Erstkennwort-Schritt (Ausnahme E)“ | Fenster bleibt bis zur ersten Anmeldung (wie heute, nur kürzer); Installer-Text: „beim ersten Anmelden als admin ein Kennwort festlegen“. Alternative Installer-Abfrage braucht eine zweite PBKDF2-Implementierung (`PasswordService.cs:8-22`) in PowerShell – Drift-Risiko, nicht testbar. |
| b) Cookie ohne `Secure` | `Program.cs:55-61` setzt nur `HttpOnly`/`IsEssential`/`Name`; Antiforgery nicht konfiguriert | `options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest` für Sitzung und `AddAntiforgery(o => o.Cookie.SecurePolicy = …SameAsRequest)` | HTTPS-Systeme (IDEAL-Prod, AKE-Prod/Test/Test-IDEAL, `ziele.psd1:9`, `:11-13`) senden das Cookie nur noch verschlüsselt; HTTP-Systeme (IDEAL-Test :88 `:10`, DEV `:14-15`) laufen unverändert. `Always` würde dort die Anmeldung brechen. Den heutigen Default (laut ASP.NET-Core-Doku `None`) belegt der Dev-Lauf in den Browser-Entwicklerwerkzeugen. |
| c) SMTP ohne TLS | `SmtpUseSsl` false → `SecureSocketOptions.None` (`MailService.cs:71`, `:78`): auch ein angebotenes STARTTLS wird nicht genutzt; ein gesetzter Benutzer meldet sich unverschlüsselt an (`:81-82`) | Warnung im Dienst-Log beim Senden, wenn `SmtpUsername` gesetzt und `SmtpUseSsl` false, plus `Pruefung/SmtpBenutzer` im Update-Skript | kein Verhaltenswechsel. Alternative `StartTlsWhenAvailable` verschlüsselt opportunistisch, aber ein Relay mit ungültigem/selbstsigniertem Zertifikat lässt dann jeden Versand scheitern (Fehlermails, Lieferverzug, Meldebestand). Wie die Relays je Standort konfiguriert sind, steht nur in den Server-Dateien → Prüfschritt (Rückfrage 9). |

## Technischer Loesungsentwurf

Kein neues Muster, keine neue Abhängigkeit, kein neuer Filter. ~~keine Migration~~ *(ueberholt durch Nachtrag 2026-11-16: eine
additive Migration für `User.MussKennwortAendern`, siehe Migrations-/SQL-Auswirkungen.)*

0. **Modell:** `User.MussKennwortAendern` (`bool`, `[Display(Name = "Muss Kennwort bei der nächsten Anmeldung ändern")]`) in
   `Models/User.cs` (Klasse `User : AuditableEntity`, `:5`); Migration `AddUserMussKennwortAendern` (`defaultValue: false`), SQL/152,
   FreshInstall an beiden Stellen. Name bewusst deutsch wie die Entscheidung (Muster `AltbestandPruefen`, SQL/141).
1. **`AccountController.Login` POST** (`:64-116`) – die Kennwortprüfung `:82-101` wird ersetzt:
   - `user.PasswordHash` leer → wenn `IsDevelopment()` (Rückfrage 3) wie heute anmelden, sonst Meldung R, View, keine Sitzung.
   - Feld leer → „Falsches Passwort.“ (Regel R; kein Leer-Hash mehr, siehe Seed).
     ~~Feld leer → wenn `VerifyPassword(hash, "")` (Ausnahme E): ist `vm.NewPassword` leer → View mit `KennwortFestlegen = true` …~~
     *(ueberholt, Nachtrag 2026-11-16)*
   - `VerifyPassword(hash, vm.Password)` falsch → „Falsches Passwort.“ wie heute (`:85-90`).
   - Richtig und `MussKennwortAendern = false` → Sitzung wie heute (`:104-115`).
   - Richtig und `MussKennwortAendern = true` (Regel P): ist `vm.NewPassword` leer → View mit `KennwortFestlegen = true`, Hinweis
     „Ihr Kennwort wurde vom Administrator vergeben. Bitte jetzt ein eigenes Kennwort festlegen.“, **keine Sitzung**. Sonst prüfen
     (nicht leer/Whitespace, `NewPassword == NewPasswordRepeat`, `!VerifyPassword(hash, NewPassword)`, Regeln nach Rückfrage 14) –
     Fehler → Meldung im `vm.ErrorMessage`, `KennwortFestlegen = true`, keine Sitzung, nichts gespeichert. Gültig →
     `PasswordHash = HashPassword(neu)`, `MussKennwortAendern = false`, `ModifiedAt/By/ByWindows`, `UpdateAsync`, Sitzung wie
     `:104-110`, `TempData["SuccessMessage"] = "Kennwort geändert."`. Prüfung am Schreibort nach ADR 0026 (bisheriges Kennwort und
     Haken werden im selben POST erneut geprüft, nichts aus dem Client übernommen).
   - **Kein** neuer Pfad: der Schritt postet an dieselbe Action `/Account/Login`; die LoginRedirect-Ausnahmeliste
     (`LoginRedirectMiddleware.cs:20-28`, [[fallstricke]] „LoginRedirect zählt Account-Pfade einzeln auf“) bleibt unverändert.
     `NormalizeUserForSession` greift auf `/account/*` ohnehin (`WindowsAutoLoginMiddleware.cs:68`).
   - Die Meldungen bleiben im bestehenden `vm.ErrorMessage` (`Login.cshtml:56-61`), kein TempData-`ErrorMessage`.
2. **`LoginViewModel`**: `NewPassword`, `NewPasswordRepeat` (`DataType.Password`), `KennwortFestlegen` (bool).
   **`Login.cshtml`**: Hinweis `:79` ersetzen durch „Kein Kennwort? Mit Windows anmelden oder beim Administrator melden.“; bei
   `KennwortFestlegen` Überschrift „Neues Kennwort festlegen“ (~~„Erstes Kennwort für admin festlegen“~~ *ueberholt*), Benutzername
   schreibgeschützt, Feld „Bisheriges Kennwort“ (`autocomplete="current-password"`) und die zwei neuen Felder mit
   `autocomplete="new-password"`, Knopf „Kennwort ändern und anmelden“, Link „Abbrechen“ (GET `/Account/Login`, keine Sitzung);
   im Entwicklungsmodus der Hinweis (Rückfrage 3). Skill `frontend-design` ist Pflicht (Bootstrap-Bestand, Kontrast AA).
3. **`UsersController.Index`** (`:70-94`): aus der schon geladenen Liste `users` (`:77`) die aktiven ohne Hash und die aktiven mit
   `MussKennwortAendern` ermitteln (kein `VerifyPassword` mehr nötig, ~~`admin` mit Leer-Hash~~ *ueberholt*) → `ViewBag`, Banner in
   `Views/Users/Index.cshtml` (Bootstrap `alert-warning`, Text nicht nur Farbe). Keine neue Spalte, daher keine Listen-View-Pflichten.
3a. **`UsersController.Create`/`Edit`** (`:103-151`, `:230-326`) + `UserEditViewModel` (neu `MussKennwortAendern`,
   `ErstkennwortErzeugen`) + Views (`Create.cshtml:89-93`, `Edit.cshtml:173-177`): Feld „Erstkennwort“ (bisher „Passwort“), Checkbox
   „Erstkennwort erzeugen“ und Checkbox „Muss Kennwort bei der nächsten Anmeldung ändern“ (Edit zeigt den gespeicherten Stand).
   Server: erzeugen hat Vorrang vor dem eingegebenen Feld; neues Kennwort (eingegeben oder erzeugt) → Hash + Haken (Abwählen nach
   Rückfrage 15); Haken ohne vorhandenes Kennwort → Speichern mit `TempData["WarningMessage"]` „Haken gesetzt, aber kein Kennwort –
   der Benutzer kann sich im Formular erst nach dem Setzen eines Kennworts anmelden.“. Erzeugtes Kennwort genau einmal anzeigen
   (Rückfrage 12). Audit wie heute (`:131-133` bzw. `:310-312`).
3b. **`AccountController.Profile` POST** (`:222-227`): bei gesetztem `newPassword` zusätzlich `MussKennwortAendern = false`.
3c. **`IPasswordService`/`PasswordService`**: `GenerateInitialPassword()` (eine Zeile `RandomNumberGenerator.GetString(alphabet, 12)`
   bzw. `GetItems`; .NET-Bordmittel, keine Abhängigkeit).
4. **`Program.cs`** Seed (`:259-279`): Bedingung `IsNullOrEmpty(hash) || VerifyPassword(hash, "")` → Erstkennwort nach Rückfrage 11,
   `MussKennwortAendern = true`, `ModifiedAt = DateTime.Now`, `ModifiedBy = ModifiedByWindows = "system"` (bei Neuanlage
   `Created*` wie `:268-270`); kein `HashPassword("")` mehr. Danach `if (adminUser.MussKennwortAendern) app.Logger.LogWarning("Benutzer
   admin hat ein Erstkennwort – bei der ersten Anmeldung ändern.")` (~~Leer-Hash-Warnung~~ *ueberholt*); Cookie-Flags (BH-5b).
5. **`MailService`** (BH-5c Empfehlung): vor `ConnectAsync` (`:79`) `LogWarning`, wenn `username` gesetzt und `!useSsl`.
6. **`00_FreshInstall.sql`**: `:1260-1263` löschen; Guards `:1803`/`:1838`. Nachweis siehe AK 9-11.
7. **`Install-IdealAkeWms.ps1`**: BH-3/BH-4 wie oben; Text `:29` („admin-Passwort setzen (ist nach der Installation LEER …)“) und
   `:481` („Passwort LEER“) auf „als admin mit dem Erstkennwort anmelden, das WMS verlangt sofort ein eigenes Kennwort“ umstellen;
   bei Rückfrage 11 = S2 gibt Schritt 6 nach erfolgreichem Aufwärmen (`$script:WebGestartet`) das Erstkennwort aus der Datei einmal aus
   und löscht die Datei (fehlt sie: Hinweis „admin hat bereits ein Kennwort“).
8. **Manifest** `nachlauf.psd1`: Einträge unter „Deploy“.
9. **ADR 0027** – Umsetzungs-Lieferobjekt (Dev-Lauf schreibt die Datei im Hauptcheckout, `secondbrain/architektur/adr/`; die Spec-Phase
   legt sie nicht an). Nummer geprüft 2026-11-16: höchste vorhandene ADR 0026, 0028/0029 von
   [[2026-11-16-beschichtungslogik-je-standort-spec]] bzw. [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]] reserviert. Entwurf
   (angepasst durch Nachtrag 2026-11-16):

   > **0027 – Formular-Anmeldung nur mit gespeichertem Kennwort; Erstkennwort durch Admin mit Pflichtwechsel; AD-Benutzer ohne
   > Kennwort nur per Windows.**
   > Kontext: BH-1, BH-5a. Optionen A/B/C wie oben; für Benutzer ohne Kennwort zusätzlich „Selbstvergabe beim ersten Login“ –
   > verworfen, weil jeder, der den Benutzernamen kennt, das Konto übernehmen kann (wer zuerst kommt, setzt das Kennwort; der echte
   > Benutzer ist ausgesperrt). Entscheidung: Regel R (kein Formular ohne gespeichertes, nicht leeres Kennwort), Regel P
   > (Admin/Seed vergibt Erstkennwort, `User.MussKennwortAendern`, Pflichtwechsel vor der Sitzung, zustandslos, Server prüft je POST),
   > `admin`-Seed mit Erstkennwort + Haken statt Leer-Hash, Entwicklungs-Ausnahme (falls Rückfrage 3 so entschieden), kein Schalter,
   > Übergang über `Pruefung/BenutzerOhneKennwort`. Konsequenzen: Terminals und Handgeräte brauchen Kennwörter (Erstkennwort mit
   > Pflichtwechsel, Gerätekonten nach Rückfrage 15); QA unter Kestrel; Rückweg admin per `/Users/Edit` oder SQL `NULL` + Neustart →
   > Erstkennwort + Haken; Windows-SSO prüft weder Kennwort noch Haken; ergänzt (nicht ersetzt) ADR 0002.

## Migrations-/SQL-Auswirkungen

- ~~**Keine EF-Migration**, kein Modellwechsel, keine `MigrationId`~~ *(ueberholt durch Nachtrag 2026-11-16)*. Keine AgentJobs
  betroffen: kein Skript unter `SQL/AgentJobs/` liest oder schreibt `dbo.Users` (Suche `dbo.Users` dort ohne Treffer, 2026-11-16).
- **EF-Migration `AddUserMussKennwortAendern`** (additiv, nicht destruktiv): Model `User.MussKennwortAendern` →
  `dotnet ef migrations add AddUserMussKennwortAendern --project IdealAkeWms` (Spalte `bit NOT NULL`, `defaultValue: false`; folgt
  auf `20261114080950_AddPurchaseOrderReceiptAbsendekennung`, Dev-Lauf prüft die letzte Migration beim Start, weil der laufende
  /goal-Lauf [[2026-11-16-goal-lauf-beschichtung-fa-sdata-arbeitsgaenge]] im selben Worktree baut).
- **`SQL/152_AddUserMussKennwortAendern.sql`** im Muster `SQL/141_AddPickingOrderAltbestandPruefen.sql`: `OBJECT_ID(N'[dbo].[Users]',
  N'U')`-Vorprüfung mit `NOEXEC`, dann eigener Batch `IF COL_LENGTH(N'dbo.Users', N'MussKennwortAendern') IS NULL ALTER TABLE
  [dbo].[Users] ADD [MussKennwortAendern] bit NOT NULL CONSTRAINT [DF_Users_MussKennwortAendern] DEFAULT CAST(0 AS bit);`, danach
  separater Batch `__EFMigrationsHistory`-Insert mit `IF NOT EXISTS`. Bestehende Zeilen bekommen `0` (kein Pflichtwechsel für
  vorhandene Kennwörter, kein Datenverlust, kein Backup-Zwang über das übliche hinaus). **Nummer 152**: höchste Datei im Worktree
  `SQL/150_…` (geprüft 2026-11-16), 151 bleibt für die bedingte Index-Reparatur dieser Spec (Rückfrage 4, Antwortzeile 4 nennt sie so),
  152 ist frei ([[2026-11-16-beschichtungslogik-je-standort-spec]] Antwort 9 gibt sie zurück, [[2026-11-16-goal-lauf-beschichtung-fa-sdata-arbeitsgaenge]]
  Z. 16 „152 frei/ungenutzt“), 153 ist von [[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec]] reserviert. Dev-Lauf prüft
  beim Start erneut.
- `SQL/00_FreshInstall.sql`: Stelle (a) Schema-Objekte korrigiert (`:1260-1263` weg, zwei Guards) **und** im `CREATE TABLE [dbo].[Users]`
  (`:24-55`) die Spalte `[MussKennwortAendern] BIT NOT NULL DEFAULT 0` ergänzt; der `admin`-Insert (`:1541-1549`, `PasswordHash` NULL)
  bleibt – der Start-Seed setzt Erstkennwort + Haken. Stelle (b) `MigrationId`-Block: neue Zeile nach `:3262-3264`. ADR 0004 bleibt
  erfüllt (Konsolidat = Modell).
- **SQL/151 nur nach Rückfrage 4** (P-2b zeigt fehlende Indizes auf IDEAL-Prod): `SQL/151_RepairFreshInstallIndexes.sql` = der
  korrigierte Abschnitt 15 (`:1209-1277` ohne die zwei Zeilen), jede Anweisung mit `IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE
  name = …)` bzw. `OBJECT_ID(…,'F') IS NULL`, eigener Batch, keine `ROLLBACK`, keine sqlcmd-Direktiven, kein History-Eintrag (keine
  Migration). Läuft nur auf IDEAL-Prod (FreshInstall-Herkunft, Namen identisch); auf upgegradeten DBs können gleichwertige Indizes
  unter EF-Namen existieren – dort **nicht** laufen lassen (Duplikate).
- Prüfabfragen (lesend, Gerald in SSMS auf der WMS-DB des jeweiligen Systems; **nicht** auf Sage):

```sql
-- P-1 Benutzer ohne Kennwort (je System: IDEAL-Prod, IDEAL-Test, AKE-Prod, AKE-Test, AKE-Test-IDEAL)
SELECT [Key], [Value] FROM dbo.AppSettings WHERE [Key] IN (N'WindowsAuthAktiv', N'WindowsAuthBerechtigungsgruppe');

SELECT CASE WHEN u.WindowsUserName IS NULL THEN N'Lokal' ELSE N'AD' END AS Typ,
       u.Id, u.Name, u.WindowsUserName, u.IsPicker,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.BdeTerminals t WHERE t.UserId = u.Id) THEN 1 ELSE 0 END AS IstTerminal,
       STUFF((SELECT N', ' + r.[Key] FROM dbo.UserRoles ur JOIN dbo.Roles r ON r.Id = ur.RoleId
              WHERE ur.UserId = u.Id ORDER BY r.SortOrder FOR XML PATH('')), 1, 2, N'') AS Rollen
FROM dbo.Users u
WHERE u.IsActive = 1 AND (u.PasswordHash IS NULL OR u.PasswordHash = N'')
ORDER BY Typ, u.Name;

SELECT CASE WHEN WindowsUserName IS NULL THEN N'Lokal' ELSE N'AD' END AS Typ, COUNT(*) AS AktivOhneKennwort
FROM dbo.Users WHERE IsActive = 1 AND (PasswordHash IS NULL OR PasswordHash = N'')
GROUP BY CASE WHEN WindowsUserName IS NULL THEN N'Lokal' ELSE N'AD' END;
-- Ob admin noch das leere Kennwort hat, zeigt SQL nicht (Hash mit Salz, PasswordService.cs:10-21):
-- Prüfschritt im Browser des alten Stands: admin + leeres Kennwort -> wird angemeldet = sofort Kennwort setzen.
-- (Nachtrag 2026-11-16: nicht mehr zwingend – der Start-Seed von v1.99.24 stellt einen Leer-Hash-admin auf Erstkennwort + Haken um.)

-- P-1c nach dem Deploy (ab SQL/152): offene Erstkennwörter
SELECT u.Id, u.Name, u.WindowsUserName, u.ModifiedAt, u.ModifiedBy
FROM dbo.Users u WHERE u.IsActive = 1 AND u.MussKennwortAendern = 1 ORDER BY u.Name;

-- P-2a falsche Indizes/Spalten (erwartet: NULL, NULL und 0 Zeilen)
SELECT COL_LENGTH(N'dbo.ProductionOrders', N'AssignedPickerId') AS Spalte1, COL_LENGTH(N'dbo.ProductionOrders', N'IsReleasedForPicking') AS Spalte2;
SELECT name FROM sys.indexes WHERE name IN (N'IX_ProductionOrders_IsReleasedForPicking_IsDone', N'IX_ProductionOrders_AssignedPickerId');

-- P-2b fehlen Indizes aus Abschnitt 15? (IDEAL-Prod; Liste = 00_FreshInstall.sql:1209-1277 ohne :1260-1263)
SELECT v.n AS Fehlt FROM (VALUES
 (N'IX_StockMovements_ArticleId'),(N'IX_StockMovements_StorageLocationId'),(N'IX_StockMovements_Timestamp'),
 (N'IX_StockMovements_SourceStorageLocationId'),(N'IX_StockMovements_ProductionOrderId'),(N'IX_SageBookingQueueItems_Status'),
 (N'IX_SageBookingQueueItems_StockMovementId'),(N'IX_StorageLocations_IsActive'),(N'IX_StorageLocations_Source'),
 (N'IX_StorageLocations_IstBuchbar'),(N'IX_StorageLocations_StorageAreaId'),(N'IX_StorageAreas_Lagerkennung'),
 (N'UQ_ArticleSageDetails_ArticleId'),(N'IX_SyncLogs_Timestamp_Desc'),(N'IX_SyncLogs_Service_Level'),
 (N'IX_WorkstationUsers_WorkstationId'),(N'IX_WorkstationUsers_UserId'),(N'IX_ProductionOrders_OrderNumber'),
 (N'IX_ProductionOrders_SubOrderNumber'),(N'IX_ProductionOrders_ArticleNumber'),(N'IX_ProductionOrders_IsDone'),
 (N'IX_ProductionOrders_IsCancelled'),(N'IX_ProductionOrders_ProductionWorkplaceId'),(N'IX_PickingItems_ProductionOrderId'),
 (N'IX_PickingItems_POId_IsPicked'),(N'IX_ProductionWorkplaceUsers_ProductionWorkplaceId_UserId'),
 (N'IX_ProductionWorkplaceUsers_UserId'),(N'IX_WorkOperations_ProductionOrderId'),(N'IX_WorkOperations_ProductionOrderId_Sequence'),
 (N'IX_WorkOperations_ProductionWorkplaceId')) v(n)
WHERE NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.name = v.n)
UNION ALL
SELECT N'FK_StockMovements_ProductionOrders_ProductionOrderId' WHERE OBJECT_ID(N'dbo.FK_StockMovements_ProductionOrders_ProductionOrderId', N'F') IS NULL;
```

Der Dev-Lauf gleicht die P-2b-Liste gegen den Abschnitt 15 des korrigierten Skripts ab (gleiche Namen, keine vergessen).

## Einstellungen

Kein neuer Schlüssel. Bewusst **kein** Schalter für die Anmelderegel (Variante C verworfen, Begründung unter „Varianten BH-1“).
`MailSettings:*` bleiben appsettings-only (ADR 0008/0024), unverändert in Name und Bedeutung.
`User.MussKennwortAendern` (Nachtrag 2026-11-16) ist ein Benutzer-Datenfeld, keine Einstellung nach ADR 0020 (kein AppSetting,
kein ServiceSetting, kein Schalter).

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:ProductionOrdersEnabled` (bestehend, nur **Installationswert** geändert, Variante A Rückfrage 6) | Bool (Schalter) | `true` / `false` | Katalog unverändert `true` (`ServiceSettingDefinitions.cs:199`); **Neuinstallation per Skript: `false`** | aus = der Dienst holt keine Fertigungsaufträge aus Sage | Dienst `SyncWorker.cs:43`, Pflege `/ServiceSettings` | beide | „Einschalten startet den FA-Abgleich aus Sage beim nächsten Zyklus – erst nach Teil 2 / Standort-Konfiguration.“ (nur Installer-Ausgabe; Katalogtext unverändert) |

## Audit-Feld-Auswirkungen

- Pflichtwechsel im Login (Regel P; ~~Erstkennwort-Schritt Ausnahme E~~ *ueberholt*): `ModifiedAt = DateTime.Now` (Lokalzeit,
  ADR 0025), `ModifiedBy` = Name des Benutzers (es gibt noch keine Sitzung), `ModifiedByWindows =
  _currentUserService.GetWindowsUserName()` – beim Formular-Login `"SYSTEM"` (bewusst akzeptiert, [[fallstricke]] §4
  „Antiforgery-Wurzel-Fix“). Gleiches Muster wie `AccountController.cs:225-227`. Gilt für Löschen des Hakens und neuen Hash zugleich.
- Haken setzen/löschen und Erstkennwort über `/Users/Edit`: bestehende Audit-Zeilen `UsersController.cs:310-312`; `/Users/Create`:
  `Created*` (`:131-133`), wie heute.
- `/Account/Profile` löscht den Haken: bestehende Audit-Zeilen `AccountController.cs:225-227`.
- Start-Seed setzt Erstkennwort + Haken: `ModifiedAt = DateTime.Now`, `ModifiedBy`/`ModifiedByWindows = "system"` (wie `Created*` in
  `Program.cs:268-270`); bei Neuanlage nur `Created*`. Bisher setzt `Program.cs:277` den Hash **ohne** Audit – wird mitkorrigiert.
- `SQL/152` setzt den Default `0` für alle Bestandszeilen ohne Audit (Schemaänderung, kein fachliches Update).
- Installer-`UPDATE` auf `ServiceSettings` (BH-4 A): Tabelle ohne Audit-Spalten (`00_FreshInstall.sql:1097-1102`, kein `AuditableEntity`,
  vgl. [[fallstricke]] „AppSettings ist kein AuditableEntity“) – die Installer-Ausgabe und das Transcript (`Install-…ps1:104`) belegen es.
- Rückweg „admin-Kennwort per SQL `NULL`“: Hand-Schritt ohne Audit; Doku nennt das.

## Akzeptanzkriterien

1. Lokaler aktiver Benutzer ohne Kennwort: Formular mit leerem Feld → Meldung R, keine Sitzung (Startseite leitet wieder auf
   `/Account/Login`). Mit beliebigem Text im Feld → dieselbe Meldung.
2. AD-Benutzer ohne Kennwort: Formular → Meldung R (mit Hinweis „Mit Windows anmelden“, wenn `WindowsAuthAktiv`); SSO am Domänen-PC
   meldet ihn weiter an (TS-40.1, Manual-UAT).
3. Benutzer mit Kennwort ohne Haken: richtiges Kennwort → angemeldet; falsches → „Falsches Passwort.“; leeres Feld → „Falsches
   Passwort.“ (kein Pflichtwechsel-Schritt).
4. *(Neu gefasst, Nachtrag 2026-11-16; ~~`admin` mit Leer-Hash, leeres Feld → Schritt „Erstes Kennwort festlegen“~~ ueberholt.)*
   Benutzer mit Kennwort **und** Haken: richtiges Kennwort → Schritt „Neues Kennwort festlegen“, **keine Sitzung** (`/` leitet weiter
   auf Login). Im Schritt jeweils Meldung, keine Sitzung, Hash und Haken unverändert bei: neues Kennwort leer bzw. nur Leerzeichen;
   neues = bisheriges; Wiederholung ungleich; bisheriges Kennwort falsch. Gültige Eingabe → angemeldet, Hash neu, Haken `false`,
   `ModifiedAt/By/ByWindows` gesetzt; danach Anmeldung mit dem Erstkennwort → „Falsches Passwort.“, mit dem neuen → direkt angemeldet.
5. Ein manipulierter POST mit `NewPassword` für einen Benutzer **ohne** Haken, ohne Hash oder mit falschem bisherigem Kennwort ändert
   nichts (Hash unverändert, keine Sitzung). (~~Hash bestätigt das leere Kennwort nicht~~ *ueberholt*.)
5a. Abbruch des Schritts (Link „Abbrechen“, Zurück, Tab schließen) ergibt keine Sitzung; der nächste Login mit dem Erstkennwort zeigt
   den Schritt erneut.
5b. Benutzer **ohne** Kennwort, bei dem der Haken manuell gesetzt ist: weiterhin Meldung R, keine Sitzung (Regel R vor Regel P).
6. `IsDevelopment()` (nur `dotnet run`): Benutzer ohne Hash meldet sich mit leerem Feld an, Hinweis „Entwicklungsmodus …“ ist auf der
   Anmeldeseite sichtbar – nur falls Rückfrage 3 so entschieden; unter IIS (`Production`/`DEV`) gilt AK 1.
7. `/Users` zeigt für Admins den Hinweis mit Anzahl und Namen der aktiven Benutzer ohne Kennwort, getrennt lokal/AD, und die aktiven
   Benutzer mit offenem Erstkennwort (Haken, `admin` eingeschlossen; ~~„admin hat noch kein Kennwort“~~ *ueberholt*); ohne Treffer kein
   Hinweis.
8. Web-Start, solange `admin` den Haken trägt, schreibt genau eine Warnung ins Log, ohne das Kennwort. (~~mit Leer-Hash~~ *ueberholt*)
8a. Start-Seed: `admin` fehlt, hat `PasswordHash` NULL/leer **oder** einen Leer-Hash → nach dem Start Erstkennwort (Weg nach
   Rückfrage 11) + Haken, `Modified*` = `system`; die Anmeldung mit leerem Feld ergibt „Falsches Passwort.“. `admin` mit eigenem
   Kennwort ohne Haken → unverändert (Hash und `ModifiedAt` gleich). Kein Code-Weg setzt mehr `HashPassword("")` (Suche ohne Treffer).
8b. `/Users/Create` und `/Users/Edit`: eingegebenes Kennwort → Hash + Haken gesetzt; „Erstkennwort erzeugen“ → Hash + Haken, das
   erzeugte Kennwort (12 Zeichen) steht genau einmal im Erfolgs-Banner (bzw. Ort nach Rückfrage 12) und ist nach Neuladen weg; es
   steht weder in der DB noch im Log. Haken manuell setzen ohne neues Kennwort → gespeichert, Audit gesetzt. Haken manuell abwählen →
   nach Rückfrage 15. Haken ohne Kennwort → Warnhinweis. AD-Benutzer: dieselben Felder, SSO danach unverändert (AK 2).
8c. `/Account/Profile` mit neuem Kennwort löscht den Haken.
8d. `SQL/152` zweimal hintereinander auf einer DEV-Kopie: erster Lauf legt die Spalte an und trägt die History ein, zweiter meldet
   „existiert bereits“, Exit 0; Bestandsbenutzer haben `0`. Die FreshInstall-Prüf-DB (AK 9/10) hat die Spalte und die `MigrationId`.
9. `00_FreshInstall.sql` (Kopie mit ersetztem Kopf `:7-17` → `USE [WMS_FreshInstall_Pruefung]`) läuft mit
   `sqlcmd -S . -E -C -b -I -f 65001` auf einer neu angelegten leeren DB der **lokalen** Instanz `.` mit Exit-Code 0 und ohne
   „Msg“/„Meldung“ durch; ein zweiter Lauf gegen dieselbe DB ebenso. Vorher `SELECT @@SERVERNAME` ins Protokoll – nie gegen
   `AKESQL20` oder einen IDEAL-Server. Danach `DROP DATABASE [WMS_FreshInstall_Pruefung]`. Die Kopie liegt im Scratch-Ordner, nicht im Repo.
10. In der Prüf-DB aus AK 9: alle Namen aus P-2b vorhanden; `SELECT COUNT(*) FROM dbo.__EFMigrationsHistory` = Anzahl der Migrationen
    in `IdealAkeWms/Migrations` (ohne `.Designer.cs` und Snapshot); ein Web-Start (Kestrel, `ConnectionStrings__DefaultConnection`
    auf die Prüf-DB) läuft ohne Migrationsfehler und versieht `admin` mit Erstkennwort + Haken (Pflichtwechsel erreichbar;
    ~~mit Leer-Hash an (Erstkennwort-Schritt erreichbar)~~ *ueberholt*). Erst danach AK 9 `DROP`.
11. `FreshInstallIndexSpaltenTests` grün; die Ausnahmeliste nennt je Tabelle den Grund. Mit den alten Zeilen `:1260-1263` schlägt der
    Test fehl (im Dev-Lauf einmal belegt).
12. Installer: kein Parameter `SageLagerbuchungCredential`, kein Abschnitt `SageLagerbuchung` mit Kennwort in den geschriebenen Dateien;
    Warnung bei `-SmtpCredential`; nach erfolgreichem Aufwärmen steht `Sync:ProductionOrdersEnabled = false` in der DB (Variante A),
    sonst eine Warnung mit der Anweisung. PowerShell-Parser ohne Fehler
    (`[System.Management.Automation.Language.Parser]::ParseFile(…)`); voller Lauf = Manual-UAT bei der nächsten Neuinstallation.
13. `IDEALAKEWMSService/appsettings.json` enthält (falls Rückfrage 6 so) nur noch die gelesenen Abschnitte; Dienst-Tests grün; der
    Feiertag-Rückfall bleibt (Schlüssel vorhanden).
14. HTTPS-System: Sitzungs- und Antiforgery-Cookie tragen `Secure`; HTTP-System (DEV :8081/:8082): Anmeldung funktioniert, Cookies
    ohne `Secure`.
15. SMTP (Empfehlung): mit gesetztem `SmtpUsername` und `SmtpUseSsl = false` schreibt der Dienst je Versand eine Warnung ins Log; die
    Mail geht wie bisher (DEV: Pickup-Ordner → keine Warnung, weil kein SMTP-Weg, `MailService.cs:61-68`).
16. `Update-IdealAkeWms.ps1 -NurPruefen` auf DEV zeigt `Pruefung/BenutzerOhneKennwort` mit Zahl und Hinweis (Nicht-Prod); auf einem
    Prod-Ziel mit Zahl > 0 bricht der Lauf vor dem Stoppen ab (Logik `Update-IdealAkeWms.ps1:701`, im Dev-Lauf per Code-Lesung und
    `-NurPruefen` gegen eine DEV-Kopie mit `Prod=$true`-Testziel oder als Manual-UAT belegt). `NachlaufManifestDriftTests` grün.
17. Doku: `docs/betrieb/03` (Z. 92, 115), `07` (Abschnitt 2: Meldung R, Erstkennwort, Rückweg admin), `09` (Z. 20-21, 153-154,
    R1/R2/R4/R5/R7 Z. 187-193) und `README.md:63` sagen dasselbe wie der Code; Hilfe (Anmeldung, Benutzerverwaltung) und
    `Views/Help/Changelog.cshtml` v1.99.24; beide `AppVersion.cs` auf 1.99.24. *Nachtrag 2026-11-16:* `07-troubleshooting.md`
    Abschnitt 2 beschreibt Meldung R, den Pflichtwechsel und den umgedeuteten Rückweg (Erstkennwort mit Haken; SQL `NULL` + Neustart);
    `09-sicherheit-konten-rechte.md` Z. 21 (`admin` „ohne Kennwort angelegt“ → Erstkennwort + Pflichtwechsel) und R1/R2 (Z. 187-188)
    entsprechend; Installer-Texte `:29`/`:481` ohne „LEER“.
18. Build und alle Tests grün; neue Tests in `AccountControllerTests` für AK 1, 3, 4, 5, 5a, 5b (und 6, falls entschieden), in
    `UsersControllerTests` für AK 8b, für den Seed (AK 8a, InMemory) und `GenerateInitialPassword` (Länge, Alphabet, zwei Aufrufe
    verschieden).
19. Manifest: `SQL/152` als Schema-Eintrag (vor den Web-Schritten), `Pruefung/BenutzerOhneKennwort` mit dem neuen Abhilfe-Text
    („Erstkennwort mit Pflichtwechsel setzen“); `NachlaufManifestDriftTests` grün.

## Test-Szenarien

Neu **TS-139** in `docs/TESTSZENARIEN.md` (danach `secondbrain/tests/testszenarien-index.md`); angepasst: TS-1.6 (`:174` „admin mit
Passwort leer“ → Erstkennwort + Pflichtwechsel, Verweis TS-139.4), TS-40.4 (`:5250` „ohne Passwort“ → „ohne Kennwort, Formular gesperrt“), TS-40.7
(`:5299` „Formular-Anmeldung mit lokalen Credentials“ → „mit Kennwort“). Windows-SSO/Negotiate, LDAP und Mobilgeräte = **Manual-UAT**.

- **TS-139.1 Lokaler Benutzer ohne Kennwort.** Vorb.: DEV-IIS (nicht `dotnet run`), Benutzer `ts139-lokal` ohne Kennwort, aktiv, Rolle
  `stock`. Schritte: abmelden, Name, Feld leer → Anmelden; dann mit Text `x`. Erwartet: Meldung R, Anmeldeseite bleibt, `/` leitet auf
  Login. Negativ: inaktiver Benutzer → „Benutzer nicht gefunden oder inaktiv.“ wie bisher.
- **TS-139.2 AD-Benutzer ohne Kennwort.** Vorb.: AKE-Test (HTTPS, IIS), `WindowsAuthAktiv = true`, AD-Benutzer ohne Kennwort. Schritte:
  a) Handy-Browser, Formular, Feld leer; b) Knopf „Mit Windows anmelden“ am Handy; c) Domänen-PC frische Sitzung. Erwartet: a) Meldung R
  mit Windows-Hinweis; b)/c) angemeldet (TS-40.9/40.1). **Manual-UAT.** Negativ: `WindowsAuthAktiv = false` → Meldung ohne Windows-Hinweis, kein Knopf.
- **TS-139.3 Kennwort richtig/falsch/leer.** Vorb.: Benutzer mit Kennwort. Erwartet wie AK 3.
- **TS-139.4 Erstkennwort admin (Seed/Rückweg).** *(neu gefasst, Nachtrag 2026-11-16; ~~Feld leer → Schritt erscheint~~ ueberholt)*
  Vorb.: Prüf-DB aus AK 10 bzw. DEV-Kopie mit `admin`-Hash `NULL` (Rückweg) + App-Pool-Neustart; zweite Variante: DEV-Kopie mit
  Leer-Hash-`admin` aus dem alten Stand. Schritte: Erstkennwort auf dem Weg nach Rückfrage 11 holen; `admin` mit leerem Feld → „Falsches
  Passwort.“; `admin` + Erstkennwort → Schritt „Neues Kennwort festlegen“; gültig abschließen. Erwartet wie AK 4/8a; `/Users`-Hinweis
  „offenes Erstkennwort: admin“ vorher sichtbar, nachher weg; Log-Warnung beim Start vorher vorhanden, enthält kein Kennwort.
- **TS-139.4a Pflichtwechsel lokaler Benutzer – positiv.** Vorb.: DEV-IIS, Admin angemeldet. Schritte: `/Users/Create` → `ts139-erst`,
  Rolle `stock`, Kennwort `Start-139a` eingeben → speichern; `/Users/Edit` zeigt Haken gesetzt; abmelden; `ts139-erst` + `Start-139a`.
  Erwartet: Schritt „Neues Kennwort festlegen“, `/` leitet auf Login (keine Sitzung); neues `Neu-139a` zweimal + bisheriges →
  angemeldet, „Kennwort geändert.“; `/Users/Edit` Haken weg, `ModifiedBy = ts139-erst`; abmelden, `Start-139a` → „Falsches Passwort.“,
  `Neu-139a` → direkt angemeldet.
- **TS-139.4b Pflichtwechsel – Negativfälle.** Vorb.: `ts139-erst` erneut mit Erstkennwort (Edit, neues Kennwort `Start-139b`).
  Schritte je Fall Login + Schritt: (1) neues Kennwort leer; (2) nur Leerzeichen; (3) neu = `Start-139b`; (4) Wiederholung ungleich;
  (5) bisheriges Kennwort im Schritt falsch; (6) Link „Abbrechen“ bzw. Browser-Zurück, dann `/` aufrufen; (7) per Browser-Konsole POST
  mit `NewPassword` für einen Benutzer ohne Haken. Erwartet: (1)-(5) Meldung, keine Sitzung, Haken bleibt, Login mit `Start-139b`
  zeigt den Schritt wieder; (6) keine Sitzung, Login-Seite; (7) nichts geändert (AK 5).
- **TS-139.4c Erzeugtes Erstkennwort.** Schritte: `/Users/Edit` `ts139-erst`, „Erstkennwort erzeugen“ → speichern. Erwartet: Banner
  zeigt ein 12-stelliges Kennwort genau einmal (Ort nach Rückfrage 12); Seite neu laden bzw. `/Users/Edit` erneut → nicht mehr sichtbar;
  `SELECT PasswordHash, MussKennwortAendern FROM dbo.Users WHERE Name = N'ts139-erst'` zeigt Hash ≠ Klartext und `1`; Web-Log
  enthält das Kennwort nicht. Login mit dem erzeugten Kennwort → Pflichtwechsel wie 139.4a.
- **TS-139.4d Haken manuell.** Schritte: Benutzer mit eigenem Kennwort, `/Users/Edit` Haken setzen ohne neues Kennwort → speichern →
  Login mit dem bisherigen Kennwort → Schritt erscheint. Abwählen: nach Rückfrage 15 (erlaubt → nächster Login ohne Schritt; nie →
  Checkbox nicht abwählbar bzw. Server setzt zurück). Negativ: Haken bei Benutzer **ohne** Kennwort → Warnhinweis beim Speichern,
  Login mit leerem Feld → Meldung R (AK 5b).
- **TS-139.4e AD-Benutzer mit Erstkennwort.** Vorb.: AKE-Test, `WindowsAuthAktiv = true`, AD-Benutzer. Schritte: Erstkennwort unter
  `/Users/Edit`; a) Domänen-PC frische Sitzung; b) Handy-Formular mit Erstkennwort. Erwartet: a) SSO angemeldet ohne Schritt, Haken
  bleibt; b) Pflichtwechsel wie 139.4a. **Manual-UAT** (Handgerät: Bildschirmtastatur/Scanner-Fokus nach Rückfrage 16).
- **TS-139.5 Übergang Terminal.** Vorb.: BDE aktiv, Terminal-Benutzer ohne Kennwort (DEV). Schritte: `-NurPruefen` → Zahl ≥ 1 mit
  Hinweis „Erstkennwort mit Pflichtwechsel setzen“; als Admin unter `/Users/Edit` Erstkennwort setzen; `-NurPruefen` → 0; am Terminal
  mit Erstkennwort anmelden → Pflichtwechsel (bzw. ohne, falls Rückfrage 15 Gerätekonten ohne Haken erlaubt) → BDE-Terminal öffnet
  mit Default-Werkbank. Negativ: ohne Kennwort → Meldung R.
- **TS-139.6 Entwicklungsmodus** (nur falls Rückfrage 3 = Ausnahme): `dotnet run`, Benutzer ohne Kennwort → angemeldet, Hinweis sichtbar;
  dieselbe DB über DEV-IIS → Meldung R.
- **TS-139.7 Erstinstallation auf leerer DB.** Schritte und Erwartung wie AK 9-10, Protokoll mit `@@SERVERNAME`, Exit-Codes, Zählungen,
  `DROP`. Negativ: Wächter-Test mit den alten Zeilen rot (AK 11).
- **TS-139.8 Index-Prüfung IDEAL-Prod** (nur falls SQL/151): `-NurPruefen` zeigt `Pruefung/FreshInstallIndizes` mit Zahl; Reparatur in 4a
  nach Bestätigung; danach P-2b leer. **Manual-UAT Gerald.**
- **TS-139.9 Installer.** Parser-Prüfung; Code-Lesung: keine Kennwort-Ausgabe außer dem `admin`-Erstkennwort (nur bei Rückfrage 11 = S2,
  einmal, Datei danach gelöscht), kein `SageLagerbuchung`-Kennwort; Voll-Lauf bei der
  nächsten Neuinstallation (Manual-UAT): `/ServiceSettings` zeigt `Sync:ProductionOrdersEnabled = false`, Ausgabe nennt Einschalten nach Teil 2.
- **TS-139.10 Cookies.** AKE-Test (HTTPS) und DEV (HTTP): Entwicklerwerkzeuge › Anwendung › Cookies `IdealAkeWms.Session` und
  `.AspNetCore.Antiforgery.*`: HTTPS mit `Secure`, HTTP ohne; Anmeldung und ein Formular-POST (Profil speichern) auf beiden ohne 400.
- **TS-139.11 SMTP-Hinweis** (Empfehlung): DEV-Dienst mit `SmtpHost` auf einen nicht vorhandenen Host, `SmtpUsername = test`,
  `SmtpUseSsl = false`, Fehlermail auslösen → Warnung im Notifications-Log, danach der bekannte Versandfehler. Negativ: ohne Benutzer
  keine Warnung. Danach Werte zurück.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

> [!success] Final (QA-Lauf 2026-11-16, gegen den echten Diff `5163df17..4f3bf15d` ohne `f12f46f7`, plus QA-Fix `8f24ec51`) – geht allem darunter vor
> - **deploy.web: true** (`IdealAkeWms/`: AccountController, UsersController, Program.cs, AdminSeed, PasswordService, Views Account/Users/Help).
> - **deploy.service: true** (`IDEALAKEWMSService/`: `MailService`-Warnung, `appsettings.json` ohne tote Blöcke – die Server-Datei wird nicht überschrieben, der Dienst braucht nur die neuen DLLs).
> - **deploy.migration: true** (`IdealAkeWms/Migrations/20261116165705_AddUserMussKennwortAendern.cs` neu) → `SQL/152_AddUserMussKennwortAendern.sql`, additiv. Kein SQL/151.
> - **Paket:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` – Version **1.99.29** (HEAD enthält die darüberliegende Spec Wareneingang-CSV `f12f46f7`), Commit `8f24ec51`, `publish-info.txt` vorhanden, beide Komponenten + `SQL\` + `deploy\`. Im Ordner liegt zusätzlich ein altes `deploy.zip` (07:31, nicht aus diesem Lauf) – nicht mitkopieren bzw. ignorieren.
> - **Reihenfolge je System:** (1) vorher P-1 (Benutzer ohne Kennwort) und auf IDEAL-Prod P-2b; Kennwörter setzen/deaktivieren im alten Stand; (2) Update-Skript: Backup → Web+Dienst stoppen → `SQL/152` (Schema, vor dem Web-Start) → Dateien → Web-Start (Seed stellt `admin` mit Leer-Hash/NULL auf `wms-admin` + Pflichtwechsel um) → Dienst; (3) **sofort** als `admin` anmelden, falls der Seed umgestellt hat (Start-Log-Warnung); (4) Anwender-Info.
> - **Manifest** geprüft: `SQL/152`, `Pruefung/BenutzerOhneKennwort` (Vorher, sperrt Prod), `Pruefung/FreshInstallIndizes` (IDEAL, **jetzt `System=@('Prod')`**, QA-Fix B-1), `Pruefung/SmtpBenutzer` (Nachher), `v1.99.28-AnmeldungKennwort`, `v1.99.28-AnwenderInfo` – alle Deploy-Schritte der Spec sind drin.
> - Nach dem Merge nur dann aus `main` neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat:
>   `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
>   `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

*Vorläufige Angaben (Spec-/Dev-Lauf):*

> [!info] Stand nach Umsetzung 2026-11-16 (Dev-Lauf, Commits `6447db0a`, `4559e0a2`) – geht den vorläufigen Angaben darunter vor
> - **Version 1.99.28** (nicht 1.99.24), Changelog [[2026-11-16-v1-99-28-sicherheit-anmeldung-installer]], ADR [[0027-formular-anmeldung-nur-mit-kennwort-erstkennwort-mit-pflichtwechsel]].
> - **Web + Dienst:** ja (Dienst: `MailService`-Warnung, `appsettings.json` ohne tote Blöcke; Server-Datei wird nicht überschrieben).
> - **Migration:** `SQL/152_AddUserMussKennwortAendern.sql`, MigrationId `20261116165705_AddUserMussKennwortAendern`. **Kein SQL/151**
>   (nicht bauen, solange P-2b fehlt). Nachzug `4f3bf15d`: **`Pruefung/FreshInstallIndizes`** (Firma IDEAL – nur dort Erstinstallation
>   über `00_FreshInstall.sql`; AKE-DBs entstanden über Migrationen, Indexnamen dort nicht belegt; Phase Vorher, `anzeigen`): zählt die
>   30 Indizes + FK `FK_StockMovements_ProductionOrders_ProductionOrderId` aus Abschnitt 15, die fehlen. > 0 = SQL/151 nötig, vor dem Deploy
>   mit Gerald klären; sperrt IDEAL-Prod, auf IDEAL-Test nur Hinweis, baut nichts. Lokal belegt: leere DB 31, FreshInstall neu 0. Siehe
>   [[2026-11-16-anmeldung-zusatzfunde-folgeaufgabe]].
> - **Manifest** (`scripts/deploy/nachlauf.psd1`, alle `Quelle='2026-11-16-v1-99-28-sicherheit-anmeldung-installer'`):
>   `SQL/152` (Schema, nach `SQL/150`); `Pruefung/BenutzerOhneKennwort` (Vorher, anzeigen, sperrt Prod; **Abweichung:** Abfrage schließt
>   `admin` aus, weil der Seed ihn auf `wms-admin` + Haken umstellt – sonst sperrte jede Erstinstallation den Prod-Deploy);
>   `Pruefung/FreshInstallIndizes` (IDEAL, Vorher, anzeigen); `Pruefung/SmtpBenutzer` (Nachher, `Appsettings='MailSettings:SmtpUsername'`); `v1.99.28-AnmeldungKennwort` (Info, Vorher);
>   `v1.99.28-AnwenderInfo` (Info, Nachher). `NachlaufManifestDriftTests` erlaubt SQL-Prüfungen jetzt auch in Phase Vorher (nur `anzeigen`).
> - **admin (Rückfrage 11 = S1):** festes Erstkennwort `wms-admin` + Haken, keine Datei unter `App_Data`, keine Installer-Ausgabe.
> - **Installer:** `-SageLagerbuchungCredential` entfernt, `Sync:ProductionOrdersEnabled=false` per DB-UPDATE nach dem Aufwärmen,
>   SMTP-Warnung. Kopfzeile „Stand v1.71.1“ unverändert.
> - `v1.71.0-AppsettingsProd` unverändert (Text seit ADR 0024 überholt – gemeldet, Folgeaufgabe).

- **Web-App:** ja (Account, Users-Hinweis, Program.cs).
- **Service:** ja, falls BH-5c (MailService) bzw. die Bereinigung der Dienst-`appsettings.json` umgesetzt wird; die bestehende
  Server-Datei wird nicht überschrieben.
- **Migration:** ~~nein~~ *(ueberholt, Nachtrag 2026-11-16)* **ja, additiv:** `SQL/152_AddUserMussKennwortAendern.sql` (Schema, vor
  dem Web-Start; das neue Web liest die Spalte beim Seed und im Login). SQL/151 weiterhin nur nach Rückfrage 4, kein EF-Bezug.
- **Reihenfolge Übergang:** (1) P-1 je System, (2) Kennwörter setzen bzw. deaktivieren **im alten Stand** (ohne Haken, die Spalte gibt
  es noch nicht), (3) ~~`admin` mit leerem Kennwort prüfen und ggf. setzen~~ *ueberholt: der Seed stellt Leer-Hash-`admin` selbst auf
  Erstkennwort + Haken um; wer das nicht will, setzt `admin` im alten Stand ein eigenes Kennwort*, (4) Deploy inkl. SQL/152 (Prüfung
  sperrt Prod sonst), (5) nach dem Deploy auf Wunsch Haken für die in (2) gesetzten Kennwörter unter `/Users/Edit` setzen, P-1c,
  (6) Anwender-Info.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):

| Id | Version | Typ | Firma | Phase | Ausführung | Inhalt / Text |
|---|---|---|---|---|---|---|
| `SQL/152` (Nachtrag 2026-11-16) | 1.99.24 | Schema | beide | Schema | automatisch | `Datei='152_AddUserMussKennwortAendern.sql'; MigrationId='<beim Dev-Lauf>_AddUserMussKennwortAendern'; Voraussetzung='additiv, Spalte bit NOT NULL DEFAULT 0'; Quelle='<changelog-slug v1.99.24>'`. Eingereiht nach `SQL/150` (`nachlauf.psd1:97-98`); Deploy-Reihenfolge = Manifest, nicht Dateisortierung. |
| `Pruefung/BenutzerOhneKennwort` (Text geändert, Nachtrag 2026-11-16) | 1.99.24 | Pruefung | beide | Vorher | anzeigen | Zählung unverändert wie in der Zeile darunter. **Neuer Text** (ersetzt den alten): „Aktive Benutzer ohne Kennwort, die sich ab v1.99.24 nicht mehr anmelden können (lokal, bzw. AD bei WindowsAuthAktiv aus): vorher unter /Users/Edit ein Erstkennwort setzen (ab v1.99.24 mit Pflichtwechsel bei der ersten Anmeldung) oder deaktivieren; Liste mit P-1 der Spec 2026-11-14-sicherheit-anmeldung-und-installer.“ Prod: Abbruch vor dem Stoppen (Rückfrage 2). |
| ~~`Pruefung/BenutzerOhneKennwort`~~ *(Text ueberholt, siehe Zeile oben; Zählabfrage gilt)* | 1.99.24 | Pruefung | beide | Vorher | anzeigen | `Pruefung='SELECT COUNT(*) FROM dbo.Users u WHERE u.IsActive = 1 AND (u.PasswordHash IS NULL OR u.PasswordHash = N'''') AND (u.WindowsUserName IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''WindowsAuthAktiv'' AND LOWER([Value]) = N''true''))'` (Vergleich wie `WindowsAutoLoginMiddleware.cs:93-94`, ohne Trim). Text: „Aktive Benutzer ohne Kennwort, die sich ab v1.99.24 nicht mehr anmelden können (lokal, bzw. AD bei WindowsAuthAktiv aus): vorher unter /Users/Edit ein Kennwort setzen oder deaktivieren; Liste mit P-1 der Spec 2026-11-14-sicherheit-anmeldung-und-installer.“ Prod: Abbruch vor dem Stoppen (Rückfrage 2; bei „nur Hinweis“ Phase Nachher). |
| `v1.99.24-AnmeldungKennwort` | 1.99.24 | Info | beide | Vorher | anzeigen | *(Text angepasst, Nachtrag 2026-11-16)* „Ab v1.99.24 meldet das Formular nur mit Kennwort an; vom Admin vergebene Kennwörter (Erstkennwort) muss der Benutzer bei der ersten Anmeldung ändern. Vorher P-1 ausführen; AD-Benutzer ohne Kennwort nur noch per Windows-Anmeldung. admin mit leerem Kennwort wird beim ersten Start auf ein Erstkennwort mit Pflichtwechsel umgestellt (Weg nach Rückfrage 11) – wer das nicht will, setzt admin vorher ein Kennwort.“ |
| `v1.99.24-AnwenderInfo` | 1.99.24 | Info | beide | Nachher | anzeigen | „Anwender-Info: Anmeldung am Formular nur noch mit Kennwort; wer vom Administrator ein Erstkennwort bekommt, legt bei der ersten Anmeldung ein eigenes fest; Terminals/Handgeräte mit dem neuen Kennwort anmelden; Domänen-PCs unverändert per Windows.“ |
| `Pruefung/FreshInstallIndizes` (nur Rückfrage 4) | 1.99.24 | Pruefung | IDEAL (`System=@('Prod')`) | Haltepunkt | Reparatur | `Pruefung` = Anzahl aus P-2b (als `SELECT COUNT(*) FROM (…) x`), `Datei='151_RepairFreshInstallIndexes.sql'`. Text: „Indizes aus Abschnitt 15 der Erstinstallation fehlen (Abbruch 2026-11-04): Reparatur legt sie idempotent an (nur IDEAL-Prod, FreshInstall-Herkunft).“ |
| `Pruefung/SmtpBenutzer` (nur Rückfrage 9 = Empfehlung) | 1.99.24 | Pruefung | beide | Nachher | anzeigen | `Appsettings='MailSettings:SmtpUsername'`. Text: „SMTP-Anmeldung als {Wert} ({Datei}): mit SmtpUseSsl = false geht das Kennwort unverschlüsselt – anonymes Relay oder SmtpUseSsl = true (Port 465).“ |

  ~~Keine Schema-, Daten-, Einmal- oder Ordner-Einträge~~ *(ueberholt: ein Schema-Eintrag SQL/152)*; keine Daten-, Einmal- oder
  Ordner-Einträge (bei Rückfrage 11 = S2 nutzt der Seed das vorhandene `App_Data`, kein neuer Ordner – Dev-Lauf prüft das Schreibrecht
  des App-Pools dort, belegt durch die Kommissionierfotos seit v1.30.4, `Program.cs:415`); kein Trockenlauf-Skript. Bestehender Eintrag `v1.71.0-AppsettingsProd`
  („mit Abschnitt SageLagerbuchung“) bleibt; Hinweis an den Dev-Lauf: Text ist seit ADR 0024 überholt – melden, nicht still ändern.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
  DEV beider Standorte (`standort: beide`); Update-Skript nach `ziele.psd1`.

## Offene Rueckfragen

1. **BH-1 Regel:** A streng (Empfehlung), B nur AD-Benutzer, oder C = A + Übergangsschalter? Begründung unter „Varianten BH-1“.
   *Neubewertung 2026-11-16: weiter offen (nur Bestätigung). Der Nachtrag hält „ohne gesetztes Kennwort gilt weiter Regel R“ fest
   = Kern von A; B ist damit nicht vereinbar. Offen bleibt, ob A ohne Schalter (C) bestätigt wird.*
2. **Übergang:** Ergebnis P-1 je System (IDEAL-Prod, IDEAL-Test, AKE-Prod, AKE-Test, AKE-Test-IDEAL) – wie viele lokale/AD-Benutzer,
   Terminals, Kommissionierer ohne Kennwort, und steht `WindowsAuthAktiv` auf `true`? Soll `Pruefung/BenutzerOhneKennwort` den
   Prod-Deploy sperren (Phase Vorher, Empfehlung) oder nur hinweisen (Phase Nachher)?
   *Neubewertung 2026-11-16: geändert, weiter offen. Die Abhilfe der Prüfung lautet jetzt „Erstkennwort mit Pflichtwechsel setzen“
   (Manifest-Text angepasst); P-1-Ergebnis je System und Sperre/Hinweis sind nicht entschieden.*
3. **Lokale QA unter Kestrel:** Ausnahme nur bei `IsDevelopment()` mit sichtbarem Hinweis (Empfehlung: hält die QA-Läufe mit
   `QA-Agent`/`QA-Picker` ohne Geheimnis im Brain lauffähig, greift unter IIS nie) oder keine Ausnahme?
4. **BH-2:** Ergebnis P-2b auf IDEAL-Prod. Bei Treffern: SQL/151 + `Pruefung/FreshInstallIndizes` (Empfehlung) oder Reparatur von
   Hand? Guards für `WarehouseRequisitions`/`-Items` und Wächter-Test mit in diese Spec (Empfehlung: ja, beides)? P-2a auf allen
   Systemen zur Bestätigung (erwartet leer).
5. **BH-3:** `-SageLagerbuchungCredential` ersatzlos entfernen (Empfehlung) oder behalten und mit Hinweis abbrechen? SMTP-Kennwort
   weiter in der Dienst-Datei mit Installer-Warnung (Empfehlung) oder eigener ADR für den Umzug nach `ServiceSettings` (Typ `Geheim`)?
6. **BH-4:** Variante A – Installer setzt `Sync:ProductionOrdersEnabled = false` in der DB (Empfehlung, schließt die Autostart-Lücke)
   oder B – nur die wirkungslose Zeile streichen? Tote Blöcke aus `IDEALAKEWMSService/appsettings.json` entfernen (Empfehlung ja,
   `Feiertag*` bleiben)?
7. **BH-5a:** Erstkennwort-Schritt (Empfehlung), Installer-Abfrage mit Hash in PowerShell, oder unverändert mit Warnung?
   *Neubewertung 2026-11-16: erledigt durch Nachtrag – Erstkennwort durch Admin bzw. Seed + Pflichtwechsel (Haken); die
   Leer-Hash-Ausnahme E, die Installer-Abfrage und „unverändert“ entfallen. Folgefrage zum admin-Erstkennwort = Rückfrage 11.*
8. **BH-5b:** `SecurePolicy = SameAsRequest` für Sitzung und Antiforgery (Empfehlung), `Always` (bricht IDEAL-Test :88 und DEV)
   oder unverändert?
9. **BH-5c:** Warnung + `Pruefung/SmtpBenutzer` (Empfehlung), `StartTlsWhenAvailable` (Zertifikatsrisiko am Relay) oder unverändert?
   Prüfschritt dazu: je Server in der Dienst-`appsettings*.json` `MailSettings:SmtpHost`, `SmtpPort`, `SmtpUseSsl` und ob
   `SmtpUsername` gesetzt ist (Werte, nie das Kennwort, in die Antwort).
10. **Zusatzfunde** (keine Sperre nach Fehlversuchen, keine Mindestlänge, unterschiedliche Meldungen verraten vorhandene Namen
    `AccountController.cs:77`/`:87`, Installer-Mindestzahl 75 Migrationen `Install-IdealAkeWms.ps1:433` = BH-7): Folgeaufgabe
    (Empfehlung) oder in diese Spec?
11. **admin-Erstkennwort (neu, aus Nachtrag 2026-11-16):** Wie kommt `admin` nach Installation bzw. Rückweg (`PasswordHash` NULL +
    Neustart) zu seinem Erstkennwort? **S1** festes, im Betriebshandbuch dokumentiertes Erstkennwort (eine Zeile; jeder mit Doku-Zugang
    kennt es bis zur ersten Anmeldung – ähnlich dem heutigen Leer-Fenster). **S2 (Empfehlung)** der Web-Start erzeugt ein
    Zufalls-Erstkennwort (`GenerateInitialPassword`), schreibt es in `App_Data\admin-erstkennwort.txt` (Rechte: App-Pool +
    Administratoren), loggt nur den Dateipfad; der Installer gibt es nach dem Aufwärmen einmal aus und löscht die Datei; nach
    erfolgreichem Pflichtwechsel löscht das Web die Datei, falls noch vorhanden. Nur wer Zugriff auf den Server hat, kommt an das
    Kennwort. **S3** anderer Weg.
12. **Anzeige-Ort des erzeugten Erstkennworts (neu):** einmal im Erfolgs-Banner nach Speichern (`TempData["SuccessMessage"]`, read-once;
    Default-TempData-Provider = mit DataProtection verschlüsseltes Cookie, ein Request lang im Browser; Empfehlung) oder eigene
    Anzeigeseite ohne Redirect bzw. Druckzettel zum Aushändigen?
13. **Gültigkeitsdauer des Erstkennworts (neu):** keine (Empfehlung; `/Users`-Hinweis und P-1c zeigen offene Erstkennwörter) oder
    Ablauf nach n Tagen? Ablauf braucht eine eigene Spalte (z. B. `ErstkennwortGesetztAm`), denn `ModifiedAt` wird von jeder
    anderen Bearbeitung überschrieben (`UsersController.cs:310`).
14. **Kennwortregeln beim Pflichtwechsel (neu):** nur nicht leer, neu ≠ bisheriges, Wiederholung gleich (Empfehlung; heute gibt es keine
    Regeln, `UsersController.cs:136`/`:307`, `AccountController.cs:222` prüfen nur „nicht leer“) oder zusätzlich Mindestlänge n
    (berührt Zusatzfund Mindestlänge, Rückfrage 10 – dann auch für `/Users` und `/Account/Profile`)?
15. **Haken abwählen / Kennwort ohne Haken (neu):** Darf der Admin für Gerätekonten (BDE-Terminal `BdeTerminal.UserId`, gemeinsame
    Handgeräte-Konten) ein Kennwort ohne Pflichtwechsel setzen bzw. den Haken abwählen? Erlaubt (Empfehlung: Checkbox maßgeblich, bei
    neuem Kennwort vorausgewählt) oder nie (neues Kennwort setzt den Haken immer)?
16. **Handgeräte-/Terminal-Login mit Pflichtwechsel (neu):** Ist der Zwischenschritt auf Handgerät/BDE-Terminal akzeptiert (drei
    Kennwortfelder auf Bildschirmtastatur; Hardware-Scanner-Fokus auf der Login-Seite)? Prüfschritt: am Netum-Handgerät und an einem
    BDE-Terminal einmal TS-139.4a durchspielen. Oder Gerätekonten grundsätzlich ohne Haken (hängt an Rückfrage 15)?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. BH-1 Regel A / B / C → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): A, ohne Schalter.**
2. Übergang: P-1-Ergebnis je System; Prüfung sperrt Prod (Vorher) oder nur Hinweis (Nachher) → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): Prüfung sperrt Prod (Phase Vorher), Abhilfe Erstkennwort mit Pflichtwechsel. P-1 je System vor dem Deploy erheben (Gerald).**
3. Kestrel-QA: Ausnahme bei IsDevelopment() ja/nein → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): ja, mit sichtbarem Hinweis.**
4. BH-2: P-2b-Ergebnis IDEAL-Prod; SQL/151 + Prüfung ja/nein; Guards + Wächter-Test ja/nein → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): Guards + Wächter-Test ja; SQL/151 + Pruefung/FreshInstallIndizes nur bei Treffern in P-2b (vor Deploy IDEAL-Prod erheben, Gerald).**
5. BH-3: SData-Parameter entfernen ja/nein; SMTP Warnung oder eigener ADR → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): Parameter entfernen; SMTP bleibt in der Dienst-Datei mit Installer-Warnung.**
6. BH-4: Variante A / B; tote Blöcke entfernen ja/nein → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): A; tote Blöcke entfernen (Feiertag* bleiben).**
7. BH-5a: Erstkennwort / Installer-Abfrage / unverändert → **Durch Nachtrag 16.11. erledigt: Erstkennwort + Pflichtwechsel (siehe 11–16).**
8. BH-5b: SameAsRequest / Always / unverändert → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): SameAsRequest.**
9. BH-5c: Warnung + Prüfung / StartTlsWhenAvailable / unverändert; MailSettings je Server → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): Warnung + Pruefung/SmtpBenutzer; MailSettings je Server vor Deploy erheben (Gerald).**
10. Zusatzfunde: Folgeaufgabe oder in diese Spec → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): Folgeaufgabe.**
11. admin-Erstkennwort: S1 fest dokumentiert / S2 Zufall in App_Data-Datei, Installer zeigt einmal / S3 → **Gerald 2026-11-16 17:28: S1 – festes Standard-Erstkennwort `wms-admin` (im Betriebshandbuch dokumentiert), admin muss es bei der ersten Anmeldung ändern (Haken MussKennwortAendern). Gilt auch für den Rückweg (PasswordHash NULL + Neustart → Standard-Erstkennwort + Haken). Installer-Abschluss und Start-Warnung fordern zur sofortigen Anmeldung auf (Fenster bis zur ersten Anmeldung).** 
12. Anzeige erzeugtes Erstkennwort: Erfolgs-Banner einmal / eigene Seite / Druckzettel → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): Erfolgs-Banner einmal.**
13. Gültigkeit Erstkennwort: keine / Ablauf nach n Tagen (n = ?) → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): keine; offene Erstkennwörter im /Users-Hinweis.**
14. Kennwortregeln Pflichtwechsel: nur leer/gleich/Wiederholung / zusätzlich Mindestlänge n → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): nur nicht leer / neu ≠ alt / Wiederholung gleich; Mindestlänge mit den Zusatzfunden (Folgeaufgabe).**
15. Haken abwählen bzw. Kennwort ohne Haken für Gerätekonten: erlaubt / nie → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): erlaubt (Checkbox maßgeblich, bei neuem Kennwort vorausgewählt).**
16. Pflichtwechsel an Handgerät/Terminal akzeptiert (Prüfschritt) / Gerätekonten ohne Haken → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): Gerätekonten ohne Haken; Pflichtwechsel einmal am Netum-Handgerät durchspielen (TS-139.4a).**

## Nachtrag 2026-11-16 16:27 – Erstkennwort durch Admin + Pflichtwechsel

**Entscheidung Gerald 2026-11-16 16:27** (wörtlich übernommen aus dem Auftrag, in den Rumpf eingearbeitet am 2026-11-16):

> Benutzer ohne Kennwort sollen bei der Erstanmeldung ein eigenes Kennwort vergeben. Umsetzen als „Erstkennwort durch Admin +
> Pflichtwechsel“ — NICHT Selbstvergabe ohne Kennwort (das wäre Kontoübernahme durch jeden, der den Benutzernamen kennt).
> - Neue Spalte `User.MussKennwortAendern` (bool, NOT NULL DEFAULT 0): Model → EF-Migration → idempotentes SQL-Skript mit Guard →
>   `__EFMigrationsHistory` separat → `SQL/00_FreshInstall.sql` an beiden Stellen; Audit-Felder bei jedem Setzen/Löschen des Hakens.
> - `/Users/Create` und `/Users/Edit`: Erstkennwort setzen (optional vom WMS erzeugt und genau einmal angezeigt) setzt den Haken
>   automatisch; Haken auch manuell setzbar.
> - Login: richtiges Kennwort + Haken gesetzt → Zwischenschritt „Neues Kennwort festlegen“ (neu ≠ alt, nicht leer, Wiederholung
>   gleich, bestehende Kennwortregeln), erst danach Sitzung; Haken wird dabei gelöscht. Ohne gesetztes Kennwort gilt weiter Regel R.
> - admin nach Installation: statt der Leer-Hash-Ausnahme E → Seed mit Erstkennwort + Haken bzw. Erstkennwort-Ablauf; Ausnahme E und
>   den Rückweg „PasswordHash NULL“ anpassen (Rückweg muss weiter funktionieren, auf „Erstkennwort mit Haken setzen“ umdeuten).
> - AD-Benutzer: SSO unverändert; Erstkennwort nur bei Bedarf (Handgeräte ohne SSO).
> - Übergang P-1 (`Pruefung/BenutzerOhneKennwort`): auf „Erstkennwort mit Pflichtwechsel setzen“ umstellen.
> - ADR 0027 ergänzen; Akzeptanzkriterien und TS-139 erweitern; Manifest und `docs/betrieb/07`, `/09` in affected_code.

**Eingearbeitet in:** Frontmatter (`updated`, `sql_vorschlag`, `affected_code`, `open_questions`, `deploy.migration: true`), Hinweis
oben, Umfang, Soll (Regel P, `admin`-Seed, Rückweg; Ausnahme E als ueberholt markiert), BH-5-Tabelle Zeile a, Lösungsentwurf 0-4, 7, 9
(ADR 0027), Migrations-/SQL (Migration, SQL/152, FreshInstall, P-1c), Einstellungen, Audit, AK 3-5b, 7-8d, 10, 17-19, TS-1.6, TS-139.4
bis 139.5, 139.9, Deploy (Migration, Reihenfolge, Manifest SQL/152, Prüfungs- und Info-Texte).

**Belegte Nummern:** SQL/152 (Begründung unter Migrations-/SQL), ADR 0027 bleibt (0028/0029 anderweitig reserviert), TS-139 bleibt.

**Belegte Code-Stellen (Worktree, Stand v1.99.23):** Login POST `AccountController.cs:64-116` (Kennwortprüfung `:82-101`, Sitzung
`:104-110`), Profile-Kennwort `:222-227`; `User : AuditableEntity` `Models/User.cs:5`, `PasswordHash` `:19-20`; `UsersController`
Create `:103-151` (Hash `:136-137`), CreateAdUser `:200-209` (kein Hash), Edit `:273-326` (Hash `:307-308`, Audit `:310-312`); Views
`Users/Create.cshtml:89-93`, `Users/Edit.cshtml:173-177` (Kennwortfeld auch für AD-Benutzer); Seed `Program.cs:259-279` (Leer-Hash
`:267`, `:275-278`, ohne Audit); FreshInstall `Users` `:22-57`, `admin`-Insert `:1541-1549`, letzte `MigrationId` `:3262-3264`;
`PasswordService.cs:8-44`; Manifest `SQL/150` `nachlauf.psd1:97-98`; Installer `:29`, `:481`. Schreibwege `PasswordHash =`/`new User`
im Web- und Dienstcode (ohne Tests): nur Program.cs, AccountController.cs, UsersController.cs (Suche 2026-11-16). `SQL/AgentJobs`
berührt `dbo.Users` nicht.

**Neubewertung der Rückfragen:**
- **1 – weiter offen (Bestätigung):** Regel R ist durch den Nachtrag bestätigt (= Kern von A), formal fehlt A ohne Schalter vs. C.
- **2 – geändert, weiter offen:** Abhilfe jetzt Erstkennwort mit Pflichtwechsel (Manifest-Text); P-1-Ergebnisse und Sperre/Hinweis offen.
- **7 – erledigt:** Erstkennwort + Pflichtwechsel; Ausnahme E, Installer-Abfrage und „unverändert“ entfallen.

**Neue Rückfragen aus dem Nachtrag:** 11 (admin-Erstkennwort-Weg), 12 (Anzeige-Ort), 13 (Gültigkeitsdauer), 14 (Kennwortregeln),
15 (Haken abwählen / Gerätekonten), 16 (Handgeräte-/Terminal-Login mit Pflichtwechsel).

**Abgeleitet, nicht ausdrücklich entschieden (zur Kenntnis, bei Widerspruch melden):** `/Account/Profile` mit neuem Kennwort löscht den
Haken (selbst gewähltes Kennwort); der Seed stellt auch bestehende Leer-Hash-`admin`-Konten auf Erstkennwort + Haken um (sonst wären
sie nach Regel R ausgesperrt); der Pflichtwechsel-Schritt verlangt das bisherige Kennwort erneut (zustandslos, kein Klartext im
Formular, kein Session-Merker); Spalte `MussKennwortAendern` bekommt für Bestandsbenutzer `0`.

## QA-Nachweis 2026-11-16 (qa-agent)

**Ergebnis: Testbereit.** Reviewt: Commits `6447db0a`, `4559e0a2`, `4f3bf15d` (Basis `5163df17`), ohne `f12f46f7` (eigene Spec). Build, Tests, DEV-Deploy und Publish auf HEAD inkl. `f12f46f7` und QA-Fix `8f24ec51`.

### 1. Build und Tests (Worktree, HEAD `4f3bf15d`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (bestehend).
- `dotnet test`: **IdealAkeWms.Tests 2952 bestanden / 1 übersprungen / 0 Fehler (2953)**; **IDEALAKEWMSService.Tests 663 / 0 / 0**.
- Nach QA-Fix `8f24ec51` (nur Manifest + Doku): `NachlaufManifestDriftTests` 17/17 grün.

### 2. Code-Review (selbst, in diesem Lauf)
Geprüft: AccountController Login/StartSession/Profile, LoginViewModel, Login.cshtml (Kennwortfelder werden nicht zurückgeschrieben – `DataType.Password`, kein Klartext in versteckten Feldern; hidden nur `KennwortFestlegen`/`returnUrl`, `Url.IsLocalUrl` unverändert), UsersController Index/Create/Edit + `ApplyPassword`, Views Users (JS ohne eingebettete Server-Werte), AdminSeed + Program.cs (Cookie `SameAsRequest` für Session und Antiforgery), `PasswordService.GenerateInitialPassword`, User-Modell, Migration + Snapshot, SQL/152, FreshInstall (Users-Spalte, Abschnitt 15, Guards WarehouseRequisitions/-Items, MigrationId mit `COL_LENGTH`-Bedingung), MailService, Dienst-`appsettings.json` (Dienst liest `Sync:*`/`WorkerSettings:*`/`ErrorNotification:*` nur über `Common/ServiceSettings.cs:19-55` aus der DB, `IConfiguration` dort nur für den Connection-String; `Feiertag*` bleiben), Installer (kein Zugriff mehr auf entfernte Blöcke, Parser 0 Fehler), nachlauf.psd1 + `Update-IdealAkeWms.ps1:695-703` (Prüfungen ohne Versionsfilter, Vorher+Prod>0 = Abbruch), Drift-Test-Änderung, neue Tests.

Fokuspunkte: Regel R/P korrekt (Hash leer → R außer `IsDevelopment()` mit leerem Feld; leeres Feld/falsches Kennwort → „Falsches Passwort.“; Haken → Schritt, bisheriges Kennwort und Haken je POST neu geprüft, zustandslos). Kontoübernahme: kein Weg setzt ohne bestätigtes Kennwort einen Hash (manipulierte POSTs auf DEV geprüft). Audit: Login-Wechsel (`ModifiedBy` = Benutzer, `ModifiedByWindows` = `SYSTEM` wie Spec), Edit, Profile, Seed (`system`); Create `Created*`. Kein `HashPassword("")` mehr (Suche ohne Treffer). Restrisiken laut Entscheidung: `wms-admin` ist bis zur ersten admin-Anmeldung jedem mit Betriebshandbuch bekannt (S1, Freigabe 11) – wie bisher das Leer-Fenster; Benutzernamen-Aufzählung über unterschiedliche Meldungen = Folgeaufgabe [[2026-11-16-anmeldung-zusatzfunde-folgeaufgabe]].

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| B-1 | Mittel | `Pruefung/FreshInstallIndizes` ohne Systemfilter: auf über Migrationen entstandenen IDEAL-DBs tragen gleichwertige Indizes andere Namen → Falschtreffer „SQL/151 nötig“. Gemessen: `IDEAL_WMS_DEV` (Kopie IDEAL-Test) = **24**, `AKE_WMS_DEV` 0, frische FreshInstall-DB 0. Der Spec-Entwurf sah `System=@('Prod')` vor. | **Behoben** `8f24ec51`: `System=@('Prod')`; `-NurPruefen` IDEAL-DEV listet die Prüfung nicht mehr. TS-139.8 nachgezogen (sagte noch „Prüfung nicht gebaut“). |
| B-2 | Niedrig | `/Users/Create` GET belegt den Haken schon beim Öffnen vor (`MussKennwortAendern = true`), die Spec sagt „bei eingegebenem/erzeugtem Kennwort vorausgewählt“ (das JS tut das zusätzlich). Folge: Benutzer ohne Kennwort angelegt → Warnung „Haken gesetzt, aber kein Kennwort“ und gespeicherter Haken. | Offen, Entscheidung Gerald (1 Zeile). Verhalten sonst korrekt und gemeldet. |
| B-3 | Niedrig | `/Users`-Hinweis „offenes Erstkennwort“ zählt auch aktive Benutzer mit Haken **ohne** Kennwort (DEV: `Testuser` stand in beiden Listen). | Offen, Entscheidung Gerald (Filter auf gesetzten Hash). Hängt mit B-2 zusammen. |
| B-4 | Info | `Pruefung/SmtpBenutzer` meldet jeden gesetzten SMTP-Benutzer, auch mit `SmtpUseSsl = true` (Text nennt die Bedingung). Nur Hinweis. | Hingenommen. |
| B-5 | Info | `Pruefung/BenutzerOhneKennwort` läuft bei **jedem** künftigen Update: ein später ohne Kennwort angelegter lokaler Benutzer sperrt den nächsten Prod-Deploy. Gewollt („melden“), aber dauerhaft. | Zur Kenntnis. |

Defektklassen-Liste: kein `@Html.Raw` in `<script>`, kein `hidden` + `d-*`, keine stille `?? 0`-Rückfallstelle, keine parallelen Arrays, Regeln serverseitig (JS setzt nur den Haken vor), Regel R/P an einer Stelle (Login POST).

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1, 3, 4, 5, 5a, 5b | automatisiert (`AccountControllerLoginTests`) und DEV-IIS (unten) |
| 2 | Meldung mit Windows-Hinweis automatisiert + DEV; SSO = **Manual-UAT** |
| 6 | automatisiert; `dotnet run` nicht ausgeführt = Manual-UAT |
| 7, 8, 8a | automatisiert (`ErstkennwortTests`) + DEV (Seed-Umstellung, Start-Log, Hinweis) |
| 8b | automatisiert + DEV-Browser (echtes Model-Binding) |
| 8c | nur automatisiert (`Profile_NeuesKennwort_LoeschtHaken`); DEV braucht Sitzung mit Haken (nur SSO) – Manual-UAT |
| 8d | SQL/152 zweimal auf Prüf-DB (SQL Server) |
| 9, 10 | FreshInstall-Lauf lokale Instanz (unten); Web-Start auf Prüf-DB nicht ausgeführt (Seed auf DEV belegt) |
| 11 | automatisiert (`FreshInstallIndexSpaltenTests`) |
| 12 | Parser + Code-Lesung; Voll-Lauf = Manual-UAT |
| 13 | Dienst-Tests grün, Code-Lesung |
| 14 | DEV HTTP belegt; HTTPS = Manual-UAT |
| 15 | Code-Lesung; Lauf NICHT MÖGLICH auf DEV (Mail aus) |
| 16, 19 | `-NurPruefen` DEV + `NachlaufManifestDriftTests`; Prod-Abbruch per Code-Lesung `Update-IdealAkeWms.ps1:701` |
| 17 | Doku-Lesung (TS-1.6/40.4/40.7, Installer-Texte ohne „LEER“) |
| 18 | grün |

### 4. Geänderte bestehende Tests
- `AccountControllerTests` – Typ-Änderung (neuer Ctor-Parameter `IWebHostEnvironment`, Production).
- `NachlaufManifestDriftTests` – `SQL/152` registriert; Prüfungen in Phase `Vorher` zugelassen, nur mit `anzeigen` – Verhalten bewusst geändert (Freigabe-Antwort 2, AK 19). Kein „anderes“.

### 5. Checkliste
Migration + SQL/152 (Guard, History separat) ✓; FreshInstall an beiden Stellen ✓; AgentJobs nicht betroffen ✓; Audit ✓; Version beide `AppVersion.cs` (1.99.28, HEAD 1.99.29 durch Folgespec) + Changelog ✓; TS-139 + Index ✓; ADR 0027 vorhanden. ADR 0020: keine neue Einstellung (`Sync:ProductionOrdersEnabled` nur Installationswert).

### 6. DEV-Deploy und Testlauf
`standort: beide` → beide Sites, elevated, `dev-deploy.ps1 -Quelle <worktree>` (HEAD `4f3bf15d`): AKE „HTTP 200, Fusszeile v1.99.29 … Fertig: AKE v1.99.29 auf http://localhost:8081/“, Exit 0; IDEAL dasselbe für :8082, Exit 0. Footer = AppVersion 1.99.29 ✓. Migration in beiden DEV-DBs eingetragen. (QA-Fix `8f24ec51` berührt keine deployte Komponente.)

| Schritt | Ergebnis | Status |
|---|---|---|
| 139.1 `Testuser` (IDEAL, lokal ohne Kennwort) leer / `x` | Meldung R mit Windows-Hinweis, `/` → 302 Login | OK |
| 139.2 AD/SSO/Handy | – | NICHT MÖGLICH (Manual-UAT) |
| 139.3 / AK 8a negativ | AKE-DEV `admin` mit eigenem Kennwort nach Deploy unverändert (`ModifiedAt` 2026-07-10); falsches Kennwort → „Falsches Passwort.“ | OK |
| 139.4 Seed Variante 2 | IDEAL-DEV `admin` mit Leer-Hash → nach Start `wms-admin`, Haken 1, `ModifiedBy=system`; Start-Log `[WRN] Benutzer admin hat ein Erstkennwort …` ohne Kennwort; leer → „Falsches Passwort.“; `wms-admin` → Schritt, keine Sitzung | OK |
| 139.4b (1)-(5) an `admin` | (1)/(2) „Bitte ein neues Kennwort eingeben.“, (3) „…unterscheiden.“, (4) „Die Wiederholung stimmt nicht…“, (5) „Falsches Passwort.“; Hash+Haken unverändert | OK |
| 139.4b (7) manipulierter POST | AKE `admin` ohne Haken + falsches Kennwort + `NewPassword` → „Falsches Passwort.“, Hash unverändert; AKE `lager` ohne Kennwort + `NewPassword` → Meldung R, Hash bleibt NULL | OK |
| 139.4 Wechsel `admin` im Browser | Schritt-Ansicht korrekt (Benutzer schreibgeschützt, Felder leer, Abbrechen), gültig → angemeldet, Haken 0, `ModifiedBy=admin`, `ModifiedByWindows=SYSTEM` | OK |
| 139.4 Rückweg | `PasswordHash=NULL` + App-Pool-Recycle → `wms-admin` + Haken + Warnung; Login → Schritt. **Endzustand IDEAL-DEV: admin = `wms-admin` mit Pflichtwechsel** | OK |
| 139.4a `ts139-erst` (IDEAL) | Create mit `Start-139a`, Haken vorbelegt; Login → Schritt; Wechsel auf `Neu-139a` → 302 `/`, Haken 0, `ModifiedBy=ts139-erst`; alt → „Falsches Passwort.“, neu → angemeldet | OK |
| 139.4a am Netum | – | NICHT MÖGLICH (Gerät) |
| 139.4c erzeugt | Haken per JS mit angehakt; Banner „Erstkennwort: <12 Zeichen> – jetzt notieren …“ einmal, nach Neuladen weg; DB Hash ≠ Klartext, Haken 1; Web-Logs ohne Kennwort; Login → Schritt; Abbrechen → `/` 302 | OK |
| 139.4d a/c/d | c) neues Kennwort, Haken abgewählt → direkt angemeldet; a) Haken manuell → Schritt; d) Haken bei `Testuser` ohne Kennwort → Warnung, Login → Meldung R (Haken danach per SQL zurück); dabei B-3 beobachtet | OK |
| 139.4e AD mit Erstkennwort | – | NICHT MÖGLICH (SSO/Handy) |
| 139.5 `-NurPruefen` (aus `publish\deploy`, `-System DEV`) | IDEAL-DEV `[Pruefung/BenutzerOhneKennwort] Abweichung 5 - Hinweis: …`, AKE-DEV 7; admin zählt nicht; `[Pruefung/SmtpBenutzer] OK`; keine fehlenden Schema-Skripte; FreshInstallIndizes auf DEV nicht gelistet (nach B-1) | OK; Terminal NICHT MÖGLICH |
| 139.6 `dotnet run` | – | NICHT MÖGLICH in diesem Lauf (automatisiert belegt) |
| 139.7 / AK 9-10 | `@@SERVERNAME` = PC-WEICHBOLD; Scratch-Kopie mit `USE [WMS_FreshInstall_QA]` auf neuer leerer DB: Lauf 1 und 2 Exit 0, 0 Meldungen; History 100 = 100 Migrationen; Spalte vorhanden; falsche Indizes 0; FreshInstallIndizes-Zählung 0; admin `PasswordHash NULL`; DB gedroppt | OK |
| AK 8d SQL/152 | Prüf-DB mit Bestand: Lauf 1 „angelegt … eingetragen“, Lauf 2 „existiert bereits“, Exit 0, Bestand 0, History 1; gedroppt | OK |
| 139.8 IDEAL-Prod P-2b | – | NICHT MÖGLICH (Gerald) |
| 139.9 Installer | Parser 0 Fehler; kein SageLagerbuchung-Parameter/Schreibblock | OK; Voll-Lauf NICHT MÖGLICH |
| 139.10 Cookies | DEV HTTP: `IdealAkeWms.Session` und `.AspNetCore.Antiforgery.*` ohne `Secure`; Formular-POSTs ohne 400 | OK (HTTP); HTTPS NICHT MÖGLICH |
| 139.11 SMTP-Hinweis | Mailversand auf DEV aus | NICHT MÖGLICH |

DEV-Testdaten: IDEAL-DEV neuer Benutzer `ts139-erst` (Rolle Lager, Kennwort aus TS-139.4d, ohne Haken).

### 7. Publish
`<worktree>\publish\` neu: `IDEALAKEWMSWeb`, `IDEALAKEWMSWebService`, `SQL`, `deploy`, `publish-info.txt` (Version 1.99.29, Commit `8f24ec516516114560d435b858e26277de101fd6`, Erstellt 2026-11-16 18:32). Aus dem Paket lief `-NurPruefen` gegen beide DEV-Systeme fehlerfrei.

### Manuelle Testliste für Gerald (offen)
1. Vor dem Deploy: P-1 je System (IDEAL-Prod, IDEAL-Test, AKE-Prod, AKE-Test, AKE-Test-IDEAL); betroffene Konten Kennwort setzen oder deaktivieren (Prod-Lauf bricht sonst ab).
2. Vor dem Deploy IDEAL-Prod: P-2b; Treffer > 0 → SQL/151 klären (Prüfung sperrt Prod).
3. MailSettings je Dienst-Datei erheben (Host, Port, SmtpUseSsl, Benutzer ja/nein).
4. TS-139.2 (Handy-Formular, Knopf „Mit Windows anmelden“, Domänen-PC SSO).
5. TS-139.4e (AD-Benutzer mit Erstkennwort: SSO ohne Schritt, Handy mit Pflichtwechsel).
6. TS-139.4a einmal am Netum-Handgerät.
7. TS-139.5 an einem BDE-Terminal (Gerätekonto ohne Haken).
8. TS-139.10 auf AKE-Test (HTTPS): beide Cookies mit `Secure`, Login + Profil speichern ohne 400.
9. TS-139.11 SMTP-Warnung auf einem Testsystem mit SMTP-Benutzer.
10. TS-139.6 einmal per `dotnet run`.
11. AK 8c: Benutzer mit Haken per SSO angemeldet → Profil neues Kennwort → Haken weg.
12. TS-139.9 Installer-Voll-Lauf bei der nächsten Neuinstallation.
13. Entscheiden: B-2 / B-3.
14. Nach dem Prod-Deploy sofort als `admin` anmelden, wenn das Start-Log die Erstkennwort-Warnung zeigt.
15. Hinweis: IDEAL-DEV `admin` steht nach diesem Lauf auf `wms-admin` mit Pflichtwechsel.