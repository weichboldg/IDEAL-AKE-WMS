---
type: spec
title: "Dashboard: Reiter „Favoriten“ – Benutzer markieren Kacheln per Stern"
slug: 2026-11-23-dashboard-favoriten-reiter-spec
status: Gemerged
created: 2026-11-23
updated: 2026-11-28
source_backlog: "[[2026-11-23-dashboard-favoriten-reiter]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
vorgaenger: "[[2026-11-19-dashboard-kacheln-nach-kategorie-und-berechtigung-spec]]"
version_vorschlag: "1.99.51 (Worktree IdealAkeWms/AppVersion.cs:5 = 1.99.47; 1.99.48 Sage-AG-Bugfix, 1.99.49 BDE-Liste, 1.99.50 Dashboard-Symbole in Entwürfen reserviert. Dev-Lauf prüft erneut)"
testszenario_vorschlag: "TS-176 (docs/TESTSZENARIEN.md im Worktree: höchstes Kapitel TS-174 Z. 13507; TS-171/172/175 in Entwürfen reserviert, TS-165 reserviert. Dev-Lauf prüft erneut)"
affected_code:
  - "IdealAkeWms/Views/Home/Index.cshtml:1-37 (Reiter Favoriten/Alle, Stern-Knopf je Kachel, Hinweise)"
  - "IdealAkeWms/Controllers/HomeController.cs:10 (Index mit Reiter-Parameter + Favoriten laden; neue POST-Actions FavoritUmschalten, FavoritenBereinigen)"
  - "IdealAkeWms/Services/Navigation/NavigationKatalog.cs:153-167 (NavigationErgebnis: neue reine Methode Favoriten(keys))"
  - "IdealAkeWms/Data/Repositories/IUserViewPreferenceRepository.cs:5-12 + UserViewPreferenceRepository.cs:15-54 (nur genutzt, unverändert)"
  - "IdealAkeWms/wwwroot/css/site.css (.dashboard-card: Stern-Knopf über der Karte, Fokusring)"
  - "IdealAkeWms.Tests/Services/NavigationSichtTests.cs:250-257 (Home-Wächter erweitern) + neu IdealAkeWms.Tests/Controllers/HomeControllerFavoritenTests.cs"
  - "IdealAkeWms/Views/Help/Index.cshtml:472-485 (Hilfe „Startseite und Menue“), Views/Help/Changelog.cshtml, beide AppVersion.cs"
  - "docs/anwender/18-startseite-und-menue.md (+ Fotos), docs/TESTSZENARIEN.md (TS-176)"
open_questions:
  - "1. Standard-Reiter beim Öffnen: „Favoriten“, sobald mindestens ein sichtbarer Favorit existiert, sonst „Alle“; der zuletzt gewählte Reiter wird nicht gespeichert? Empfehlung: ja."
  - "2. Reihenfolge im Reiter Favoriten: Katalogreihenfolge, als eine flache Kachelreihe ohne Kategorie-Überschriften (kein Sortieren durch den Benutzer)? Empfehlung: ja. Alternative: frei sortierbar mit Pfeil-Knöpfen je Kachel (Mehraufwand, eigene Folgenotiz)."
  - "3. Favoriten auch im Menü? Empfehlung: nein. Das Logo führt schon auf die Startseite, die dann den Reiter Favoriten zeigt. Alternative: Dropdown „Favoriten“ ganz links in der Navbar (der Quelltext-Wächter T3 muss dafür erweitert werden)."
  - "4. Verhalten bei Rechte- oder Modulentzug: Der Favorit bleibt gespeichert, ist aber ausgeblendet. Der Reiter Favoriten zeigt einen gelben Hinweis mit den betroffenen Titeln und den Knopf „Nicht verfügbare entfernen“. Kommt das Recht zurück, erscheint der Favorit wieder. Empfehlung: ja. Alternative: beim nächsten Aufruf automatisch löschen und einmal melden."
  - "5. Der Admin-Knopf „Alle Ansichten zurücksetzen“ bzw. „Zurücksetzen“ in Benutzer bearbeiten löscht künftig auch die Favoriten des Benutzers. Angezeigt wird der Eintrag als „Startseite.Favoriten“. Ist das so in Ordnung? Empfehlung: ja (bestehende Rücksetz-Logik, kein Sonderfall)."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald"
freigabe_am: "2026-11-23"
---

## Ziel / Nutzen (das Warum)

Seit v1.99.45 zeigt die Startseite jede freigeschaltete Funktion als Kachel nach Kategorie ([[0034-navigation-menue-und-kacheln-aus-einer-quelle]]).
Für Admins und Mehrrollen-Benutzer sind das Dutzende Kacheln. Wer täglich drei davon braucht, sucht trotzdem. Die Vorgänger-Spec hat
Favoriten ausdrücklich ausgeschlossen ([[2026-11-19-dashboard-kacheln-nach-kategorie-und-berechtigung-spec]], Out-of-Scope Z. 110). Das hier
ist der Folgeschritt: Jeder Benutzer markiert Kacheln mit einem Stern, und ein Reiter „Favoriten“ zeigt nur diese. Gespeichert wird in der
DB, damit Terminal und PC dieselben Favoriten zeigen.

### Ist-Stand (belegt, Worktree `2026-08-07-ideal-teile-1-5`, v1.99.47)

- **Startseite** `Views/Home/Index.cshtml:1-37`: holt `(await Navigation.GetAsync()).Kacheln` (:5), Hinweis bei null Kacheln (:10-13), je
  Kategorie eine `<section>` (:14-37), jede Kachel ist ein Link um die ganze Karte (:22-32). `HomeController.Index` ist nur `View()` (:10).
- **Katalog** `Services/Navigation/NavigationKatalog.cs`: `NavZiel` (:9-10, `Kategorie == null` = nur Menü), `NavZiele.Alle` in
  Katalogreihenfolge (:136-149), `NavigationErgebnis.Zeigt` (:158) und `Kacheln` (:164-166), Regel je Key in `NavigationSicht` (:172-256),
  scoped (`Program.cs:171`).
- **Wächter** `IdealAkeWms.Tests/Services/NavigationSichtTests.cs`: T3 Menü (:213-235) prüft jeden Link mit `asp-controller=… asp-action=…`
  im Navbar-Block; T4 (:250-257) verlangt, dass `Home/Index.cshtml` kein `ViewBag.` und kein `asp-controller=` enthält und
  `HomeController.cs` kein `ViewBag`.
- **Benutzerbezogene Ablage in der DB, schon vorhanden:** Tabelle `UserViewPreferences` (`Models/UserViewPreference.cs:3-10`:
  `UserId`, `ViewKey`, `SettingsJson`, erbt `AuditableEntity`). Schema: `SQL/00_FreshInstall.sql:2072-2089` mit `ViewKey NVARCHAR(50)`,
  `SettingsJson NVARCHAR(MAX)`, `UNIQUE (UserId, ViewKey)`, `ON DELETE CASCADE` am Benutzer. Repository
  `IUserViewPreferenceRepository.cs:5-12` (`GetByUserAndViewAsync`, `SaveAsync` als Upsert mit Audit-Feldern
  `UserViewPreferenceRepository.cs:21-44`, `DeleteByUserAndViewAsync` :46-54), scoped registriert `Program.cs:154`.
  Es gibt schon zwei **serverseitige** Nutzer mit eigenem Schlüssel außerhalb der Spaltenauswahl:
  `PurchaseOrdersController.cs:94` (`"PurchaseOrders.Ueberfaellige"`, Lesen/Schreiben :136-139) und
  `FaHierarchyKommissionierListenController.cs:48` (`"FaHierarchyKommissionier.Ziele"`, Löschen :137).
- **Nicht wiederverwendbar:** Die Profil-Schalter des Scanners („Scannen ohne Feld“ u. a.) liegen nur im Browser-`localStorage`
  (`Views/Account/Profile.cshtml:165-224`, Kommentar :165 „kein Server-Schreiben“). Sie widersprechen also der Anforderung „gleiche
  Favoriten am Terminal und am PC“. Die Profil-Spalten am `User` (`Models/User.cs:79-110`, z. B. `DefaultWorkbenches`) bräuchten eine neue
  Spalte samt Migration und würden zusätzlich von zwei Formularen überschrieben (`AccountController.cs:250-277`, `UsersController.cs:313-337`).
- **API-Grenze:** `UserViewPreferencesApiController` nimmt nur ViewKeys an, die in `ColumnDefinitions` registriert sind (:29-30, :45-46, :67-68).
  Ein Favoriten-Schlüssel ist dort **nicht** registriert. Damit kann ihn niemand über `PUT api/user-view-preferences/...` mit beliebigem JSON
  beschreiben, nur über die neue, prüfende Action.
- **Admin-Sicht:** `UsersController.Edit` listet alle gespeicherten Ansichts-Einstellungen des Benutzers (:291). `Users/Edit.cshtml:207-239`
  zeigt sie mit Zurücksetzen-Knopf, der Name fällt auf den rohen ViewKey zurück (:218). `ResetViewPreferences` löscht einzeln oder alle
  (`UsersController.cs:380-398`). Das betrifft Rückfrage 5.
- **Rechte je Request:** Rollen cacht `CurrentUserService` nur im Feld der scoped Instanz (`CurrentUserService.cs:177-194`, scoped
  `Program.cs:170`). Ein entzogenes Recht wirkt also ab dem nächsten Seitenaufruf, auch für Favoriten.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Startseite mit zwei Reitern: „Favoriten“ und „Alle“ (heutige Kategorie-Ansicht unverändert).
- Stern-Knopf je Kachel zum Hinzufügen und Entfernen, Speicherung je Benutzer in `UserViewPreferences` (ViewKey `Startseite.Favoriten`).
- Ausblenden nicht mehr sichtbarer Favoriten mit sichtbarem Hinweis und Bereinigen-Knopf (Rückfrage 4).
- Tests, Hilfe, Anwender-Changelog, Anwenderdoku Kapitel 18, Testszenario, Brain.

**Out-of-Scope**
- Neue Tabelle, neue Spalte, Migration: entfällt durch Wiederverwendung von `UserViewPreferences`.
- Favoriten im Menü (Rückfrage 3, Empfehlung nein) und freie Sortierung (Rückfrage 2, Empfehlung nein).
- Favoriten für reine Menüziele ohne Kachel (`Kategorie == null`, z. B. Sage-Lagerbuchungen, BDE-Stammdaten). Die Notiz sagt „nur aus
  Kacheln“.
- Vom Admin vorbelegte Favoriten je Rolle oder Standort.
- Rechte- oder Filteränderungen. Die Sichtbarkeit bleibt allein `NavigationSicht` (ADR 0034).

## Fachliche Anforderungen

F1 **Was ein Favorit ist.** Ein Favorit ist der Key eines `NavZiel` mit Kategorie, also einer Kachel. Gespeichert wird je Benutzer eine Liste
von Keys. Es gibt keine Titel, keine URL und keine Rechte-Kopie: Titel, Ziel und Sichtbarkeit kommen bei jedem Aufruf aus dem Katalog bzw. aus
`NavigationSicht`.

F2 **Sichtbar ist nur, was die Kachel ohnehin zeigt.** Im Reiter Favoriten erscheint ein Favorit genau dann, wenn seine Kachel im Reiter „Alle“
sichtbar wäre (`nav.Zeigt(z)` und `z.Kategorie != null`). Ein Favorit kann also nie mehr zeigen als die Startseite heute (F3 der
Vorgänger-Spec bleibt gültig).

F3 **Stern.** Jede Kachel im Reiter „Alle“ und im Reiter „Favoriten“ hat einen Stern-Knopf. Leer heißt „kein Favorit“, voll heißt
„Favorit“. Ein Klick schaltet um. Hinzufügen geht nur für eine aktuell sichtbare Kachel (Server prüft). Entfernen geht für jeden gespeicherten
Key, auch einen nicht mehr sichtbaren.

F4 **Reiter.** Die Reiter „Favoriten“ und „Alle“ stehen über den Kacheln. Standard beim Öffnen laut Rückfrage 1: „Favoriten“, wenn mindestens
ein Favorit sichtbar ist, sonst „Alle“. Über `?reiter=favoriten|alle` lässt sich der Reiter erzwingen. Das nutzt die Weiterleitung nach dem
Stern-Klick, damit der Benutzer im selben Reiter bleibt. Hat der Benutzer keine einzige Kachel, gibt es keine Reiter, nur den Hinweis von heute
(`Index.cshtml:10-13`).

F5 **Reihenfolge** (Rückfrage 2): Im Reiter Favoriten stehen die Kacheln in Katalogreihenfolge (`NavZiele.Alle`), als eine Reihe ohne
Kategorie-Überschriften.

F6 **Leerer Reiter Favoriten:** Hinweis `alert alert-info`: „Noch keine Favoriten. Markieren Sie im Reiter „Alle“ eine Kachel mit dem
Stern.“

F7 **Rechte- oder Modulentzug** (Rückfrage 4): Ein gespeicherter Key, der nicht sichtbar ist (Recht fehlt, Modul aus, Kachel nicht mehr im
Katalog), wird nicht angezeigt und nicht still gelöscht. Der Reiter Favoriten zeigt dann `alert alert-warning`: „Nicht verfügbar
(Recht oder Modul fehlt): <Titel, …>“. Für Keys, die der Katalog nicht mehr kennt, steht dort „nicht mehr vorhandene Funktion“. Daneben steht
der Knopf „Nicht verfügbare entfernen“, der genau diese Keys aus der Liste nimmt. Kommt das Recht zurück, ist der Favorit wieder da.

F8 **Ohne Benutzerkennung** (`GetCurrentAppUserId()` null): kein Stern, keine Reiter, Startseite wie heute.

F9 **Standort:** Die Funktion gilt an AKE und IDEAL gleich. Welche Kacheln es gibt, regelt wie bisher `NavigationSicht` je Standort-DB.

## Technischer Loesungsentwurf

Ponytail-Leiter: Ablage (`UserViewPreferences` + Repository), Sichtbarkeit (`NavigationSicht`) und Kartenmuster (`Index.cshtml`) gibt es
schon. Neu sind eine reine Auswertungsmethode, zwei POST-Actions und das Reiter-Markup. Es gibt kein JS-Framework, kein AJAX und keine neue
Tabelle. Der Stern ist ein normales Formular mit POST und Weiterleitung, das funktioniert an jedem Terminal-Browser.

1. **Speicherformat:** ViewKey `"Startseite.Favoriten"` (20 Zeichen, < 50 laut `00_FreshInstall.sql:2077`) als `internal const` im
   `HomeController` (Muster `PurchaseOrdersController.cs:94`, `FaHierarchyKommissionierListenController.cs:48`). `SettingsJson` =
   `{"ziele":["Einbuchung","FaListe"]}`, gelesen und geschrieben mit `System.Text.Json`. Ist das JSON kaputt oder fehlt die Zeile, gilt die
   Liste als leer. Ein kaputter Wert wird nicht still überschrieben, sondern per `ILogger` als Warnung protokolliert. Wird die Liste leer, wird
   die Zeile über `DeleteByUserAndViewAsync` entfernt, sie bleibt nicht als leeres JSON stehen.
2. **`NavigationErgebnis.Favoriten(IReadOnlyCollection<string> keys)`** (in `NavigationKatalog.cs`, rein, ohne Dienst):
   liefert `(IReadOnlyList<NavZiel> Sichtbar, IReadOnlyList<string> NichtVerfuegbar)`. `Sichtbar` = `NavZiele.Alle` in Katalogreihenfolge, gefiltert
   auf `keys.Contains(z.Key) && z.Kategorie != null && Zeigt(z)`. `NichtVerfuegbar` = die übrigen Keys, bei bekannten Keys der Titel, bei
   unbekannten der Key selbst. Doppelte Keys zählen einmal.
3. **`HomeController`** (Konstruktor: `NavigationSicht`, `IUserViewPreferenceRepository`, `ICurrentUserService`, `ILogger<HomeController>`):
   - `Index(string? reiter)`: Favoriten-Keys lesen und dann `View(new StartseiteViewModel(keys, reiter))`. Das ViewModel ist ein `record` im
     selben Projekt (kein ViewBag, Wächter T4).
   - `[HttpPost, ValidateAntiForgeryToken] FavoritUmschalten(string key, string? reiter)`: Ohne Benutzer-Id → `RedirectToAction(Index)`.
     Liste lesen. Ist der Key enthalten, wird er entfernt (immer erlaubt). Sonst wird er nur hinzugefügt, wenn
     `NavZiele.Alle` ihn mit Kategorie kennt **und** `nav.Zeigt` wahr ist, sonst `TempData["WarningMessage"] = "Diese Funktion ist für Sie
     nicht verfügbar."`. Speichern über `SaveAsync(userId, key, json, GetDisplayName(), GetWindowsUserName())` (setzt die Audit-Felder,
     Repository :26-40), danach `TempData["SuccessMessage"]` („„<Titel>“ zu den Favoriten hinzugefügt.“ / „… entfernt.“) und
     `RedirectToAction(nameof(Index), new { reiter })`.
   - `[HttpPost, ValidateAntiForgeryToken] FavoritenBereinigen()`: entfernt alle Keys aus `NichtVerfuegbar` (F7), dann SuccessMessage mit
     Anzahl und Redirect auf den Reiter Favoriten.
   - Kein `RequireXxxAccess`-Filter: Die Startseite hat heute auch keinen (`HomeController.cs:7-10`), und jeder Benutzer verwaltet nur seine
     eigene Zeile (Benutzer-Id aus der Session, nie aus dem Request).
4. **`Home/Index.cshtml`**: Bootstrap-5-Reiter (`nav nav-tabs` mit `role="tablist"`, Panels `tab-pane`), aktiver Reiter serverseitig nach F4
   gesetzt, Umschalten per vorhandenem Bootstrap-JS ohne Reload. Panel „Alle“ = das bestehende Kategorie-Markup (:14-37). Panel „Favoriten“ =
   eine `row g-4` mit denselben Karten, darüber die Hinweise F6/F7.
   **Kein interaktives Element im Link:** Heute umschließt `<a>` die ganze Karte (:22). Der Stern darf dort nicht hinein (ungültiges HTML,
   Tastatur und Screenreader brechen). Lösung: Karte als `position-relative`, Link mit `stretched-link` auf dem Titel, das Stern-Formular als
   Geschwister mit `position-absolute top-0 end-0` und eigenem `z-index` über dem Link. Das Formular schreibt `asp-action` **ohne**
   `asp-controller` (Wächter T4 :253).
   Knopf: `<button type="submit" class="btn btn-link ..." aria-pressed="true|false" aria-label="Zu Favoriten hinzufügen: <Titel>" bzw.
   „Aus Favoriten entfernen: <Titel>“>` mit Inline-SVG-Stern (`aria-hidden="true" focusable="false"`, leerer bzw. voller Pfad, keine
   Icon-Bibliothek). Trefferfläche mindestens 44 × 44 px. Die Bedeutung trägt die Form (leer/voll) plus `aria-pressed`, nicht die Farbe.
5. **Frontend:** Die Umsetzung **muss** den Skill `frontend-design` ausdrücklich aufrufen (CLAUDE.md). Es gelten die Leitplanken aus
   CLAUDE.md und der Vorgänger-Spec (Konsistenz vor Eigenständigkeit, nur Bootstrap 5 und `.dashboard-card`). Stern-Farbe `--ake-primary`
   #053153 (≥ 12:1 auf Weiß laut QA v1.99.45). `--ake-orange` oder `--ake-green` sind für den Stern nicht erlaubt (< 3:1, Vorgänger-Spec
   Punkt 4). Der Fokusring am Stern ist sichtbar wie an der Karte. Bei 390 px eine Spalte, kein Querscrollen.
6. **Tests** (xUnit/FluentAssertions/Moq; Muster `NavigationSichtTests`, `UserViewPreferencesApiControllerTests`):
   - `NavigationErgebnis.Favoriten`: Reihenfolge = Katalog trotz anderer Speicherreihenfolge; unsichtbarer Key → `NichtVerfuegbar` mit Titel;
     unbekannter Key → `NichtVerfuegbar` mit Key; Ziel ohne Kategorie (z. B. `SageLagerbuchungen`) → nie `Sichtbar`; Duplikate einmal.
   - `HomeControllerFavoritenTests` (EF-InMemory für `UserViewPreferences`, Muster `PurchaseOrdersControllerTests.cs:209-220`):
     Hinzufügen sichtbar → Zeile mit Audit-Feldern; Hinzufügen unsichtbar oder ohne Kategorie → keine Zeile, WarningMessage; Entfernen des
     letzten → Zeile weg; Entfernen eines unsichtbaren Keys erlaubt; Bereinigen entfernt nur nicht verfügbare; ohne Benutzer-Id kein Schreiben;
     kaputtes JSON → leere Liste, kein Wurf.
   - Wächter T4 (`NavigationSichtTests.cs:250-257`) erweitern: `Home/Index.cshtml` enthält kein `<button` und kein `<form` innerhalb eines
     `<a ` (einfacher Quelltext-Check auf Verschachtelung). Die bestehenden Bedingungen bleiben (kein `ViewBag.`, kein `asp-controller=`).
   - ViewKey-Länge ≤ 50 als Fakt-Test gegen die Konstante.

Betroffene Module: [[controller]] (Start/Hilfe), [[module]].

## Migrations-/SQL-Auswirkungen

Keine. Es gibt keine Migration, kein `SQL/XX`, keine Änderung an `00_FreshInstall` und keine AgentJobs. Die Tabelle `UserViewPreferences` gibt
es schon (`00_FreshInstall.sql:2072-2089`, Migration `20260410124248_AddUserViewPreferences`, :3298). Die reservierte Nummer SQL/164
(BDE-Liste-Entwurf) bleibt unberührt.

Bestehende Daten: Es gibt keine Zeilen mit ViewKey `Startseite.Favoriten`. Die Prüfabfrage für den Dev-Lauf je DEV-DB, Erwartung 0:
```sql
SELECT COUNT(*) AS Treffer FROM dbo.UserViewPreferences WHERE ViewKey = N'Startseite.Favoriten';
```

## Einstellungen

keine. Favoriten sind eine Benutzer-Ansichtseinstellung in `UserViewPreferences`, also keine AppSetting, keine ServiceSetting und keine
kuratierte Maske nach ADR 0020. Es gibt keinen Modulschalter: Die Funktion ist immer an, ohne Stern-Klick bleibt die Startseite wie heute (F4:
ohne Favoriten öffnet „Alle“).

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

`UserViewPreference` erbt `AuditableEntity` (`UserViewPreference.cs:3`). `SaveAsync` setzt beim Anlegen `CreatedAt/By/ByWindows` und beim
Ändern `ModifiedAt/By/ByWindows` (`UserViewPreferenceRepository.cs:26-40`). Die Actions übergeben `GetDisplayName()` und
`GetWindowsUserName()` wie `UserViewPreferencesApiController.cs:55-56`. Löschen ist ein hartes Löschen (Bestandsverhalten, :46-54, ebenso
beim Admin-Zurücksetzen).

## Akzeptanzkriterien

1. Ein Klick auf den leeren Stern einer sichtbaren Kachel legt sie als Favorit an. Nach dem Neuladen und auf einem anderen Gerät mit demselben
   Benutzer ist der Stern voll.
2. Ein Klick auf den vollen Stern entfernt den Favoriten. Ist es der letzte, gibt es keine Zeile `Startseite.Favoriten` mehr für den Benutzer.
3. Der Reiter Favoriten zeigt genau die sichtbaren Favoriten in Katalogreihenfolge, ohne Kategorie-Überschriften (Rückfrage 2).
4. Standard-Reiter beim Öffnen laut Rückfrage 1. Nach dem Stern-Klick bleibt der Benutzer im Reiter, in dem er geklickt hat.
5. Ein POST mit einem Key, der für den Benutzer nicht sichtbar ist, keine Kategorie hat oder unbekannt ist, legt nichts an und zeigt eine
   WarningMessage.
6. Rechte- oder Modulentzug: Der Favorit fehlt im Reiter, der gelbe Hinweis nennt seinen Titel, und die DB-Zeile ist unverändert. Nach
   „Nicht verfügbare entfernen“ ist er aus der Liste. Ohne Bereinigen und mit wiederhergestelltem Recht ist er wieder sichtbar.
7. Leerer Reiter Favoriten → Hinweis F6. Benutzer ohne jede Kachel → keine Reiter, nur der Hinweis von heute.
8. Kein Favorit führt auf „Zugriff verweigert“ oder „… ist deaktiviert“.
9. Reiter „Alle“ ist bis auf den Stern identisch mit v1.99.47 bzw. dem dann aktuellen Stand (Kategorien, Reihenfolge, Titel, Symbole).
10. Barrierefreiheit: Tab-Reihenfolge Reiter → je Kachel Link, dann Stern. `aria-pressed` und `aria-label` mit Titel. Fokusring sichtbar.
    Stern-Trefferfläche ≥ 44 px. Kontrast Stern ≥ 3:1. Bei 390 px eine Spalte, kein Querscrollen. Kein `<button>`/`<form>` innerhalb eines
    `<a>`.
11. `PUT api/user-view-preferences/Startseite.Favoriten` antwortet weiterhin 400 (Key nicht in `ColumnDefinitions`).
12. Build und alle Tests grün (Web + Service).

## Test-Szenarien

Neues Kapitel **TS-176 „Startseite: Reiter Favoriten (v1.99.51)“** in `docs/TESTSZENARIEN.md` (Nummer und Version prüft der Dev-Lauf),
Querverweis aus TS-170, danach eine Zeile in [[testszenarien-index]]. Standort: DEV **AKE** und DEV **IDEAL**.

**Vorbedingungen:** Prüfabfrage aus „Migrations-/SQL-Auswirkungen“ je DEV-DB = 0. Testbenutzer admin sowie ein Benutzer mit Rolle picking
(ohne masterdata_read). Zwei Geräte oder zwei Browser.

| Nr | Schritt | Erwartet |
|---|---|---|
| 176.1 | admin, erster Aufruf ohne Favoriten | Reiter „Alle“ aktiv, alle Kacheln wie vorher, jede mit leerem Stern. Reiter Favoriten: Hinweis F6. |
| 176.2 | Stern an drei Kacheln aus verschiedenen Kategorien setzen (z. B. Einstellungen, Einbuchung, FA-Liste) | Erfolgsmeldung, Stern voll, Reiter bleibt „Alle“. Reiter Favoriten: Einbuchung, FA-Liste, Einstellungen (Katalogreihenfolge). |
| 176.3 | Startseite neu öffnen, dann im zweiten Browser / am Terminal anmelden | Reiter Favoriten aktiv, dieselben drei Kacheln. |
| 176.4 | Im Reiter Favoriten einen Stern abwählen | Kachel verschwindet, Reiter bleibt „Favoriten“. Alle abwählen → Hinweis F6; DB: keine Zeile `Startseite.Favoriten`. |
| 176.5 | Negativ Recht: picking-Benutzer favorisiert „Kommissionierung“; Admin entzieht picking | Nächster Aufruf: Kachel fehlt, gelber Hinweis „Nicht verfügbar … Kommissionierung“. DB-Zeile unverändert. Recht zurück → Favorit wieder da. |
| 176.6 | Negativ Modul: admin favorisiert „Terminal“ (BDE), `BdeAktiv` aus | Wie 176.5 mit Modul. „Nicht verfügbare entfernen“ → Hinweis weg, Key aus der Liste. Schalter zurücksetzen. |
| 176.7 | Negativ manipuliert: per DevTools im Stern-Formular `key` auf `Benutzer` ändern (als picking) bzw. auf `SageLagerbuchungen` bzw. `Quatsch` | Kein Favorit angelegt, WarningMessage. |
| 176.8 | Negativ API: `PUT /api/user-view-preferences/Startseite.Favoriten` | 400. |
| 176.9 | Benutzer ohne Rolle | Keine Reiter, nur der Hinweis „keine Funktionen freigeschaltet“. |
| 176.10 | Barrierefreiheit 1440 px und 390 px: nur Tastatur, Screenreader-Ansage des Sterns | Reihenfolge laut AK 10, Ansage „Zu Favoriten hinzufügen: <Titel>, Umschalter, nicht gedrückt“. Eine Spalte bei 390 px. |
| 176.11 | Admin: Benutzer bearbeiten → „Startseite.Favoriten“ zurücksetzen | Favoriten des Benutzers leer (Rückfrage 5). |

## Etappen (nur bei epic: true)

Kein Epic.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorläufig – vom QA-Lauf 2026-11-23 bestätigt, siehe „QA-Nachweis → Deploy (final)“ am Ende (Paket = Bündel v1.99.53).*

- **Web-App:** ja (HomeController, Home/Index, Katalog, CSS, Hilfe).
- **Service:** nein (kein Dienst-Code). Die Version wird trotzdem in **beiden** `AppVersion.cs` angehoben (vorläufig 1.99.51), wie bei
  v1.99.45. Deshalb steht `deploy.service: true` (nur Versionsnummer).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):
  `@{ Id='v1.99.51-StartseiteFavoriten'; Version='1.99.51'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen';
  Text='Startseite: neuer Reiter "Favoriten". Jeder Benutzer markiert Kacheln mit dem Stern; gespeichert je Benutzer in der DB (gleich an PC und Terminal). Admin "Ansichten zuruecksetzen" loescht auch die Favoriten.' }`
  – sonst keine (kein SQL, kein Einmal-Skript, keine Einstellung; die Prüfabfrage oben ist eine Vorab-Kontrolle des Dev-Laufs, kein Zustand
  nach dem Deploy).
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (Dienst nur wegen der
  Versionsnummer: `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`).
- **Reihenfolge zu parallelen Entwürfen:** [[2026-11-23-bugfix-dashboard-symbole-nachschaerfen-spec]] (v1.99.50) ändert die Symbole in
  `NavigationKatalog.cs:23-133` und den Wächter `NavigationSichtTests.cs:86-92`. Diese Spec ändert `NavigationErgebnis` (:153-167) und
  `Home/Index.cshtml`. Es gibt keine inhaltliche Abhängigkeit, aber dieselben Dateien: im Bündel **nach** der Symbol-Spec umsetzen.
- **Doku-Pflichten:** Anwender-Changelog `Views/Help/Changelog.cshtml`. Hilfe `Views/Help/Index.cshtml:472-485` um „Favoriten“ ergänzen (Stern,
  Reiter, warum ein Favorit fehlt). `docs/anwender/18-startseite-und-menue.md` mit neuen Fotos (Reiter Favoriten, Stern, Hinweis nicht
  verfügbar) per Playwright gegen DEV, nur ansehen. Stern-Klicks auf DEV sind Schreibvorgänge in `UserViewPreferences` des Testbenutzers. Das
  ist harmlos, wird aber danach zurückgesetzt. Brain: Changelog, [[feature-map]] (Zeile Startseite), [[controller]] (Home: neue
  POST-Actions), [[fallstricke]] neuer Eintrag (nächste Nummer, derzeit 60): „Stern in einer Link-Karte: kein interaktives Element im `<a>`
  (stretched-link + Geschwister-Formular); Favoriten nie ohne `NavigationSicht` rendern“. Kein neuer ADR nötig: Die Entscheidung bleibt im
  Rahmen von ADR 0034. Die Ablage folgt dem bestehenden Muster `UserViewPreferences` mit Server-Key.

## Offene Rueckfragen

1. Standard-Reiter beim Öffnen: „Favoriten“, sobald mindestens ein sichtbarer Favorit existiert, sonst „Alle“. Der zuletzt gewählte Reiter wird
   nicht gespeichert. **Empfehlung: ja.** Ein gemerkter Reiter wäre ein zweiter gespeicherter Wert, ohne dass jemand danach gefragt hat.
2. Reihenfolge im Reiter Favoriten: Katalogreihenfolge, eine flache Kachelreihe ohne Kategorie-Überschriften, kein Sortieren durch den
   Benutzer? **Empfehlung: ja.** Alternative: frei sortierbar über Pfeil-Knöpfe „nach vorn/nach hinten“ je Kachel. Das kostet zwei Knöpfe
   je Kachel und eine Reihenfolge-Logik und wäre besser eine eigene Folgenotiz, falls die Katalogreihenfolge in der Praxis stört.
3. Favoriten auch im Menü? **Empfehlung: nein.** Das Logo (`_Layout.cshtml:38`) führt auf die Startseite, und die öffnet mit Favoriten.
   Alternative: Dropdown „Favoriten“ ganz links in der Navbar. Dann muss der Wächter T3 (`NavigationSichtTests.cs:213-235`, er erkennt nur
   `asp-controller`-Links) eine Schleife über Favoriten ausdrücklich zulassen. Auf schmalen Terminals wird die Navbar außerdem noch voller.
4. Verhalten bei Rechte- oder Modulentzug: Der Favorit bleibt gespeichert, wird ausgeblendet, und der Reiter Favoriten zeigt einen gelben
   Hinweis mit den Titeln samt Knopf „Nicht verfügbare entfernen“. Kommt das Recht zurück, ist er wieder da. **Empfehlung: ja**, so ist es
   gemeldet statt still, und ein vorübergehend abgeschaltetes Modul kostet keine Favoriten. Alternative: beim nächsten Aufruf automatisch
   löschen und einmal per WarningMessage melden.
5. Admin „Zurücksetzen“ / „Alle Ansichten zurücksetzen“ in Benutzer bearbeiten (`Users/Edit.cshtml:207-239`, `UsersController.cs:380-398`)
   löscht künftig auch die Favoriten des Benutzers. Der Eintrag erscheint dort als „Startseite.Favoriten“ (Rückfall auf den rohen Key, :218).
   In Ordnung? **Empfehlung: ja.** Das ist die bestehende Rücksetz-Logik ohne Sonderfall. Ein Anzeigename würde einen Eintrag in
   `ColumnDefinitions` erfordern, und der würde den Key ungewollt für die API freischalten (`UserViewPreferencesApiController.cs:45-46`).

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Standard-Reiter: Favoriten wenn vorhanden, sonst Alle; nicht merken → **ja, wie empfohlen**
2. Reihenfolge: Katalogreihenfolge, flach, kein Sortieren → **ja, wie empfohlen**
3. Favoriten im Menü: nein → **nein, wie empfohlen (kein Menü)**
4. Rechteentzug: ausblenden + gelber Hinweis + „Nicht verfügbare entfernen“, nicht still löschen → **ja, wie empfohlen**
5. Admin-Zurücksetzen löscht auch Favoriten, Anzeige „Startseite.Favoriten“ → **ja, wie empfohlen**

## QA-Nachweis (qa-agent, 2026-11-23)

Bündel-QA im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Bereich `0b6fff36..a93bd910`
(vier Specs v1.99.50–v1.99.53 aus [[2026-11-23-goal-lauf-dashboard-we-lagerbestellung]]). Die QA prüft je Spec den eigenen Commit-Bereich, Build und Tests decken den ganzen Worktree ab.

### 1. Build und Tests

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 30 Warnungen (alle bekannt, u. a. CS8620 in `BfAuftragWorkflowTests`, NU1903 Pkcs).
- `dotnet test` vor dem QA-Fix: IdealAkeWms.Tests 3472 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 790 / 0 / 0.
- Nach dem QA-Fix `a93bd910`: IdealAkeWms.Tests **3473 bestanden / 1 übersprungen / 0 Fehler**, IDEALAKEWMSService.Tests **790 / 0 / 0**.

### 6. DEV-Deploy

- `powershell -ExecutionPolicy Bypass -File D:\Dev\scripts\dev-deploy.ps1 -Standort AKE|IDEAL -Quelle <worktree>` in einer erhöhten Shell, Web und Dienst,
  zweimal (nach Build-Grün `f7b26ebc`, nach dem QA-Fix `a93bd910`). Ausgabe jeweils „HTTP 200, Fusszeile v1.99.53 … Fertig: AKE v1.99.53“ bzw. „IDEAL v1.99.53“, Exit 0.
- AKE http://localhost:8081 und IDEAL http://localhost:8082 zeigen in der Fußzeile **v1.99.53** = `AppVersion` des Branches.
- Beide DEV-DBs: letzte Migration `20261123124036_AddStockMovementWarehouseRequisitionItem`, Spalte `StockMovements.WarehouseRequisitionItemId` vorhanden.
- DEV-Sicherheit geprüft: ServiceSetting `SageLagerbuchungAktiv` = false auf AKE_WMS_DEV und IDEAL_WMS_DEV, kein `Sync:*`, keine Mail eingeschaltet.
  `SData:BaseUrl` zeigt auf AKE DEV auf sagetest01, auf IDEAL DEV auf idealsage2. Deshalb wurde keine Sage-Übergabe ausgelöst.

### 7. Publish

`<worktree>\publish\` geleert und neu erstellt: `IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` (`dotnet publish -c Release`, beide Exit 0), `SQL\` → `publish\SQL\`
(inkl. `166_…`), `scripts\deploy\` → `publish\deploy\` (inkl. `Update-IdealAkeWms.ps1`), `publish-info.txt`: Version 1.99.53, Commit
`a93bd91039e22bae194422e11e253d1cd6cbab78`, Branch, vier Spec-Slugs, Erstellt 2026-11-23 14:12, Komponenten IDEALAKEWMSWeb + IDEALAKEWMSWebService.
Paket-ZIP/Shares macht der Koordinator.

### Bündel-weite geänderte Bestandstests (Punkt 4)

| Test | Einordnung |
|---|---|
| `PickingControllerTests` | nur Typ: Quell-Vorschlag nach `QuellLagerplatzVorschlag.Waehle` verschoben (Lagerbestellung) |
| `PurchaseOrdersControllerTests`, `SageWareneingaengeListeTests` | nur Typ: Ctor um `IArticleRepository` (WE) |
| `PurchaseOrdersReceiveTests` | Ctor + erwartete Meldung bewusst geändert (WE F1/AK 5, Mehrdeutigkeit) |
| `WarehousePickingControllerTests` | Ctor + `GetByIdAsync`-Setup wegen Status-Guard F4 (Lagerbestellung AK 16, bewusst) |
| `NachlaufManifestDriftTests` | + SQL/166 (Lagerbestellung) |
| `CsvExportCoverageTests` | bewusst: Sortiertext `data-filter-text` (Lagerbestellung Abweichung 6) |
| `AppSettingDefinitionsTests` | Anzahl 67 → 68 (neue Einstellung Lagerbestellung) |

Keine Änderung der Klasse „sonstiges“: Keine Fixture wurde angepasst, nur damit ein Test grün wird.

### 2. Code-Review (Commits `4802462c`, `563adecd`, selbst durchgeführt)

Geprüft: `HomeController` (Index mit `reiter`, POST `FavoritUmschalten` und `FavoritenBereinigen` mit Antiforgery; Entfernen geht immer,
Hinzufügen nur bei sichtbarer Kachel mit Kategorie; eine leere Liste löscht die Zeile; Audit über `SaveAsync` mit DisplayName/Windows-Name),
`NavigationErgebnis.Favoriten` (Katalogreihenfolge, nicht verfügbare Keys mit Titel oder „(nicht mehr vorhandene Funktion)“), `Home/Index.cshtml`
(Stern-Formular als Geschwister des stretched-link, kein `<form>` oder `<button>` im `<a>`, `aria-pressed`/`aria-label`), `StartseiteViewModel`, `site.css`
(`.dashboard-stern` z-index 2, 44 px, `--ake-primary`). Der Key steht bewusst nicht in `ColumnDefinitions`, daher antwortet die API mit 400.
Die Regel liegt serverseitig, kein JS und kein `@Html.Raw` in Skripten.
**Befunde: keine.**

### 3. Nachweisart je AK

| AK | Nachweis |
|---|---|
| 1, 2, 3, 4, 6 | automatisch (`HomeControllerFavoritenTests`, `NavigationSichtTests.Favoriten_*`) **und** DEV IDEAL (siehe 6c) |
| 5 | automatisch (`HomeControllerFavoritenTests`), manipulierter POST im Browser nicht ausgeführt |
| 7, 8 | automatisch; leerer Reiter mit Hinweis F6 auf DEV gesehen |
| 9 | DEV-Sicht (Reiter „Alle“ AKE/IDEAL 1440 px) |
| 10 | teils: `NavigationSichtTests.Startseite_KeinFormularOderKnopfInnerhalbEinesLinks` automatisch, Tab-Reihenfolge Link → Stern mit aria-label auf DEV geprüft, Kontrast Stern `#053153` auf Weiß 13,4:1 (berechnet). Fokusring, Screenreader und 390 px: **Manual-UAT** |
| 11 | DEV: `PUT /api/user-view-preferences/Startseite.Favoriten` → **400** (curl mit Negotiate) |
| 12 | Build/Tests grün |

### 5. Checkliste

Migration/SQL: entfällt. Version 1.99.51 in beiden `AppVersion.cs`, Changelog v1.99.51, TS-176 in `docs/TESTSZENARIEN.md`, Testindex Zeile 176 vorhanden.
Manifest-Info `v1.99.51-StartseiteFavoriten` steht in `scripts/deploy/nachlauf.psd1`. Einstellungen (ADR 0020): keine, Benutzer-Ansicht in `UserViewPreferences`.
Brain-Nachzug (fallstricke §60, controller.md Home, feature-map, Changelog) ist laut Lauf-Notiz offen und Aufgabe des Koordinators.

### 6c. Testlauf auf DEV IDEAL (8082, Benutzer Gerald, UserId 8)

| Schritt | Ergebnis | Status |
|---|---|---|
| Stern bei Einstellungen, Einbuchung, FA-Liste im Reiter „Alle“ | Erfolgsmeldung, Reiter bleibt „Alle“, Zähler „Favoriten (n)“ steigt; DB `{"ziele":["Einstellungen","Einbuchung","FaListe"]}`, ModifiedBy gesetzt | OK |
| Neu laden ohne Parameter | Standard-Reiter Favoriten (3), Reihenfolge Einbuchung, FA-Liste, Einstellungen (Katalog) | OK |
| Tastatur/Accessibility-Baum | je Kachel Link, dann Stern; aria-label „Aus Favoriten entfernen: <Titel>“ | OK |
| Nicht verfügbare Keys (DB: „Quatsch“, „OffeneBestellungenEinkauf“ auf IDEAL unsichtbar) | gelber Hinweis nennt „Quatsch (nicht mehr vorhandene Funktion), Offene Bestellungen (Einkauf)“, Knopf „Nicht verfügbare entfernen“ | OK |
| „Nicht verfügbare entfernen“ | Meldung „2 nicht verfügbare Favoriten entfernt.“; DB wieder nur die drei Ziele | OK |
| Alle drei Sterne im Reiter Favoriten entfernen | bleibt im Reiter Favoriten, am Ende „Favoriten (0)“ + Hinweis F6; DB-Zeile gelöscht (COUNT 0) | OK |
| API-PUT auf `Startseite.Favoriten` | HTTP 400 | OK |
| Rechte-/Modulentzug mit einem echten zweiten Benutzer, Admin-Zurücksetzen, manipulierter POST | nicht durchgeführt (Unit-Tests decken das ab) | NICHT MOEGLICH, Manual-UAT |
| Screenreader, Fokusring sichtbar, 390 px | Chrome-Screenshot im Lauf eingefroren, Headless-Mindestbreite etwa 500 px | NICHT MOEGLICH, Manual-UAT |

Test-Favoriten danach zurückgesetzt (keine Zeile mehr für UserId 8).

### Deploy (final, aus dem echten Diff)

- Web: ja (HomeController, Home/Index, NavigationKatalog/-Ergebnis, ViewModel, CSS, Hilfe, Changelog). Dienst: ja, **nur Versionsnummer**. Migration: nein.
- Manifest: `v1.99.51-StartseiteFavoriten` (Info, Nachher, anzeigen) vorhanden.
- Paket: `<worktree>\publish` mit **v1.99.53** (Bündel), Commit `a93bd910`. Ablauf: Publish aus dem Worktree → Testsystem → testen → Merge.
  Nach dem Merge nur dann neu aus main publishen, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.

### Manuelle Testcheckliste (offen für den Menschen)

1. TS-176 komplett auf dem Testsystem (AKE und IDEAL), mit zwei Benutzern: Favorit setzen, an einem zweiten Gerät denselben Stand prüfen.
2. Rechteentzug: dem Benutzer eine Rolle nehmen (Favorit fehlt, Hinweis nennt den Titel), Rolle zurückgeben (Favorit wieder da). Modul-Schalter aus/an genauso.
3. Admin „Ansichten zurücksetzen“ löscht auch die Favoriten.
4. Tastatur: Tab-Reihenfolge, Fokusring am Stern sichtbar, Enter/Leertaste schaltet. Screenreader liest „Zu/Aus Favoriten …: <Titel>“ und den Zustand.
5. 390 px: eine Spalte, kein Querscrollen, Stern mit 44 px antippbar.
6. Die Doku-Fotos Kapitel 18 (Reiter Favoriten, Stern, Hinweis) sind nachgezogen (Koordinator).
