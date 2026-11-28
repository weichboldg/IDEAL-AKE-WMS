---
type: spec
title: "Lieferverzug-Mail: Tagessperre an der Sammelliste, Protokollfehler getrennt, Fehlermail höchstens einmal am Tag (LV-1 bis LV-3)"
slug: 2026-11-28-lieferverzug-mail-tagessperre-review-befunde-spec
status: Entwurf
created: 2026-11-28
updated: 2026-11-28
source_backlog: "[[2026-11-14-review-befunde-lieferverzug]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
version: "1.99.94 (vorläufig)"
testszenario: "TS-215 (vorläufig)"
affected_code:
  - IDEALAKEWMSService/Services/LieferverzugMailService.cs
  - IDEALAKEWMSService/Services/LieferverzugPlaner.cs
  - IDEALAKEWMSService.Tests/Services/LieferverzugMailTests.cs
  - IdealAkeWms/Views/Help/Index.cshtml
  - IdealAkeWms/AppVersion.cs
  - IDEALAKEWMSService/AppVersion.cs
  - docs/TESTSZENARIEN.md
open_questions:
  - "1 Abweichung 2 / QA-Befund B1 der Spec 2026-11-12 („einmal am Tag = irgendeine Protokollzeile von heute“, strenge F6-Lesart) wurde damals akzeptiert. Diese Spec hebt das auf: Der Tag gilt erst mit gesendeter Sammelliste als erledigt. Bestätigt?"
  - "2 (Fachbereich: Till Zöhrer) Nach einer Störung (z. B. Mailserver kurz weg) kommt die Sammelliste im nächsten Sync-Zyklus, also u. U. erst Stunden nach den Lieferanten-Mails. Ist das gewünscht, oder soll sie dann lieber erst am nächsten Morgen kommen?"
  - "3 Ohne Sammelliste (Lieferverzug:EinkaufAktiv aus oder keine Empfänger) gibt es keinen Tagesabschluss mehr: Ein Lieferant, der heute noch keine Mail hatte, kann sie auch am Nachmittag bekommen (z. B. nach geändertem Liefertermin). Höchstens eine Mail je Lieferant und Tag bleibt. Einverstanden?"
  - "4 LV-2: Mail gesendet, Protokoll scheitert. Empfehlung A: Lauf sofort abbrechen und melden; nach der DB-Störung geht diese eine Mail im nächsten Zyklus ein zweites Mal raus (Restrisiko). Variante B: Protokollzeile vor dem Versand schreiben und bei Versandfehler löschen (keine Doppelmail, dafür bei Absturz zwischen Zeile und Versand eine fehlende Erinnerung). A oder B?"
  - "5 Merker „heute schon gemeldet“ für die Fehlermail = ein Fehler-Eintrag (Level Error) des Laufs „Lieferverzug-Mails“ von heute im Aktivitätsprotokoll (Muster KommissionierNachtrag/LetzteBestellung). Dafür wird die Grenzen-Meldung von Warnung auf Fehler angehoben. Einverstanden?"
  - "6 Endet ein Lauf mit fehler > 0 (Kreditor-Mail oder Sammelliste mit Exception), schließt er als „Run fehlgeschlagen“ und schickt die Fehlermail (höchstens einmal am Tag). nicht_gesendet (Mailversand nicht konfiguriert) bleibt eine Warnung ohne Fehlermail, weil die Fehlermail denselben Mailservice nutzt. Einverstanden?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Lieferverzug-Mail: Tagessperre, Protokollfehler, Fehlermail (LV-1 bis LV-3)

Quelle: [[2026-11-14-review-befunde-lieferverzug]] (Sichtung 2026-11-28: LV-1, LV-2, LV-3 offen, Datei unverändert). Vorgänger-Specs:
[[2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec]] (v1.99.10, F6/F7), [[2026-11-13-lieferverzug-nachbesserung-keyuser-spec]] (v1.99.19).
Alle Zeilennummern unten beziehen sich auf den Hauptcheckout `main` (Bündel-Stand v1.99.89), nicht auf den alten Bündel-Worktree.

## Ziel / Nutzen (das Warum)

Die Lieferverzug-Mail geht an externe Lieferanten. Eine solche Mail lässt sich nicht zurückholen ([[fallstricke]] Z. 620). Heute kann
der Lauf auf zwei Arten danebengehen, ohne dass es jemand merkt:

- **Die Sammelliste fehlt bis morgen.** Scheitert sie nach erfolgreichen Kreditor-Mails, gilt der Tag trotzdem als erledigt. Der Einkauf
  erfährt nicht, dass Lieferanten angeschrieben wurden (LV-1).
- **Doppelte Mails an Lieferanten.** Scheitert das Protokoll nach dem Versand, wird die Mail als „Fehler“ gezählt und im nächsten Zyklus
  erneut gesendet (LV-2).
- **Viele Fehlermails am Tag.** Bei überschrittener Grenze ohne Protokollzeile geht in jedem Zyklus eine Fehlermail raus, bei 15-Minuten-Takt
  bis zu ~68 am Tag (LV-3).

Nach dieser Änderung ist der Tag erst erledigt, wenn die Sammelliste raus ist. Ein Lieferant bekommt höchstens eine Mail am Tag. Ein
Protokollfehler stoppt den Lauf, statt weitere Mails zu senden. Die Fehlermail kommt höchstens einmal am Tag.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- LV-1: Die Tagessperre hängt an der Sammelliste (`Art = Einkauf`), nicht mehr an irgendeiner Protokollzeile. Zusätzlich bekommt jeder
  Lieferant höchstens eine Kreditor-Mail am Tag (je Modus). Ein Lauf mit `fehler > 0` endet als „fehlgeschlagen“ und löst die Fehlermail aus.
- LV-2: Ein Protokollfehler nach dem Versand wird getrennt gefangen, als „gesendet, Protokoll fehlgeschlagen“ gemeldet und bricht den Lauf ab.
  Das Protokoll wird nach dem Versand ohne Abbruch-Token gespeichert.
- LV-3: Die Fehlermail des Laufs (Grenze, Fehler, Protokollfehler) geht höchstens einmal am Tag raus.
- Hilfe-Text, Tests, TS-215, Version.

**Out-of-Scope**
- Fehlermails von `RunResilientAsync` (`SyncWorker.cs:661-674`). Wirft der Lauf als Ganzes (z. B. DB beim Start nicht erreichbar), meldet
  das der Worker in jedem Zyklus. Das gilt für alle Sync-Schritte und ist nicht Lieferverzug-spezifisch.
- Throttling im `SyncErrorNotifier` selbst (`SyncErrorNotifier.cs:28-78`, keine Drosselung): würde alle Schritte betreffen.
- Q5 der Nachbesserungs-Spec (Einleitungssatz `LieferverzugMailHtml.cs:98`), Befunde B2/B3 der QA 2026-11-12.
- Wiederholung einer Kreditor-Mail, die mit SMTP-Fehler scheiterte (z. B. 550), bei geschlossenem Tag: bleibt wie heute bis morgen aus.
  Die Sammelliste zeigt „Fehler beim Versand: …“.

## Fachliche Anforderungen

### F1 Tagesabschluss = Sammelliste gesendet (LV-1)
- Ein Tag gilt als erledigt, wenn heute eine Protokollzeile `Art = Einkauf` existiert. Dann startet kein Lauf mehr (wie heute: kein
  Lauf-Eintrag, keine Mail).
- Kreditor-Zeilen von heute schließen den Tag **nicht** mehr. Fehlt die Sammelliste (Exception, stilles Überspringen, Abbruch nach der
  ersten Kreditor-Mail, Dienst-Stopp), versucht es der nächste Zyklus im Zeitfenster erneut.
- **Höchstens eine Kreditor-Mail je Lieferant und Tag:** Hat ein Lieferant (Sage-Konto) heute im aktuellen Modus (Testbetrieb bzw. echt)
  schon eine Kreditor-Mail bekommen, bekommt er heute keine weitere. Die Sammelliste zeigt dann die Statuszeile
  „Kreditor-Mail: heute bereits erinnert“. Ein Abbruch mitten in den Kreditor-Mails holt damit nur die fehlenden Lieferanten nach.
- Ist die Sammelliste aus oder hat sie keine Empfänger, schließt sich der Tag nie. Die Regel „höchstens eine Mail je Lieferant und Tag“
  verhindert dann Doppelmails (Rückfrage 3). Bei leerer Empfängerliste bleibt die Warnung je Zyklus wie bisher (Spec 2026-11-12,
  Abweichung 8).

### F2 Lauf mit Fehlern wird gemeldet (LV-1)
- Endet der Lauf mit `fehler > 0`, schließt er mit „Run fehlgeschlagen: …“ (Level Error) statt „Run erfolgreich beendet“. Dabei bleiben
  dieselben Counts. Der Lauf löst die Fehlermail aus, höchstens einmal am Tag (F4).
- `nicht_gesendet > 0` (Mailservice übersprungen, kein SMTP-Host) bleibt eine Warnung ohne Fehlermail (Rückfrage 6). Grund: Die Fehlermail
  geht über denselben `IMailService` (`SyncErrorNotifier.cs:72`), sie würde ebenso übersprungen.

### F3 Protokollfehler nach dem Versand (LV-2)
- Scheitert das Speichern der Protokollzeilen nach einer gesendeten Mail (Kreditor oder Sammelliste), gilt:
  - Die Mail zählt als gesendet. Der Lauf zählt `protokoll_fehler` und **sendet keine weitere Mail**, weder Kreditor noch Sammelliste.
  - Eintrag (Error): „Mail an <Empfänger/Lieferant> gesendet, Protokoll fehlgeschlagen: <Meldung> – Lauf abgebrochen.“
  - Abschluss mit „Run fehlgeschlagen“ und Fehlermail (F4).
- Wird der Dienst nach dem Versand gestoppt, wird das Protokoll trotzdem gespeichert. Ein Stopp-Signal (Abbruch-Token) bricht das
  Speichern nicht mehr ab.
- Restrisiko bei Variante A (Rückfrage 4): Ist die DB nach der Störung wieder da, geht die eine betroffene Mail im nächsten Zyklus ein
  zweites Mal raus.

### F4 Fehlermail höchstens einmal am Tag (LV-3)
- Merker: Gibt es heute schon einen Eintrag mit Level `Error` des Laufs `LieferverzugMail` im Aktivitätsprotokoll, sendet der Lauf heute
  keine Fehlermail mehr (Rückfrage 5).
- Grenze überschritten (`KreditorMaxJeLauf`):
  - Noch nicht gemeldet: Eintrag mit Level **Error** (bisher Warning) und eine Fehlermail. Der Fehler-Eintrag ist zugleich der Merker.
  - Schon gemeldet: kein eigener Lauf mehr nur wegen der Grenze. Läuft der Lauf aus einem anderen Grund (z. B. die Sammelliste fehlt noch),
    schreibt er die Grenze als Warnung, ohne Fehlermail.
- DryRun: keine Fehlermail (wie bisher, `LieferverzugMailService.cs:114`).

## Technischer Loesungsentwurf

Muster: „Entscheidung im Helper, Ausführung im Dienst“ ([[services]] §Lieferverzug-Mails). Der Merker im Aktivitätsprotokoll folgt dem
vorhandenen Muster „SyncLogs als Merker, keine eigene Tabelle“ (`KommissionierNachtragService.cs:68-70`, `LetzteBestellungSyncService.cs:76-78`).

### Ist-Stand (belegt)
- Tagessperre: `LieferverzugMailService.cs:68`, `PurchaseOrderReminders.AnyAsync(SentAt heute)` ohne Filter auf `Art`.
- Es gibt genau einen Schreiber der Tabelle, `ProtokollAsync` (`:206-218`): Kreditor-Zeilen `:139`, Einkauf-Zeilen `:158`. Die Suche
  nach `PurchaseOrderReminder` in `*.cs` findet sonst nur Modell, `ApplicationDbContext`, Migrationen und Tests. Nur der Dienst ruft den Lauf
  auf (`SyncWorker.cs:514-523`), einen Web-Auslöser gibt es nicht.
- Einkauf-Zeilen gibt es nur nach einer gesendeten Sammelliste. Sie haben immer `Testbetrieb = false` (`:158`, Parameter `false`). Je Lauf
  mit Sammelliste entsteht mindestens eine Zeile, denn `AnzahlFaellig > 0` ist durch `:99` garantiert (Positionen der Gruppen + „Zu
  klären“). Eine Einkauf-Zeile von heute heißt also: Die Sammelliste ist heute raus.
- Kreditor-Versand und Protokoll stehen im selben `try` (`:129-148`). Der `catch` (`:142`) zählt einen Protokollfehler als „Fehler beim
  Versand“. Dasselbe gilt für die Sammelliste (`:154-172`).
- `ProtokollAsync` speichert mit dem Lauf-Token (`:217`, `SaveChangesAsync(ct)`). `ct` ist das `stoppingToken` des Workers
  (`SyncWorker.cs:521`).
- Abschluss immer `FinishSuccessAsync` (`:176`). `FinishFailedAsync` schreibt Level `Error` (`SyncRun.cs:44-54`). `DisposeAsync` ohne Finish
  schreibt nur `Warning` (`SyncRun.cs:56-63`). Die Einträge schreibt `SyncRun` über eine eigene `IDbContextFactory` (`SyncRun.cs:78`), sie
  hängen also nicht am `_ctx` des Laufs. `SyncLog.Timestamp` ist Lokalzeit (`SyncLog.cs:10`).
- Heute schreibt der Lauf `LieferverzugMail` keinen Eintrag mit Level `Error`: Er nutzt nur `LogWarningAsync` (`:105`, `:108`, `:113`,
  `:136`, `:147`, `:164`, `:171`) und `FinishSuccessAsync`. Der neue Merker ist also nicht durch Altbestand vorbelegt.
- Fehlermail bei Grenze in jedem Lauf (`:109-115`). Ohne Protokollzeile wiederholt sich der Lauf wegen `:68`.
- `SyncErrorNotifier` drosselt nicht und nutzt denselben `IMailService` (`SyncErrorNotifier.cs:28-78`, `:72`).

### Änderungen
1. **`LieferverzugMailService.RunAsync`**
   - `:68`: Die Sperre prüft nur noch `r.Art == PurchaseOrderReminderArt.Einkauf && SentAt heute`.
   - Neu, nach der Sperre: `heuteAngeschrieben`, die Menge der `LieferantKto ?? ""` aus Kreditor-Zeilen von heute mit
     `Testbetrieb == e.Testbetrieb`. Sie geht an den Planer.
   - Neu: `heuteGemeldet = _ctx.SyncLogs.AnyAsync(Service == SyncLogServices.LieferverzugMail && Level == SyncLogLevel.Error && Timestamp
     heute)`.
   - `:99`: `plan.GrenzeUeberschritten` zählt nur noch, solange `!heuteGemeldet`. Ohne Sammelliste, ohne fällige Kreditor-Mail und ohne
     ungemeldete Grenze entsteht damit kein Lauf.
   - `:109-115`: Ist die Grenze noch nicht gemeldet, schreibt der Lauf `LogErrorAsync`, ruft (nicht im DryRun) `NotifyAsync` auf und setzt
     `heuteGemeldet = true`. Sonst schreibt er `LogWarningAsync`.
   - Kreditor-Schleife `:129-148`: Der Versand bleibt im bisherigen `try/catch`. Liefert `ok == true`, folgt das Protokoll in einem eigenen
     `try`. Wirft es, wird `protokollFehler` gesetzt, die Gruppe bekommt den Status „gesendet, Protokoll fehlgeschlagen – …“ (Grund
     `Fehler`), der Lauf schreibt `LogErrorAsync` und verlässt die Schleife mit `break`.
   - Sammelliste `:151-173`: Sie wird bei `protokollFehler` übersprungen. Ihr eigenes Protokoll fängt der Lauf genauso getrennt.
   - Abschluss `:176`: Bei `fehler > 0 || protokollFehler` ruft der Lauf `FinishFailedAsync(<Kurztext>, counts)` auf, sonst wie bisher
     `FinishSuccessAsync`. Danach kommt, falls `!dryRun && !heuteGemeldet`, ein `NotifyAsync("Lieferverzug-Mails", …)` mit demselben
     Kurztext. Neuer Count `protokoll_fehler`.
2. **`ProtokollAsync` `:217`:** `SaveChangesAsync(CancellationToken.None)`. Die Mail ist schon draußen, die Zeile muss gespeichert werden.
   Nach einem Protokollfehler ruft der Lauf kein weiteres `SaveChangesAsync` auf `_ctx` auf. Sonst würden die im Change-Tracker hängenden
   `Added`-Zeilen still mitgespeichert. Der Abbruch per `break` und das Überspringen der Sammelliste stellen das sicher.
3. **`LieferverzugPlaner.Planen`** (`:86-89`): Neuer optionaler Parameter `IReadOnlySet<string>? heuteAngeschrieben = null` am Ende, die
   Testaufrufe (`LieferverzugMailTests.cs:47`, `:64`, `:331`) bleiben gültig. In `Entscheiden` steht nach der Prüfung auf veralteten Stand
   (`:162`) und vor der Wiederholregel (`:182`) neu: Enthält die Menge `g.Kto ?? ""`, gilt
   `Setzen(g, LieferverzugGrund.NichtDran, Mit("heute bereits erinnert", zusatz), null)`. So zählt der Lieferant nicht für die Grenze
   (`:140`). `LieferverzugMailHtml` zeigt nur `Status` an (dort kein Bezug auf `LieferverzugGrund.`), deshalb ist keine HTML-Änderung nötig.
4. **Hilfe** `Index.cshtml:210-211` („höchstens einmal am Tag“) ersetzen durch: „Der Tag ist erledigt, sobald die Sammelliste gesendet ist.
   Nach einer Störung holt der nächste Lauf sie nach. Jeder Lieferant bekommt höchstens eine Mail am Tag.“ In `:222-224` ergänzen:
   „Fehlermail höchstens einmal am Tag.“
5. **Brain** (Dev-Lauf): Absatz Lieferverzug in [[services]] (Z. 369 ff.) um Tagesabschluss und Merker ergänzen. In der Spec 2026-11-12
   Abweichung 2/B1 als *überholt durch diese Spec* kennzeichnen, nicht löschen.

Abgelehnte Wege: eine neue `Art` in `PurchaseOrderReminders` als Merker für die Fehlermail. Die Pflichtspalten `BelPosId`/`Belegnummer`
(SQL/145 Z. 27-28) passen nicht zu einer Meldung. Ebenso abgelehnt: ein statischer Merker im Arbeitsspeicher. Er geht beim Neustart
verloren, und F6 der Vorgänger-Spec verlangt den Zustand ausdrücklich nicht im Arbeitsspeicher.

## Migrations-/SQL-Auswirkungen

Keine. Keine neue Spalte, keine Migration, kein SQL-Skript. Die vorläufig reservierte Nummer SQL/175 wird nicht gebraucht und bleibt frei.
Die vorhandenen Indizes tragen die neuen Abfragen: `IX_PurchaseOrderReminders_SentAt` (SQL/145 Z. 38). `SyncLogs` wird bereits von zwei
Diensten so gelesen (siehe oben). FreshInstall unberührt.

## Einstellungen

keine. Es kommt kein Schlüssel dazu und keiner ändert sich. `Lieferverzug:KreditorMaxJeLauf`, `ErrorNotification:*` und
`Lieferverzug:EinkaufAktiv` wirken wie bisher. Nur die Häufigkeit der Fehlermail ändert sich (F4).

## Audit-Feld-Auswirkungen

Keine. `PurchaseOrderReminders` hat bewusst keine Audit-Felder (SQL/145 Z. 5, Spec 2026-11-12 F7). `SyncLogs` hat ebenfalls keine. Keine
Rollen und keine Zugriffsfilter betroffen, keine neue Listenansicht (kein Listen-View-Pattern nötig).

## Akzeptanzkriterien

1. Ein Lauf sendet Kreditor-Mails, die Sammelliste wirft (SMTP-Exception). Der nächste Zyklus am selben Tag sendet die Sammelliste und
   keine Kreditor-Mail erneut. Danach gibt es heute genau einen Satz Einkauf-Zeilen.
2. Ein Lauf sendet Kreditor-Mails, die Sammelliste wird still übersprungen (`SendMitOptionenAsync == false`). Der nächste Zyklus versucht
   die Sammelliste erneut.
3. Nach gesendeter Sammelliste startet am selben Tag kein Lauf mehr, auch nach einem Neustart: kein Lauf-Eintrag, keine Mail. Der
   bestehende Test `Lauf_Testbetrieb_UmgeleitetProtokolliertEinmalAmTag` bleibt grün.
4. Ein Lieferant mit einer Kreditor-Zeile von heute im aktuellen Modus bekommt heute keine weitere Kreditor-Mail. In der Sammelliste steht
   bei ihm „heute bereits erinnert“. Testbetrieb-Zeilen sperren keine echte Mail und umgekehrt.
5. Abbruch nach der ersten von zwei Kreditor-Mails, im Test simuliert durch eine vorhandene Kreditor-Zeile von heute für Lieferant 1 ohne
   Einkauf-Zeile: Der Lauf sendet nur die Mail an Lieferant 2 und die Sammelliste.
6. Ein Lauf mit `fehler > 0` endet mit „Run fehlgeschlagen“ (`FinishedFailed`) und vollständigen Counts. Er ruft `NotifyAsync` genau einmal
   auf.
7. Ein Lauf mit `nicht_gesendet > 0` und `fehler = 0` endet „erfolgreich“ mit Warnung und ruft `NotifyAsync` nicht auf.
8. Gesendete Kreditor-Mail, `SaveChangesAsync` des Protokolls wirft: Die Statuszeile lautet „gesendet, Protokoll fehlgeschlagen …“
   (nicht „Fehler beim Versand“). Es geht keine weitere Kreditor-Mail und keine Sammelliste raus. Count `protokoll_fehler = 1`, Abschluss
   „Run fehlgeschlagen“, `NotifyAsync` einmal.
9. Das Protokoll speichert mit `CancellationToken.None`. Ein bereits abgebrochenes Lauf-Token nach dem Versand verhindert die Zeile nicht.
10. Grenze überschritten, Einkauf aus: Der erste Zyklus des Tages schreibt Level Error und ruft `NotifyAsync` einmal auf. Jeder weitere
    Zyklus am selben Tag erzeugt keinen Lauf-Eintrag und keine Fehlermail.
11. Grenze überschritten, die Sammelliste fehlt noch, die Grenze ist heute schon gemeldet: Der Lauf sendet die Sammelliste, schreibt die
    Grenze als Warning und ruft `NotifyAsync` nicht auf.
12. Gibt es heute schon einen Error-Eintrag `LieferverzugMail`, ruft ein Lauf mit `fehler > 0` `NotifyAsync` nicht auf. Er endet trotzdem
    „fehlgeschlagen“.
13. DryRun: keine Fehlermail, keine Protokollzeile, Abschluss wie bisher mit `[DryRun]` (bestehender Test `Lauf_DryRun_KeineMailKeineZeile`
    grün).
14. Die Hilfeseite beschreibt Tagesabschluss, „höchstens eine Mail je Lieferant und Tag“ und „Fehlermail höchstens einmal am Tag“.

## Test-Szenarien

**Unit** (`IDEALAKEWMSService.Tests/Services/LieferverzugMailTests.cs`, Fake-Mail, InMemory, `FakeSyncLogger`):
- Neu: AK 1, 2, 4, 5, 6, 7, 8, 10, 11, 12, dazu ein Planer-Fall „heute bereits erinnert“ ohne DB. Für den Merker legt der Test eine
  `SyncLog`-Zeile (`Service = LieferverzugMail`, `Level = Error`, heute) an, denn `FakeSyncLogger` schreibt keine `SyncLogs`. Für AK 8
  braucht es einen Kontext, dessen `SaveChangesAsync` wirft, z. B. per Interceptor oder abgeleitetem Testkontext. Der Dev-Lauf wählt den
  kleinsten Weg, den `TestDbContextFactory` zulässt.
- Anpassen: `Lauf_Grenze_KeineKreditorMail_Fehlermail` (`:483-491`) erwartet `Level == "Error"` statt `"Warning"`.
  `Lauf_FehlerEinerKreditorMail_UebrigeLaufen` (`:458-468`) erwartet zusätzlich `FinishedFailed` und `NotifyAsync` einmal, die übrigen
  Erwartungen bleiben.

**TS-215 (vorläufig) in `docs/TESTSZENARIEN.md`** – „Lieferverzug-Mails: Tagesabschluss an der Sammelliste, Fehlermail einmal am Tag
(v1.99.94)“, Querverweis TS-128:
- Vorbedingung wie TS-128: `MailSettings:PickupDirectory` gesetzt (DEV legt `.eml` ab, nichts geht an Externe). **Vor jedem Schritt mit
  `Testbetrieb = false` prüfen, dass PickupDirectory gesetzt ist.** `PurchaseOrderReminders` des Tages löschen.
- 215.1 Sammelliste fehlt: `EinkaufEmpfaenger` leeren, Lauf abwarten (Kreditor-Testmails nicht gesendet, Warnung). Empfänger wieder
  eintragen, nächster Zyklus: Sammelliste kommt, `Art 0`-Zeilen von heute vorhanden. Folgezyklus: kein Lauf.
- 215.2 Abbruch simulieren: Nach einem Lauf die `Art 0`-Zeilen von heute löschen und die `Art 1`-Zeilen behalten. Nächster Zyklus: Nur die
  Sammelliste kommt, darin steht „heute bereits erinnert“, keine neue Kreditor-`.eml`.
- 215.3 Grenze einmal gemeldet: `Testbetrieb = false`, `KreditorMaxJeLauf = 1`, `EinkaufAktiv = false`, `ErrorNotification` an (Pickup).
  Erster Zyklus: ein Error-Eintrag und eine Fehlermail-`.eml`. Zwei weitere Zyklen: kein Lauf-Eintrag, keine weitere `.eml`. Danach die
  Einstellungen zurücksetzen.
- 215.4 SMTP-Fehler nur der Sammelliste und Protokollfehler sind auf DEV nicht erzeugbar (Befund-Nachmessung 2026-11-14). Nachweis nur
  per Unit-Test, im Testbericht so vermerken.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja, nur für den Hilfe-Text (`Views/Help/Index.cshtml`) und `AppVersion`. Ohne Hilfe-Änderung wäre es nein.
- **Service:** ja (`LieferverzugMailService`, `LieferverzugPlaner`).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine. Kein SQL-Skript, keine Einstellung, kein Leitstand-Schritt, kein Ordner.
  Das geänderte Verhalten steht in Hilfe und Anwender-Changelog.
- **Publish-Befehle** (im Worktree):
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- **Nummern (vorläufig, der Dev-Lauf prüft sie neu):** Version 1.99.94 (beide `AppVersion.cs`), TS-215, kein SQL (175 nicht belegt).

## Offene Rueckfragen

1. Abweichung 2 / QA-Befund B1 der Spec [[2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec]] (Z. 518-519, 560-561: „einmal am Tag
   = irgendeine Protokollzeile von heute“, strenge F6-Lesart) wurde am 2026-11-12 akzeptiert. Diese Spec hebt das auf: Der Tag gilt erst mit
   gesendeter Sammelliste als erledigt (F1). Bestätigt?
2. (Fachbereich: Till Zöhrer) Nach einer Störung (z. B. Mailserver kurz weg) kommt die Sammelliste im nächsten Sync-Zyklus, also unter
   Umständen erst Stunden nach den Lieferanten-Mails. Ist das gewünscht, oder soll sie dann lieber erst am nächsten Morgen kommen?
3. Ohne Sammelliste (`Lieferverzug:EinkaufAktiv` aus oder keine Empfänger) gibt es keinen Tagesabschluss mehr. Ein Lieferant, der heute noch
   keine Mail hatte, kann sie dann auch am Nachmittag bekommen, z. B. nach einem in Sage geänderten Liefertermin. „Höchstens eine Mail je
   Lieferant und Tag“ bleibt. Einverstanden?
4. LV-2, Mail gesendet und Protokoll gescheitert. *Empfehlung A:* den Lauf sofort abbrechen und melden (F3). Nach der DB-Störung geht diese
   eine Mail im nächsten Zyklus ein zweites Mal raus (Restrisiko). *Variante B:* die Protokollzeile vor dem Versand schreiben und bei einem
   Versandfehler löschen. Das vermeidet die Doppelmail, kehrt aber F7 („Zeile erst nach `true`“) um: Stürzt der Dienst zwischen Zeile und
   Versand ab, fehlt eine Erinnerung, bis die Wiederholfrist abläuft. A ist der kleinere Eingriff und lässt F7 unberührt. A oder B?
5. Merker „heute schon gemeldet“ für die Fehlermail = ein Eintrag mit Level Error des Laufs „Lieferverzug-Mails“ von heute im
   Aktivitätsprotokoll (Muster KommissionierNachtrag/LetzteBestellung). Dafür wird die Grenzen-Meldung von Warnung auf Fehler angehoben.
   Einverstanden?
6. Endet ein Lauf mit `fehler > 0` (Kreditor-Mail oder Sammelliste mit Exception), schließt er als „Run fehlgeschlagen“ und schickt die
   Fehlermail (höchstens einmal am Tag). `nicht_gesendet` (Mailversand nicht konfiguriert) bleibt eine Warnung ohne Fehlermail, weil die
   Fehlermail denselben Mailservice nutzt. Einverstanden?

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)
<!--
  Von der Koordination eingetragen, wortgetreu, je Antwort:
  <Nr>. <Name>, <yyyy-mm-dd>, [<Jira-Schluessel> Kommentar](<Link>): "<Antwort>"
  Das ist fachlicher Input, keine Freigabe. Entscheidend bleibt der Freigabe-Block darunter;
  uebernimmt der Mensch eine Antwort, genuegt dort "wie Fachbereich".
-->

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Abweichung 2/B1 aufheben, Tagesabschluss an der Sammelliste →
2. (Fachbereich: Till Zöhrer) Sammelliste nach Störung im nächsten Zyklus statt am nächsten Morgen →
3. Ohne Sammelliste kein Tagesabschluss, höchstens eine Mail je Lieferant und Tag →
4. LV-2 Variante A (abbrechen, Restrisiko eine Doppelmail) oder B (Vorab-Protokoll) →
5. Merker = Error-Eintrag im Aktivitätsprotokoll, Grenze als Error →
6. fehler > 0 = „Run fehlgeschlagen“ + Fehlermail, nicht_gesendet bleibt Warnung →
