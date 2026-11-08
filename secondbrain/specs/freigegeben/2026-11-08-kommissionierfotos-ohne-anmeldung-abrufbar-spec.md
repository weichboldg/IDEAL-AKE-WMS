---
type: spec
title: Kommissionierfotos nur mit Anmeldung und Recht Kommissionierung; Foto-Löschen nur im eigenen FA (Sicherheits-Hotfix)
slug: 2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-spec
status: Testbereit
created: 2026-11-08
updated: 2026-11-08
source_backlog: "[[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar]]"
bug: "[[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-bug]]"
depends_on: ""
version: "1.30.4"
sql_nummern: keine
testszenario: "TS-124"
task: ""
worktree: .claude/worktrees/2026-11-08-hotfix-kommissionierfotos
branch: feature/2026-11-08-hotfix-kommissionierfotos
standort: beide
affected_code:
  - "IdealAkeWms/Program.cs:432-434 (Foto-Ordner + Pfad-Migration)"
  - "IdealAkeWms/Program.cs:456-485 (Login-Middleware -> eigene Klasse)"
  - "IdealAkeWms/Program.cs:487-488 (Kommentar UseStaticFiles)"
  - "IdealAkeWms/Middleware/LoginRedirectMiddleware.cs (neu, aus Program.cs:457-485)"
  - "IdealAkeWms/Services/PhotoStorage.cs (neu: Ordner, Dateinamen-Prüfung, Migration)"
  - "IdealAkeWms/Controllers/Api/PhotoController.cs:30-32, 62-64, 81-97 (+ neue GET-Action File)"
  - "IdealAkeWms/wwwroot/js/photo-upload.js:107, 116-121 (Delete mit productionOrderId)"
  - "IdealAkeWms/Views/Help/Index.cshtml:802 (Hilfetext Sichtbarkeit)"
  - ".gitignore (IdealAkeWms/App_Data/)"
  - "IdealAkeWms.Tests/Middleware/LoginRedirectMiddlewareTests.cs (neu)"
  - "IdealAkeWms.Tests/Controllers/PhotoControllerTests.cs (neu)"
  - "IdealAkeWms.Tests/Services/PhotoStorageTests.cs (neu)"
  - "IdealAkeWms/AppVersion.cs, IDEALAKEWMSService/AppVersion.cs, IdealAkeWms/Views/Help/Changelog.cshtml (v1.30.4)"
  - "docs/TESTSZENARIEN.md (Kapitel 124)"
  - "Bug: [[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-bug]]"
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold (Auftrag Hotfix)"
freigabe_am: 2026-11-08
---

> [!warning] Ausnahme Schranke 1
> Gerald hat diesen Hotfix ausdrücklich direkt freigegeben (Auftrag vom 2026-11-08). Die Spec liegt deshalb gleich in `specs/freigegeben/`. Schranke 2 (manueller Test, dann Merge) gilt unverändert.

## Ziel / Nutzen (das Warum)
Kommissionierfotos zeigen Ware, Aufträge und Kunden. Heute bekommt sie jeder ohne Anmeldung, der die URL kennt. Ein Benutzer mit dem Recht Kommissionierung kann außerdem Fotos jedes FA löschen. Der Fix macht beides dicht. Er muss **vor Lauf B (QM-Fotos)** stehen, weil dort weitere Fotos dazukommen ([[2026-11-08-buendel-uebersicht]]).

## Nachgestellt
Der Aufrufer hat die Lücke am 2026-11-08 auf AKE-DEV (`localhost:8081`) nachgestellt:
- Stand: Bündel v1.94.0, nicht main. Der main-Stand startet auf der AKE-DEV-DB nicht, weil diese DB das Schema des Bündels hat.
- Der betroffene Code ist zwischen main und Bündel gleich (Login-Middleware bis `UseStaticFiles` in `Program.cs`, `PhotoController.cs`, `photo-upload.js`). Per `git diff` geprüft, keine Unterschiede.
- Testdatei: `D:\Dev\AKE\Web\wwwroot\Fotos\Kommissionierung\REPRO-TEST_20261108000000_1.jpg`.
- curl **ohne** Anmeldung:
  - `/Fotos/Kommissionierung/REPRO-TEST_20261108000000_1.jpg` → **200 image/jpeg** (die Datei wird ausgeliefert)
  - `/` → 302 auf `/Account/Login` (Gegenprobe: die Anmeldepflicht greift sonst)
  - `/Home/Index.x` → 404, also nicht 302 (jeder Pfad mit Punkt umgeht die Login-Umleitung)
- Ergebnis: Die Lücke ist nachgestellt und belegt.
- **Nachtrag 2026-11-08 – main-Stand und Screenshots.** Auf der IIS-Site AKE-DEV (localhost:8081) startet ein main-Stand nicht (AKE_WMS_DEV hat
  das Bündel-Schema, OSEON-QA Lauf 2 Befund U1). Der main-Stand **v1.30.3** (Publish `D:\Dev\QA\Web`, Version per DLL geprüft, DB-Kopie
  `AKE_WMS_QA_MAIN`, Kestrel :5182, gleicher DEV-Rechner) liefert ein echtes Foto ohne Anmeldung aus (Bild 2). Abweichung vom Auftrag
  („DEV AKE localhost:8081, main-Stand“): Kestrel + DB-Kopie statt IIS-Site; der Code der Lücke ist zwischen main und Bündel identisch (git diff).
  - Bild 1 – AKE-DEV :8081 (Bündel-Stand, IIS), anonymes Profil, Repro-URL: Bildbetrachter statt Anmeldeseite (defektes Bildsymbol, weil die
    14-Byte-Repro-Datei kein echtes JPEG ist; curl `200 image/jpeg`, `Content-Length: 14`).
    ![[kommissionierfotos-01-vorher-akedev-8081-foto-ohne-anmeldung.png]]
  - Bild 2 – main v1.30.3, anonym, echtes JPEG sichtbar:
    ![[kommissionierfotos-02-vorher-kestrel-v1302-foto-ohne-anmeldung.png]]
  - Bild 3 – nach dem Fix v1.30.4, dieselbe URL anonym: Anmeldeseite.
    ![[kommissionierfotos-03-nachher-v1304-foto-ohne-anmeldung.png]]
  - Ablage: `bugs/anhaenge/2026-11-08-kommissionierfotos/` (Originale und weitere Nachweise in `D:\Dev\QA\Nachweis-v1304\`).

## Befund im Code (main v1.30.3, Worktree)
- Der Foto-Ordner liegt unter `wwwroot`: `Program.cs:433`, `PhotoController.cs:30-32, 62-64, 87-89` (dreimal kopiert).
- Die Login-Middleware lässt `/api/`, `/lib/`, `/css/`, `/js/` und `/_framework/` durch, außerdem **jeden Pfad mit `.`** (`Program.cs:462-470`). Danach liefert `UseStaticFiles` (`:488`) alles aus `wwwroot`. `MapStaticAssets` (`:490`) kennt nur Dateien, die beim Build vorhanden waren. Hochgeladene Fotos liefert also `UseStaticFiles` aus.
- `/api/` ist absichtlich ausgenommen. Die Foto-API schützt `[RequirePickingAccess]` (`PhotoController.cs:9`). Der Filter prüft `CanPickAsync` = Admin oder `picking` (`CurrentUserService.cs:91-92`) und leitet sonst auf AccessDenied um (`RequirePickingAccessAttribute.cs:23-27`).
- `Delete` (`PhotoController.cs:81-97`) bekommt nur `fileName`. Es prüft auf `..`, `/` und `\`, aber nicht den FA. Das JS sendet nur `fileName` (`photo-upload.js:120`).
- **Wer sieht die Fotos heute:** nur die Stückliste der Kommissionierung `PickingController.Bom` (`PickingController.cs:206-207`, `[RequirePickingAccess]`). Die Foto-Karte erscheint nur bei `!Model.ReadOnly` (`Bom.cshtml:325-326, 363`). Die Read-only-Stücklisten (`FaWorklistController.cs:284-285`, `FaCompletionController.cs:303`) zeigen keine Fotos. Auch die Liste kommt über `[RequirePickingAccess]` (`GET /api/photos/{id}`, `PhotoController.cs:55`). Die Hilfe sagt „für alle Benutzer sichtbar, die den Auftrag öffnen“ (`Views/Help/Index.cshtml:802`). Gemeint ist: wer die Kommissionier-Stückliste öffnen darf. **Wenn die Dateien nur mit `[RequirePickingAccess]` ausgeliefert werden, sehen genau dieselben Benutzer die Fotos wie heute in der Oberfläche.**
- **Weitere Verweise auf `/Fotos`** (Grep über den Worktree, ohne bin/obj): nur `PhotoController.cs:51, 73` (URL in der Antwort), `Program.cs:432-433, 487` und Prosa in Hilfe und Doku. Kein Verweis in Views für Drucke oder Mails, im Service oder in `SQL/`. Die URL steht in **keiner** DB-Spalte, denn die API baut sie bei jedem Abruf neu aus dem Dateinamen (`PhotoController.cs:69-74`). Grenze dieser Aussage: Was Benutzer selbst kopiert haben (Lesezeichen, weitergeleitete Links), kann der Grep nicht sehen. Darum bleibt die URL gleich.

## Umfang (In-Scope / Out-of-Scope)
**In-Scope**
1. Foto-Ablage nach `ContentRoot/App_Data/Fotos/Kommissionierung`. Einmalige, idempotente Verschiebung der vorhandenen Dateien beim Start.
2. Auslieferung unter der **gleichen URL** `/Fotos/Kommissionierung/{fileName}` über eine neue GET-Action im `PhotoController` (`[RequirePickingAccess]`).
3. Die Login-Ausnahme für Pfade mit `.` gilt nur noch für echte Dateien in `wwwroot`. Die Middleware wird dafür eine testbare Klasse.
4. `Delete` prüft den FA: Das JS sendet `productionOrderId` mit, und der Dateiname muss mit `{OrderNumber}_` beginnen.
5. Tests, TS-124, Hilfetext, Version 1.30.4, Changelogs.

**Out-of-Scope (Zusatzfunde, nur melden)**
- Z1: `Upload` prüft weder Dateityp noch Größe und hat keine Null-Prüfung für `photo` (`PhotoController.cs:24-45`).
- Z2: Das JS sendet zwar den Header `RequestVerificationToken` (`photo-upload.js:64, 121`), aber weder der `PhotoController` noch ein globaler Filter prüfen ihn. In `Program.cs:130` steht nur `AddControllersWithViews()`, und es gibt kein `[ValidateAntiForgeryToken]`. Es gibt also keinen CSRF-Schutz auf Upload und Delete, nur den SameSite-Default des Session-Cookies.
- Z3: Fotos hängen am `OrderNumber`-Präfix (`PhotoController.cs:36, 69`). FAs mit gleicher OrderNumber oder einer OrderNumber, die mit `<andere>_` beginnt, teilen sich die Fotos. Die neue Delete-Prüfung übernimmt bewusst dieselbe Regel.

## Fachliche Anforderungen
- Ohne Anmeldung liefert keine Foto-URL ein Bild. Die Anfrage wird auf die Anmeldung umgeleitet.
- Wer angemeldet ist, aber kein Recht Kommissionierung hat, bekommt kein Bild (AccessDenied).
- Mit Admin oder `picking` funktionieren Anzeigen, Vollbild (`window.open`), Hochladen und Löschen wie bisher. Das gilt auch für Fotos, die vor dem Update hochgeladen wurden.
- Löschen klappt nur für Fotos des FA, dessen Stückliste offen ist.
- CSS, JS, Bilder und Favicon der Anmeldeseite laden weiterhin ohne Anmeldung.

## Technischer Loesungsentwurf
**1. Ablage außerhalb von `wwwroot`** (neue Klasse `Services/PhotoStorage.cs`, statisch, ohne DI)
- `GetDirectory(IWebHostEnvironment env)` gibt `Path.Combine(env.ContentRootPath, "App_Data", "Fotos", "Kommissionierung")` zurück. Die drei Kopien in `PhotoController` und die eine in `Program.cs:433` nutzen nur noch diese Methode.
- **Warum `App_Data`:** ASP.NET Core liefert nur `WebRootPath` statisch aus, den `ContentRoot` dagegen nie. Zusätzlich sperrt IIS das Segment `App_Data` in der Standard-Konfiguration per `requestFiltering/hiddenSegments`, schon bevor das ASP.NET Core Module die Anfrage bekommt (Schutz in zweiter Linie). Auf den Servern ist das **nicht geprüft**, siehe Deploy-Schritt 1. Der Ordner liegt im Site-Verzeichnis und bleibt beim Kopieren eines Publish erhalten, genau wie heute `wwwroot/Fotos`. Weil ein Publish-Ordner kein `App_Data` enthält, wird er beim Überkopieren auch nicht überschrieben. Der App-Pool braucht dort Schreibrecht, siehe Deploy.
- Kein neues Setting (YAGNI): Der Pfad ist fest, genau wie heute.
- `TryResolve(string dir, string? fileName, out string fullPath)`: Das ist die gemeinsame Pfadprüfung für Delete und Auslieferung. Abgelehnt wird: leer, `..`, `/`, `\`, `Path.GetFileName(fileName) != fileName`, Endung nicht `.jpg` (Upload schreibt immer `.jpg`, `PhotoController.cs:39`) und ein `Path.GetFullPath(...)`, das nicht mit `dir + Path.DirectorySeparatorChar` beginnt.
- `MigrateLegacy(IWebHostEnvironment env, ILogger logger)`: Ist `WebRootPath/Fotos/Kommissionierung` vorhanden, wird jede Datei mit `File.Move(src, dst, overwrite: false)` in den neuen Ordner verschoben. Liegt am Ziel schon eine gleichnamige Datei, bleibt die Quelle liegen und es gibt **eine Warnung je Datei**. Am Ende kommt eine Info-Zeile „Kommissionierfotos verschoben: n, übersprungen: m“, nur wenn n+m > 0. Der alte Ordner bleibt stehen, auch wenn er leer ist. Ein zweiter Lauf findet nichts mehr und ist damit idempotent. Wirft ein Move (gesperrte Datei), wird das je Datei geloggt und der Start läuft weiter. Ein Fehler darf den Start nicht abbrechen. `Directory.CreateDirectory` für den neuen Ordner darf den Start dagegen abbrechen, so wie heute `Program.cs:434`. Ein fehlendes Schreibrecht fällt so sofort auf und nicht erst beim ersten Upload.
- `Program.cs:432-434`: `Directory.CreateDirectory(PhotoStorage.GetDirectory(env))` und danach `PhotoStorage.MigrateLegacy(...)`.

**2. Auslieferung unter der gleichen URL (gewählte Variante)**
- Neue Action in `PhotoController`: `[HttpGet("/Fotos/Kommissionierung/{fileName}")] public IActionResult File(string fileName)`. Durch die absolute Route mit führendem `/` gilt der Präfix `api/photos` nicht, und der Class-Level-Filter `[RequirePickingAccess]` greift trotzdem. Ist `TryResolve` falsch, gibt die Action `BadRequest()` zurück. Fehlt die Datei, `NotFound()`. Sonst `PhysicalFile(fullPath, "image/jpeg")`. Der Name `File` kollidiert mit `ControllerBase.File(...)`. Der Dev-Lauf wählt deshalb einen freien Namen, zum Beispiel `GetFile`.
- Folge für das Dateisystem: **Die URL ist kein Dateipfad mehr.** `/Fotos/Kommissionierung/x.jpg` wird auf `App_Data/Fotos/Kommissionierung/x.jpg` abgebildet. Unter `wwwroot` liegt nach der Migration nichts mehr. `Upload` und `Get` geben weiter `url = "/Fotos/Kommissionierung/{fileName}"` zurück (`PhotoController.cs:51, 73`, unverändert). Darum braucht `photo-upload.js` für die Anzeige keine Änderung.
- Abgelehnte Alternative (Umleitung auf eine neue URL): Auch sie braucht den geschützten Controller, dazu eine zweite URL und einen zusätzlichen Roundtrip, und alte Lesezeichen gehen über einen Umweg. Das bringt keinen Gewinn.
- Rest-Datei unter `wwwroot` (Migration übersprungen): Durch `UseRouting` (`Program.cs:443`) ist der Controller-Endpunkt schon gewählt. `StaticFileMiddleware` liefert nichts aus, wenn ein Endpunkt mit RequestDelegate gewählt ist. Dann greift der Filter. Das prüft der Test in AK 6.

**3. Login-Ausnahme nur für echte statische Dateien**
- Das Lambda `Program.cs:457-485` wird zu `Middleware/LoginRedirectMiddleware.cs`, nach dem Muster `WindowsAutoLoginMiddleware` (`Program.cs:454`, Tests in `IdealAkeWms.Tests/Middleware/WindowsAutoLoginMiddlewareTests.cs`). Die Ctor-Abhängigkeit ist `IWebHostEnvironment`. Die Präfixliste bleibt **unverändert** (`fallstricke.md`, „LoginRedirect zaehlt Account-Pfade einzeln auf“).
- `path.Contains(".")` wird zu `path.Contains('.') && env.WebRootFileProvider.GetFileInfo(context.Request.Path.Value!).Exists`. Dabei wird der Originalpfad verwendet und nicht der kleingeschriebene, weil `PhysicalFileProvider` unter Windows ohnehin nicht auf Groß/Klein achtet.
- In Development ist `WebRootFileProvider` ein Composite-Provider, der auch die Static Web Assets (`_content/...`, Scoped CSS) enthält. In der Produktion liegen sie physisch im publizierten `wwwroot`. Die Anmeldeseite (`Views/Account/Login.cshtml:13-14, 54, 98-100`) und das Layout (`_Layout.cshtml:10-13, 21, 329-331`) nutzen nur `/lib/`, `/css/`, `/js/` (Präfix-Ausnahme), `/images/ideal-ake-logo.svg` und `/favicon.ico` (echte Dateien). **Fingerprint-URLs** von `MapStaticAssets` (`Program.cs:490`, `.WithStaticAssets()` `:495`) existieren nicht physisch. Unter `/lib/`, `/css/` und `/js/` sind sie durch die Präfixe abgedeckt. Liefert die Seite eine andere Fingerprint-URL ohne diese Präfixe aus, ergänzt der Dev-Lauf die Ausnahme um `context.GetEndpoint()?.Metadata.GetMetadata<StaticAssetDescriptor>() != null` und belegt das. Ohne Beleg bleibt die Ausnahme weg (AK 8 prüft es).
- Wirkung auf andere Pfade mit Punkt (zum Beispiel `/Home/Index.x` oder Route-Werte mit Punkt): Ohne Anmeldung leiten sie jetzt auf die Anmeldung um. So ist es gewollt.

**4. Delete mit FA-Prüfung**
- Signatur: `Delete([FromForm] int productionOrderId, [FromForm] string fileName)`. Fehlt der FA, gibt es `NotFound()`. Liefert `TryResolve` falsch oder beginnt `!fileName.StartsWith($"{order.OrderNumber}_", StringComparison.Ordinal)`, gibt es `BadRequest()`, und **die Datei wird nicht angefasst**. Sonst wird wie bisher gelöscht, und die Antwort ist `Ok()`. Bei einer Datei, die es nicht gibt, bleibt es wie bisher bei `Ok()`.
- `photo-upload.js:120`: `data: { fileName: fileName, productionOrderId: productionOrderId }`. `productionOrderId` steht schon im Closure (`:2`).

## Migrations-/SQL-Auswirkungen
Keine. Es gibt keine Migration, kein `SQL/`-Skript, keine Änderung an `00_FreshInstall.sql` und keine an `SQL/AgentJobs`. Die Datei-Verschiebung beim Start ist keine DB-Migration.

## Einstellungen
keine

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen
Keine. Fotos sind Dateien ohne Entität. Kein `AuditableEntity`-Update.

## Rollen / Zugriff
Es gibt keine neue Rolle und keinen neuen Filter. Die Auslieferung nutzt den vorhandenen `[RequirePickingAccess]` (Admin, `picking`), also denselben Kreis wie die Foto-Karte heute. Deshalb muss weder `controller.md` noch `RoleOverview` geändert werden. Der Hilfetext `Views/Help/Index.cshtml:802` wird genauer: „Die Fotos sehen nur angemeldete Benutzer mit dem Recht Kommissionierung, die die Stückliste öffnen.“

## Akzeptanzkriterien
1. Ohne Anmeldung leitet `GET /Fotos/Kommissionierung/<vorhandene>.jpg` per 302 auf `/Account/Login` um. Es kommt kein Bild.
2. Ohne Anmeldung leitet `GET /Home/Index.x` per 302 auf `/Account/Login` um (Unit-Test der Middleware: Pfad mit Punkt, keine echte Datei).
3. Ohne Anmeldung liefern `/css/site.css`, `/images/ideal-ake-logo.svg` und `/favicon.ico` den Status 200 (Unit-Test: echte Datei im Provider, also kein Redirect). Die Anmeldeseite ist im privaten Fenster vollständig gestaltet.
4. Wer angemeldet ist, aber weder `picking` noch Admin hat, bekommt bei `GET /Fotos/Kommissionierung/<datei>.jpg` eine Umleitung auf AccessDenied und kein Bild.
5. Mit `picking` liefert dieselbe URL 200 `image/jpeg` aus `App_Data/Fotos/Kommissionierung`. Vorschau und Vollbild funktionieren in der Stückliste.
6. Nach dem ersten Start liegen alle Dateien aus `wwwroot/Fotos/Kommissionierung` in `App_Data/Fotos/Kommissionierung`, und der alte Ordner ist leer. Ein zweiter Start ändert nichts und loggt keine Verschiebung. Eine Rest-Datei unter `wwwroot` (künstlich angelegt) ist ohne Anmeldung **nicht** abrufbar.
7. Neue Uploads landen in `App_Data/Fotos/Kommissionierung`, nicht unter `wwwroot`.
8. Kein Static Asset, das bisher anonym erreichbar war und von der Anmeldeseite genutzt wird, bricht. Prüfung: Netzwerk-Tab auf der Anmeldeseite, nur Status 200 oder 304.
9. `POST /api/photos/delete` mit `productionOrderId` von FA A und einem Dateinamen von FA B liefert 400, und die Datei von FA B bleibt bestehen.
10. `Delete` und Auslieferung lehnen `..\..\appsettings.json`, `../x.jpg`, `a/b.jpg`, `x.png` und leere Namen mit 400 ab. Auf dem Dateisystem wird nichts gelesen oder gelöscht.
11. Löschen eines eigenen Fotos aus der Stückliste funktioniert (Datei weg, Kachel weg).
12. `dotnet build` und `dotnet test` sind grün, dazu neue Tests: `LoginRedirectMiddlewareTests`, `PhotoControllerTests` (Delete fremder FA, Delete eigener FA, Auslieferung ok/404/Traversal, unbekannter FA → 404) und `PhotoStorageTests` (Migration verschiebt, zweiter Lauf no-op, Kollision bleibt liegen). Die Recht-Prüfung (AK 4) prüft schon der vorhandene Filter. Ein Test reicht, der zeigt, dass die neue Action unter dem Class-Level-`[RequirePickingAccess]` liegt (Reflection auf das Attribut).

## Test-Szenarien
Neues **Kapitel 124 „Kommissionierfotos nur mit Anmeldung und Recht Kommissionierung (Hotfix v1.30.4)“** in `docs/TESTSZENARIEN.md`, danach Eintrag in `secondbrain/tests/testszenarien-index.md`.

**Wahl der Nummer:** main hat als letztes Kapitel 107 (`docs/TESTSZENARIEN.md:6367`, im Worktree geprüft). Das Bündel belegt 108–123 (Index-Zeilen 151–163; letztes TS-123 laut [[2026-11-08-nachbesserungen-vor-buendel-merge-goal-lauf]]). TS-124 und höher ist im Brain nirgends vergeben (Grep `TS-12[4-9]`, `Kapitel 12[4-9]`). Beim Vorwärts-Merge in das Bündel gibt es deshalb keine Nummernkollision. Es kann aber einen Textkonflikt geben, weil beide Seiten hinter Kapitel 107 anhängen. Diesen Konflikt löst der Mensch so: Kapitel 124 kommt hinter 123.

Skizze:
- 124.1 Privates Fenster, Foto-URL aufrufen → Anmeldeseite (Screenshot). Gegenprobe vor dem Update: dasselbe zeigte das Bild.
- 124.2 Privates Fenster, `/Home/Index.x` → Anmeldeseite. Die Anmeldeseite selbst ist korrekt gestaltet (Logo, CSS).
- 124.3 Anmelden mit `picking`, Stückliste mit Altfotos öffnen → Vorschau und Vollbild gehen. Die URL ist dieselbe wie vorher.
- 124.4 Anmelden ohne `picking` (zum Beispiel nur `fa_worklist`), Foto-URL direkt aufrufen → AccessDenied.
- 124.5 Foto hochladen → die Datei liegt unter `App_Data\Fotos\Kommissionierung`. Löschen → die Datei ist weg.
- 124.6 Negativ: Im DevTools-Fenster `POST /api/photos/delete` mit fremdem `productionOrderId`/`fileName` senden → 400, die Datei bleibt.
- 124.7 Neustart des App-Pools → das Log enthält keine Verschiebung mehr, und die Fotos sind unverändert.
- Auf IDEAL-DEV: Smoke 124.1 + 124.3.

## Etappen (nur bei epic: true)
entfällt (kein Epic)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy
**Hotfix AKE und IDEAL produktiv**. Nur Web, kein Service, keine Migration. **Von der QA am 2026-11-08 gegen den echten Diff `be0244c8..29a2bfe2` bestätigt:** Änderungen nur unter `IdealAkeWms/` (Web), `IdealAkeWms.Tests/`, `docs/`, `.gitignore`. Unter `IDEALAKEWMSService/` nur `AppVersion.cs` (1.30.4, Versionsgleichstand) – der Dienst muss dafür **nicht** neu publiziert werden. Keine Datei unter `*/Migrations/`, kein `SQL/`-Skript. Ein Publish-Ordner enthält kein `App_Data` (geprüft: `D:\Dev\QA\Web-v1304` nach `dotnet publish`), das Überkopieren überschreibt also keine Fotos.

- **Web-App:** ja
- **Service:** nein
- **Migration:** nein
- **Publish-Befehl (im Worktree):** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

Reihenfolge je Standort (erst DEV AKE und IDEAL, nach der Abnahme produktiv AKE und IDEAL):
1. **Vorab prüfen (unverifiziert, Mensch):** die App-Pool-Identität der Site (IIS-Manager → Site → Basic Settings → Application Pool, dann Identity). Optional die hiddenSegments: `%windir%\system32\inetsrv\appcmd list config "<Site>" -section:system.webServer/security/requestFiltering`. Die Ausgabe sollte `App_Data` unter `hiddenSegments` zeigen. Fehlt der Eintrag, ist das kein Blocker, denn ASP.NET Core liefert `App_Data` ohnehin nicht aus.
2. Den Ordner anlegen und das Recht vergeben, **bevor** die neue Version startet:
   `mkdir "<SiteRoot>\App_Data\Fotos\Kommissionierung"`
   `icacls "<SiteRoot>\App_Data" /grant "IIS AppPool\<PoolName>:(OI)(CI)M"`
   Die Identität kann je nach Pool abweichen, siehe Schritt 1. Fehlt das Recht, startet die App nicht (`Directory.CreateDirectory` beim Start) oder die Verschiebung loggt Fehler.
3. Den Inhalt von `<SiteRoot>\wwwroot\Fotos\Kommissionierung` zählen (Sicherung optional: Ordner kopieren).
4. Den App-Pool stoppen, den Publish wie gewohnt überkopieren (Ordner **nicht** vorher leeren) und den App-Pool starten.
5. Das Log prüfen: „Kommissionierfotos verschoben: n, uebersprungen: m“ muss zur Zählung aus Schritt 3 passen (n + m). Der alte Ordner ist danach leer.
   - **m > 0 (Ergänzung QA):** Es gibt eine Warnung „… nicht verschoben: im geschuetzten Ordner existiert bereits eine Datei gleichen Namens“ je Datei, und diese Warnung kommt **bei jedem Start wieder** (auf QA-Kestrel belegt, Start 2). Die Rest-Datei ist nicht abrufbar (die Route liefert immer die `App_Data`-Datei). Der Mensch vergleicht beide Dateien und entfernt die Rest-Datei aus `wwwroot\Fotos\Kommissionierung` von Hand.
   - Ein Lesefehler auf den **ganzen** alten Ordner (nicht je Datei) bricht den Start ab. Das ist unkritisch, weil der App-Pool dort bisher schreiben musste (Upload/Löschen bis v1.30.3), aber falls die Rechte inzwischen enger sind: dem Pool dort ebenfalls `M` geben (Verschieben = Löschen an der Quelle).
6. Im privaten Fenster TS-124.1 und angemeldet TS-124.3.
7. **Publish-Befehl** nur Web (im Worktree, oder aus `main` nach dem Merge):
   `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
   Ablauf des Menschen: Publish aus dem Worktree → Testsystem → Test → Merge. Nach dem Merge nur dann neu aus `main` publizieren, wenn der Merge die getesteten Dateien mit parallelen `main`-Änderungen zusammengeführt hat.

**Rückbau:** App-Pool stoppen, Fotos zurückverschieben mit `robocopy "<SiteRoot>\App_Data\Fotos\Kommissionierung" "<SiteRoot>\wwwroot\Fotos\Kommissionierung" /MOV`, die vorige Version (v1.30.3) überkopieren und den App-Pool starten. Ohne das Zurückverschieben zeigt v1.30.3 weder Alt- noch Neufotos, denn sie liest nur `wwwroot`. Mit dem Rückbau ist auch die Lücke wieder offen (bewusst, nur als Notfall). Der `robocopy /MOV` nimmt auch Fotos mit, die unter v1.30.4 hochgeladen wurden. Ordner `App_Data` und das vergebene Recht dürfen stehen bleiben.

**Danach (Mensch):** den Fix per Vorwärts-Merge main → Bündel-Worktree (`ideal-teile-1-5`) nachziehen. Im Bündel gilt seine eigene Version, 1.30.4 bleibt der main-Eintrag im Changelog. In `docs/TESTSZENARIEN.md` kommt Kapitel 124 hinter 123. Erst danach Lauf B (QM-Fotos) starten. Ob die QM-Fotos dieselbe Ablage nutzen sollen, gehört in deren Spec.

## Blocker (Grundsatzfragen)
keine

## Offene Rueckfragen
keine

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)
Freigabe per Auftrag (Gerald Weichbold, 2026-11-08). Keine offenen Fragen.

## Umsetzung (Dev 2026-11-08, v1.30.4)

Commit `29a2bfe2` im Worktree `2026-11-08-hotfix-kommissionierfotos` (Basis main `be0244c8`, enthält v1.30.3).
- `Services/PhotoStorage.cs` (GetDirectory, GetLegacyDirectory, TryResolve, MigrateLegacy), `Program.cs` legt den Ordner an und verschiebt den
  Altbestand beim Start; `Middleware/LoginRedirectMiddleware.cs` ersetzt das Lambda (Ausnahme: Punkt **und** echte Datei im
  `WebRootFileProvider`; Präfixliste unverändert); `PhotoController`: alle drei Pfade über `PhotoStorage`, `Delete(productionOrderId, fileName)`
  mit FA-Prüfung, neue Action `GetFile` unter `/Fotos/Kommissionierung/{fileName}`; `photo-upload.js` sendet `productionOrderId`.
- Abweichung: Die Migrationstests stehen in `PhotoControllerTests` statt in einer eigenen `PhotoStorageTests`-Datei (gleicher Umfang).
  `StaticAssetDescriptor`-Ausnahme nicht gebaut (AK 8 prüft die QA).
- `.gitignore` `IdealAkeWms/App_Data/`, Version 1.30.4, Anwender-Changelog, Hilfetext Fotos, Kapitel 124 (TS-124.1–124.7).
- Build 0 Fehler; Tests Dienst 202, Web 1130/1 übersprungen (19 neue).
- **DEV-Hinweis:** AKE_WMS_DEV hat das Bündel-Schema, ein main-Stand startet dort nicht (OSEON-QA Lauf 2, U1). QA-Umgebung daher wie dort:
  Kopie `AKE_WMS_QA_MAIN` (main-Schema) + Kestrel.

## QA v1.30.4 (2026-11-08)

qa-agent, unabhängig, in diesem Lauf. Prüfbereich `git diff be0244c8..29a2bfe2` (13 Dateien, +360/−51). Worktree `.claude/worktrees/2026-11-08-hotfix-kommissionierfotos`, erhöhte Shell.
**Urteil: Testbereit.** Kein offener Befund im Code. Eine Brain-Lücke (Testindex) hat die QA selbst geschlossen (B-1).

### 1. Build und Tests (im Worktree, selbst ausgeführt)
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 25 Warnungen (vorbestehend, z. B. `TrackingController.cs:261` CS8602; keine in geänderten Dateien).
- `dotnet test`:
  - `IdealAkeWms.Tests`: **1130 bestanden, 1 übersprungen, 0 fehlgeschlagen** (1131)
  - `IDEALAKEWMSService.Tests`: **202 bestanden, 0 übersprungen, 0 fehlgeschlagen**

### 2. Code-Review (selbst, synchron)
Geprüft: `Program.cs` (Pipeline 433–470), `Middleware/LoginRedirectMiddleware.cs`, `Services/PhotoStorage.cs`, `Controllers/Api/PhotoController.cs` (ganz), `Filters/RequirePickingAccessAttribute.cs`, `CurrentUserService.CanPickAsync` (`:91-92`), `photo-upload.js` (Delete + Einbindung `Bom.cshtml:637` mit `asp-append-version`), `Login.cshtml` (Layout null, Assets), `AccountController` (WindowsLogin/AccessDenied), neue Tests, Bündel-`Program.cs` (Vorwärts-Merge-Verträglichkeit: gleiche Lambda- und Pipeline-Struktur).
- **Middleware-Reihenfolge:** `UseRouting` (`Program.cs:446`) → Auth → Session → `WindowsAutoLoginMiddleware` → `LoginRedirectMiddleware` (`:460`) → `UseStaticFiles` (`:463`) → `MapStaticAssets`/Controller. Der Endpoint ist vor `UseStaticFiles` gewählt; `StaticFileMiddleware` überspringt Anfragen mit gewähltem Endpoint. **Real belegt** auf Kestrel: eine Rest-Datei gleichen Namens unter `wwwroot` (14 568 Byte) wurde nie ausgeliefert; angemeldet kam die `App_Data`-Datei (9 384 Byte, `cmp` gleich), anonym AccessDenied.
- **Absolute Route** `[HttpGet("/Fotos/Kommissionierung/{fileName}")]` unter `[Route("api/photos")]`: der Präfix gilt nicht, die URL bleibt gleich (real belegt).
- **Filter greift auf `GetFile`:** Class-Level `[RequirePickingAccess]` (`PhotoController.cs:10`), kein `[AllowAnonymous]`. Real: ohne `picking` → 302 `/Account/AccessDenied`; Test `GetFile_LiegtUnterClassLevelKommissionierRecht_AK4`.
- **`TryResolve`** (`PhotoStorage.cs:18-29`): leer, `..`, `/`, `\`, `GetFileName != name`, Endung ≠ `.jpg`, `GetFullPath` außerhalb des Ordners → abgelehnt. Eine Regel für Delete und Auslieferung (kein Drift). NTFS-Streams (`x:y.jpg`) bleiben im Foto-Ordner – kein Befund.
- **Login-Ausnahme** `path.Contains('.') && WebRootFileProvider.GetFileInfo(original).Exists` (`LoginRedirectMiddleware.cs:28`): `PhysicalFileProvider` lehnt `..` und Punkt-Dateien selbst ab; Verzeichnisse liefern `Exists=false`. Präfixliste unverändert.
- **Delete** (`PhotoController.cs:80-95`): FA zuerst (404), dann `TryResolve` + `StartsWith($"{OrderNumber}_", Ordinal)` → 400 ohne Dateizugriff. Fehlt `productionOrderId` (alter, gecachter JS-Stand), bindet `0` → 404, nichts gelöscht; wegen `asp-append-version` lädt der Browser die neue JS ohnehin (`?v=bGYblv…` geprüft, ausgelieferte Datei enthält `productionOrderId`).
- Defektklassen der Checkliste: kein `<script>`-Einbetten, kein `hidden`+`d-*`, kein stiller Fallback (fehlender FA = 404), keine parallelen Arrays, Serverprüfung statt Browserprüfung, eine Regel an einem Ort (`TryResolve`).
- **Befunde:**
  - **B-1 (Brain, behoben durch QA):** TS-124 fehlte in `secondbrain/tests/testszenarien-index.md` (Grep `TS-124|Kapitel 124` = 0 Treffer). QA hat die Zeile im Hauptcheckout ergänzt (nach Kapitel 107).
  - Keine Code-Befunde.
- **Beobachtungen (kein Befund):**
  - O-1: Fingerprint-URLs von `MapStaticAssets` **außerhalb** von `/lib/`, `/css/`, `/js/` (`/IdealAkeWms.kthmzlho01.styles.css`, `/favicon.a4vxokspod.ico`) leiten ohne Anmeldung jetzt auf die Anmeldung um (vorher 200). Keine anonym erreichbare Seite nutzt sie (Anmeldeseite: Netzwerk nur 200, siehe 6.). Die `StaticAssetDescriptor`-Ausnahme bleibt deshalb zu Recht weg.
  - O-2: `a%2Fb.jpg` über HTTP ergibt 404, nicht 400: Kestrel dekodiert `%2F` im Pfad nicht, der Routenwert ist der Literalname (ohne `/`), die Datei gibt es nicht. Kein Lesen außerhalb des Ordners. Der Unit-Test prüft `a/b.jpg` → 400.
  - O-3: Bei Namenskollision kommt die Warnung bei jedem Start erneut (spec-gemäß „Warnung je Datei“). Im Deploy-Abschnitt ergänzt.
  - Z1/Z2/Z3 der Spec (Upload ohne Typ-/Größenprüfung, kein CSRF-Schutz, OrderNumber-Präfix) bleiben unverändert offen.

### 3. Nachweisart je AK
| AK | Nachweis | Ergebnis |
|---|---|---|
| 1 Foto anonym → Login | Unit (`LoginRedirectMiddlewareTests`) + **real** Kestrel (curl 302 + Screenshot 03) | OK |
| 2 `/Home/Index.x` anonym → Login | Unit + real (302, Screenshot 04) | OK |
| 3 css/Logo/favicon 200, Anmeldeseite gestaltet | Unit + real (curl 200 je Datei, Screenshots 03/04) | OK |
| 4 ohne `picking` → AccessDenied | Reflection-Test + **real** (`QA-OhnePicking`, nur `stock_read`: 302 `/Account/AccessDenied`) | OK |
| 5 mit `picking` 200 `image/jpeg` aus App_Data, Vorschau/Vollbild | Unit + real (`QA-Picker`: 200 image/jpeg 9 384 B; Browser: Kacheln + Vollbild, Screenshots 06/07) | OK |
| 6 Migration, zweiter Start, Rest-Datei | Unit + real (Start 1: verschoben 2, übersprungen 1; Start 3 ohne Kollision: 0 Log-Zeilen; Rest-Datei anonym → AccessDenied, Screenshot 05) | OK |
| 7 Upload in App_Data | real (curl-Upload als `QA-Picker` → `App_Data\…\2638001_20261108093303_2.jpg`, `wwwroot` unverändert) | OK |
| 8 Anmeldeseite Netzwerk nur 200/304 | real (Chrome-Netzwerk: bootstrap.css, site.css?v=, Logo, jquery, validate, unobtrusive = 200; Kestrel-Log 304 bei Wiederholung) | OK |
| 9 Delete fremder FA → 400, Datei bleibt | Unit + real (3046 + Datei von 2627001 → 400, Datei da) | OK |
| 10 Pfadprüfung 400 | Unit (5 Fälle Delete + GetFile) + real (Delete alle 5 → 400; GET `..\..\appsettings.json`, `../x.jpg`, `x.png`, Leerzeichen → 400; siehe O-2) | OK |
| 11 eigenes Foto löschen | Unit + real serverseitig (Delete → 200, Datei weg, GET 404, Kachel nach Neuladen weg). **Klick auf das ×** löst `confirm()` aus → nicht ausgelöst | Server OK, Klick **Manual-UAT** |
| 12 Build/Tests + neue Tests | selbst ausgeführt (siehe 1.; `PhotoStorageTests` liegen in `PhotoControllerTests`, dokumentierte Abweichung) | OK |

Die echte Pipeline (Middleware + Routing + StaticFiles) belegt kein Unit-Test; sie ist hier auf Kestrel belegt. IIS (`App_Data`-Rechte, hiddenSegments) bleibt Manual-UAT.

### 4. Geänderte bestehende Tests
Keine. Der Zweig fügt nur `IdealAkeWms.Tests/Controllers/PhotoControllerTests.cs` und `IdealAkeWms.Tests/Middleware/LoginRedirectMiddlewareTests.cs` hinzu.

### 5. Checkliste CLAUDE.md
- Migration/SQL/FreshInstall/AgentJobs: entfällt (kein Schema). Audit-Felder: entfällt (keine Entität).
- Version 1.30.4 in beiden `AppVersion.cs` ✓, Anwender-Changelog ✓, Brain-Changelog [[2026-11-08-v1-30-4-hotfix-kommissionierfotos]] ✓, Hilfetext ✓, `docs/TESTSZENARIEN.md` Kapitel 124 ✓, Testindex ✓ (nach B-1), feature-map ✓ (Status von QA auf Testbereit gesetzt), `.gitignore` `IdealAkeWms/App_Data/` ✓.
- 5a ADR 0020: keine neue oder geänderte Einstellung.

### 6. DEV-Lauf
- **Standort:** `beide` (Frontmatter).
- **`dev-deploy.ps1` nicht ausgeführt (Auftrag):** `AKE_WMS_DEV` hat das Bündel-Schema, ein main-Stand startet dort nicht (U1, OSEON-QA Lauf 2), und der Auftrag verbietet Änderungen an `AKE_WMS_DEV` und der Bündel-DEV-Site. Ersatz wie im OSEON-Lauf:
  - DB `AKE_WMS_QA_MAIN` (main-Schema, letzte Migration `20260806120650_AllowMultipleDummyRequisitionItems`, DEV-Absicherung aus dem OSEON-Lauf).
  - Publish des Worktrees sauber in den neuen Ordner **`D:\Dev\QA\Web-v1304`**, `appsettings.DEV.json` aus `D:\Dev\QA\Web` (alle `Sync:*Enabled=false`, `ErrorNotification:Enabled=false`, `SData:BaseUrl` leer).
  - Kestrel `http://localhost:5183`, `ASPNETCORE_ENVIRONMENT=DEV`. Data-Protection-Keys zuerst `D:\Dev\QA\keys`; weil Chrome die `localhost`-Cookies der DEV-Site (8081) mitschickt und der Login-POST dann 400 lieferte, ab Start 2 nur lesend `D:\Dev\AKE\keys` (Schlüssel gültig bis 2027-01-27, keine neue Datei, Zeitstempel unverändert).
  - **Fußzeile `© 2026 IDEAL-AKE WMS v1.30.4`** = AppVersion (Screenshot 06).
  - Testbenutzer nur in `AKE_WMS_QA_MAIN`, ohne Passwort: `QA-Picker` (Id 24, nur `picking`), `QA-OhnePicking` (Id 25, nur `stock_read`); vorhanden `QA-Agent` (admin).
  - Selbst erzeugte Test-JPEGs **vor dem ersten Start** unter `wwwroot\Fotos\Kommissionierung`: `2638001_…_1.jpg` (FA 3046), `2627001_…_1.jpg` (FA 3045), `2625004_…_1.jpg` (FA 3044) und eine Kollisionsdatei gleichen Namens schon in `App_Data`.
- **Vorher-Beweis (Lücke):**
  - AKE-DEV `localhost:8081` (Bündel v1.94.0, ohne Fix), **anonymes** Chrome-Profil (headless, frisches `--user-data-dir`): `/Fotos/Kommissionierung/REPRO-TEST_20261108000000_1.jpg` → Chrome-Bildbetrachter statt Anmeldeseite; curl `200 image/jpeg`, `Content-Length: 14`, `Server: Microsoft-IIS/10.0`. Die Repro-Datei hat nur 14 Byte und ist kein echtes JPEG, daher zeigt der Screenshot das Symbol „Bild defekt“. Austausch gegen ein echtes JPEG hat das Berechtigungssystem verweigert; DEV-Site unverändert. → `D:\Dev\QA\Nachweis-v1304\01-vorher-akedev-8081-foto-ohne-anmeldung.png`
  - Vorher-Beweis mit echtem Bild: alter QA-Publish `D:\Dev\QA\Web` (main `e386bfce`, Fotocode gleich `be0244c8`) auf Kestrel `:5182`, anonym → Bild sichtbar. → `D:\Dev\QA\Nachweis-v1304\02-vorher-kestrel-v1302-foto-ohne-anmeldung.png`

| Schritt | Ausgeführt | Ergebnis | Bewertung |
|---|---|---|---|
| TS-124.1 Foto ohne Anmeldung | ja (headless, frisches Profil; curl) | 302 `/Account/Login?returnUrl=%2FFotos%2F…`, Anmeldeseite (03); vorher Bild (02) | OK |
| TS-124.2 `/Home/Index.x` + Gestaltung + Netzwerk | ja | 302 Login, Seite mit Logo/CSS (04); Netzwerk nur 200, Wiederholung 304; `/css/site.css`, `/images/ideal-ake-logo.svg`, `/favicon.ico`, `/lib/…` anonym 200 | OK |
| TS-124.3 mit `picking`, Altfotos, Vorschau + Vollbild | ja (Chrome, `QA-Picker`, `/Picking/Bom/3046`) | Kacheln der verschobenen Altdatei + Upload sichtbar (06), Klick öffnet Vollbild unter derselben URL (07) | OK |
| TS-124.4 ohne `picking` | ja (curl, `QA-OhnePicking`) | Foto-URL und `POST /api/photos/delete` → 302 AccessDenied, kein Bild | OK |
| TS-124.5 Upload / Löschen | ja (curl als `QA-Picker`; Kachel im Browser) | Upload → `App_Data`; Delete eigener FA → 200, Datei weg, GET 404, Kachel nach Neuladen weg. × + `confirm()` nicht geklickt (Dialogverbot) | OK (Server) / Klick NICHT MÖGLICH |
| TS-124.6 fremder FA | ja (curl) | fremde Datei → 400, Datei bleibt; Traversal/`x.png`/leer → 400; unbekannter FA → 404; anonym → AccessDenied | OK |
| TS-124.7 Migration + Neustart + Rest-Datei | ja (drei Starts) | Start 1: `verschoben: 2, uebersprungen: 1` + 1 Warnung; Start 2: nur Kollisionswarnung, Dateien unverändert; Rest-Datei anonym → AccessDenied (05), angemeldet → `App_Data`-Version; Start 3 nach Entfernen der Rest-Datei: **0** Log-Zeilen, alter Ordner leer | OK |
| Zusatz | ja | anonym `/appsettings.json`, `/App_Data/…jpg`, `/Fotos/appsettings.json` → 302 Login | OK |
| IDEAL-DEV Smoke 124.1 + 124.3 | nein | IDEAL-DEV = Bündel-Stand, Auftrag: Bündel-DEV nicht verändern | NICHT MÖGLICH (Mensch) |
| IIS (App-Pool-Recht `App_Data`, hiddenSegments, Recycle) | nein | nur Kestrel | NICHT MÖGLICH (Mensch) |

Nachweise: `D:\Dev\QA\Nachweis-v1304\` (Screenshots `01`–`07`, `start1.log`–`start3.log`).
Alle selbst gestarteten Prozesse beendet (Kestrel `:5182` PID 45948, `:5183` PIDs 40932/31528/52236; headless Chrome nur mit Warten auf Ende). Ports 5182/5183 frei, kein `dotnet IdealAkeWms.dll`, kein Chrome mit QA-Profil, QA-Tabs geschlossen.
Zurückgelassen (Mensch entscheidet): `D:\Dev\QA\Web-v1304\`, Testfoto `D:\Dev\QA\Web\wwwroot\Fotos\Kommissionierung\2638001_20261101000000_1.jpg`, Benutzer 24/25 in `AKE_WMS_QA_MAIN`, Repro-Datei `D:\Dev\AKE\Web\wwwroot\Fotos\Kommissionierung\REPRO-TEST_20261108000000_1.jpg` (nicht gelöscht; darf weg).

### Manuelle Testcheckliste (offen für den Menschen)
1. **Testsystem AKE** (IIS): Deploy-Schritte 1–5; App-Pool-Identität + `icacls` auf `App_Data`; Log-Zählung = Ordner-Zählung.
2. TS-124.1 im privaten Fenster (vorher die URL eines echten Altfotos notieren).
3. TS-124.3 mit einem echten Kommissionierer (`picking`, kein Admin): Altfotos, Vorschau, Vollbild.
4. TS-124.5 in der Stückliste: Foto aufnehmen/hochladen, dann über das **×** löschen (Dialog bestätigen) → Kachel und Datei weg.
5. TS-124.4 mit einem Benutzer ohne Kommissionierung (Windows-Anmeldung) → „Zugriff verweigert“.
6. TS-124.7: App-Pool recyceln → keine neue Zeile „Kommissionierfotos verschoben“.
7. TS-124.2: Anmeldeseite im privaten Fenster unter IIS, DevTools-Netzwerk nur 200/304.
8. **Testsystem IDEAL:** Schritte 1, 2, 3 (Smoke 124.1 + 124.3).
9. Nach Abnahme: Merge, produktiv AKE und IDEAL nach Deploy-Abschnitt, danach Vorwärts-Merge main → Bündel (`ideal-teile-1-5`, Kapitel 124 hinter 123) **vor** Lauf B (QM-Fotos).
