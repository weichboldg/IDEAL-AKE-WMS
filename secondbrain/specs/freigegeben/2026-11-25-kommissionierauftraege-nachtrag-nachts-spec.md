---
type: spec
title: Kommissionieraufträge – Nachtrag nachts im Dienst, Knopf „Fehlende … erzeugen“ nur für Admins
slug: 2026-11-25-kommissionierauftraege-nachtrag-nachts-spec
status: Freigegeben
created: 2026-11-25
updated: 2026-11-25
source_backlog: "[[2026-11-25-kommissionierauftraege-nachtrag-automatisch-knopf-nur-admin]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - "IDEALAKEWMSService/Services/KommissionierNachtragService.cs (neu: Lauf, Fälligkeit, Zählung, Aktivitäts-Protokoll)"
  - "IDEALAKEWMSService/Common/DienstBenutzer.cs (neu: ICurrentUserService für den Dienst, fester Name)"
  - "IDEALAKEWMSService/Workers/SyncWorker.cs (neuer RunResilientAsync-Schritt nach :543)"
  - "IDEALAKEWMSService/Program.cs (Registrierung Workflow + BOM-Graph über gemeinsame Methode, DienstBenutzer, neuer Service)"
  - "IdealAkeWms/Services/PickingOrderRegistrierung.cs (neu: eine Registrierungsmethode für BOM-Weiche + Kommissionierauftrag-Repos + PickingOrderWorkflow, genutzt von Web- und Dienst-Program.cs)"
  - "IdealAkeWms/Program.cs (:89-115, :130-132, :195 auf die gemeinsame Methode umstellen)"
  - "IdealAkeWms/Services/PickingOrderWorkflow.cs (Zeilenzähler und Überdeckung in PickingOrderErzeugung, Soll-Aufbau :79-87 als statische Methode SollListe, Marker StuecklisteKleiner schreiben, Fehlertext :143-144)"
  - "IdealAkeWms/Services/PickingOrderRestmengen.cs (neu: record Ueberdeckung + Funktion Ueberdeckt, gleiche Abdeckungs-Regel Abgedeckt :19-27)"
  - "IdealAkeWms/Models/ProductionOrderPickingStatus.cs (neu: string? StuecklisteKleiner)"
  - "IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs + ProductionOrderPickingStatusRepository.cs (neu: SetStuecklisteKleinerAsync nach Muster SetPickingStatusTextAsync :123-135)"
  - "IdealAkeWms/Migrations/<ts>_AddPickingStatusStuecklisteKleiner.cs (neu) + SQL/168_AddPickingStatusStuecklisteKleiner.sql (neu, Nummer prüft der Dev-Lauf) + SQL/00_FreshInstall.sql (:447 Spalte, History-Block nach :3554)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs (neuer Name in All + DisplayNames)"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs (2 Keys: Basis, Meta, Int-Bereich)"
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs (AuftraegeNachziehen :532-548 [RequireAdminAccess]; AltbestandGeprueft :550-567 legt sofort an; MapItem :125-163 übernimmt StuecklisteKleiner; Index setzt IsAdmin)"
  - "IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs (IsAdmin; PickingLeitstandItem :60 + StuecklisteKleiner)"
  - "IdealAkeWms/Views/PickingLeitstand/Index.cshtml (:86-99 beide Formulare nur für Admins)"
  - "IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml (:185-197 Badge „Stückliste kleiner“)"
  - "IdealAkeWms/Controllers/PickingController.cs (BomSeiteAsync :260-420 Überdeckung live) + Models/ViewModels/BomViewModels.cs (:157 neue Liste) + Views/Picking/Bom.cshtml (Warnung vor :84)"
  - "IdealAkeWms/Views/Help/Index.cshtml (:1771-1773), Views/Picking/Bom.cshtml (:66), Views/Picking/Index.cshtml (:55-56), Views/Help/Changelog.cshtml"
  - "docs/TESTSZENARIEN.md (TS-191 neu, Hinweis an TS-120.2/120.6/120.13), docs/anwender/01-kommissionierung.md (:93-95, :262), docs/anwender/02-kommissionierbereiche.md (:100, :118), docs/betrieb/07-troubleshooting.md (:88-89), docs/betrieb/08-diagnose-abfragen.md (:413)"
  - "IDEALAKEWMSService.Tests (neu: KommissionierNachtragServiceTests inkl. DI-Test), IdealAkeWms.Tests (Leitstand-Filter, BomDiResolutionTests auf die gemeinsame Methode, PickingOrderRestmengen.Ueberdeckt, Workflow-Marker, AltbestandGeprueft legt an)"
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: "1 Rolle admin; 2 (b) + Warnung in Stückliste/Leitstand; 3 sofort anlegen (Gerald 2026-11-25); Umsetzung in einem späteren goal-run"
freigabe_von: Gerald
freigabe_am: 2026-11-25
---

# Kommissionieraufträge – Nachtrag nachts im Dienst, Knopf nur für Admins

Bezug: [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]] (v1.88.0 E2, v1.89.0 Q11),
[[0021-kommissionierauftrag-als-beleg-neben-der-stueckliste]], [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]],
[[0008-servicesettings-db-first-mit-typisiertem-katalog]], [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]],
[[0003-auditableentity-als-entity-basis]], [[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]].

Code-Stand für alle Fundstellen: Bündel-Worktree `feature/2026-08-07-ideal-teile-1-5`, Version 1.99.69
(`IdealAkeWms/AppVersion.cs:5`).

## Ziel / Nutzen (das Warum)

Neue Freigaben legen ihre Kommissionieraufträge schon heute selbst an (Einzelfreigabe `PickingLeitstandController.cs:450-451`,
Sammelfreigabe `:512-518`, ohne Leitstand beim Öffnen der Stückliste `PickingController.cs:239-247`). Nicht automatisch kommen
Stücklisten-Nachträge **nach** der Freigabe an: neue Positionen oder höhere Mengen in Sage bzw. in der FA-Struktur. Dafür gibt es nur den
Leitstand-Knopf. Nach dem AKE-Prod-Update hat der Knopf einmal 39 Aufträge / 6 180 Zeilen angelegt; danach meldet er bei jedem Klick
„0 … angelegt; 43 FA ohne offene Restmenge“ und verwirrt.

Entscheidung Gerald 2026-11-25: Der Nachtrag läuft **nachts im Windows-Dienst** für alle freigegebenen, offenen HauptFAs, mit derselben
idempotenten Logik `PickingOrderWorkflow.ErzeugeOffeneAsync` (`IdealAkeWms/Services/PickingOrderWorkflow.cs:56`). Der Knopf bleibt als
Notfall-Werkzeug, aber **nur für Admins** sichtbar und serverseitig geschützt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neuer Dienst-Lauf „Kommissionier-Nachtrag“ im `SyncWorker`: einmal täglich ab einer einstellbaren Stunde, über alle FAs aus
  `GetReleasedForPickingAsync` (`ProductionOrderPickingStatusRepository.cs:189-200`: HauptFA, freigegeben, nicht erledigt, nicht storniert,
  nicht `IsDonePicking`), je FA `ErzeugeOffeneAsync`.
- Aktivitäts-Protokoll mit deutschen Zähl-Schlüsseln und Warnzeilen je FA.
- Zwei Service-Einstellungen (Schalter, Uhrzeit), Katalog, Startup-Seed, Drift-Guard.
- `DienstBenutzer` als `ICurrentUserService` im Dienst: `CreatedBy` und `CreatedByWindows` = Lauf-Name.
- Eine gemeinsame Registrierungsmethode für BOM-Weiche, Kommissionierauftrag-Repositories und `PickingOrderWorkflow`, genutzt von Web und
  Dienst.
- Leitstand: beide Formulare zu `AuftraegeNachziehen` (Leiste „markierte“ `Index.cshtml:86-93` und „alle“ `:95-99`) nur für Admins sichtbar;
  Action serverseitig nur für Admins. Beide Knöpfe hängen an derselben Action, deshalb gilt die Admin-Sperre für beide (Geralds Verweis
  `Index.cshtml:91/98`).
- Texte, die auf den Knopf verweisen: Hilfe, Stückliste, Auftragsliste, Fehlertext im Workflow, Meldung nach „Altbestand geprüft“, Anwender- und
  Betriebsdoku, Testszenarien.
- „Altbestand geprüft“ legt danach sofort die fehlenden Aufträge dieses FA an (Antwort 3).
- Warnung „Stückliste kleiner als offene Kommissionierzeilen“ (Antwort 2): im Nachtlauf-Protokoll, in der Stückliste (live) und je FA-Zeile im
  Leitstand (gespeicherter Marker, eine neue Spalte).

**Out-of-Scope**
- Offene Zeilen bei kleiner gewordener Stückliste kürzen oder entfallen lassen (Antwort 2: nichts kürzen). Die Leitstand-Leitung entscheidet
  selbst.
- FAs mit `IsDonePicking = true` (alle Aufträge erledigt): Eine spätere Stücklisten-Erweiterung bekommt keinen Auftrag mehr. Das ist schon
  heute so (der Knopf nimmt dieselbe Liste, `PickingLeitstandController.cs:538`) und bleibt so.
- Modus ohne Leitstand (`LeitstandAktiv` aus): dort entstehen Aufträge weiter beim Öffnen der Stückliste (`PickingController.cs:241-247`).
  Der Nachtlauf nimmt nur FAs mit `IsReleasedForPicking`; an diesem Weg ändert sich nichts.
- Ein Web-Endpunkt, den der Dienst ruft, und eine zweite Implementierung der Restmengen-Logik im Dienst (siehe Lösungsentwurf, verworfen).
- Mehrere Läufe pro Tag.

## Fachliche Anforderungen

F1 – **Nachtlauf.** Einmal pro Tag ab der eingestellten Stunde (Default 2 Uhr) ruft der Dienst für jeden freigegebenen, offenen HauptFA
`ErzeugeOffeneAsync` auf. Für FAs ohne neue Restmenge passiert nichts (idempotent, `PickingOrderRestmengen.cs:30-41`).

F2 – **Fälligkeit und Nachholen.** Der Lauf ist fällig, wenn der jüngste Termin (heute oder gestern zur eingestellten Stunde) nach dem letzten
Start liegt. Merker ist die jüngste Startmeldung „Run gestartet“ dieses Laufs im Aktivitäts-Protokoll (`SyncLogger.cs:21`). Ein verpasster
Termin (Dienst aus, Server neu gestartet) wird nach dem Dienststart **einmal** nachgeholt. Das gilt auch für den ersten Dienststart nach dem
Deploy: Der erste Lauf kommt dann sofort und nicht erst in der Nacht. Dieselbe Regel hat `LetzteBestellungPlaner.Faellig`
(`IDEALAKEWMSService/Services/Sdata/LetzteBestellungSyncService.cs:18-26`), sie wird wiederverwendet.

F3 – **Schalter.** `Sync:KommissionierNachtragEnabled` (Default an) schaltet den Lauf ab, ohne neu zu deployen. Im DryRun des Dienstes
(`WorkerSettings:SyncDryRun`, `SyncWorker.cs:33`) läuft der Nachtrag nicht. `ErzeugeOffeneAsync` hat keinen Trockenmodus. Der Dienst
schreibt dazu eine Log-Zeile „Kommissionier-Nachtrag im DryRun übersprungen“ und setzt keinen Merker.

F4 – **Audit.** Aufträge und Zeilen aus dem Nachtlauf tragen `CreatedBy = CreatedByWindows = "KommissionierNachtrag"` (ADR 0003: Dienste
schreiben ihren Namen; Muster `LetzteBestellungSyncService.cs:40,173,195`).

F5 – **Aktivitäts-Protokoll** (Dienstname `KommissionierNachtrag`, Anzeige „Kommissionier-Nachtrag“). Zähl-Schlüssel:
`fa_geprueft`, `fa_mit_nachtrag`, `auftraege_angelegt`, `zeilen_angelegt`, `ohne_rest`, `altbestand_pruefen`, `ohne_stueckliste`, `fehler`,
`stueckliste_kleiner`. Zeilen:
- Info je FA mit Nachtrag: „FA <Nr>: <KO-Nummern> (<n> Zeilen)“.
- Warnung je FA mit Überdeckung (F8): „FA <Nr>: Stückliste kleiner als offene Kommissionierzeilen – <Artikel> Pos <P>: Stückliste <x>,
  beauftragt <y>; …“.
- Warnung je FA mit „Altbestand prüfen“, ohne Stückliste oder mit Bereichs-Meldung (`PickingOrderErzeugung.Meldungen`, Workflow `:67`,
  `:75`, `:112`).
- Warnung je FA mit `Fehler`. Das sind fehlender Standardbereich (`:93`) und Nummernkreis nach 3 Versuchen (`:143`). Wirft der
  Workflow selbst eine Ausnahme, etwa weil die Stückliste nicht lesbar ist (Sage-View oder OSEON bei Cache-Fehltreffer,
  `BomRepository.cs:27-57`), zählt das als `fehler` mit Ausnahmetext. Der Lauf geht mit dem nächsten FA weiter.
- Schlägt der Lauf als Ganzes fehl (FA-Liste nicht lesbar), endet er mit `FinishFailedAsync` und wirft weiter. `RunResilientAsync` meldet das
  über die Fehlermail (`SyncWorker.cs:615-627`).

F6 – **Knopf nur für Admins.** Beide Formulare zu `AuftraegeNachziehen` sehen nur Admins. Ein POST eines Nicht-Admins endet auf
`Account/AccessDenied` wie bei jedem `[RequireAdminAccess]` (`Filters/RequireAdminAccessAttribute.cs:23-27`). Das Verhalten für Admins
(markierte oder alle, Prüfung „nur freigegebene“, Sammelmeldung) bleibt wie in `PickingLeitstandController.cs:535-548`.

F7 – **„Altbestand geprüft“** (Antwort 3): Die Action legt danach sofort die fehlenden Aufträge **dieses FA** an (ein Aufruf
`ErzeugeOffeneAsync(id)`, Meldung wie bei der Einzelfreigabe über `AuftraegeMeldenAsync`, `PickingLeitstandController.cs:451`). Voraussetzung wie
beim Knopf: Der FA ist freigegeben, nicht erledigt und nicht storniert. Sonst bleibt es beim Löschen des Kennzeichens, und die Meldung sagt, dass
der FA nicht freigegeben ist. Die Action bleibt `[RequireLeitstandAccess]` (`:554`). Der heutige Verweis auf den Knopf (`:561-562`) entfällt.

F8 – **Kleiner gewordene Stückliste** (Antwort 2: nichts kürzen, melden und anzeigen). Heutiges Verhalten unten belegt.
- **Regel (eine Stelle):** Ein Stücklistenschlüssel (Ressource + Position, `PickingOrderRestmengen.Schluessel`) ist *überdeckt*, wenn
  1. es zu ihm mindestens eine **offene Kommissionierzeile** gibt (Status `Offen` oder `Gepickt`, also noch nicht umgebucht, in einem nicht
     stornierten Auftrag), und
  2. die beauftragte Menge (Summe `Abgedeckt` über alle Zeilen nicht stornierter Aufträge, `PickingOrderRestmengen.cs:19-27`) größer ist als das
     aktuelle Soll. Eine entfallene oder bei IDEAL nicht mehr kommissionier-relevante Position hat Soll 0.

  Bedingung 1 hält abgeschlossene Schlüssel heraus: Wurde mehr umgebucht als verlangt, ist nichts mehr zu tun, und eine Warnung wäre nur Lärm.
- **Nachtlauf:** zählt `stueckliste_kleiner` und schreibt je FA eine Warnung (F5). Offene Zeilen bleiben unverändert.
- **Leitstand:** In der FA-Zelle steht das Badge „⚠ Stückliste kleiner“. Der Tooltip nennt Artikel, Position, Stückliste und beauftragt. Sichtbar
  für alle, die den Leitstand sehen, nicht nur für Admins. Quelle ist der gespeicherte Marker `ProductionOrderPickingStatus.StuecklisteKleiner`;
  der Leitstand lädt keine Stückliste.
  - Den Marker setzt oder löscht jeder Aufruf von `ErzeugeOffeneAsync`: Nachtlauf, Freigabe, Admin-Knopf, „Altbestand geprüft“ und das Öffnen
    der Stückliste ohne Leitstand.
  - Nach einem Abschluss oder Storno verschwindet er spätestens mit dem nächsten Nachtlauf.
- **Stückliste (`Picking/Bom`, FA- und Auftragsmodus):** Ein Hinweis über der Filterkarte mit dem Text „⚠ Stückliste kleiner als offene
  Kommissionierzeilen“ und der Liste der Schlüssel. Er wird live berechnet, denn die Seite lädt die Stückliste ohnehin. So ist er nach einer
  Stücklistenänderung sofort aktuell, auch wenn der Leitstand-Marker noch den Stand der letzten Erzeugung zeigt.
- Auch die Sammelmeldung nach Freigabe, Knopf und „Altbestand geprüft“ nennt die Überdeckung. Dafür kommt eine Zeile in
  `PickingOrderErzeugung.Meldungen`, die `Sammelmeldung` (`PickingOrderWorkflow.cs:322-333`) als Warnung ausgibt.

F9 – **Texte.** Alle Verweise auf den Knopf nennen künftig den Nachtlauf, der Knopf nur noch als Admin-Notfall:
- `Views/Help/Index.cshtml:1771-1773`. Der Backlog nennt `:1742`, im Worktree steht der Text jetzt bei `:1773`.
- `Views/Picking/Bom.cshtml:66`, `Views/Picking/Index.cshtml:55-56`.
- `PickingOrderWorkflow.cs:143-144`, neuer Text: „… – der nächtliche Lauf versucht es erneut; sofort: Admin im Leitstand „Fehlende
  Kommissionieraufträge erzeugen“.“
- `PickingLeitstandController.cs:561-562`: Der Verweis auf den Knopf entfällt, die Action legt sofort an (F7).

### Heutiges Verhalten bei kleiner gewordener Stückliste (Beleg für Rückfrage 2, beantwortet: F8)

- `PickingOrderRestmengen.Berechne` geht nur über die **Soll**-Positionen der aktuellen Stückliste und gibt nur Reste `> 0` zurück
  (`PickingOrderRestmengen.cs:36-41`). Eine gesenkte Menge oder eine entfernte Position erzeugt also nichts und ändert nichts.
- `SollMenge` wird nur beim Anlegen geschrieben (`PickingOrderWorkflow.cs:130` Erzeugen, `:214` Folgeauftrag). Weitere Schreiber gibt es
  nicht: Die Suche nach `SollMenge =` im ganzen Worktree (ohne Tests) trifft nur diese zwei Stellen und die Migration. Die Suche nach
  `UPDATE … PickingOrderLines` / `SetProperty(…SollMenge` trifft nichts.
- Ergebnis: Offene Zeilen behalten ihr altes Soll. Ein entfernter Artikel bleibt im offenen Auftrag, und der Kommissionierer pickt mehr als
  die Stückliste verlangt. Darüber informiert heute niemand.

## Technischer Loesungsentwurf

### Wie die Erzeugung in den Dienst kommt (Optionen)

Befund:
- Der Dienst referenziert das Web-Projekt (`IDEALAKEWMSService/IDEALAKEWMSService.csproj:33-35`).
- Er nutzt bereits Web-Klassen über DI:
  - `SyncLogger` (`IDEALAKEWMSService/Program.cs:47-48`)
  - `IPurchaseOrderReceiptRepository` „Rest-Regel über das Web-Repository“ (`:89-90`)
  - `IProductionOrderPickingStatusRepository` (`:93`)
  - `CachedSettingRepository` (`:44-45`)
- Es gibt **keinen** Fall, in dem der Dienst einen Web-Endpunkt ruft. Die Suche nach `BaseUrl`/`/api/` im Dienst trifft nur Mail-Links
  (`BfAuftragEmailService.cs:122-124`) und Sage-SData.
- Es gibt **keine** `ICurrentUserService`-Implementierung außerhalb von Web und Tests (`CurrentUserService.cs:6`,
  `IdealAkeWms.Tests/Helpers/FakeCurrentUserService.cs:5`).

| Option | Kurz | Bewertung |
|---|---|---|
| **A (empfohlen)** | Dienst löst `PickingOrderWorkflow` direkt aus DI auf; eigene kleine `ICurrentUserService`-Implementierung `DienstBenutzer` | Keine doppelte Restmengen-Logik, folgt dem Muster „Web-Klasse im Dienst“, Transaktion und Nummernkreis bleiben an einem Ort |
| B | Web-`CurrentUserService` im Dienst registrieren | Ohne `HttpContext` schreibt er `"SYSTEM"` (`CurrentUserService.cs:40,49`) statt des Lauf-Namens und widerspricht ADR 0003. Braucht zusätzlich `AddHttpContextAccessor`, Rollen- und User-Repository. Verworfen |
| C | Dienst ruft einen Web-Endpunkt | Kein Vorbild, braucht Dienstkonto als WMS-Benutzer, Windows-Auth-Handshake und Fehlerpfad über HTTP. Verworfen |
| D | Logik im Dienst nachbauen | Zwei Restmengen-Regeln, die auseinanderlaufen. Gerald: keine Duplikation. Verworfen |

**A im Detail:**

1. **Gemeinsame Registrierung** `IdealAkeWms/Services/PickingOrderRegistrierung.cs` (statische Methode
   `AddKommissionierauftraege(IServiceCollection)`). Sie enthält genau die heutigen Web-Zeilen:
   - `ServiceSettingRepository` + `IServiceSettingRepository` als `GuardedServiceSettingRepository` (`IdealAkeWms/Program.cs:89-93`)
   - BOM-Graph mit lazy Weiche (`:97-110`)
   - FA-Hierarchie-Knoten mit Cache (`:113-114`)
   - `FaHierarchyTreeBuilder` (`:195`)
   - `IPickingAreaRepository`, `IPickingOrderRepository`, `PickingOrderWorkflow` (`:130-132`)

   Web- und Dienst-`Program.cs` rufen sie auf. `IProductionOrderRepository` und `IProductionOrderPickingStatusRepository` bleiben, wo sie
   sind (Web `:79-80`, Dienst `:93`). Im Dienst kommt `IProductionOrderRepository` dazu.

   Grund für die gemeinsame Methode statt einer Kopie: Der BOM-Graph ist absichtlich zyklisch und nur durch die lazy Delegates auflösbar.
   `BomDiResolutionTests.cs:15-31` existiert genau, weil er leicht bricht. Zwei Kopien würden still auseinanderlaufen.
   `BomDiResolutionTests.BuildProvider` ruft danach die gemeinsame Methode statt des Spiegels (`:60-72`).

   Abhängigkeiten im Dienst:
   - `IMemoryCache` ist da (`IDEALAKEWMSService/Program.cs:41`).
   - `OseonConnection` braucht `BomRepository` im Ctor (`BomRepository.cs:18-19`). Er wird nur im Flachmodus lazy gebaut
     (`BomRepositoryMasterSwitch.cs:44`), ist in `IDEALAKEWMSService/appsettings.json:5` vorhanden und wird bei IDEAL
     (hierarchisch) nie gebaut.
2. **`IDEALAKEWMSService/Common/DienstBenutzer.cs`** implementiert `ICurrentUserService`:
   - `GetDisplayName()` und `GetWindowsUserName()` geben den festen Namen zurück.
   - `GetCurrentAppUserId`/`GetCurrentAppUserName` → `null`, `IsLoggedIn` → `false`, `GetDefaultPageSizeAsync` → `null`.
   - Alle Rollen-Abfragen werfen `NotSupportedException("Der Dienst hat keine Rollen")`.

   Der Workflow nutzt im Erzeugungsweg nur die beiden Namensmethoden (`PickingOrderWorkflow.cs:126,131`). Eine Rollenabfrage im Dienst wäre
   ein Fehler und soll laut scheitern statt still `false` zu liefern. Registrierung:
   `AddScoped<ICurrentUserService>(_ => new DienstBenutzer(KommissionierNachtragService.ServiceUser))`.

   ponytail: Der Name ist fest auf diesen Lauf gesetzt. Nutzt später ein zweiter Dienst-Lauf Web-Workflows, wird der Name je Scope gesetzt.
3. **`IDEALAKEWMSService/Services/KommissionierNachtragService.cs`** nach dem Muster `LetzteBestellungSyncService`:
   - Ctor `(ApplicationDbContext, IServiceScopeFactory, IConfiguration, ILogger<…>, ISyncLogger)`, `ISyncLogger` als letzter Parameter
     (ADR 0010).
   - Konstanten: `ServiceUser = "KommissionierNachtrag"`, `KeyEnabled`, `KeyUhrzeit`, `DefaultUhrzeit = 2`.
   - `RunWennFaelligAsync(bool dryRun, DateTime jetzt, CancellationToken)`:
     - Schalter lesen (`ServiceSettings.GetBoolSafeAsync`), bei DryRun Log-Zeile und `false`.
     - Uhrzeit lesen (`GetIntSafeAsync`).
     - Merker aus `SyncLogs` lesen (Service = neuer Name, Message „Run gestartet“, wie `LetzteBestellungSyncService.cs:76-78`).
     - `LetzteBestellungPlaner.Faellig(jetzt, letzterStart, new[] { uhrzeit })`, dann `RunAsync`.
   - `RunAsync`:
     - `BeginRunAsync`.
     - FA-Ids aus `IProductionOrderPickingStatusRepository.GetReleasedForPickingAsync()`.
     - **je FA ein eigener Scope** (`IServiceScopeFactory`): `PickingOrderWorkflow` auflösen, `ErzeugeOffeneAsync(id)` in try/catch.
       Grund: Ein Scope über alle FAs sammelt Tausende Zeilen im Change-Tracker (Einmal-Lauf AKE 6 180 Zeilen). Ein unerwarteter Fehler soll
       den nächsten FA nicht mit einem verschmutzten Kontext treffen. `VerwerfeAenderungen` greift nur im Nummernkreis-Pfad
       (`PickingOrderWorkflow.cs:139`, `Nummernkreis.cs:39-46`).
     - Zählen, Zeilen loggen, `FinishSuccessAsync(counts)`.
   - Der FA-Name für die Meldung kommt aus der FA-Liste (`OrderNumber`), ohne zweiten Lesezugriff.
4. **`PickingOrderErzeugung`** bekommt:
   - `int ZeilenAngelegt`, gesetzt in der Schleife `PickingOrderWorkflow.cs:118-136` und übernommen bei `:147-149`. Das ist der kleinste Weg
     zum Zähler `zeilen_angelegt`, ohne die Aufträge nachzulesen.
   - `List<PickingOrderRestmengen.Ueberdeckung> Ueberdeckt` (siehe Punkt 8).
5. **`SyncWorker`**: neuer Block nach der Arbeitsgang-Erkennung (`SyncWorker.cs:543`), also nach FA-Import (`:44`), BOM-Cache (`:205`),
   FA-Hierarchie (`:352`) und Materialisierung (`:372`). Damit sieht der Lauf die frischste Stückliste:
   - AKE: Cache zuerst (`BomRepository.cs:27-30`), gepflegt vom BOM-Cache-Sync.
   - IDEAL: lokale `FaHierarchyNodes`, gepflegt vom FA-Hierarchie-Sync.

   Ausführung über `RunResilientAsync("Kommissionier-Nachtrag", …)` mit eigenem Scope, wie `SyncWorker.cs:491-496`.
6. **Leitstand (Web):**
   - `AuftraegeNachziehen` bekommt `[RequireAdminAccess]` statt `[RequireLeitstandAccess]` (`PickingLeitstandController.cs:534`). Admin
     ist in `CanManagePickingReleaseAsync` enthalten (`CurrentUserService.cs:137-138`), der Klassenfilter `[RequirePickingOrLeitstandAccess]`
     (`:12`) bleibt.
   - `PickingLeitstandViewModel` bekommt `IsAdmin`, gesetzt in `Index` aus `IsAdminAsync()` (`CurrentUserService.cs:82-83`).
   - `Index.cshtml:86-93` und `:95-99` werden zusätzlich in `@if (Model.IsAdmin)` gefasst. Das JS ist bereits null-sicher (`:664`, `:733`).
   - `AltbestandGeprueft` (`:550-567`, F7):
     - Nach `AltbestandGeprueftAsync` den Status lesen (`_pickingStatusRepository.GetByProductionOrderIdAsync(id)`,
       `IProductionOrderPickingStatusRepository.cs:16`).
     - Bei `IsReleasedForPicking && !IsDonePicking && !order.IsDone && !order.IsCancelled` (dieselbe Bedingung wie
       `GetReleasedForPickingAsync`, `ProductionOrderPickingStatusRepository.cs:193`) folgt
       `await AuftraegeMeldenAsync(new[] { (order.OrderNumber, await _pickingOrderWorkflow.ErzeugeOffeneAsync(id)) })`.
     - Die Erfolgsmeldung nennt zuerst die geprüften Altbestand-Nummern, danach die Meldung der Erzeugung.
     - Summary-Kommentar `:550-551` nachziehen.
7. **Nebenläufigkeit** mit Freigaben, Knopf oder einem zweiten Lauf: kein neuer Mechanismus nötig, weil der Workflow das abdeckt.
   - Die Restmengen werden **innerhalb** der Serializable-Transaktion aus den aktiven Zeilen berechnet (`PickingOrderWorkflow.cs:100-103`,
     `PickingOrderRepository.cs:17-20, 72-79`). Die nächste Nummer wird dort ebenfalls gelesen (`:117`).
   - Eindeutige Indizes auf `(Jahr, LaufendeNummer)` und `Nummer` (`SQL/133_AddPickingAreasAndOrders.sql:166-171`).
   - Bei Deadlock oder Unique-Verletzung verwirft `Nummernkreis.MitVersuchenAsync` den Tracker und versucht es bis zu 3-mal in einer neuen
     Transaktion (`Nummernkreis.cs:30-49`). Der zweite Versuch rechnet die Reste neu und sieht die Zeilen der Gegenseite. Es entsteht also
     nichts doppelt.
   - Unter InMemory läuft die Aktion ohne Transaktion (`PickingOrderRepository.cs:74`). Gleichzeitigkeit ist damit nur manuell prüfbar (TS-191.9).
   - Die Altbestand-Prüfung (`:64`) und das Lesen der Stückliste (`:71`) liegen außerhalb der Transaktion, wie heute bei der Freigabe.
8. **Warnung „Stückliste kleiner“ (F8): wie sie billig ermittelt wird.**

   Befund:
   - Der Leitstand lädt je Seitenaufruf weder Stücklisten noch Kommissionierzeilen.
     - `AuftraegeAnhaengenAsync` (`PickingLeitstandController.cs:364-369`) holt nur die Auftragsköpfe in einer Abfrage
       (`PickingOrderRepository.GetByProductionOrdersAsync`, `:42-48`, ohne `Zeilen`).
     - Die Status-Zeile `ProductionOrderPickingStatus` liegt dagegen schon je FA im Speicher (`GetByProductionOrderIdsAsync`, Controller `:218`
       und `:303`) und geht als `ps` in `MapItem` (`:125-163`), das flach und hierarchisch dieselbe Methode nutzt (`:227`, `:305`).
     - Ein Feld dort kostet den Leitstand **keine** zusätzliche Abfrage.
   - Live im Leitstand zu rechnen hieße je FA eine Stückliste zu laden: Sage-View, OSEON oder FA-Hierarchie (`BomRepository.cs:27-57`). Das
     schließt der Backlog aus.
   - Der Ort, an dem Soll und Zeilen ohnehin zusammenkommen, ist `ErzeugeOffeneAsync`. Dort liegt `soll` vor (`:81-87`), und die aktiven Zeilen
     werden in der Transaktion gelesen (`:103`). Die Überdeckung kostet dort keinen Lesezugriff extra.
   - Die Stücklistenseite lädt die Stückliste ohnehin (`PickingController.cs:283`). Sie braucht nur die Zeilen aller Aufträge, eine Abfrage
     `GetAktiveZeilenAsync(id)` (`PickingOrderRepository.cs:17-20`, schon ohne stornierte Aufträge und mit `PickingOrder` für die Regel
     `Abgedeckt`).

   Umsetzung:
   - **`PickingOrderRestmengen`**:
     - `record Ueberdeckung(string ArticleNumber, string? BomPosition, decimal Soll, decimal Beauftragt)`.
     - `static List<Ueberdeckung> Ueberdeckt(IEnumerable<Soll> soll, IEnumerable<PickingOrderLine> zeilen)`: Gruppen über `Schluessel`. Soll wird je
       Schlüssel summiert wie in `Berechne` (`:36-38`), Beauftragt über `Abgedeckt`. Treffer sind Schlüssel mit mindestens einer Zeile
       `Offen`/`Gepickt` und `Beauftragt > Soll` (Soll 0, wenn der Schlüssel fehlt). Sortiert nach Artikel und Position. Die Funktion ist DB-frei
       und unit-testbar wie `Berechne`.
     - `static string? Text(IReadOnlyList<Ueberdeckung>)` gibt `null` bei leer zurück, sonst „<Artikel> Pos <P>: Stückliste <x>, beauftragt <y>“,
       mit „; “ verbunden und auf 1000 Zeichen gekürzt (Ende „…“). Leitstand-Tooltip, Nachtlauf-Warnung und Marker nutzen denselben Text.
   - **`PickingOrderWorkflow`**:
     - Den Soll-Aufbau `:79-87` als `public static List<PickingOrderRestmengen.Soll> SollListe(BomQueryResult bom, ProductionOrder order)`
       (`IBomRepository.cs:10`) herausziehen. `ErzeugeOffeneAsync` und `BomSeiteAsync` nutzen dieselbe Methode, damit die Relevanz-Regel bei
       IDEAL nicht zweimal existiert.
     - In der Transaktion `lauf.Ueberdeckt = PickingOrderRestmengen.Ueberdeckt(soll, zeilen)` **vor** dem frühen Return bei `OhneRest` (`:104`). Die
       Zeilen von `:103` werden dafür in eine Variable gelegt.
     - Nach erfolgreicher Transaktion `r.Ueberdeckt` übernehmen. Bei Treffern kommt eine Meldung in `r.Meldungen` („FA <Nr>: Stückliste kleiner
       als offene Kommissionierzeilen – <Text>“).
     - Marker schreiben **nur bei Änderung**, damit die Audit-Felder nicht jede Nacht wechseln:
       `if (ps != null && ps.StuecklisteKleiner != neu) await _status.SetStuecklisteKleinerAsync(id, neu, _user.GetDisplayName(), _user.GetWindowsUserName())`.
       `ps` ist bei `:98` schon geladen.
     - Kein Marker-Update bei den frühen Returns (Altbestand `:64`, ohne Stückliste `:72`, ohne Standardbereich `:91`) und bei `!ok`. Dort
       gibt es keine verlässliche Berechnung, und der alte Stand bleibt.
   - **`ProductionOrderPickingStatusRepository.SetStuecklisteKleinerAsync(int productionOrderId, string? text, string modifiedBy, string modifiedByWindows)`**
     nach dem Muster `SetPickingStatusTextAsync` (`:123-135`) mit Audit-Feldern. Einen Caching-Decorator für dieses Repository gibt es nicht,
     also ist nichts zu invalidieren. Web und Dienst registrieren direkt die Klasse (`IdealAkeWms/Program.cs:80`,
     `IDEALAKEWMSService/Program.cs:93`).
   - **Leitstand:**
     - `PickingLeitstandItem` (`PickingLeitstandViewModel.cs:60`) bekommt `string? StuecklisteKleiner`, in `MapItem` gesetzt mit
       `ps?.StuecklisteKleiner`.
     - `_PickingLeitstandRow.cshtml` zeigt direkt nach dem Altbestand-Block (`:185-197`) bei `!string.IsNullOrEmpty(item.StuecklisteKleiner)` an:
       `<div class="mt-1"><span class="badge text-bg-warning" title="Stückliste kleiner als offene Kommissionierzeilen: @item.StuecklisteKleiner">⚠ Stückliste kleiner</span></div>`.
     - Das ist dasselbe Badge-Muster wie „⚠ Altbestand prüfen“ (`:189`): Symbol und Text, also nicht nur Farbe. Es wird keine Spalte und kein
       Spaltenfilter hinzugefügt.
   - **Stückliste:**
     - `BomViewModel` (`BomViewModels.cs`, bei `:157`) bekommt `List<PickingOrderRestmengen.Ueberdeckung> StuecklisteKleiner`.
     - `BomSeiteAsync` setzt sie aus `PickingOrderRestmengen.Ueberdeckt(PickingOrderWorkflow.SollListe(bomResult, order), await _pickingOrderRepository.GetAktiveZeilenAsync(id))`.
       Es rechnet mit dem **ungefilterten** `bomResult`, also vor der Auftragsfilterung `:291-293`.
     - `Bom.cshtml` zeigt vor der Filterkarte (`:84`) ein `alert alert-warning` mit Überschrift „⚠ Stückliste kleiner als offene
       Kommissionierzeilen“, einer Liste der Schlüssel (Artikel, Position, Stückliste, beauftragt) und dem Satz „Offene Zeilen werden nicht
       automatisch gekürzt – bitte im Leitstand entscheiden.“
     - Andere Aufrufer der View setzen die Liste nicht (Default leer), also zeigen sie nichts.
   - **Nachtlauf:** `stueckliste_kleiner` zählt FAs mit `Ueberdeckt.Count > 0`. Die Warnzeile kommt aus `Meldungen` (F5).

   Verworfen:
   - (i) Nur der Nachtlauf schreibt den Marker. Das ist nicht weniger Code, der Marker bliebe aber nach einer Freigabe bis zur Nacht veraltet.
   - (ii) Der Marker hängt am `PickingOrder` wie `AltbestandPruefen`. Die Überdeckung gilt aber je FA, nicht je Auftrag, und der Leitstand lädt
     die Aufträge nur als Liste.
   - (iii) Der Marker kommt ins Aktivitäts-Protokoll, und der Leitstand liest es. Das wäre ein Lesepfad quer zu ADR 0010 und würde bei einer
     Freigabe am Tag nicht aktualisiert.

Muster: Repository-Pattern + Decorator (ADR 0001) unverändert; Aktivitäts-Protokoll (ADR 0010); ServiceSettings-Katalog (ADR 0008/0020);
Read/Edit-Split mit Action-Filter (ADR 0006). Keine neue Listen-View.

## Migrations-/SQL-Auswirkungen

*Überholt (Stand vor den Freigabe-Antworten): „Keine Migration“. Antwort 2 verlangt die Leitstand-Anzeige je FA, dafür gibt es eine Spalte.*

Eine additive Migration, nicht daten-destruktiv, kein Backfill (ADR 0004):
1. Model: `ProductionOrderPickingStatus.StuecklisteKleiner` (`string?`, `[MaxLength(1000)]`). Der Text wird auf 1000 Zeichen gekürzt (Punkt 8).
2. `dotnet ef migrations add AddPickingStatusStuecklisteKleiner --project IdealAkeWms`.
3. `SQL/168_AddPickingStatusStuecklisteKleiner.sql` nach dem Muster `SQL/167_AddStockMovementAbteilung.sql`:
   - Tabellen-Guard `OBJECT_ID(N'dbo.ProductionOrderPickingStatuses', N'U')` mit `NOEXEC`.
   - `IF COL_LENGTH(N'dbo.ProductionOrderPickingStatuses', N'StuecklisteKleiner') IS NULL ALTER TABLE … ADD [StuecklisteKleiner] NVARCHAR(1000) NULL;`
     im eigenen Batch.
   - `__EFMigrationsHistory`-Insert im separaten Batch.
   - Nummer 168: höchste im Worktree ist `SQL/167` (Manifest `nachlauf.psd1:125`). Der Dev-Lauf prüft die Nummer erneut, weil parallele
     Entwürfe sie beanspruchen können.
4. `SQL/00_FreshInstall.sql` an **zwei** Stellen nachziehen:
   - die Spalte im `CREATE TABLE` der `ProductionOrderPickingStatuses` (bei `:447`, nach `IsDonePicking`)
   - die `MigrationId` im History-Block nach `:3552-3554`
5. `SQL/AgentJobs/`: nicht betroffen. Die Suche nach `ProductionOrderPickingStatuses` in `SQL/AgentJobs/` trifft nichts, und die Spalte ist
   nullable.

Die beiden neuen ServiceSettings legt der Web-Start aus dem Katalog an (`IdealAkeWms/Program.cs:310-324`, idempotent), ohne SQL.

Bestand nach dem Deploy: Die Spalte ist überall `NULL`. Der erste Dienststart holt den Nachtlauf nach (F2) und setzt den Marker für alle
freigegebenen, offenen HauptFAs. Vorher zeigt der Leitstand kein Badge, die Stückliste rechnet aber schon live.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:KommissionierNachtragEnabled` | Bool | `true` / `false` (Auswahl) | `true` | An: Der Dienst legt einmal täglich fehlende Kommissionieraufträge für Stücklisten-Nachträge freigegebener, offener HauptFAs an. Aus: kein Nachtrag; nur Admins können ihn im Leitstand auslösen. | Dienst: Lauf „Kommissionier-Nachtrag“ (`SyncWorker`); Aktivitäts-Protokoll | beide | „Legt Kommissionieraufträge an, die danach in der Kommissionierung erscheinen. Nach dem Einschalten bzw. dem ersten Dienststart läuft der Nachtrag einmal sofort.“ |
| `Sync:KommissionierNachtragUhrzeit` | Int | 0–23 (Bereich `(0, 23, "Uhr")` wie `ServiceSettingDefinitions.cs:231-233`, Server prüft beim Speichern) | `2` | Ab dieser Stunde läuft der Nachtrag einmal pro Tag. Ein verpasster Termin wird nach dem Dienststart einmal nachgeholt. | Dienst: Lauf „Kommissionier-Nachtrag“ | beide | – |

Katalog: Basisliste (Kategorie „Sync“, Beschreibung ≤ 500 Zeichen, `ServiceSettingDefinitionsTests.cs:50-60`), `Meta` mit Anzeigename,
Verwendung, Standort leer = beide, Warnhinweis (`ServiceSettingDefinitions.cs:35`), Int-Bereich (`:228-236`). Das erfüllt den Drift-Guard
(ADR 0008). Keine AppSetting-Änderung.

## Audit-Feld-Auswirkungen

- Neue `PickingOrder`/`PickingOrderLine` aus dem Nachtlauf: `CreatedAt = DateTime.Now` (Workflow `:53`), `CreatedBy` und `CreatedByWindows` =
  `KommissionierNachtrag` (über `DienstBenutzer`).
- Der Nachtlauf ändert keine bestehenden Kommissionierzeilen (Antwort 2: nichts kürzen).
- `ProductionOrderPickingStatus.StuecklisteKleiner`: `SetStuecklisteKleinerAsync` setzt `ModifiedAt = DateTime.Now`, `ModifiedBy` und
  `ModifiedByWindows`, nur bei geändertem Wert.
  - Nachtlauf: `KommissionierNachtrag` (über `DienstBenutzer`).
  - Freigabe, Knopf, „Altbestand geprüft“ und Stückliste ohne Leitstand: der angemeldete Benutzer.
- Leitstand-Knopf (Admin) und „Altbestand geprüft“: unverändert aus `ICurrentUserService` des angemeldeten Benutzers.

## Akzeptanzkriterien

1. Mit Schalter an und Uhrzeit H startet der Dienst im ersten Zyklus ab H genau einen Lauf „Kommissionier-Nachtrag“. Weitere Zyklen am selben
   Tag starten keinen zweiten (Merker = „Run gestartet“ im Aktivitäts-Protokoll).
2. Ist seit dem letzten Termin kein Lauf erfolgt (z. B. erster Dienststart nach dem Deploy), startet der Lauf im ersten Zyklus nach dem Dienststart
   genau einmal.
3. Bei Schalter aus oder `WorkerSettings:SyncDryRun = true` gibt es keinen Lauf und keinen Merker. Im DryRun erscheint eine Log-Zeile.
4. Für einen freigegebenen, offenen HauptFA mit nach der Freigabe ergänzter Stücklistenposition legt der Lauf einen Auftrag an, der genau den
   Rest enthält (gleicher Bereich nach Regel, Prio und Kommissionierer vom FA). Ein zweiter Lauf legt nichts an (`ohne_rest`).
5. Aufträge und Zeilen aus dem Lauf haben `CreatedBy = CreatedByWindows = "KommissionierNachtrag"`.
6. FAs mit „Altbestand prüfen“, ohne Stückliste oder mit Fehler werden übersprungen bzw. gezählt (`altbestand_pruefen`, `ohne_stueckliste`,
   `fehler`) und je FA als Warnung protokolliert. Eine Ausnahme bei einem FA bricht den Lauf nicht ab.
7. Das Aktivitäts-Protokoll zeigt den Lauf unter „Kommissionier-Nachtrag“ mit den Zählern aus F5. `SyncLogServices.All` und `DisplayNames`
   enthalten ihn.
8. Sub-FAs, nicht freigegebene, erledigte, stornierte und vollständig kommissionierte FAs werden nicht angefasst.
9. Ein Benutzer mit Rolle `leitstand` ohne `admin` sieht im Leitstand keinen der beiden Knöpfe „Fehlende Kommissionieraufträge …“. Ein direkter
   POST auf `/PickingLeitstand/AuftraegeNachziehen` leitet auf AccessDenied um und legt nichts an.
10. Ein Admin sieht beide Knöpfe, und sie wirken wie bisher (TS-120.2/120.13).
11. Hilfe (`Help/Index`), Stückliste (`Picking/Bom`), Auftragsliste (`Picking/Index`), Workflow-Fehlertext und Altbestand-Meldung nennen den
    Nachtlauf und den Knopf nur noch als Admin-Notfall.
12. Die neuen Keys stehen in `/ServiceSettings` mit Anzeigename, Wirkung, Default und Warnhinweis. Die Uhrzeit außerhalb 0–23 wird beim Speichern
    abgelehnt.
13. Die DI-Auflösung im Dienst gelingt mit `ValidateOnBuild` (`KommissionierNachtragService`, `PickingOrderWorkflow` mit `DienstBenutzer`).
    Web und Dienst registrieren den BOM-Graph über dieselbe Methode, und `BomDiResolutionTests` ist grün.
14. Ein Schlüssel ist überdeckt, wenn es eine offene Zeile (`Offen`/`Gepickt`) gibt und die beauftragte Menge über dem aktuellen Soll liegt
    (entfallene Position = Soll 0). Dann schreibt der Lauf je FA eine Warnung mit Artikel, Position, Stückliste und beauftragt und zählt
    `stueckliste_kleiner`. Offene Zeilen bleiben unverändert (SollMenge und Status vor/nach gleich).
15. „Altbestand geprüft“ durch einen Leitstand-Benutzer ohne Admin legt für einen freigegebenen, offenen FA sofort die fehlenden Aufträge dieses
    FA an und meldet sie wie die Einzelfreigabe. Für einen nicht freigegebenen FA wird nur das Kennzeichen gelöscht, und die Meldung sagt das.
16. Leitstand: Nach einem `ErzeugeOffeneAsync`-Aufruf mit Überdeckung zeigt die FA-Zeile das Badge „⚠ Stückliste kleiner“ mit den Schlüsseln im
    Tooltip, für jeden Leitstand-Benutzer.
    - Nach einem Aufruf ohne Überdeckung ist das Badge weg.
    - Der Leitstand-Seitenaufruf lädt dafür keine Stückliste: keine zusätzliche Abfrage, Feld aus der bereits geladenen Status-Zeile.
    - Ein Aufruf, der den Wert nicht ändert, lässt `ModifiedAt` unverändert.
17. Stückliste (`Picking/Bom`, FA- und Auftragsmodus): Bei überdeckten Schlüsseln erscheint vor der Filterkarte der Hinweis „⚠ Stückliste kleiner
    als offene Kommissionierzeilen“ mit Artikel, Position, Stückliste und beauftragt.
    - Er ist live berechnet, also sofort nach einer Stücklistenänderung sichtbar, ohne Nachtlauf.
    - Ohne Überdeckung erscheint kein Hinweis.
18. Freigabe, Admin-Knopf und „Altbestand geprüft“ melden eine Überdeckung zusätzlich als Warnung in der Sammelmeldung.
19. Migration `SQL/168_…` ist idempotent: Zweimal ausgeführt gibt es keinen Fehler und keinen doppelten History-Eintrag. `00_FreshInstall.sql`
    enthält Spalte und `MigrationId`.

## Test-Szenarien

Neues Kapitel **TS-191 — Kommissionier-Nachtrag nachts, Knopf nur für Admins** in `docs/TESTSZENARIEN.md`. Stand Worktree: höchstes Kapitel
TS-190 (`docs/TESTSZENARIEN.md:13864`). Der Dev-Lauf prüft die Nummer erneut, weil andere Entwürfe vom 2026-11-24 ebenfalls „nächste freie“
beanspruchen. Danach `secondbrain/tests/testszenarien-index.md` nachziehen. In TS-120.2, 120.6 und 120.13 einen Hinweis ergänzen: „Knopf
seit v1.99.x nur Admin; Nachtrag automatisch → TS-191“.

Vorbedingungen: DEV AKE und DEV IDEAL, Leitstand aktiv, ein Admin- und ein Leitstand-Konto (ohne `admin`). Zu Unit-Tests: Die Gleichzeitigkeit
ist unter InMemory nicht prüfbar (`PickingOrderRepository.cs:74`), Rollen-UAT braucht ein echtes Nicht-Admin-Konto.

| Nr | Schritte | Erwartet |
|---|---|---|
| 191.1 | AKE: freigegebenen FA mit Aufträgen wählen; Testsystem: Stücklistenposition ergänzen bzw. Menge erhöhen (Sage-View/BOM-Cache); `Sync:KommissionierNachtragUhrzeit` = aktuelle Stunde; Merker vorher per Diagnose prüfen; einen Dienst-Zyklus abwarten | Ein neuer KO-Auftrag nur mit dem Rest; `CreatedBy` = KommissionierNachtrag; Protokoll „Kommissionier-Nachtrag“ mit `auftraege_angelegt ≥ 1`, Info-Zeile mit FA und KO-Nummer |
| 191.2 | Nächsten Zyklus abwarten; dann Uhrzeit auf die nächste Stunde setzen und danach abwarten | Kein zweiter Lauf in derselben Stunde; der nächste Lauf legt 0 an, `ohne_rest` = Zahl der FAs |
| 191.3 | FA mit „Altbestand prüfen“ freigegeben; Lauf auslösen | `altbestand_pruefen` = 1, Warnzeile mit FA; kein Auftrag |
| 191.4 | Freigegebener FA ohne Stückliste | `ohne_stueckliste` = 1, Warnung |
| 191.5 | Schalter aus → Zyklus; Schalter an + `WorkerSettings:SyncDryRun = true` → Zyklus | Kein Lauf im Protokoll; im DryRun Log-Hinweis |
| 191.6 | Dienst stoppen, Uhrzeit auf eine schon vergangene Stunde, Dienst starten | Genau ein nachgeholter Lauf |
| 191.7 | IDEAL (hierarchisch): HauptFA freigegeben, Position mit Kommissionieren-Kennzeichen im Struktur-Test ergänzen; Lauf | Auftrag nur mit relevanten Positionen (`KommissionierRelevanzFilter`); Sub-FAs nie |
| 191.8 | Leitstand als Leitstand-Benutzer öffnen; markieren; POST `/PickingLeitstand/AuftraegeNachziehen` (z. B. per Browser-Konsole mit Token) | Beide Knöpfe fehlen; POST → AccessDenied, kein Auftrag. Als Admin: beide Knöpfe sichtbar und wirksam |
| 191.9 | Negativ/Gleichzeitig: Lauf auslösen und während des Laufs denselben FA im Leitstand (Admin) nachziehen | Keine doppelten Zeilen (Diagnose unten liefert nichts); höchstens Warnzeile „Versuch … gescheitert“ im Log |
| 191.10 | Stücklistenmenge eines beauftragten, noch offenen Artikels senken (bzw. Position entfernen); sofort `Picking/Bom` des FA öffnen; danach Lauf; danach Leitstand | Stückliste: Hinweis „⚠ Stückliste kleiner als offene Kommissionierzeilen“ mit Artikel/Position/Stückliste/beauftragt **schon vor dem Lauf**; Lauf: `stueckliste_kleiner` = 1, Warnzeile; Leitstand: Badge „⚠ Stückliste kleiner“, Tooltip mit demselben Text; offene Zeile unverändert (SollMenge, Status) |
| 191.11 | Als Leitstand-Benutzer (ohne Admin) bei einem freigegebenen FA mit „Altbestand prüfen“ und Stücklisten-Nachtrag „Altbestand geprüft“ | Kennzeichen weg; sofort Auftrag/Aufträge für diesen FA; Meldung nennt geprüfte Nummern und neue KO-Nummern; kein Verweis auf den Knopf |
| 191.12 | Hilfe, Stückliste ohne Aufträge, leere Auftragsliste ansehen | Texte nennen den Nachtlauf, den Knopf nur als Admin |
| 191.13 | Negativ zu 191.10: Stücklistenmenge wieder auf den alten Wert; Stückliste öffnen; Lauf; Leitstand. Danach einen Artikel mit mehr Ist als Soll **umbuchen** (keine offene Zeile mehr) | Hinweis und Badge verschwinden nach dem Lauf; `ModifiedAt` der Status-Zeile ändert sich in einem zweiten Lauf ohne Änderung nicht. Über-Umbuchung ohne offene Zeile löst keine Warnung aus |
| 191.14 | Freigabe (Einzel) eines FA, dessen Stückliste kleiner ist als vorhandene offene Aufträge (z. B. Freigabe zurückgenommen mit Fortschritt, Stückliste gesenkt, neu freigegeben) | Sammelmeldung enthält die Warnung „Stückliste kleiner …“; Badge sofort im Leitstand |
| 191.15 | Migration: `SQL/168_…` zweimal gegen DEV ausführen | Zweiter Lauf ohne Fehler, kein doppelter History-Eintrag |

Diagnose-Abfrage (TS-191.9; gleicher Stücklistenschlüssel mehrfach durch den Nachtlauf angelegt, soll leer sein; `Storniert = 90`,
`Models/PickingOrder.cs:16`, gespeichert als Zahl):

```sql
SELECT l.ProductionOrderId, l.ArticleNumber, l.BomPosition, COUNT(*) AS Zeilen, SUM(l.SollMenge) AS Soll
FROM dbo.PickingOrderLines l
JOIN dbo.PickingOrders o ON o.Id = l.PickingOrderId
WHERE o.Status <> 90
  AND o.CreatedBy = 'KommissionierNachtrag'
GROUP BY l.ProductionOrderId, l.ArticleNumber, l.BomPosition
HAVING COUNT(*) > 1;
```

Diagnose Marker (TS-191.10/191.13):

```sql
SELECT p.OrderNumber, s.StuecklisteKleiner, s.ModifiedAt, s.ModifiedBy
FROM dbo.ProductionOrderPickingStatuses s
JOIN dbo.ProductionOrders p ON p.Id = s.ProductionOrderId
WHERE s.StuecklisteKleiner IS NOT NULL;
```

Unit-Tests (InMemory reicht): `PickingOrderRestmengen.Ueberdeckt` deckt diese Fälle ab:
- Menge gesenkt
- Position entfallen
- nur umgebuchte Zeilen (kein Treffer)
- „vollständig“ abgeschlossen (kein Treffer)
- doppelter Stücklistenschlüssel
- Text-Kürzung auf 1000

Dazu Workflow-Tests:
- Marker wird gesetzt und gelöscht.
- Kein Update bei gleichem Wert.
- Kein Update bei Altbestand oder ohne Stückliste.

Grenze der ersten Abfrage (TS-191.9): Sie sieht nur Doppel innerhalb der Nachtlauf-Aufträge. Ein Doppel zwischen Nachtlauf und gleichzeitiger Freigabe findet man nur,
indem man für den Test-FA `SUM(SollMenge)` je Schlüssel über alle nicht stornierten Aufträge mit dem Stücklisten-Soll vergleicht (von Hand,
TS-191.9).

## Etappen (nur bei epic: true)

Kein Epic.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt es gegen den echten Diff.*

- **Web-App:** ja (Leitstand-Filter/View, Badge, Stücklisten-Hinweis, Texte, gemeinsame Registrierung, Katalog-Seed der neuen ServiceSettings)
- **Service:** ja (neuer Lauf, DI)
- **Migration:** ja. `SQL/168_AddPickingStatusStuecklisteKleiner.sql` ist additiv und nullable, ohne Backfill und nicht daten-destruktiv. Ein
  DB-Backup ist nach ADR 0004 nicht zwingend, das übliche Backup vor dem Deploy reicht.
  *Überholt: „Migration: nein“ aus dem Stand vor den Freigabe-Antworten.*
- **Reihenfolge:** Schema (SQL/168) vor dem Start von Web und Dienst. Beide lesen die Spalte über das EF-Modell, `ProductionOrderPickingStatus`
  wird im Leitstand und im Workflow geladen. Danach Web (seedet die Keys, `IdealAkeWms/Program.cs:310-324`), dann Dienst. Ohne Seed liest der Dienst die Defaults
  (`GetBoolSafeAsync`/`GetIntSafeAsync`). Der erste Dienststart holt den Lauf sofort einmal nach (F2), auch tagsüber. Das ist gewollt und
  idempotent, an AKE nach dem Einmal-Lauf vom 2026-11-24 voraussichtlich ohne neue Aufträge.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022; Version = nächste freie, Stand Worktree 1.99.69 → voraussichtlich 1.99.70):
  - `@{ Id='SQL/168'; Version='1.99.70'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Datei='168_AddPickingStatusStuecklisteKleiner.sql'; MigrationId='<ts>_AddPickingStatusStuecklisteKleiner'; Voraussetzung='additiv: ProductionOrderPickingStatuses.StuecklisteKleiner NVARCHAR(1000) NULL (Marker Stueckliste kleiner als offene Kommissionierzeilen, Leitstand-Badge), kein Backfill'; Quelle='2026-11-25-kommissionierauftraege-nachtrag-nachts-spec' }`. Die Position folgt der Deploy-Reihenfolge nach `SQL/167` (`nachlauf.psd1:125-126`), nicht der Dateisortierung.
  - `@{ Id='v1.99.70-KommNachtrag-Lauf'; Version='1.99.70'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Neuer Dienst-Lauf "Kommissionier-Nachtrag": Sync:KommissionierNachtragEnabled (Default true), Sync:KommissionierNachtragUhrzeit (Default 2). Der erste Dienststart holt den Lauf einmal sofort nach - danach Aktivitaetsprotokoll "Kommissionier-Nachtrag" pruefen (Zaehler fehler/altbestand_pruefen).'; Quelle='2026-11-25-kommissionierauftraege-nachtrag-nachts-spec' }`
  - `@{ Id='v1.99.70-KommNachtrag-Knopf'; Version='1.99.70'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Leitstand: Knoepfe "Fehlende Kommissionierauftraege ... erzeugen" nur noch fuer Admins; Nachtraege legt der Dienst nachts an; "Altbestand geprueft" legt die Auftraege sofort an; neues Badge "Stueckliste kleiner" (offene Zeilen werden nicht gekuerzt). Leitstand-Benutzer informieren.'; Quelle='2026-11-25-kommissionierauftraege-nachtrag-nachts-spec' }`
  - Keine Daten-, Einmal- oder Prüfungs-Einträge. Es gibt keinen Backfill (den Marker setzt der erste Nachtlauf), und der Seed läuft beim
    Web-Start. *Überholt: „Keine Schema-… Einträge“.*
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

**Brain-Nachzug (Dev-Lauf, Hauptcheckout):**
- `codebase/services.md`: Lauf und Keys
- `codebase/datenmodell.md`: `ProductionOrderPickingStatus.StuecklisteKleiner` (Schreiber nur `ErzeugeOffeneAsync`)
- `codebase/controller.md:165`: `AuftraegeNachziehen` → `[RequireAdminAccess]`
- `feature-map.md`, Changelog
- `fallstricke.md`: „Web-Workflow im Dienst: `ICurrentUserService` gibt es dort nur als `DienstBenutzer`; Rollenabfragen werfen“ (mit Warum)
- Anwender- und Betriebsdoku laut `affected_code`

## Offene Rueckfragen

*Alle drei beantwortet (Freigabe-Antworten, Gerald 2026-11-25) und in den Rumpf übernommen: 1 → F6, 2 → F8, 3 → F7. Der Text bleibt als Verlauf.*

1. **Was heißt „Admin“?**
   - Empfehlung: die Rolle `admin` über das vorhandene `[RequireAdminAccess]` (`Filters/RequireAdminAccessAttribute.cs:23`,
     `CurrentUserService.cs:82-83`). `admin` ist die Wildcard des Rollenkonzepts (`codebase/controller.md:47-48`, ADR 0006). Keine neue
     Rolle, keine Änderung an `RoleOverview`.
   - Alternative: ein eigenes Recht, z. B. `leitstand_nachtrag`. Das braucht laut CLAUDE.md drei Stellen (Attribut, `controller.md`,
     `Views/Users/RoleOverview.cshtml`) und eine Rollenpflege an beiden Standorten. Lohnt nur, wenn Nicht-Admins (Leitstand-Leitung) den
     Notfall-Knopf behalten sollen.
2. **Stückliste nach der Freigabe kleiner geworden** (Menge gesenkt oder Position entfernt). Heute bleibt das offene Soll unverändert und es gibt
   keine Meldung (Beleg im Abschnitt „Heutiges Verhalten“). Optionen:
   - (a) nur dokumentieren
   - (b) **Empfehlung:** nichts ändern, aber der Nachtlauf meldet je FA eine Warnung „Stückliste kleiner: Artikel, Position, Soll,
     beauftragt“ und zählt `stueckliste_kleiner`. Eine kleine reine Funktion neben `PickingOrderRestmengen.Berechne`, gleiche Abdeckungs-Regel
     `Abgedeckt` (`PickingOrderRestmengen.cs:19-27`). Die Leitstand-Leitung entscheidet dann selbst (Zeile im Auftrag nicht umbuchen,
     „Vollständig“ abschließen).
   - (c) offene Zeilen automatisch kürzen bzw. auf „Entfallen“ setzen. Das greift in laufende Kommissionierung ein: Zeilen können schon
     „gepickt“ sein. Es braucht eigene Regeln für gepickt/umgebucht und Audit-Updates. Die Empfehlung ist, (c) erst mit einer eigenen Spec
     anzugehen.
3. **„Altbestand geprüft“ ohne Admin-Knopf.** Nach der Bestätigung (`PickingLeitstandController.cs:550-567`) verweist die Meldung heute auf den
   Knopf, den ein Leitstand-Benutzer künftig nicht mehr sieht. Optionen:
   - **Empfehlung:** Die Action ruft danach für diesen einen FA `ErzeugeOffeneAsync(id)` auf und meldet wie die Freigabe. Das ist eine Zeile,
     derselbe Erzeugungsweg, und die Aufträge sind sofort da.
   - Alternative: Die Meldung lautet „Aufträge legt der Nachtlauf an“, und der FA wartet bis zur nächsten Nacht.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

(keine – kein Jira-Vorgang)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. „Admin“ = Rolle `admin` über `[RequireAdminAccess]` oder eigenes Recht? → **Rolle `admin` über `[RequireAdminAccess]` (Gerald 2026-11-25).**
2. Kleiner gewordene Stückliste: (a) dokumentieren, (b) melden im Nachtlauf, (c) offene Zeilen kürzen? → **(b), nichts kürzen. Zusätzlich die Warnung in der Stückliste bzw. im Leitstand der Kommissionierung anzeigen (Gerald 2026-11-25).**
3. „Altbestand geprüft“: sofort Aufträge für diesen FA anlegen oder bis zur Nacht warten? → **Sofort für diesen FA anlegen (Gerald 2026-11-25).**
