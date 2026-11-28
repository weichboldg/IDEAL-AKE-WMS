---
type: spec
title: Offene Bestellungen – tägliche Erinnerungs-Mail bei Lieferverzug (Einkauf-Sammelliste, Kreditor-Mail mit Testbetrieb)
slug: 2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec
status: Gemerged
created: 2026-11-12
updated: 2026-11-28
freigabe_von: "Gerald Weichbold (Chat Koordination, 2026-11-12 14:58)"
freigabe_am: 2026-11-12
source_backlog: "[[2026-11-12-offene-bestellungen-mahnmail-lieferverzug]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version: "1.99.10"
version_vorschlag: "1.99.10 (umgesetzt; 1.99.9 = System-Kürzel im Bündel)"
sql_nummer_vorschlag: "145 (umgesetzt; SQL/144 = System-Kürzel)"
testszenario: "TS-128"
affected_code:
  - "NEU IDEALAKEWMSService/Services/LieferverzugMailService.cs – Lauf (Gate, Zeitfenster, Stand-Prüfung, Versand, Protokoll); ISyncLogger letzter Ctor-Parameter"
  - "NEU IDEALAKEWMSService/Services/LieferverzugPlaner.cs – reine Entscheidung (fällig, Wiederholung, Empfänger, Testbetrieb-Umleitung, Cap), unit-getestet"
  - "NEU IDEALAKEWMSService/Services/LieferverzugMailHtml.cs – HTML + Text der Kreditor- und Einkauf-Mail (CI, reine Funktion)"
  - "IDEALAKEWMSService/Workers/SyncWorker.cs:475-486 – neuer Block direkt NACH „Offene Bestellungen (Sage)“, eigenes Gate"
  - "IDEALAKEWMSService/Services/IMailService.cs:5 + MailService.cs:18-72 – zweite Sende-Methode mit Antwortadresse und eingebettetem Logo, liefert gesendet ja/nein (bestehende Signatur unverändert)"
  - "IDEALAKEWMSService/Program.cs:86-97 – DI-Registrierung"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs (Basis, Meta :33-120, IntMeta :123-130) – neue Kategorie „Lieferverzug-Mail“"
  - "IdealAkeWms/Models/SettingDefinition.cs + IdealAkeWms/Services/Settings/SettingValidator.cs:21 – Prüfung von Mail-Adresslisten beim Speichern"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:37/59/95 – neuer Dienstname LieferverzugMail + DisplayName"
  - "IdealAkeWms/Data/Repositories/PurchaseOrderReceiptRepository.cs:11-21 + Models/ViewModels/PurchaseOrderReceiveViewModel.cs:31 – Rest-Regel als EINE Funktion für Maske und Mail"
  - "NEU IdealAkeWms/Models/PurchaseOrderReminder.cs + Data/ApplicationDbContext.cs + Migration + SQL/145 + SQL/00_FreshInstall.sql (2 Stellen)"
  - "nur bei Rückfrage 5 = ja: IdealAkeWms/Models/Creditor.cs, Controllers/CreditorsController.cs:253-258 (ApplyWmsFelder), Views/Creditors/Edit+Details"
  - "IDEALAKEWMSService/IDEALAKEWMSService.csproj – Logo Branding/IDEAL_AKE_Logo_rgb_klein.jpg als EmbeddedResource"
  - "docs/TESTSZENARIEN.md (TS-128), Views/Help/Changelog.cshtml, beide AppVersion.cs, scripts/deploy/nachlauf.psd1, README (Betrieb)"
open_questions:
  - "1 Termin: Gilt der Cache-Termin (Positions-Liefertermin, sonst Beleg-Liefertermin) unverändert als Referenz? Ist das in Sage der bestätigte oder der gewünschte Termin? (Prüfabfrage P-M1)"
  - "2 Zählweise des Zeitraums: Kalendertage oder Arbeitstage, und ab wann fällig (heute >= Termin + N)?"
  - "3 Termin/Uhrzeit des Versands: Stunde einstellbar, nur Montag bis Freitag?"
  - "4 Wiederholung: Kreditor beim ersten Fälligwerden und dann alle n Tage, Neustart bei geändertem Termin, Sicherheitsgrenze je Lauf; Einkauf täglich?"
  - "5 Je Kreditor abschaltbar und eigene Erinnerungsadresse als WMS-Felder am Kreditor (Migration)?"
  - "6 Sprache der Kreditor-Mail: Deutsch, oder Deutsch und Englisch in einer Mail, oder je Land?"
  - "7 Absender, Antwortadresse und CI-Standort: Absender bleibt MailSettings:FromAddress, Antwortadresse und Standort als Einstellung?"
  - "8 Teillieferungen und Rahmenabrufe: Rest > 0 zählt, EBZ bleibt außen vor?"
  - "9 Protokoll: eigene Tabelle je Mail und Position plus Aktivitäts-Protokoll?"
  - "10 Sonderfälle: Lieferant gesperrt, Kreditor inaktiv/fehlt/ohne gültige Adresse – keine Kreditor-Mail, nur in der Einkauf-Liste?"
  - "11 Inhalt der Kreditor-Mail: Mengen in der Basiseinheit; Positionen mit abweichender Einheit ohne Menge?"
  - "12 Standort: nur AKE (IDEAL hat keinen SData-Endpunkt für offene Bestellungen)?"
  - "13 Audit: Protokolltabelle ohne AuditableEntity (nur Dienst schreibt, nie geändert)?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Offene Bestellungen: tägliche Erinnerungs-Mail bei Lieferverzug

## Ziel / Nutzen (das Warum)

Überfällige Bestellpositionen fallen heute nur auf, wenn jemand die Liste „Offene Bestellungen“ mit „nur überfällig“ öffnet
(`PurchaseOrdersController.cs:83`). Der Dienst soll das einmal am Tag selbst erledigen: Der Einkauf erhält eine Sammelliste,
die Kreditoren auf Wunsch eine Erinnerung. Bevor externe Mails echt rausgehen, läuft alles im **Testbetrieb** über den Einkauf,
denn eine Mail an einen Lieferanten lässt sich nicht zurückholen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neuer Dienst-Lauf „Lieferverzug-Mails“ im `SyncWorker`, direkt nach „Offene Bestellungen (Sage)“, höchstens einmal am Tag.
- Quelle ist ausschließlich der vorhandene Cache `SagePurchaseOrderLines` (kein neuer Sage-Read).
- **Einkauf-Sammelliste** (entschieden): eine Mail über alle Kreditoren an die Einkauf-Adressen.
- **Kreditor-Mail**: eine Mail je Kreditor mit allen fälligen Positionen, getrennt ein- und ausschaltbar.
- **Testbetrieb** (entschieden, Default an): Jede Kreditor-Mail geht unverändert an den Einkauf, mit „TESTBETRIEB – wäre an … gegangen“
  im Betreff und als Banner im Kopf.
- HTML im CI (Farben, Adresse, Logo, genderneutrale Sprache) plus Text-Teil (multipart/alternative).
- Einstellungen in `/ServiceSettings` (ADR 0008/0020), Protokolltabelle, Aktivitäts-Protokoll.

**Out-of-Scope**
- Rahmenabrufe `EBZ`: Sie stehen nicht im Cache (`EinkaufBestellungenSyncService.cs:32`, `:113`) und bleiben deshalb außen vor.
- Antworten der Kreditoren auswerten oder Termine nach Sage zurückschreiben. Der Dienst schreibt nichts nach Sage.
- IDEAL, solange es dort keinen Bestell-Cache gibt (Rückfrage 12).
- Neuer Mail-Baukasten für die bestehenden Mails. Die Meldebestand-Mail ohne Text-Teil (`NotificationWorker.cs:65`) ist ein Zusatzfund
  und wird hier nicht mitgebaut.

## Fachliche Anforderungen

### Belegter Ausgangsstand (Code, Worktree `2026-08-07-ideal-teile-1-5`)

**Termin-Feld.** `SagePurchaseOrderLine.Liefertermin` (`IdealAkeWms/Models/SagePurchaseOrderLine.cs:43-44`, `DateTime?`, nur Datum).
- **Geschrieben** wird es nur vom Abgleich `EinkaufBestellungenSyncService`. Er liest das SData-Feld `Liefertermin` der Position und nimmt,
  wenn es leer oder unlesbar ist, `BelegLiefertermin` des Belegs: `var termin = Datum(z.Liefertermin) ?? Datum(z.BelegLiefertermin);`
  (`EinkaufBestellungenSyncService.cs:125`), gespeichert in `:146`. `Datum()` schneidet die Uhrzeit ab (`:206-208`). Unlesbare Werte zählt der
  Lauf als Warnung „unlesbarer Liefertermin“ (`:126`, `:167`).
- SData-Quelle: Ressource `EinkaufPositionenInfo` mit `where Belegkennzeichen eq 'EBB'` (`:31-35`). Die Feldtypen sind belegt
  (P-W1 in [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]]: `Liefertermin` dateTime, `BelegLiefertermin` dateTime).
- **Gelesen** wird es von der Maske „Offene Bestellungen“: Spaltenfilter (`PurchaseOrdersController.cs:43`), Horizont (`:82`),
  Sortierung (`:92`), Markierung überfällig `Liefertermin < heute` (`:131`).
- **Nicht belegt** ist die fachliche Bedeutung des Feldes in Sage, also ob es der bestätigte (AB-)Termin oder der Wunschtermin ist.
  Dafür gibt es die Prüfabfrage P-M1. Alle Aussagen dazu, wann eine Position fällig ist, gelten bis dahin als **unverifiziert**.

**Offene Menge.** Der Cache enthält nur offene Positionen (die SData-View liefert nur sie, `EinkaufBestellungenSyncService.cs:17-18`).
`MengeOffen = PositionGGBestellt − PositionGGGeliefert` ist in der Basiseinheit gespeichert (`:143`, `SagePurchaseOrderLine.cs:28-33`).
Wareneingänge, die im WMS gebucht, aber noch nicht in Sage sind, zieht die Rest-Regel ab: `Rest = Max(0, MengeOffen − Abzuziehen(SyncedAt))`
(`PurchaseOrderReceiveViewModel.cs:31`, `PurchaseOrderReceiptRepository.cs:20`).

**Stand.** Der Cache wird je Lauf komplett ersetzt (`EinkaufBestellungenSyncService.cs:171-175`). Liest SData 0 Zeilen, bleibt der alte Cache
stehen und `SyncedAt` ändert sich nicht (Empty-Guard `:82-87`). Ein Lesefehler wirft (`:71-77`). `GetStandAsync` liefert `MAX(SyncedAt)`
(`SagePurchaseOrderLineRepository.cs:39-40`).

**Kreditor-Adresse.** `Creditor.Email` (`Creditor.cs:46-49`, max. 200). Für Sage-Kreditoren schreibt sie der Kontokorrent-Abgleich aus
`Adressen.EMail` (`KontokorrentSyncService.cs:56`, `:289`). Ein leerer Sage-Wert überschreibt dabei nicht (`:271`), und es gibt keine
Formatprüfung, nur die Länge. Bei Sage-Kreditoren ist die Adresse in der Maske schreibgeschützt (`CreditorsController.cs:182-187`).
Die Bestellposition verweist über `A0Empfaenger` → `Creditor.SageKto` (`EinkaufBestellungenSyncService.cs:92-94`, `:118`), ohne FK.

**Mail-Infrastruktur.** `IMailService.SendAsync(subject, htmlBody, recipients, textBody, ct)` (`IMailService.cs:5`). `MailService` schreibt
alle Empfänger in `To` (`MailService.cs:43-44`). Absender ist `MailSettings:FromAddress`/`SenderName` aus appsettings (`:38-39`), eine
Antwortadresse und Anhänge gibt es nicht. Fehlen Empfänger oder SMTP-Host, kehrt der Dienst **still** zurück und loggt nur (`:20-32`),
der Aufrufer erfährt davon nichts. Vorlagen für HTML: `PartRequisitionEmailService.cs:140-209` (Kopf `#053153`, Linie `#43A6E2`) und
`NotificationWorker.cs:94-198`, eine tabellenbasierte, mailclient-feste Hülle mit Link über `Notifications:AppBaseUrl` (`:62`, `:170`).
Text-Teil nach Fallstrick „Mails immer multipart/alternative“ ([[fallstricke]] §5): Vorbild `WarehouseRequisitionEmailService.cs:64`.

**Logo.** Die Datei `Branding/IDEAL_AKE_Logo_rgb_klein.jpg` liegt im Repo, wird aber von keinem `.csproj` veröffentlicht
(Grep `Branding` in `*.csproj` = 0 Treffer). Das Web-Logo `wwwroot/images/ideal-ake-logo.svg` ist ein SVG und taugt für Mails nicht.

### F1 Fälligkeit
Eine Position ist **fällig**, wenn alle folgenden Bedingungen erfüllt sind:
1. `Liefertermin` ist gesetzt. Positionen ohne Termin werden nie fällig.
2. `heute ≥ Liefertermin + N`, mit N = `Lieferverzug:TageNachTermin` (Zählweise Rückfrage 2).
3. `Rest > 0`, berechnet nach der Rest-Regel (Rückfrage 8).

### F2 Einkauf-Sammelliste (entschieden)
- Höchstens eine Mail je Tag an `Lieferverzug:EinkaufEmpfaenger`, nur wenn mindestens eine Position fällig ist.
- Gruppiert nach Kreditor (Name + Kto), je Position: Bestellung (`Belegnummer`), Artikelnummer, Bezeichnung, bestellt, geliefert, Rest,
  Einheit, Liefertermin, Tage überfällig.
- Je Kreditor eine Statuszeile zur Kreditor-Mail: „gesendet an <Adresse>“, „TESTBETRIEB – an Einkauf umgeleitet“,
  „nächste Erinnerung am <Datum>“, „keine/ungültige Adresse“, „abgeschaltet“, „Lieferant gesperrt“, „kein Kreditor im WMS“,
  „Kreditor-Mails aus“.
- Link „Zu den offenen Bestellungen“ auf `<AppBaseUrl>/PurchaseOrders?nurUeberfaellig=true`, wenn `Notifications:AppBaseUrl` gesetzt ist.

### F3 Kreditor-Mail
- Eine Mail je Kreditor mit allen fälligen Positionen, die laut Wiederholungsregel (Rückfrage 4) heute dran sind.
- Inhalt (Rückfrage 11): Bestellnummer, Artikelnummer, Bezeichnung, bestellte und offene Menge, Einheit, Liefertermin und eine
  Bitte um Lieferung oder einen neuen Termin per Antwort. **Keine** internen Werte wie BelPosId, Lagerplatz oder WMS-Erfassung.
- Gesendet wird nur an genau eine gültige Adresse, die vorher mit `MailboxAddress.TryParse` geprüft wird. Ist die Adresse ungültig,
  geht keine Mail raus, und die Einkauf-Liste zeigt „ungültige Adresse“.

### F4 Testbetrieb (entschieden)
- `Lieferverzug:Testbetrieb = true` (Default): Jede Kreditor-Mail aus F3 geht **inhaltlich unverändert** an `Lieferverzug:EinkaufEmpfaenger`.
- Betreff: `TESTBETRIEB – wäre an <Kreditor-Name>, <Adresse> gegangen – <normaler Betreff>`. Fehlt eine Adresse, steht dort „(keine Adresse)“.
- Kopf: ein orangefarbener Banner `#ef7d00` mit demselben Text, nicht nur als Farbe, sondern ausgeschrieben.
- Ist `EinkaufEmpfaenger` leer, geht keine Mail raus. Der Lauf schreibt eine Warnung, und die Fehlermail läuft über `RunResilientAsync`
  nur bei einem echten Fehler, hier also nicht.
- Testbetrieb-Mails werden protokolliert (`Testbetrieb = 1`), zählen aber **nicht** für die Wiederholung echter Kreditor-Mails (Rückfrage 4).

### F5 Sicherheitsnetz
- **Stand zu alt:** Ist `MAX(SyncedAt)` des Caches älter als 24 h (oder der Cache leer), gehen keine Kreditor-Mails raus. Die Einkauf-Mail
  zeigt dann oben den Hinweis „Bestelldaten von <Stand> – Kreditor-Mails ausgesetzt“. Grund: Der Empty-Guard hält alte Daten fest, und ein
  Lieferant, der längst geliefert hat, würde sonst gemahnt.
- **Grenze je Lauf** `Lieferverzug:KreditorMaxJeLauf`: Wären mehr Kreditor-Mails fällig, geht keine davon raus. Das gilt nicht für den
  Testbetrieb. Der Lauf schreibt Warnung und Fehlermail, nach dem Muster `Sync:ReconcileMaxCancelPerRun`. Das schützt vor der ersten
  Mailwelle nach dem Abschalten des Testbetriebs (Anzahl vorab mit P-M3).
- **DryRun** (`WorkerSettings:SyncDryRun`): Der Lauf rechnet und protokolliert, versendet aber nichts und schreibt keine Protokollzeilen.

### F6 Zeitfenster und „einmal am Tag“
- Der Lauf startet im ersten Sync-Zyklus ab `Lieferverzug:Uhrzeit` (Stunde) an einem Versandtag (Rückfrage 3). Ob schon versendet wurde,
  steht in der Protokolltabelle (F7), nicht im Arbeitsspeicher. Ein Dienst-Neustart löst deshalb keine zweite Mail aus.
- Fällt der Abgleich „Offene Bestellungen“ im selben Zyklus aus, greift F5 über das Stand-Alter.

### F7 Protokoll
- Tabelle `PurchaseOrderReminders`: eine Zeile je versendeter Mail und Position, Felder siehe Migration. Daraus ergeben sich
  „heute schon gesendet“, die Wiederholung und der Nachweis.
- Aktivitäts-Protokoll: Lauf `LieferverzugMail` („Lieferverzug-Mails“) mit den Counts `faellig`, `kreditoren`, `einkauf_gesendet`,
  `kreditor_gesendet`, `testbetrieb_umgeleitet`, `ohne_adresse`, `abgeschaltet`, `gesperrt`, `nicht_dran`, `fehler`. Ohne Arbeit gibt es
  keinen Lauf-Eintrag, außerhalb des Zeitfensters ebenfalls keinen.

## Technischer Lösungsentwurf

Entlang der Leiter: kein neuer Worker, kein neuer Sage-Read, keine Template-Engine.

1. **Reiner Planer `LieferverzugPlaner`** (Muster „Entscheidung im Helper, Ausführung im Dienst“, [[services]] §Reine Entscheidungs-Helper).
   Eingabe: Cache-Zeilen, Rest je BelPosId, Kreditoren (Kto → Name, Adresse, aktiv, ggf. Abschaltung/Erinnerungsadresse), letzte echte
   Kreditor-Mail je (BelPosId, Liefertermin), Einstellungen, heute. Ausgabe: die Einkauf-Liste mit Statuszeilen und die Kreditor-Mails mit
   Ziel (echt oder Testbetrieb) und Grund. Alle Regeln aus F1, F3–F5 und den Rückfragen 2, 4, 8, 10 liegen hier und sind unit-getestet.
2. **Rest-Regel an EINER Stelle:** `ErfasstJeBelPos` bekommt `Rest(decimal mengeOffen, DateTime syncedAt)` als Funktion, und
   `PurchaseOrderReceiveViewModel.Rest` ruft sie auf. Der Planer nutzt dieselbe Funktion, die Daten liefert
   `PurchaseOrderReceiptRepository.GetErfasstJeBelPosAsync` mit dem Dienst-`ApplicationDbContext`. So gibt es keine zweite Kopie der
   Regel (Fallstrick: Maske und Mail laufen sonst auseinander).
3. **`LieferverzugMailHtml`** baut beide Mails als reine Funktion. Grundlage ist die tabellenbasierte Hülle aus `NotificationWorker.cs:105-195`.
   CI nach Backlog: Kopfbalken in der Hausfarbe des Standorts (AKE `#43a6e2`, IDEAL `#053153`), Fließtext `#4a494a`, Schrift
   `Arial, Helvetica, sans-serif` (Fallback für Nimbus Sans Novus), Fußzeile mit Firmenadresse in Hausfarbe und `www.ideal-ake.at`, Logo per
   CID. Kontrast: weiße Schrift auf `#43a6e2` erreicht nur rund 2,7:1. Deshalb steht auf AKE-Blau dunkle Schrift `#053153` (rund 5:1), oder AKE-Blau
   dient nur als Linie bzw. Akzent und der Text steht auf Weiß (WCAG AA, CLAUDE.md). Den Text-Teil erzeugt dieselbe Funktion, ohne Markup
   und mit nackter URL. Alle Werte gehen durch HtmlEncode.
4. **`IMailService` erweitern, nicht umbauen:** Die bestehende Methode bleibt, sodass sich alle Aufrufer und Moq-Setups nicht ändern.
   Neu ist `Task<bool> SendMitOptionenAsync(MailNachricht n, CancellationToken ct)`. Der Record `MailNachricht` hat Betreff, HTML, Text,
   Empfänger, Antwortadresse und eingebettete Bilder (Name, Bytes, MIME). Die Methode liefert `false`, wenn sie still überspringt
   (kein Host, keine Empfänger), damit nichts als „gesendet“ protokolliert wird. Die bestehende Methode delegiert intern an denselben Kern.
   Das Logo kommt über MimeKit `BodyBuilder.LinkedResources` (CID) und ist im Dienst als `EmbeddedResource` hinterlegt.
5. **`LieferverzugMailService.RunAsync(dryRun, ct)`** wird im `SyncWorker` nach dem Block `SyncWorker.cs:476-486` aufgerufen, Gate
   `Lieferverzug:MailAktiv` über `GetBoolSafeAsync`, gekapselt in `RunResilientAsync("Lieferverzug-Mails", …)`. Ablauf:
   Zeitfenster → schon gesendet? → Stand → Planer → Versand je Mail (ein Fehler bei einer Kreditor-Mail = Warnung + `fehler`, die übrigen
   laufen weiter) → Protokollzeilen nur nach `true` vom Mailservice → `FinishSuccess`. `ISyncLogger` ist der letzte Ctor-Parameter
   (ADR 0010), Name in `SyncLogServices.All` + `DisplayNames`.
6. **Einstellungen** stehen im Katalog `ServiceSettingDefinitions` (Basis, Meta, IntMeta, Choice für den Standort). Für die Adresslisten gibt
   es einen Kenner `MailListe` am `SettingDefinition`, damit `SettingValidator` jede Adresse beim Speichern prüft (ADR 0020 Punkt 2). Der
   Drift-Guard-Test deckt die neuen Keys ab.
7. Nur bei Rückfrage 5 = ja: `Creditor.ReminderDisabled` (bool) und `Creditor.ReminderEmail` (nvarchar 200, `[EmailAddress]`) als
   WMS-Felder in `ApplyWmsFelder` (`CreditorsController.cs:253-258`), also auch bei Sage-Kreditoren pflegbar. Edit und Details zeigen sie
   unter „Erinnerungen bei Lieferverzug“. Zieladresse: `ReminderEmail ?? Email`.

Rollen: keine neue Rolle. Die Einstellungen liegen unter `/ServiceSettings` (`[RequireAdminAccess]`, `ServiceSettingsController.cs:17`).
Die Kreditor-Felder liegen in der vorhandenen Bearbeitung (`[RequireMasterDataAccess]`, `CreditorsController.cs:166-176`).
Listen-View-Pattern: entfällt, es gibt keine neue Tabellenansicht. Die Protokolltabelle bekommt keine eigene Maske (YAGNI). Nachweis über
Aktivitäts-Protokoll und SQL. Erst wenn der Einkauf eine Historie in der Oberfläche braucht, wird sie eine eigene Spec.

## Migrations-/SQL-Auswirkungen

- Model `PurchaseOrderReminder` → `dotnet ef migrations add AddPurchaseOrderReminders` → `SQL/145_AddPurchaseOrderReminders.sql`
  (Migration `20261112143552_AddPurchaseOrderReminders`; SQL/144 ist mit dem System-Kürzel v1.99.9 belegt).
- Tabelle `dbo.PurchaseOrderReminders` (additiv):
  `Id` int IDENTITY PK, `Art` int NOT NULL (0 = Einkauf, 1 = Kreditor), `BelPosId` int NOT NULL, `Belegnummer` nvarchar(30) NOT NULL,
  `LieferantKto` nvarchar(20) NULL, `CreditorId` int NULL (ohne FK, wie im Cache), `Liefertermin` date NULL, `Rest` decimal(18,4) NOT NULL,
  `Empfaenger` nvarchar(1000) NOT NULL, `Testbetrieb` bit NOT NULL, `SentAt` datetime2 NOT NULL (Lokalzeit, `DateTime.Now`).
  Indizes: `(BelPosId, Art, SentAt)`, `(SentAt)`.
  Die Länge von `Belegnummer` richtet sich nach `SagePurchaseOrderLine.Belegnummer` in SQL/112. Der Dev-Lauf übernimmt sie von dort.
- Bei Rückfrage 5 = ja in **derselben** Migration: `Creditors.ReminderDisabled bit NOT NULL DEFAULT 0` (DEFAULT nur in Migration/SQL, nicht
  per EF `HasDefaultValue`, Fallstrick SData-Lesen „Sentinel“) und `Creditors.ReminderEmail nvarchar(200) NULL`.
- SQL-Skript: `OBJECT_ID`- bzw. `COL_LENGTH`-Guard, DDL in eigenem Batch, `__EFMigrationsHistory`-Insert in separatem Batch.
  `SQL/00_FreshInstall.sql` an beiden Stellen (Schema + MigrationId) nachziehen.
- Nicht daten-destruktiv. `SQL/AgentJobs/` ist nicht betroffen (der Kontokorrent-Abgleich schreibt die neuen Kreditor-Felder nicht, sie
  bleiben WMS-Felder).
- Seeds: keine. Die ServiceSettings seedet `Program.cs` aus dem Katalog (ADR 0008).
- Wachstum: Bei rund 100 Zeilen am Tag sind das etwa 36.000 im Jahr, deshalb keine Bereinigung. Eine Aufbewahrung wird bei Bedarf eigene Arbeit.

## Einstellungen

Alle in `/ServiceSettings`, Kategorie „Lieferverzug-Mail“, DB-first (ADR 0008), Server prüft beim Speichern (ADR 0020). Wirkung nur, wenn
`Lieferverzug:MailAktiv` an ist, und nur mit Daten aus `Sync:EinkaufBestellungenEnabled`. Die mit (R n) markierten Zeilen hängen an der
Rückfrage n, ihre Werte sind Vorschläge.

| Key | Anzeigename | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|---|
| `Lieferverzug:MailAktiv` | Lieferverzug-Mails | Bool | Schalter | `false` | Hauptschalter: Der Dienst prüft einmal am Tag offene Bestellpositionen auf Lieferverzug und versendet die Mails nach den Einstellungen unten. Aus = kein Lauf. | Dienst: Lauf „Lieferverzug-Mails“ nach „Offene Bestellungen (Sage)“ | AKE | „Versendet Mails, bei abgeschaltetem Testbetrieb auch an Lieferanten.“ |
| `Lieferverzug:TageNachTermin` | Tage nach Liefertermin | Int | 0–60, Einheit „Tage“ | `3` | Eine Position gilt als verzögert, wenn sie so viele Tage (R 2) nach ihrem Liefertermin noch einen Rest hat. | Dienst: Lauf „Lieferverzug-Mails“ | AKE | – |
| `Lieferverzug:Uhrzeit` (R 3) | Versand ab (Uhr) | Int | 0–23, Einheit „Uhr“ | `7` | Erster Sync-Zyklus ab dieser Stunde an einem Versandtag. | Dienst | AKE | – |
| `Lieferverzug:EinkaufAktiv` | Sammelliste an den Einkauf | Bool | Schalter | `true` | Tägliche Sammelliste aller verzögerten Positionen an die Einkauf-Adressen. | Dienst | AKE | „Versendet Mails.“ |
| `Lieferverzug:EinkaufEmpfaenger` | Einkauf: Empfänger | String (mehrzeilig, Mail-Liste) | gültige Adressen, kommagetrennt, Server prüft jede | `""` | Empfänger der Sammelliste **und** im Testbetrieb aller umgeleiteten Kreditor-Mails. Leer = keine Mail, Warnung im Lauf. | Dienst | AKE | – |
| `Lieferverzug:KreditorAktiv` | Erinnerung an Kreditoren | Bool | Schalter | `false` | Erinnerung je Kreditor mit seinen verzögerten Positionen an die Adresse aus dem Kreditor-Stamm. | Dienst | AKE | „Versendet Mails an externe Lieferanten (nicht rückholbar), sofern der Testbetrieb aus ist.“ |
| `Lieferverzug:Testbetrieb` | Testbetrieb (Kreditor-Mails an den Einkauf) | Bool | Schalter | `true` | An: Jede Kreditor-Mail geht unverändert an die Einkauf-Empfänger, Betreff und Kopf „TESTBETRIEB – wäre an … gegangen“. Aus: Mails gehen an die Kreditoren. | Dienst | AKE | „Ausschalten sendet ab dem nächsten Versandtag an externe Lieferanten.“ |
| `Lieferverzug:KreditorWiederholTage` (R 4) | Kreditor: erneut erinnern nach | Int | 1–60, Einheit „Tage“ | `7` | Abstand, in dem eine weiterhin verzögerte Position dem Kreditor erneut gemeldet wird. Ein geänderter Liefertermin beginnt neu. | Dienst | AKE | – |
| `Lieferverzug:KreditorMaxJeLauf` (R 4) | Kreditor: max. Mails je Lauf | Int | 1–500 | `30` | Sicherheitsgrenze: Wären mehr Kreditor-Mails fällig, geht keine raus, mit Warnung und Fehlermail. Gilt nicht im Testbetrieb. | Dienst | AKE | – |
| `Lieferverzug:AntwortAdresse` (R 7) | Antwortadresse | String (eine Mail-Adresse, Server prüft) | gültige Adresse oder leer | `""` | Reply-To der Kreditor-Mails, damit Antworten beim Einkauf landen. Leer = erste Einkauf-Adresse. Der Absender bleibt `MailSettings:FromAddress` (appsettings). | Dienst | AKE | – |
| `Lieferverzug:Standort` (R 7) | Absender-Firma (CI) | Choice | `AKE` = „AKE Ausseer Kälte- und Edelstahltechnik GmbH (AKE Blau)“, `IDEAL` = „IDEAL Kältetechnik GmbH (IDEAL Blau)“ | `AKE` | Bestimmt Hausfarbe, Firmenadresse und Signatur der Mails. | Dienst | beide (Wert je Installation) | – |

Bei Rückfrage 5 = ja zusätzlich die Kreditor-Felder „keine Erinnerungsmails“ und „Adresse für Erinnerungen“. Das sind Stammdaten, keine
Einstellungen.

## Audit-Feld-Auswirkungen

- `PurchaseOrderReminder`: Vorschlag **ohne** `AuditableEntity` (Rückfrage 13). Die Zeilen schreibt nur der Dienst, angehängt und nie geändert,
  wie `SagePurchaseOrderLine` (`SagePurchaseOrderLine.cs:6`, Begründung dort). Nachweis über `SentAt` und das Aktivitäts-Protokoll.
- `Creditor` (nur bei Rückfrage 5 = ja): Die vorhandene Bearbeitung setzt `Modified*` schon (`CreditorsController.cs:199` `SetModified`),
  die neuen Felder laufen über denselben Pfad.
- ServiceSettings: Hier gilt der vorhandene Speicherpfad unverändert.

## Akzeptanzkriterien

1. Ist `Lieferverzug:MailAktiv` aus, schreibt der Dienst keinen Lauf „Lieferverzug-Mails“ und versendet keine Mail.
2. Eine Position mit Liefertermin T, Rest > 0 und N = 3 wird am Tag T+3 fällig, am Tag T+2 nicht (Kalendertage; bei Arbeitstagen gemäß Rückfrage 2).
3. Positionen ohne Liefertermin, mit Rest = 0 (auch durch im WMS erfasste, noch nicht in Sage übergebene Mengen) und aus `EBZ` sind nie fällig.
4. Der Rest in der Mail stimmt für dieselbe Position mit der Spalte „Rest“ der Maske „Wareneingang zu Bestellung“ überein, weil beide
   dieselbe Funktion nutzen.
5. Die Einkauf-Mail kommt höchstens einmal je Kalendertag, auch nach einem Dienst-Neustart, und nur, wenn mindestens eine Position fällig ist.
6. Die Einkauf-Mail listet alle fälligen Positionen gruppiert nach Kreditor, mit Statuszeile je Kreditor (F2).
7. Im Testbetrieb geht keine Mail an eine Kreditor-Adresse. Jede Kreditor-Mail landet inhaltsgleich bei den Einkauf-Empfängern, mit
   „TESTBETRIEB – wäre an <Name>, <Adresse> gegangen“ im Betreff und als Text-Banner im Kopf.
8. Ist der Testbetrieb aus und `KreditorAktiv` an, erhält jeder Kreditor mit gültiger Adresse genau eine Mail mit genau seinen fälligen
   Positionen. Reply-To ist die Antwortadresse.
9. Eine Position, die dem Kreditor gemeldet wurde, wird erst nach `KreditorWiederholTage` erneut gemeldet. Ändert sich ihr Liefertermin, gilt
   sie als neu. Testbetrieb-Zeilen zählen dafür nicht.
10. Kreditor ohne, mit ungültiger oder (R 5) abgeschalteter Adresse, gesperrter Lieferant, inaktiver oder fehlender Kreditor: keine
    Kreditor-Mail, der Grund steht in der Einkauf-Liste und als Count.
11. Ist der Cache-Stand älter als 24 h, gehen keine Kreditor-Mails raus. Die Einkauf-Mail trägt den Hinweis mit Stand.
12. Wären mehr als `KreditorMaxJeLauf` echte Kreditor-Mails fällig, geht keine raus. Es gibt eine Warnung im Lauf und eine Fehlermail.
13. Überspringt der Mailservice still (kein SMTP-Host), entsteht keine Protokollzeile, und der Lauf meldet eine Warnung.
14. Die Mail ist multipart/alternative. Der HTML-Teil hat Logo (CID), Hausfarbe des gewählten Standorts, Firmenadresse und
    `www.ideal-ake.at`. Der Text-Teil hat keine HTML-Tags und enthält die nackte URL.
15. Text auf farbigem Grund erreicht mindestens 4,5:1 (AKE-Blau nie mit weißer Schrift).
16. `/ServiceSettings` zeigt die neuen Einstellungen in der Kategorie „Lieferverzug-Mail“ mit Anzeigename, Wirkung, Verwendung, Standort und
    Warnhinweis. Eine ungültige Adresse in `EinkaufEmpfaenger` oder `AntwortAdresse`, `TageNachTermin = 61` oder `Uhrzeit = 24` werden
    beim Speichern abgelehnt.
17. Im DryRun werden keine Mails und keine Protokollzeilen geschrieben, die Counts stehen im Aktivitäts-Protokoll mit „[DryRun]“.
18. Der Fehler einer einzelnen Kreditor-Mail (z. B. SMTP-Ablehnung) bricht den Lauf nicht ab. Die übrigen Mails gehen raus, die fehlerhafte
    zählt als `fehler`.
19. Build und alle Tests sind grün, einschließlich Drift-Guard `ServiceSettingDefinitionsTests` und neuen Planer-Tests für AK 2, 3, 5,
    7, 9–12.

## Test-Szenarien

Neues Kapitel **TS-128 „Lieferverzug-Mails (Einkauf, Kreditor, Testbetrieb)“** in `docs/TESTSZENARIEN.md`. Nummer geprüft und belegt (TS-128.1 – 128.15, im Dokument
ergänzt um 128.13 – 128.15 Negativfälle); `secondbrain/tests/testszenarien-index.md` ist nachgezogen.

Vorbedingungen: AKE-DEV mit `Sync:EinkaufBestellungenEnabled` an oder synthetischen Cache-Zeilen (Muster BelPosId 990001… aus TS-116),
SMTP auf ein Test-Postfach, Einkauf-Empfänger = eigenes Postfach, mindestens ein Kreditor mit Test-Adresse.

- **TS-128.1 Aus:** `MailAktiv` aus → kein Lauf im Aktivitäts-Protokoll, keine Mail.
- **TS-128.2 Fälligkeit:** Positionen mit Termin heute−2, heute−3 und heute−10, ohne Termin, sowie mit Rest 0 durch WMS-Erfassung,
  N = 3 → nur −3 und −10 in der Mail.
- **TS-128.3 Einkauf-Liste:** Uhrzeit = aktuelle Stunde → genau eine Mail, gruppiert nach Kreditor, mit Statuszeilen und Link. Dienst neu
  starten → keine zweite Mail am selben Tag.
- **TS-128.4 Testbetrieb:** `KreditorAktiv` an, `Testbetrieb` an → je Kreditor eine Mail im Einkauf-Postfach mit „TESTBETRIEB – wäre an …“;
  das Kreditor-Postfach bleibt leer.
- **TS-128.5 Echtbetrieb:** Testbetrieb aus, Kreditor-Adresse = Test-Postfach → Mail mit nur seinen Positionen, Reply-To = Antwortadresse;
  am nächsten Tag keine erneute Mail (Wiederholung 7 Tage), Termin in den Cache-Daten ändern → erneute Mail.
- **TS-128.6 Sonderfälle:** Kreditor ohne Adresse, mit ungültiger Adresse (`abc@`), gesperrter Lieferant, Position ohne Kreditor →
  keine Kreditor-Mail, Grund in der Einkauf-Liste.
- **TS-128.7 Stand alt:** `SyncedAt` aller Cache-Zeilen auf gestern−1 → Einkauf-Mail mit Hinweis, keine Kreditor-Mail.
- **TS-128.8 Grenze:** `KreditorMaxJeLauf = 1`, zwei Kreditoren fällig, Testbetrieb aus → keine Kreditor-Mail, Warnung + Fehlermail.
- **TS-128.9 Darstellung:** Mail in Outlook (Desktop + Web) und auf dem Handy: Logo sichtbar, Hausfarbe je `Standort` AKE/IDEAL, Kontrast,
  Text-Ansicht ohne Markup.
- **TS-128.10 Einstellungen (negativ):** ungültige Adresse, 61 Tage, Uhrzeit 24 → Ablehnung beim Speichern mit Meldung.
- **TS-128.11 DryRun:** `WorkerSettings:SyncDryRun` an → Counts mit „[DryRun]“, keine Mail, keine Zeile in `PurchaseOrderReminders`.
- **TS-128.12 (nur R 5 = ja) Kreditor-Felder:** Sage-Kreditor bearbeiten, „keine Erinnerungsmails“ setzen → keine Kreditor-Mail, Status
  „abgeschaltet“; Erinnerungsadresse setzen → Mail an diese statt an die Sage-Adresse.

Testbar ohne Mailserver: Planer- und HTML-Tests (Unit). Nur Manual-UAT: SMTP-Versand, Darstellung im Mailclient, Reply-To, CID-Logo.

## Etappen (nur bei epic: true)

Kein Epic, eine Umsetzung im Bündel-Worktree.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | entfällt (kein Epic) | – | – |

## Deploy

**Final (QA 2026-11-12, gegen den echten Diff `4c1b5523..dae22a49`):** web **ja** (`IdealAkeWms/`: Katalog, `SettingValidator`,
`MailAdressen`, Kreditor-Felder/Views, Hilfe/Changelog), service **ja** (`IDEALAKEWMSService/`: Lauf, Planer, Mail-HTML, `MailService`,
`SyncWorker`, Logo als EmbeddedResource), migration **ja** (`20261112143552_AddPurchaseOrderReminders`, SQL/145, additiv).
**Publish erledigt:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (IDEALAKEWMSWeb + IDEALAKEWMSWebService,
SQL, deploy; `publish-info.txt` Version 1.99.10, Commit `dae22a49df492eafa442028cbb4241fa4c67ba02`). Ablauf des Menschen: Publish aus dem
Worktree → Testsystem → Test → Merge. Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen
main-Änderungen zusammengeführt hat. Reihenfolge: DB-Backup → Dienst stoppen → SQL/145 (bzw. EF-Migrate beim ersten Web-Aufruf) → Web →
Dienst starten → `/ServiceSettings` prüfen (Hauptschalter aus, Testbetrieb an).
> [!warning] `MailSettings:PickupDirectory` in Produktion und auf den Testsystemen **NIE** setzen – dann geht keine Mail raus, sondern
> landet als `.eml` im Ordner. Nur DEV/QA. Vor dem Ende des Testbetriebs: P-M1 (Termin-Bedeutung) und P-M4 (Relay/Absender extern) durch
> den Menschen, dazu P-M3 (erste Welle).

*Ursprüngliche Planung (Spec-Agent):*

- **Web-App:** ja. Katalog `ServiceSettingDefinitions` und `SettingValidator` liegen im Web-Projekt, bei Rückfrage 5 = ja kommen die
  Kreditor-Masken dazu.
- **Service:** ja (neuer Lauf, `MailService`, SyncWorker).
- **Migration:** ja, `SQL/145_AddPurchaseOrderReminders.sql` (additiv, nicht destruktiv).
- **Reihenfolge:** Dienst stoppen → Schema → Web → Dienst starten. Danach `/ServiceSettings`: Der Hauptschalter steht aus, der Testbetrieb an.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023; Version 1.99.10, umgesetzt wie im Abschnitt „Umsetzung“ – Firma `beide`, Prüfung Phase `Nachher`):
  - `@{ Id='SQL/145'; Version='1.99.10'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch';
    Datei='145_AddPurchaseOrderReminders.sql'; MigrationId='20261112143552_AddPurchaseOrderReminders'; Voraussetzung='additiv';
    Quelle='2026-11-12-v1-99-10-lieferverzug-mail' }`
  - `@{ Id='v1.99.10-Lieferverzug'; Version='1.99.10'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Text='Neu in /ServiceSettings > Lieferverzug-Mail: Hauptschalter (Standard aus), Testbetrieb (Standard an), Einkauf-Empfaenger, Tage nach
    Liefertermin, Uhrzeit, Wiederholung, Grenze je Lauf, Antwortadresse, Absender-Firma. Erst Einkauf-Empfaenger eintragen, dann mit
    Testbetrieb einschalten. Testbetrieb erst nach Freigabe durch den Einkauf ausschalten (vorher Pruefabfrage P-M3).' }`
  - `@{ Id='v1.99.10-LieferverzugInfo'; Version='1.99.10'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Text='Einkauf informieren: taegliche Sammelliste verzoegerter Bestellpositionen; im Testbetrieb kommen auch die Lieferanten-Mails beim
    Einkauf an (Betreff TESTBETRIEB). Mail-Relay fuer externe Empfaenger mit der IT klaeren (P-M4).' }`
  - `@{ Id='Pruefung/LieferverzugEmpfaenger'; Version='1.99.10'; Typ='Pruefung'; Firma='AKE'; Phase='Haltepunkt'; Ausfuehrung='anzeigen';
    Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Lieferverzug:MailAktiv'' AND LOWER(LTRIM(RTRIM([Value]))) = N''true'')
    AND NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Lieferverzug:EinkaufEmpfaenger'' AND LEN(LTRIM(RTRIM([Value]))) > 0) THEN 1 ELSE 0 END';
    Text='Lieferverzug-Mails sind an, aber ohne Einkauf-Empfaenger: es geht keine Mail raus (auch keine Testbetrieb-Mail).' }`
  - Daten-/Einmal-Skripte: keine.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

### Prüfabfragen für den Menschen (Fremdsystem bzw. Daten, vom Agenten nicht ausgeführt)

**P-M1 (Sage-SQL AKE, vor Rückfrage 1): Was ist `Liefertermin`?** Die View heißt laut [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] P-W1
`dbo.vwDPSBS_CWS_Mde_EinkaufpositionenInfo`.
```sql
SELECT TOP 30 BelegnummerFormatiert, BelPosId, Liefertermin, BelegLiefertermin, PositionGGBestellt, PositionGGGeliefert
FROM dbo.vwDPSBS_CWS_Mde_EinkaufpositionenInfo
WHERE Belegkennzeichen = 'EBB' AND Liefertermin < GETDATE()
ORDER BY Liefertermin;

SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'vwDPSBS_CWS_Mde_EinkaufpositionenInfo'
  AND (COLUMN_NAME LIKE '%termin%' OR COLUMN_NAME LIKE '%woche%' OR COLUMN_NAME LIKE '%bestaet%' OR COLUMN_NAME LIKE '%AB%');
```
Danach drei der Positionen in der Sage-Belegerfassung öffnen: Entspricht `Liefertermin` dem Wunschtermin der Bestellung oder dem vom
Lieferanten bestätigten Termin? Gibt es ein eigenes Feld für den bestätigten Termin oder eine Lieferwoche?

**P-M2 (WMS-DB AKE, vor Rückfragen 5, 6, 10): Adressen und Land der Kreditoren mit offenen Bestellungen.**
```sql
SELECT c.Country, COUNT(DISTINCT c.Id) AS Kreditoren
FROM dbo.Creditors c JOIN dbo.SagePurchaseOrderLines l ON l.CreditorId = c.Id
GROUP BY c.Country ORDER BY 2 DESC;

SELECT COUNT(DISTINCT l.LieferantKto) AS ohneAdresse
FROM dbo.SagePurchaseOrderLines l LEFT JOIN dbo.Creditors c ON c.Id = l.CreditorId
WHERE c.Id IS NULL OR c.Email IS NULL OR LTRIM(RTRIM(c.Email)) = '';

SELECT c.SageKto, c.Email FROM dbo.Creditors c
WHERE c.Email LIKE '%;%' OR c.Email LIKE '%,%' OR c.Email LIKE '% %' OR c.Email NOT LIKE '%_@_%._%';
```

**P-M3 (WMS-DB AKE, vor dem Abschalten des Testbetriebs): Wie groß wäre die erste Welle?**
```sql
SELECT COUNT(DISTINCT LieferantKto) AS Kreditoren, COUNT(*) AS Positionen
FROM dbo.SagePurchaseOrderLines
WHERE Liefertermin <= DATEADD(day, -3, CAST(GETDATE() AS date)) AND MengeOffen > 0;
```
Das ist eine Näherung ohne Abzug der WMS-Erfassung. Die genaue Zahl nennt der Testbetrieb im Count `testbetrieb_umgeleitet`.

**P-M4 (IT, vor dem Echtbetrieb):** Darf der SMTP-Relay aus `MailSettings:SmtpHost` mit Absender `MailSettings:FromAddress` an externe
Domains zustellen (SPF/DMARC der Absenderdomain, Relay-Freigabe)? Der Code kann das nicht prüfen.

## Offene Rueckfragen

1. **Termin.** Belegt ist: Der Cache nimmt den Positions-Liefertermin, sonst den Beleg-Liefertermin (`EinkaufBestellungenSyncService.cs:125`).
   Nicht belegt ist, ob das in Sage der bestätigte oder der gewünschte Termin ist. *Empfehlung:* Das Cache-Feld unverändert verwenden,
   damit Maske und Mail dieselbe Wahrheit haben. P-M1 vor dem Abschalten des Testbetriebs ausführen. Gibt es ein eigenes Feld für den
   bestätigten Termin, wird das eine Folge-Spec mit Erweiterung des Abgleichs.
2. **Zählweise.** *Empfehlung:* Kalendertage, fällig ab `heute ≥ Liefertermin + N`. Das ist einfach und für Lieferanten nachvollziehbar.
   Wochenenden fängt Rückfrage 3 ab.
3. **Termin und Uhrzeit des Versands.** *Empfehlung:* Stunde einstellbar (Default 7 Uhr), Versand nur Montag bis Freitag. An
   Feiertagen wird trotzdem versendet, weil der Dienst die Feiertagstabelle hier nicht nutzt. Soll sie berücksichtigt werden, kostet das
   eine Abfrage mehr.
4. **Wiederholung.** *Empfehlung:* Einkauf täglich als Liste, solange überfällig. Kreditor beim ersten Fälligwerden, danach alle
   `KreditorWiederholTage` (Default 7). Ein geänderter Liefertermin gilt als neue Position. Testbetrieb-Mails zählen nicht. Dazu die
   Sicherheitsgrenze `KreditorMaxJeLauf` (Default 30) gegen die erste Welle nach dem Abschalten des Testbetriebs.
5. **Je Kreditor abschaltbar und eigene Erinnerungsadresse.** `Creditor.Email` stammt bei Sage-Kreditoren aus der allgemeinen
   Adresse (`Adressen.EMail`, `KontokorrentSyncService.cs:289`) und ist im WMS schreibgeschützt (`CreditorsController.cs:182-187`).
   *Empfehlung:* ja, zwei WMS-Felder am Kreditor („keine Erinnerungsmails“, „Adresse für Erinnerungen“), pflegbar auch bei Sage-Kreditoren.
   Das bedeutet zwei Spalten in derselben Migration. Bei Nein entfallen AK 10 (Teil „abgeschaltet“) und TS-128.12.
6. **Sprache.** Es gibt kein Sprachfeld. `Creditor.Country` ist Freitext aus Sage `LieferLand` (`KontokorrentSyncService.cs:287`), das
   Format ist unbelegt (P-M2). *Empfehlung:* eine Mail, oben Deutsch, darunter dieselbe Tabelle mit englischem Text („Reminder: overdue
   delivery“). Das braucht keine Spracherkennung und bleibt für NL-Lieferanten lesbar. Genderneutral, z. B. „Guten Tag“ statt
   „Sehr geehrte Damen und Herren“.
7. **Absender, Antwortadresse und Standort.** Der Absender ist heute für alle Mails `MailSettings:FromAddress` (appsettings, Default
   `wms@ake.at`, `MailService.cs:39`). *Empfehlung:* Der Absender bleibt so, Reply-To bekommt die Einstellung `Lieferverzug:AntwortAdresse`
   (Einkauf-Postfach), und das CI kommt aus der Auswahl `Lieferverzug:Standort` (AKE/IDEAL). Es wird nicht aus
   `ProduktionsauftragHierarchisch` abgeleitet, denn das ist ein Betriebsmodus und kein Firmenkennzeichen. Soll der Absender eine
   Einkauf-Adresse sein, muss die IT das Relay dafür freigeben (P-M4).
8. **Teillieferungen und Rahmenabrufe.** *Empfehlung:* Maßgeblich ist der Rest nach der Rest-Regel. Eine teilweise gelieferte Position mit
   Rest > 0 bleibt fällig, eine vollständig gelieferte fällt aus dem Cache. `EBZ` bleibt außen vor, weil es nicht im Cache steht.
9. **Protokoll.** *Empfehlung:* Tabelle `PurchaseOrderReminders` (eine Zeile je Mail und Position) plus Lauf im Aktivitäts-Protokoll. Ohne
   Tabelle sind die Wiederholung (4) und „einmal am Tag nach Neustart“ nicht machbar. Keine eigene Maske (Nachweis per SQL/Protokoll).
10. **Sonderfälle.** *Empfehlung:* In folgenden Fällen geht keine Kreditor-Mail raus, der Grund steht aber in der Einkauf-Liste: gesperrter
    Lieferant (`LieferantGesperrt`), inaktiver Kreditor, kein Kreditor im WMS, keine oder ungültige Adresse. Positionen ohne Termin erscheinen
    nicht in der Mail, sie sind in der Maske „Offene Bestellungen“ schon markiert.
11. **Inhalt und Einheit der Kreditor-Mail.** Die Mengen im Cache sind in der **Basiseinheit**, der Lieferant kennt aber die Bestelleinheit
    (`SagePurchaseOrderLine.cs:28-39`, `EinheitAbweichend`). *Empfehlung:* Menge mit Einheit `MengeBasiseinheit` anzeigen. Bei
    `EinheitAbweichend = true` zeigt die Kreditor-Mail statt der Menge „siehe Bestellung“, die Einkauf-Liste die Basismenge mit Hinweis.
    Keine Preise, keine Lieferanten-Artikelnummer (nicht im Cache).
12. **Standort.** Laut Katalog läuft der Bestell-Abgleich nur an AKE, IDEAL hat keinen SData-Endpunkt (`ServiceSettingDefinitions.cs:98`).
    *Empfehlung:* `standort: AKE`, der Code bleibt standortneutral (Auswahl CI), IDEAL folgt automatisch, sobald dort der Cache gefüllt wird.
13. **Audit der Protokolltabelle.** *Empfehlung:* kein `AuditableEntity`. Die Zeilen schreibt nur der Dienst, angehängt und nie geändert,
    wie beim Cache `SagePurchaseOrderLine` (`SagePurchaseOrderLine.cs:6`). `SentAt` in Lokalzeit (ADR 0010-Konvention).

## Freigabe-Nachtrag 2026-11-12 (Gerald): Rumpf-Änderungen aus den Antworten 7 und 12

Gilt vor dem Text oben.
- **Antwort 7:** Für die Kreditor-Mail gibt es eine eigene Einstellung „Absenderadresse Kreditor-Mail“ (leer = bisherige FromAddress); die Einkauf-Mail nutzt weiter die FromAddress. Serverseitig als E-Mail-Adresse validiert (ADR 0020).
- **Antwort 12:** `standort: beide`. Keine AKE-Sonderwege im Code; alle neuen Einstellungen je Standort. Auf IDEAL ist der Schalter bis zur SData-Schnittstelle aus; der Lauf ist ohne Bestell-Abgleich ohnehin wirkungslos (24-h-Regel).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Termin (Cache-Feld unverändert, P-M1 vor Echtbetrieb) → **Wie empfohlen (Gerald 2026-11-12).**
2. Zählweise (Kalendertage, heute ≥ Termin + N) → **Wie empfohlen (Gerald 2026-11-12).**
3. Versandzeit (Stunde einstellbar, Default 7, Mo–Fr) → **Wie empfohlen (Gerald 2026-11-12).**
4. Wiederholung (Einkauf täglich; Kreditor erstmals + alle 7 Tage, Termin-Änderung = neu, Grenze 30) → **Wie empfohlen (Gerald 2026-11-12).**
5. Kreditor-Felder „keine Erinnerungsmails“ + „Adresse für Erinnerungen“ (Migration) → **Wie empfohlen (Gerald 2026-11-12).**
6. Sprache (eine Mail Deutsch + Englisch) → **Wie empfohlen (Gerald 2026-11-12).**
7. Absender bleibt FromAddress, Reply-To-Einstellung, CI-Auswahl AKE/IDEAL → **Abweichend: Für die Kreditor-Mail eine eigene, einstellbare Absenderadresse (z. B. einkauf@…); Einkauf-Mail mit der bisherigen FromAddress. Reply-To einstellbar, CI-Auswahl AKE/IDEAL. Das SMTP-Relay (Versand an Externe, Absender erlaubt) prüft Gerald vor dem Ende des Testbetriebs (Gerald 2026-11-12).**
8. Rest > 0 zählt, EBZ außen vor → **Wie empfohlen (Gerald 2026-11-12).**
9. Protokolltabelle + Aktivitäts-Protokoll, keine Maske → **Wie empfohlen (Gerald 2026-11-12).**
10. Sonderfälle nur in der Einkauf-Liste → **Wie empfohlen (Gerald 2026-11-12).**
11. Basiseinheit, bei abweichender Einheit „siehe Bestellung“ → **Wie empfohlen (Gerald 2026-11-12).**
12. standort AKE → **Abweichend: beide Standorte. Alles je Standort über die Standort- bzw. Service-Einstellungen einstellbar (Schalter, Empfänger, Absender, CI). IDEAL startet aus und wird eingeschaltet, sobald die IDEAL-SData-Schnittstelle da ist, also heute oder morgen (Gerald 2026-11-12).**
13. Protokolltabelle ohne AuditableEntity → **Wie empfohlen (Gerald 2026-11-12).**

## Umsetzung (goal-Lauf 2026-11-12, v1.99.10)

**Worktree** `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`. Status bleibt (setzt der qa-agent).

- **Commits:** `37c831a4` (Lauf, Planer, Mail-HTML, MailService, Kreditor-Felder, Migration, SQL/145, Einstellungen, Tests, Manifest, Version),
  `dae22a49` (TS-128, Hilfe, Anwender-Changelog, README).
- **Version** 1.99.10 (beide `AppVersion.cs`). **SQL/145_AddPurchaseOrderReminders.sql**, Migration `20261112143552_AddPurchaseOrderReminders`
  (Tabelle `PurchaseOrderReminders`, `Creditors.ReminderDisabled`/`ReminderEmail`), FreshInstall Schema + MigrationId. Additiv.
- **TS-128** (128.1 – 128.15) in `docs/TESTSZENARIEN.md`, Index nachgezogen. AK-Verweise auf TS-128 bleiben gültig.
- **Einstellungen** (Kategorie „Lieferverzug-Mail“, alle Standort „beide“): wie Tabelle oben plus `Lieferverzug:KreditorAbsender`
  („Absenderadresse Kreditor-Mail“, eine Adresse, leer = `MailSettings:FromAddress`, Warnhinweis Relay). Adressfelder mit Serverprüfung über den
  neuen Kenner `SettingDefinition.Mail` (`Eine`/`Liste`) und die eine Regel `IdealAkeWms/Services/MailAdressen.cs`. `Lieferverzug:Standort` als Choice.
- **Manifest** `scripts/deploy/nachlauf.psd1`: `SQL/145` (Schema), `Pruefung/LieferverzugEmpfaenger` (Firma beide, Phase **Nachher**, anzeigen),
  `v1.99.10-Lieferverzug` (Einstellung, beide), `v1.99.10-LieferverzugInfo` (Info, beide). Drift-Test um `SQL/145` ergänzt.
- **Mail ohne Versand (DEV-QA):** neue appsettings-Option `MailSettings:PickupDirectory` – `MailService` legt jede Mail als `.eml` ab statt
  SMTP (Vorrang vor `SmtpHost`), README dokumentiert. MailKit hat kein Pickup; ~6 Zeilen über `MimeMessage.WriteToAsync`.
- **Build/Test:** `dotnet build IdealAkeWms.slnx` 0 Fehler; `IdealAkeWms.Tests` 2685 bestanden / 1 übersprungen; `IDEALAKEWMSService.Tests`
  589 bestanden (neu `LieferverzugMailTests`, 25 Fälle inkl. Pickup-`.eml`). Keine Mail an externe Adressen (Fake-Mailservice, `.invalid`).

### Befunde / Abweichungen (zur QA)

1. **Wiederholung im Testbetrieb:** Testbetrieb-Zeilen zählen für die Wiederholung **im** Testbetrieb, echte nur im Echtbetrieb (AK 9 gilt).
   Sonst bekäme der Einkauf im Testbetrieb täglich alle Kreditor-Mails. Begründung in [[fallstricke]] („Lieferverzug-Wiederholung …“).
2. **Einmal am Tag** = irgendeine `PurchaseOrderReminders`-Zeile von heute. Danach auch keine Kreditor-Mail für tagsüber neu fällige Positionen
   (strenge Lesart F6). Wird keine Mail versendet (z. B. nichts fällig, SMTP fehlt), prüft der nächste Zyklus erneut.
3. **DE+EN:** eine Tabelle mit zweisprachigen Spaltenköpfen, deutscher und englischer Text darüber – statt die Tabelle zu wiederholen (R 6).
4. `Belegnummer` nvarchar(**20**) wie `SagePurchaseOrderLines` (SQL/112), nicht 30 – so verlangt es die Spec selbst.
5. **Kontrast/CI:** AKE-Blau nur als Band mit Schrift `#053153` bzw. als Linie; Firmenname in der Fußzeile `#053153`, Adresse `#4a494a`
   (nicht „in Hausfarbe“, weil AKE-Blau auf Weiß 2,7:1). Testbanner `#ef7d00` mit schwarzer Schrift. Firmendaten aus `Branding/briefkopf.html:63-75`.
6. **Counts** zusätzlich zu F7: `kein_kreditor`, `inaktiv`, `ausgesetzt` (Stand alt + Grenze), `nicht_gesendet` (Mailservice übersprungen),
   `kreditor_geplant` (nur DryRun). Fehlermail bei Grenze nur außerhalb DryRun.
7. **Sonderfälle im Testbetrieb** wie im Echtbetrieb: ohne gültige Adresse keine Testbetrieb-Mail (Status in der Sammelliste). Der Betrefftext
   „(keine Adresse)“ ist vorhanden, wird so aber nicht ausgelöst.
8. Ist `EinkaufEmpfaenger` leer, schreibt jeder Zyklus im Zeitfenster eine Warnung (Melden statt still); Update-Skript-Prüfung meldet es nach dem Deploy.
9. `ServiceSettings.Value` ist nvarchar(500): die Einkauf-Empfängerliste ist damit auf 500 Zeichen begrenzt.
10. `IPurchaseOrderReceiptRepository` jetzt auch im Dienst registriert (Rest-Regel aus EINER Funktion `ErfasstJeBelPos.Rest`/`RestAus`, die Maske ruft `RestAus`).
11. Zusatzfund (nicht gebaut, wie Spec): Meldebestand-Mail ohne Text-Teil (`NotificationWorker.cs:65`).

**Nur Manual-UAT:** SMTP-Versand und Relay an Externe (P-M4), Absender/Reply-To im echten Client, Darstellung Outlook Desktop/Web/Handy
(CID-Logo, Farben), Prüfabfragen P-M1 – P-M3. Unit-getestet: Planer, HTML/Text, Lauf (Fake-Mail, InMemory), Pickup-`.eml`, Einstellungsprüfung,
Kreditor-Felder, Rest-Funktion.

## QA-Nachweis (qa-agent, 2026-11-12)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Commit-Range `4c1b5523..dae22a49` (37c831a4, dae22a49), HEAD `dae22a49`. Kein QA-Fix-Commit
nötig.

### 1 Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (vorbestehend).
- `dotnet test`: `IdealAkeWms.Tests` 2685 bestanden / 1 übersprungen / 0 Fehler (2686); `IDEALAKEWMSService.Tests` 589 bestanden / 0 / 0.

### 2 Code-Review (selbst, synchron)
Geprüft: `LieferverzugPlaner` (Fälligkeit, Wiederholungsschlüssel (BelPosId, Termin), Reihenfolge der Sonderfälle, Grenze alle-oder-keine nur
echte Mails, 24-h-Regel `jetzt − MAX(SyncedAt)`; beide Uhren `DateTime.Now`, `EinkaufBestellungenSyncService.cs:62`), `LieferverzugMailService`
(Gate/Zeitfenster/„heute schon gesendet“ über die Tabelle, Protokoll erst nach `true`, Einzel-Fehlerfang, DryRun ohne Mail/Zeile, Fehlermail nur
bei Grenze und nicht im DryRun, `ISyncLogger` letzter Ctor-Parameter), `MailService` (bestehende `SendAsync` delegiert unverändert; Pickup hat
Vorrang, sonst identischer SMTP-Pfad; außer dem Test-Fake kein weiterer `IMailService`-Implementierer), `LieferverzugMailHtml` (alle Werte
`HtmlEncode`, nur feste `<br>` in Kopfzellen; Kontrast AKE-Band #43a6e2/#053153 ≈ 5,0:1, Testbanner #ef7d00/#000 ≈ 7,6:1, IDEAL-Band weiß auf
#053153), Rest-Regel `ErfasstJeBelPos.Rest/RestAus` (Maske ruft `RestAus`, Dienst `Rest` – eine Formel), `MailAdressen` (eine Regel für
Validator, Kreditor-Maske und Versand), Kreditor-Felder (`ApplyWmsFelder`, Serverprüfung in `ValidateUniqueAsync`, Audit über `SetModified`),
Katalog/ADR 0020, SQL/145 + FreshInstall (2 Stellen), Manifest, `SyncLogServices.All` + `DisplayNames`.
Defektklassen der Vorlage: kein `<script>`-Embedding, kein `hidden` + `d-*`, keine stille `?? 0` (fehlender Rest = nicht fällig), keine parallelen
Arrays, Validierung serverseitig (Validator + Controller), Regel nicht dupliziert.

Befunde (keiner blockierend, keiner behoben – Bewertung):
- **B1 (Hinweis):** „Einmal am Tag“ sperrt auf irgendeine Protokollzeile von heute. Wirft nach erfolgreichen Kreditor-Mails der Versand der
  Sammelliste (SMTP-Fehler), kommt die Sammelliste an diesem Tag nicht mehr (Warnung + `fehler` im Lauf). Akzeptiert (Abweichung 2, strenge F6-Lesart).
- **B2 (Hinweis, kosmetisch):** Bei veraltetem Stand prüft der Planer Adresse/Sperre vor „Stand alt“; Kreditoren ohne Adresse zählen dann als
  `ohne_adresse`, nicht als `ausgesetzt` (DEV IDEAL: `ohne_adresse=98, ausgesetzt=0`). Es geht keine Kreditor-Mail raus, AK 11 ist erfüllt.
- **B3 (Hinweis Manual-UAT):** Die Sammelliste mit ~1000 Positionen hat ≈ 1,1 MB HTML (DEV AKE). Outlook zeigt das, Webmailer kürzen evtl. – in
  TS-128.9 im echten Client ansehen.
- **B4 (vorbestehend):** `ServiceSettings.Value` max. 500 Zeichen (Abweichung 9) – gilt für alle Listen-Einstellungen, nicht neu.

Bewertung der Abweichungen 1–9 aus „Umsetzung“: **1** fachlich richtig (sonst täglich alle Testmails), AK 9 bleibt erfüllt (echte Zeilen nur aus
dem echten Modus) – akzeptiert. **2** akzeptiert (s. B1). **3** DE+EN mit zweisprachigen Spaltenköpfen statt Tabellen-Wiederholung – gleiche
Aussage, kürzer, akzeptiert (in .eml belegt). **4** nvarchar(20) folgt der Quelle SQL/112 – richtig. **5** Kontrast-Begründung nachgerechnet –
richtig. **6** zusätzliche Counts sind „Melden statt still“ – akzeptiert. **7** ohne gültige Adresse keine Testbetrieb-Mail – konsistent mit dem
Echtbetrieb, akzeptiert. **8** Warnung je Zyklus bei leeren Empfängern – gewollt, durch die Manifest-Prüfung ergänzt. **9** vorbestehend (B4).

### 3 Beweisart je Akzeptanzkriterium
| AK | Beweis |
|---|---|
| 1 | Unit (Lauf-Tests) + DEV: MailAktiv aus → 0 Läufe (Ausgangszustand) |
| 2, 3 | Unit (`Faelligkeit_*`, `Lauf_ImWmsVollErfasst_*`); EBZ nicht im Cache (Spec belegt) |
| 4 | Unit (eine Funktion, `SageWareneingangGrundlageTests`) – Vergleich mit der Maske: Manual-UAT |
| 5 | Unit (InMemory) + **DEV SQL Server**: Dienst-Neustart 16:01, nächster Zyklus 16:05 → kein zweiter Lauf, keine neue .eml |
| 6 | Unit + DEV .eml (97 Gruppen mit Statuszeile, Link) |
| 7 | Unit + DEV .eml (92 Testbetrieb-Mails, alle `To: qa-einkauf@example.invalid`, Betreff + Banner „TESTBETRIEB – wäre an …“) |
| 8 | Unit (Fake-Mailservice) – echter Versand/Reply-To im Client: **Manual-UAT** |
| 9, 10, 12 | Unit (Planer); Grenze mit echter Fehlermail: Manual-UAT (ErrorNotification auf DEV aus) |
| 11 | Unit + DEV IDEAL (Stand 07.11. → Hinweis in der Sammelliste, 0 Kreditor-Mails) |
| 13, 17, 18 | Unit (Fake-Mailservice/InMemory) |
| 14 | Unit + DEV .eml (multipart/alternative, related + Logo `Content-Id: <logo-ideal-ake>`, Text ohne Tags, nackte URL) – Darstellung im Client: **Manual-UAT** |
| 15 | Code + Nachrechnung – Sichtprüfung im Client: Manual-UAT |
| 16 | Unit (`SettingValidatorTests`) + DEV Browser: `abc@` in „Einkauf: Empfänger“ → „Nicht gespeichert“, DB-Wert unverändert |
| 19 | erfüllt (Punkt 1) |

### 4 Geänderte bestehende Tests
Nur additiv, keine Zeile entfernt: `NachlaufManifestDriftTests` (Liste um `SQL/145`), `SageWareneingangGrundlageTests` (neue Asserts zur
Rest-Funktion), `CreditorsControllerTests`, `ServiceSettingDefinitionsTests`, `SettingValidatorTests` (neue Fälle). Kein „Sonstiges“.

### 5 Checkliste / 5a ADR 0020
Migration + SQL/145 (OBJECT_ID-/COL_LENGTH-Guard, History separat) ✓, FreshInstall Schema (Creditors-Spalten + Tabelle) und MigrationId ✓, Audit
(Creditor über `SetModified`, auf DEV `ModifiedBy` belegt; Protokolltabelle bewusst ohne, Antwort 13) ✓, beide `AppVersion` 1.99.10 + Anwender-
und Brain-Changelog ✓, TS-128 + Index ✓, Manifest (SQL/145, Prüfung, Einstellung, Info) ✓. ADR 0020: Bool-Schalter, Int mit Grenzen + Einheit,
Standort als Choice, Mail-Adressen serverseitig geprüft, Anzeigename/Wirkung/Verwendung/Default/Standort „beide“, Warnhinweise bei Außenwirkung
(MailAktiv, EinkaufAktiv, KreditorAktiv, Testbetrieb, KreditorAbsender) ✓ – in `/ServiceSettings` DEV AKE angesehen.

### 6 DEV-Deploy und Testlauf
- Standort `beide`. `dev-deploy.ps1 -Standort AKE|IDEAL -Quelle <Worktree>` (elevated): beide Exit 0, „HTTP 200, Fusszeile v1.99.10“ = AppVersion.
  Migration per EF auf beiden DEV-DBs; `SQL/145` danach zusätzlich auf AKE_WMS_DEV und IDEAL_WMS_DEV: Exit 0, keine Ausgabe (idempotent).
- Nach dem Deploy die 5 AKE-DEV-Sync-Schalter wieder `true` (Auftrag Gerald); `SageLagerbuchungAktiv`/`SageWareneingangBelegAktiv` false,
  `Sync:SageSystemKuerzel=DEV`.
- **Mail-Umleitung:** `MailSettings:PickupDirectory` in `D:\Dev\AKE\Service\appsettings.DEV.json` = `D:\Dev\mail-pickup\ake` und in
  `D:\Dev\IDEAL\Service\appsettings.DEV.json` = `D:\Dev\mail-pickup\ideal` (Sicherung `*.bak-qa-20261112`). **Bleibt gesetzt.** Vor dem Lauf
  belegt mit einer Probe (deployte `IDEALAKEWMSService.dll` + DEV-Konfiguration, Empfänger `.invalid`): je eine `.eml`, kein SMTP.
- Testlauf (Testbetrieb an, Empfänger `qa-einkauf@example.invalid`, Antwort `qa-antwort@example.invalid`, AKE-Absender `qa-absender@example.invalid`):

| Schritt | Ergebnis | Status |
|---|---|---|
| TS-128.1 Aus | vor dem Einschalten 0 Läufe `LieferverzugMail` auf beiden DEV | OK |
| TS-128.2 Fälligkeit | AKE faellig=1014 (Termin ≤ heute−3, Rest > 0); Gegenprobe Termin −2 nicht gezielt gebaut | OK (teilweise, Rest per Unit) |
| TS-128.3 Sammelliste + Neustart | AKE 1 Sammelliste (97 Lieferanten, Statuszeilen, Link `…/PurchaseOrders?nurUeberfaellig=true`); Neustart → kein zweiter Lauf | OK |
| TS-128.4 Testbetrieb | 92 Kreditor-Mails, alle an `qa-einkauf@example.invalid`, Betreff/Banner „TESTBETRIEB – wäre an <Name>, <Adresse> gegangen“, From = KreditorAbsender, Reply-To = Antwortadresse; Dienst-Log 93× „NICHT gesendet, sondern abgelegt“, 0× SMTP | OK |
| TS-128.5 Echtbetrieb | würde externe Adressen anschreiben | NICHT MÖGLICH (Mail-Grenze) |
| TS-128.6 Sonderfälle | 5× „keine Adresse“ in der AKE-Liste; gesperrt/ungültig nicht in den Daten | OK (teilweise) |
| TS-128.7 Stand alt | IDEAL (Stand 07.11.): Hinweis „Bestelldaten von 07.11.2026 19:25 – Kreditor-Mails ausgesetzt“, 0 Kreditor-Mails | OK |
| TS-128.8 Grenze | braucht Testbetrieb aus + Fehlermail | NICHT MÖGLICH |
| TS-128.9 Darstellung | .eml: AKE-Band #43a6e2/Schrift #053153, IDEAL-Band #053153/weiß, Logo per CID, Fuß Firma/Adresse/www, DE+EN, Text ohne Tags; Mailclient nicht geöffnet | OK (Struktur), Client offen |
| TS-128.10 Einstellungen negativ | `abc@` abgelehnt („Nicht gespeichert“), DB unverändert; 61/24 nur Unit | OK |
| TS-128.11 DryRun | nicht auf DEV | Unit |
| TS-128.12 Kreditor-Felder | Sage-Kreditor Id 3: „Adresse für Erinnerungen“ gespeichert, `ModifiedBy` gesetzt | OK |
| TS-128.13–15 | Unit | Unit |

- Danach `Lieferverzug:MailAktiv=false` auf beiden DEV-DBs (Testbetrieb an, Empfänger `.invalid` stehen gelassen). DEV-Testdaten: 2017 Zeilen
  `PurchaseOrderReminders` AKE, 1018 IDEAL; Kreditor 3 `ReminderEmail=qa-kreditor3@example.invalid`.

### 7 Publish
`<worktree>\publish` geleert und neu: IDEALAKEWMSWeb + IDEALAKEWMSWebService (Exit 0), `SQL\`, `deploy\`, `publish-info.txt` (1.99.10, `dae22a49df49…`).

## Manuelle Test-Checkliste (offen für den Menschen)
1. Testsystem: Publish einspielen (Update-Skript), `/ServiceSettings` > Lieferverzug-Mail: Hauptschalter aus, Testbetrieb an. **PickupDirectory nicht setzen.**
2. TS-128.3/128.4 auf dem Testsystem mit echtem SMTP an ein internes Postfach (Einkauf-Empfänger intern, Testbetrieb an).
3. TS-128.9: Mails in Outlook Desktop/Web und auf dem Handy öffnen (Logo, Farben AKE/IDEAL, Text-Ansicht); Größe der Sammelliste (B3) beurteilen.
4. TS-128.2 Gegenprobe Termin heute−2 und Rest = 0 durch WMS-Erfassung, Rest mit der Maske „Wareneingang zu Bestellung“ vergleichen (AK 4).
5. TS-128.5 Echtbetrieb nur mit eigener Kreditor-Testadresse – erst nach P-M4 (Relay/Absender extern, IT).
6. TS-128.8 Grenze mit Fehlermail (ErrorNotification an, Testbetrieb aus, Grenze 1) auf dem Testsystem.
7. TS-128.11 DryRun auf dem Testsystem.
8. P-M1 (Termin-Bedeutung in Sage) und P-M3 (erste Welle) **vor** dem Abschalten des Testbetriebs.
9. IDEAL: Schalter bleibt aus bis zur SData-Schnittstelle für offene Bestellungen.

## Keyuser-Antworten 2026-11-13 (Till Zöhrer, Einkauf) – Einordnung Koordination
Quelle: [[2026-11-12-keyuser-fragen-einkauf]] (Mail-Fragen 9–15). Ändert den umgesetzten Stand v1.99.10 nicht; offene Folgen unten.
- **Termin (R 1, P-M1):** Wunschtermin, bei abweichender AB in Sage angepasst → Cache-Termin passt „in der Regel“. Es gibt ein Feld für
  „bestätigt“ – P-M1 soll es benennen; ob nur bestätigte Positionen erinnert werden, entscheidet Gerald (Folge-Spec, Abgleich erweitern).
- **Tage:** `Lieferverzug:TageNachTermin` = **1** (statt Default 3). Reine Einstellung je Standort, kein Code. Till: Der Einkauf spricht nach der
  Meldung zuerst mit dem Lager. **Offen (Gerald):** Soll die Kreditor-Mail trotzdem schon ab Tag 1 gehen, oder braucht es eine eigene
  Schwelle für Kreditor-Mails (heute eine gemeinsame `TageNachTermin`)? Empfehlung Koordination: eigene Einstellung `KreditorTageNachTermin`
  (Default 3) – kleine Folgeänderung.
- **Adressen (R 7):** Absender/Antwort Kreditor-Mail AKE `Einkauf@ideal-ake.at`, IDEAL `einkauf_ideal@ideal-ake.at`
  (`Lieferverzug:KreditorAbsender` + `AntwortAdresse` je Standort; Relay P-M4 für den Absender prüfen).
- **Ausnahmen (R 5):** keine; Standard-Adresse aus Sage; Felder „keine Erinnerungsmails“/„Adresse für Erinnerungen“ bleiben als Möglichkeit.
- **Rahmenabrufe (R 8):** sollen erinnert werden. Heute nicht im Cache. Der Lieferverzug-Code filtert **nicht** selbst auf EBB (kein
  Treffer `EBZ|EBB|Belegkennzeichen` in `IDEALAKEWMSService/Services/LieferverzugPlaner.cs`, `LieferverzugMailService.cs`) → sobald
  [[2026-11-11-wareneingang-teil-3-rahmenabrufe]] EBZ in den Cache bringt, sind Abrufe automatisch dabei. Achtung: Abruf-Rest kennt nur
  WMS-Erfassungen; in Sage direkt gebuchte Abruf-Lieferungen würden falsch erinnert. Gehört in die Teil-3-Spec.
- **Echtbetrieb:** nach Sichtung der Testmails durch den Einkauf (Till).
