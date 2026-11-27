---
type: aufgabe
status: offen
created: 2026-11-26
version: 1.99.80
source: "[[2026-11-26-besprechung-0830-kritische-pruefung]]"
lauf: "[[2026-11-25-kommission-lagerplatz-bde-goal-lauf]]"
---
# Lösungsvorschläge Deploy, Sage und Doku (für die Besprechung)

Entscheidungsvorlagen zu den Punkten B-1, B-3, B-4, S-1 bis S-8, H-3, H-7, H-9, F-3, F-4 und F-11 aus
[[2026-11-26-besprechung-0830-kritische-pruefung]]. F-14 steht unter S-6, F-16 unter S-7, H-C unter S-3. Grundlage ist der Lauf
[[2026-11-25-kommission-lagerplatz-bde-goal-lauf]].

> [!info] Rahmen
> - Nur recherchiert, **kein Code geändert**. Gelesen wurde der Bündel-Worktree `2026-08-07-ideal-teile-1-5` (HEAD `3c5a97df`, v1.99.80).
> - Abgefragt wurde nur lesend: die DEV-DBs `AKE_WMS_DEV` und `IDEAL_WMS_DEV` (Instanz `.`) sowie `ake_TEST2026` (SELECT).
> - Prod wurde **nicht** abgefragt. Alle Prod-Abfragen stehen fertig unten im Abschnitt [[#Prüfabfragen für Prod (Gerald)]].
> - Die BDE- und UI-Punkte (U-1…U-14, H-A, H-B, H-4, H-6, F-20/21/22/29/30) bearbeitet ein anderer Lauf und fehlen hier.

> [!warning] Korrektur zur Besprechungsnotiz (S-6/F-14)
> Die Abbildung `Y:\` → `\\idealfs1\Daten$\Produktion\CAM\` ist **keine Annahme**, sondern mit Antwort 18 entschieden (Spec
> `2026-11-20-bde-liste-isolierungsfraese-pfad-material`, Z. 165 und 491). Offen sind nur die Rückfragen 15 und 16. Siehe S-6.

---

## MUSS

### B-1 Kommissionier-Nachtrag: Default an und Sofortlauf beim ersten Dienststart

**Problem.** `Sync:KommissionierNachtragEnabled` hat den Default `"true"` (`IdealAkeWms/Models/ServiceSettingDefinitions.cs:292`).
Nach dem Deploy fehlt der Marker „Run gestartet“. `LetzteBestellungPlaner.Faellig` gibt bei `letzterStart == null` sofort `true`
zurück (`IDEALAKEWMSService/Services/Sdata/LetzteBestellungSyncService.cs:18-26`). Damit läuft der Massenlauf beim ersten Zyklus tagsüber,
synchron im SyncWorker, und die anderen Syncs warten (`IDEALAKEWMSService/Workers/SyncWorker.cs:549-554`). Der Manifest-Text kündigt
genau das an (`scripts/deploy/nachlauf.psd1:667-669`).

**Fallstrick dabei:** Ein Ausschalten über `/ServiceSettings` *vor* dem Update geht nicht. Die alte Version kennt den Schlüssel nicht
und zeigt ihn deshalb nicht an. Nach dem Update legt der erste Web-Start die fehlende Zeile mit dem Default `true` an
(`IdealAkeWms/Program.cs:286-300`, vorhandene Zeilen bleiben unberührt). Den Wert `false` gibt es also nur auf zwei Wegen: per SQL
vor dem Update oder per Code.

| | Option | Dateien | Aufwand | Risiko | Test |
|---|---|---|---|---|---|
| A | **Nur Checkliste.** Vor dem Update je Prod-DB die Zeile per SQL mit `false` anlegen (Abfrage P-3 unten). Abends in `/ServiceSettings` einschalten. Der Lauf startet dann sofort und wird im Protokoll beobachtet. | keine (Checkliste, `docs/betrieb/03` §8) | S | Der Schritt wird vergessen, dann läuft der Massenlauf tagsüber. Außerdem ein Schreibzugriff auf Prod von Hand. | Nach dem Deploy: `SELECT [Value] … WHERE [Key]=N'Sync:KommissionierNachtragEnabled'` = `false`, kein SyncLog „Kommissionier-Nachtrag“ |
| B | **Default `false`** (eine Zeile `:292`). Dazu ein Nachher-Eintrag `Pruefung/KommNachtragAus` im Manifest nach dem Muster von `Pruefung/BlechteileNachleseAus` (`nachlauf.psd1:322-325`) und angepasste Texte (`ServiceSettingDefinitions.cs:169-171`, `nachlauf.psd1:667-669`, `docs/betrieb/04`). Eingeschaltet wird abends. | `ServiceSettingDefinitions.cs`, `nachlauf.psd1`, Doku, Test der Default-Erwartung | S | Wird das Einschalten vergessen, bleibt die Funktion aus. Die Nachher-Prüfung meldet das bei jedem Update sichtbar. | Unit-Test Default; Update-Skript `-NurPruefen` auf DEV zeigt die Prüfung; einschalten → ein Lauf im Protokoll |
| C | **Default bleibt an, aber ohne Marker kein Sofortlauf.** `letzterStart ??= Dienst-Startzeit` in `KommissionierNachtragService.RunWennFaelligAsync` (`:58-74`). Der erste Lauf kommt dann zum nächsten regulären Termin (02:00). Für einen Lauf vorher bleibt der Admin-Knopf im Leitstand. | `KommissionierNachtragService.cs`, Tests `KommissionierNachtragServiceTests.cs:93-106`, Texte wie B | M | Der erste Lauf läuft unbeobachtet nachts. Die Zähler `fehler` und `altbestand_pruefen` sieht erst der Leitstand am Morgen. Die Logik ist neu, mit eigenem Test. | Unit-Test „ohne Marker, 10:00 → nicht fällig; 02:00 Folgetag → fällig“ |

**Empfehlung: B.** Es ist eine Zeile plus ein bekanntes Manifest-Muster. Das Deploy ist damit an beiden Standorten von selbst sicher,
und niemand muss vorher auf Prod schreiben. Das Einschalten passiert bewusst abends, der Sofortlauf wird beobachtet, wie in der
Besprechungsnotiz vorgeschlagen. Kommt v1.99.81 nicht vor dem Fenster, ist A der Ersatz: P-3 wird dann Pflichtschritt der Checkliste.

> [!question] Frage an Gerald
> Wie soll der neue nächtliche Kommissionier-Nachtrag nach dem Update auf Prod starten?
> 1. **(empfohlen)** Standardmäßig aus. Die IT schaltet ihn am Abend nach dem Update ein, er läuft dann einmal sofort und wird beobachtet. Das Update-Skript meldet, solange er aus ist.
> 2. Standardmäßig an, aber der erste Lauf erst in der folgenden Nacht um 02:00 (nicht sofort).
> 3. Code bleibt wie er ist. Vor dem Update wird der Schalter per SQL auf „aus“ gesetzt (Handschritt an beiden Prod-DBs).

### B-3 `Pruefung/BdeNurFaMeldungAn` bricht das Prod-Update vor dem Stoppen ab

**Problem.** Die Prüfung (`scripts/deploy/nachlauf.psd1:228-231`, Phase Vorher, beide Standorte) zählt
`AppSettings.BdeNurFaMeldung IN ('true','1','on')`. Ist der Wert auf Prod größer als 0, landet die Prüfung in `$sperre`, und das
Skript bricht vor dem Stoppen ab (`scripts/deploy/Update-IdealAkeWms.ps1:705-706`). Das ist so gewollt: Laut Manifest-Text kann der
Buchungsdienst bei eingeschalteter Einstellung Starts abweisen (Antwort 22). Drei weitere Vorher-Prüfungen wirken genauso, darunter
`Pruefung/FreshInstallIndizesDuplikate` (nur IDEAL-Prod, `nachlauf.psd1:174-177`).

| | Option | Dateien | Aufwand | Risiko | Test |
|---|---|---|---|---|---|
| A | **Vorab prüfen, nichts ändern.** Heute auf beiden Prod-Servern `Update-IdealAkeWms.ps1 -NurPruefen` ausführen. Das zeigt den Plan und alle Vorher-Prüfungen und stoppt nichts (`docs/betrieb/03` Z. 145, 154). Steht dort ein Wert über 0: mit dem Fachbereich klären und vor dem Fenster in `/Settings` ausschalten. Ersatzweise P-2 unten. | keine | S | keins. Man weiß vorher, ob das Fenster hält. | Ausgabe `-NurPruefen`: `[Pruefung/BdeNurFaMeldungAn] 0` |
| B | Die Prüfung von Vorher auf Nachher herabstufen (nur Hinweis). | `nachlauf.psd1` | S | Das Update läuft dann mit eingeschalteter Einstellung durch, und am Morgen weist der Buchungsdienst womöglich Starts ab. | `-NurPruefen` auf DEV |
| C | Die Prüfung entfernen und die Altlast im Code beseitigen. | BDE-Buchungsdienst | M–L | Das berührt die Alt-Zweige des Buchungsdiensts, ohne Spec. | – |

**Empfehlung: A.** Die Sperre ist das gewünschte Verhalten („melden statt still behandeln“). Fehlen würde nur, dass man sie vorher
sieht. `-NurPruefen` deckt alle Vorher-Prüfungen auf einmal ab, also auch FreshInstallIndizesDuplikate, MailPickup,
BenutzerOhneKennwort und FaStrukturSchluesselDoppelt.

**Nebenbefund Doku:** `docs/betrieb/03-installation-update-rollback.md:215` zitiert `nachlauf.psd1:127-130,154-161,235-238`. Richtig
ist `:228-231` für BdeNurFaMeldungAn und `:174-177` für FreshInstallIndizesDuplikate. Das wird in v1.99.81 korrigiert.

> [!question] Frage an Gerald
> Falls `-NurPruefen` auf einem Prod-Server meldet, dass „Nur FA-Meldung“ (`BdeNurFaMeldung`) noch eingeschaltet ist: Was gilt?
> 1. **(empfohlen)** Vor dem Wartungsfenster beim Fachbereich klären, ob dort noch eine Werkbank im FA-Meldemodus arbeitet. Danach in `/Settings` ausschalten, das Update läuft dann normal.
> 2. Ohne Rückfrage ausschalten, weil die Einstellung eine Altlast ist.
> 3. Das Update an diesem Standort verschieben, bis der Fachbereich entschieden hat.

### B-4 Abnahmelücken: Anleitung je NICHT-MÖGLICH-Punkt

**Problem.** Die Lauf-Notiz nennt sechs UAT-Punkte (#28, #29, #31, #34, #35, #37) und QA-Punkte (TS-201.3.3, F-28, F-7, F-2) als NICHT
MÖGLICH. Sie stehen in [[2026-11-25-kommission-lagerplatz-bde-goal-lauf]], Z. 116, 130, 131, 233 und 298-307. TS-198.6 ist inzwischen
OK (#17). TS-198.7 ist FEHLER und gehört als U-2 zum anderen Lauf.

So kann Gerald jeden Punkt nachholen:

| Punkt | Wo | Anleitung | Erwartet |
|---|---|---|---|
| TS-201.3.3, F-28, #37 Teil 1: Benutzer ohne Terminal | DEV AKE oder IDEAL | Unter Benutzer ein **lokales** WMS-Konto mit Formular-Anmeldung anlegen, **ohne** Terminal-Zuordnung. In einem privaten Browserfenster damit anmelden, dann *BDE › Terminal* und die Hilfeseite BDE mit dem Link „Terminal“ öffnen. | Startseite mit gelber Meldung „Für diesen Benutzer ist kein Terminal konfiguriert. Bitte an den BDE-Admin wenden.“, keine Fehlerseite |
| #37 Teil 2: Android-Handy | Testsystem IDEAL `idealakeweb01…:88`. DEV ist `localhost` und vom Handy aus nicht erreichbar. | Dort ein lokales Konto plus Terminal-Zuordnung anlegen (docs/betrieb/09). Am Handy anmelden, Werker anmelden, eine Teilmeldung und eine Rücknahme machen. | Buchung wie am PC, Tageshistorie zeigt sie |
| #37 Teil 3: echte Gleichzeitigkeit | Testsystem IDEAL | PC und Handy auf demselben Arbeitsgang bzw. derselben Teilezeile. Beide Geräte vorbereiten, dann gleichzeitig (Zuruf „jetzt“) *Fertig melden*. | Eine Meldung geht durch. Das andere Gerät zeigt „wird gerade an einem anderen Terminal gemeldet – bitte erneut versuchen“ (`BdeTeilelisteService.cs:590-594`) oder sieht die Zeile schon erledigt. Keine Doppelmeldung (`SELECT … FROM BdeTeilMeldungen` je Schlüssel) |
| #28 Link am Fräsen-PC | IDEAL Prod, nach dem Einmal-Skript (S-6) und mit `FraesenPfadAktiv` | Am Fräsen-PC die Teileliste öffnen, Pfad einblenden, „Pfad kopieren“ drücken, den Pfad in die Adresszeile des Explorers einfügen. | Der Ordner bzw. die Datei öffnet. Geht die Zwischenablage nicht, ist der Text markiert. |
| #29 Blechdicke | Testsystem IDEAL (Dienstkonto liest Sage IDEAL) | Siehe S-7: P-6 ausführen, dann `Sync:BlechteileFelderNachlesen = true` am Testsystem, einen Zyklus warten, die Teileliste öffnen. | Die Spalte Blechdicke zeigt Werte, bei Artikeln ohne Wert „–“ |
| #31 Nachtlauf Nachtrag | DEV AKE (siehe F-4) bzw. Prod am ersten Abend | Siehe F-4 | Ein Lauf „Kommissionier-Nachtrag“ mit Zählern im Aktivitäts-Protokoll, die Aufträge im Leitstand |
| F-7 / #34 Lagerplatz-Maske | DEV AKE gegen `ake_TEST2026` (Spec-Freigabe-Antwort 7) | **Zuerst den Dienst `AKE_WMS_DEV_Service` stoppen.** Dann `SageLagerplatzAnlageAktiv = true` in `/ServiceSettings` setzen (das Web liest den Wert selbst), die Maske „Lagerplätze in Sage anlegen“ öffnen und ein neues Lager mit 11 Zeichen eingeben. Danach `SageLagerplatzAnlageAktiv = false` setzen und erst dann den Dienst wieder starten. | Die Validierung weist 11 Zeichen ab. Weil der Dienst gestoppt ist, geht nichts an Sage. Die Outbox ist unverändert (P-4 gegen DEV). |
| F-2 Altbestand geprüft, Anlegen scheitert | – | Den Gutfall „Altbestand geprüft legt sofort an“ auf DEV prüfen, sobald ein FA mit Altbestand-Kennzeichen auftaucht. Den Fehlerfall nicht provozieren, der Unit-Test reicht. | Gelbe Meldung im Fehlerfall laut Code (`PickingLeitstandController.cs:571-586`) |
| #35 AKE Anstempeln | DEV AKE, sonst Prod am ersten Tag | In `ake_TEST2026` einen FA mit Arbeitsgängen an einer BDE-Werkbank freigeben. Den FA-Import abwarten oder im Leitstand eine Werkbank mit offenen Arbeitsgängen suchen. Dann Rüsten, Produktion und Fertig anstempeln. Gibt es keinen solchen FA: am ersten Prod-Tag mit einem Werker zusammen. | Die drei Buchungen erscheinen in der Tageshistorie |

**Empfehlung.** Die Punkte, die nur nach Prod gehen (#28, #31 auf Prod, #35 falls DEV keinen FA hat), kommen ausdrücklich als „nach
Prod nachholen“ ins Abnahmeprotokoll. TS-201.3.3, #37 und F-7 gehen vorher und gehören zu Schranke 2.

> [!question] Frage an Gerald
> Was davon muss vor dem Merge (Schranke 2) erledigt sein?
> 1. **(empfohlen)** Vor dem Merge: Benutzer ohne Terminal, Handy plus Gleichzeitigkeit am Testsystem IDEAL, Lagerplatz-Maske mit gestopptem Dienst. Nach Prod: Fräsen-PC-Link, Nachtlauf, AKE-Anstempeln (falls DEV keinen FA hat). Beides steht so im Protokoll.
> 2. Alles vor dem Merge, notfalls am Testsystem mit echten FAs.
> 3. Alles nach Prod nachholen. Es zählt die Abnahme durch Unit-Tests und QA.

---

## SOLLTE

### S-1 Was v1.99.65 bis 69 nach Prod bringt

**Problem.** AKE-Prod steht auf 1.99.64. Nach Plan stand IDEAL-Prod nach Paket 17 auf 1.99.63, ob das Paket eingespielt wurde, ist
nicht belegt. Das Update bringt deshalb zusätzlich zu 1.99.70–80 auch 1.99.65–69 mit. Quelle sind die Brain-Changelogs
1.99.65–1.99.69 und `nachlauf.psd1:129-130, 661-666`.

| Version | Inhalt | Risiko auf Prod |
|---|---|---|
| 1.99.65 | Ein Beschichtungsauftrag ohne Auswahl legt einen Entwurf mit **allen** freien Positionen an | Geändertes Verhalten fürs Lager. Wer bisher ohne Auswahl „nichts passiert“ erwartete, bekommt einen vollen Entwurf. Lager informieren. |
| 1.99.66 | Neue Gesamtansicht „Sage-Buchungen“. Der Menüpunkt „Sage-Wareneingänge“ entfällt, alte URLs leiten mit 301 weiter. Neuer Filter `RequireStockReadOrEinkaufAccess` | Lesezeichen funktionieren weiter. Wer bisher nur Einkauf hatte, sieht die Ansicht jetzt auch (gewollt). Manifest-Info `:661-663` |
| 1.99.67 | VaterFA in Meldungen, „Übergeben“ ohne Positionen gesperrt | gering |
| 1.99.68 | **SQL/167** `StockMovements.Abteilung`, dazu gehen Referenz und Memo an Sage | Schema-Änderung (automatisch). **P-2 offen:** ob die Zeilen-Referenz in Sage einen vorhandenen Wert überschreibt. Nach dem ersten Wareneingang einen Beleg in Sage ansehen. Manifest-Info `:664-666` |
| 1.99.69 | Fix für leere Entwurfsbinder. Der Wareneingang ist nur mit Modul möglich, der Menüeintrag bleibt aber sichtbar (B3-H1) | gering |

**Folge für die SQL-Liste:** Auf AKE-Prod laufen **167, 168, 164, 165, 169** in Manifest-Reihenfolge, alle automatisch
(`nachlauf.psd1:123-134`). Auf IDEAL-Prod hängt die Liste am Stand, also Abfrage P-1. Das Skript wählt die fehlenden Skripte selbst
nach `MigrationId` aus (`Update-IdealAkeWms.ps1:312`).

**Empfehlung:** kein Entscheid nötig. Den IDEAL-Stand mit P-1 belegen. Die Lager-Info (1.99.65) und die Sage-Belegkontrolle (1.99.68)
kommen in die Mail bzw. in die Nacharbeit der Checkliste.

### S-2 Mail korrigieren (fertige Sätze)

Datei: `secondbrain/aufgaben/2026-11-26-mail-wms-neuerungen-v1-99-70-80.md`.

| Zeile | Neu |
|---|---|
| 37-38 | „**Blechdicke und Fräsen-Pfad (IDEAL).** Die Teileliste zeigt die Blechdicke (fehlt der Wert, steht „–“). Den Fräsen-Pfad können Sie über das Zahnrad einblenden und mit „Pfad kopieren“ übernehmen, **sobald die IT die Pfade eingespielt und die Funktion freigeschaltet hat**.“ |
| 42-43 | „**Fehlende Kommissionieraufträge kommen nachts von selbst.** Ein nächtlicher Lauf (ab 2 Uhr) legt für freigegebene Fertigungsaufträge die noch fehlenden Kommissionieraufträge an. **Die IT schaltet den Lauf am Abend nach dem Update ein; beim ersten Mal entstehen dabei auf einen Schlag die bisher fehlenden Aufträge.**“ (bei B-1 Antwort 2: „… Der erste Lauf ist in der Nacht nach dem Update.“) |
| 63-64 | „**Lagerbestellungen an der Isolierungsfräse (IDEAL).** **An Werkbänken, für die die IT das freischaltet,** erscheinen abgeschickte Lagerbestellungen mit dem Kürzel der Werkbank in deren Teileliste (**ohne Glasbestellungen**). Der Termin ist Abschicken plus 10 Arbeitstage.“ (der Zusatz zu Glas gilt erst nach F-11) |
| 66-67 | „**Fräsen-Pfad je Artikel (IDEAL).** Der Pfad steht in Artikel-Liste, Bestandsübersicht und Lagerbestellung (über das Zahnrad einblendbar). Pflegen dürfen ihn Administratoren, Stammdaten-Pfleger und Personen mit der Rolle „Fräsen-Pfad pflegen“. Freigeschaltet wird das erst, wenn die IT die vorhandenen Pfade eingespielt hat.“ (Beleg `CurrentUserService.CanPflegeFraesenPfadAsync`) |
| 72 | „**Update mit dem Update-Skript.** Datenbank-Änderungen laufen automatisch: SQL/164 und 165 (Fräsen-Pfad, Lagerbestellungen in der Teileliste), SQL/168 (Kommissionierung), SQL/169 (Teilmeldungen); **AKE zusätzlich SQL/167** (Abteilung an Lagerbewegungen, aus 1.99.68). Vorher Datenbank-Sicherung – ein Rückfall nach SQL/169 geht nur über die Sicherung.“ |
| 77-78 | „**BDE am Handy und Terminals.** Jedes Konto, das bucht, braucht eine eigene Terminal-Zuordnung; seit 1.99.79 nimmt das WMS **Starten, Fortsetzen und Fertigmelden** nur vom eigenen Terminal an. **Handy-Konten (lokales Konto, Formular-Anmeldung) sind noch nicht angelegt – bitte je Gerät anlegen.**“ (gilt so lange, bis H-A/B-2 entschieden ist. Bei Option a: „alle Buchungen“) |
| 83 | „**Einstellung „Nur FA-Meldung“** (`BdeNurFaMeldung`) ist eine Altlast und muss vor dem Update aus sein: **Ist sie an, bricht das Update-Skript auf Prod vor dem Stoppen ab.** Vorher mit `-NurPruefen` prüfen.“ |
| neu, Lager | „**Beschichtungsauftrag ohne Auswahl** legt jetzt einen Entwurf mit allen freien Positionen an (seit 1.99.65).“ |
| neu, IT | „Nach dem ersten Wareneingang einen Beleg in Sage ansehen: Referenz und Memo kommen jetzt aus dem WMS (1.99.68).“ |

**Empfehlung:** kein Entscheid nötig, die Texte werden so übernommen. Offen sind nur die Varianten, die an B-1 und B-2 hängen.

### S-3 / H-C Sage-Mandant fest 1 und `STRING_AGG`

**Problem.** `IDEALAKEWMSService/Services/SqlSageLagerplatzAnlage.cs:14-15` legt `Mandant = 1` als Konstante fest. Die Konfliktmeldung
50014 baut ihre Liste mit `STRING_AGG … WITHIN GROUP` (`:49-52`). SQL Server übersetzt den ganzen Batch auf einmal. Auf einem Server
älter als 2017 scheitert deshalb **jede** Platz-Anlage schon beim Übersetzen, nicht nur der Konfliktfall.

**Belegt (lesend):**
- `ake_TEST2026` auf `AKESQL20` ist SQL Server 2019 (15.0.4430.1) mit Kompatibilitätsgrad 150.
- `KHKLagerplaetze` hat nur Mandant 1 (3300 Zeilen), `KHKLagerorte` nur Mandant 1 (36), `KHKMandanten` einen Mandanten.
- Die AKE-Prod-DB `ake` liegt auf derselben Instanz. Die Version ist damit für AKE belegt, der Mandant der Prod-DB noch nicht.
- IDEAL ist **nicht belegt**. Die Anmeldung an `idealsql2\SAGE100TEST` scheitert mit dem AKENB-Konto.
- Hinweis, kein Beleg: Das SData-Dataset des DEV IDEAL heißt `IDEAL_TEST_2026_03_05;1`, das `;1` ist der Mandant.

| | Option | Dateien | Aufwand | Risiko | Test |
|---|---|---|---|---|---|
| A | **Erst prüfen** (P-5 an IDEAL-Prod und -Test, P-5b Mandant an AKE-Prod). Code nur ändern, wenn die Version unter 2017 liegt oder der Mandant nicht 1 ist. | keine | S | keins, der Schalter ist standardmäßig aus (`ServiceSettingDefinitions.cs:319-321`) | Abfrage |
| B | Die Meldung ohne Aggregat bauen, aus den schon berechneten `@PlatzID = MIN(PlatzID)` und `@n`: „PlatzID x … (+n−1 weitere)“ | `SqlSageLagerplatzAnlage.cs` | S | Die Meldung nennt nur noch einen Platz | Unit-Test der Meldung, TS-192 Konfliktfall |
| C | `STUFF((SELECT ', ' + CONCAT(…) … ORDER BY PlatzID FOR XML PATH(''), TYPE).value('.', 'nvarchar(2000)'), 1, 2, '')` | `SqlSageLagerplatzAnlage.cs` | S | Bei Sonderzeichen etwas fehleranfälliger | wie B |

**Empfehlung: A.** Weder die Version noch der Mandant blockiert das Deploy. Wenn P-5 eine IDEAL-Version unter 2017 zeigt, wird B
gebaut, das ist der einfachste versionsunabhängige Weg. Ein konfigurierbarer Mandant kommt erst, wenn P-5b einen anderen Wert als 1
zeigt (YAGNI).

> [!question] Frage an Gerald
> Die Lagerplatz-Anlage nach Sage nutzt eine SQL-Funktion, die erst ab SQL Server 2017 existiert. Für IDEAL ist die Version nicht bekannt. Was tun?
> 1. **(empfohlen)** Zuerst die Version an der IDEAL-Sage-Instanz abfragen (P-5). Nur wenn sie älter als 2017 ist, die Meldung vereinfachen, bevor der Schalter an IDEAL eingeschaltet wird.
> 2. Die Meldung jetzt in v1.99.81 vorsorglich vereinfachen, unabhängig von der Version.

### S-4 Altbestand in der Outbox vor dem Einschalten

**Problem.** `StorageLocationRepository.cs:13-27` reiht einen neuen Platz in die Outbox ein, wenn `SageUebergabeStatus == Offen` ist.
Der Dienst sendet aber nur Plätze mit `Source='Manual' AND StorageAreaId IS NOT NULL AND IsActive` (`SageLagerplatzAnlageService.cs:53`).
Beim normalen Anlegen setzt der Controller `StorageAreaId = null` (`StorageLocationsController.cs:123-124`), nur das Sage-Formular setzt
ihn (`:342`). Kandidaten sind Einträge mit Status Offen oder solche, die länger als `stuckMinutes` auf Gesendet stehen (`:236-237`).

**DEV-Befund (P-4 lesend):** AKE und IDEAL haben je 1 Eintrag `StorageLocation`, Status 0 (Offen), Sendbar 0.

**Empfehlung:** kein Entscheid nötig. P-4 läuft vor dem Einschalten je Standort. Erwartet wird Sendbar = 0. Ist der Wert größer als
0, wird der Schalter nicht eingeschaltet, sondern die Liste mit Gerald geklärt. Nicht sendbare Einträge mit Status Offen sind harmlos,
der Dienst überspringt sie.

### S-5 Abbruch während des Sage-Aufrufs: bis zu 10 Minuten „Gesendet“

**Problem.** Vor dem Sage-Aufruf steht der Eintrag schon auf Gesendet mit Versuch +1. Der catch-Block greift bei
`ex is not OperationCanceledException`. Bei einem Dienst-Stopp bleibt der Eintrag deshalb auf Gesendet, bis
`Sync:SageLagerbuchungStuckMinutes` abläuft (Default 10, `SageBookingWorker.cs:228/301/316/361`). Dann wird er erneut gesendet, und
der Sage-Befehl ist idempotent. „Erneut senden“ gibt es nur bei Fehler (`StorageLocationsController.cs:387-393`).
`docs/betrieb/05-schnittstellen.md:134` beschreibt das schon.

**Empfehlung:** kein Entscheid nötig, wird so gebaut (nur Doku, v1.99.81). Ein Satz kommt in `docs/anwender/19` und
`docs/betrieb/07`: „Steht ein Platz nach einem Dienst-Neustart auf ‚Gesendet‘, ist das bis zu 10 Minuten normal; der Dienst sendet
ihn danach selbst erneut. ‚Erneut senden‘ gibt es nur bei Status Fehler.“

### S-6 / F-14 Einmal-Skript Fräsen-Pfad: was Gerald beantworten muss

**Problem.** `SQL/Einmalig/2026-11-25_v1.99.72_FraesenPfad_Erstimport.sql` startet mit `@Trockenlauf=1`, hat einen Standortwächter
(Z. 33-39) und schreibt nur in Zeilen mit `CamPath IS NULL`. Ein falscher Pfad lässt sich durch einen zweiten Lauf also **nicht**
korrigieren. Das Manifest führt das Skript an IDEAL im Modus Trockenlauf (`nachlauf.psd1:377-382`). Die Spec sagt, das blockiert die
Freigabe nicht, eine Abweichung wird nachgearbeitet (Spec Z. 766-772, 799-802).

**Diese zwei Fragen gehen an den Fachbereich** (kann so weitergeleitet werden):
- **Rückfrage 15a:** Die Pfade aus Spalte E werden **wörtlich** übernommen, nur `Y:\` wird zu `\\idealfs1\Daten$\Produktion\CAM\`
  (Antwort 18). Es wird **kein** `\<Matchcode>.tcn` angehängt. Passt das, oder soll der Pfad auf die Datei zeigen?
- **Rückfrage 15b:** Die Zeilen 155, 157 und 164-166 sagen „ist enthalten in …“ statt einen Pfad zu nennen. Bleiben sie leer und
  werden nur gemeldet, oder bekommen sie den Pfad des genannten Artikels?
- **Rückfrage 16:** Den Block Belegestation G-BL (51 Zeilen) und die Twin-Kundenseite übernehmen oder weglassen?
- Zeile 161 (`Y\Nestcad\…` ohne Doppelpunkt, Ordner mit Datum) wird ohnehin nur gemeldet und nicht geschrieben.

**Empfehlung:** Im Prod-Fenster an IDEAL nur den Trockenlauf ausführen, das Manifest tut das sowieso. Die Treffer- und
Unbekannt-Liste geht mit den drei Fragen oben an den Fachbereich. SCHARF wird erst nach der Antwort ausgeführt, danach
`FraesenPfadAktiv` einschalten.

> [!question] Frage an Gerald
> Darf das Einmal-Skript für die Fräsen-Pfade an IDEAL scharf laufen, bevor der Fachbereich die Rückfragen 15 und 16 beantwortet hat?
> 1. **(empfohlen)** Nein. Im Wartungsfenster nur der Trockenlauf. Die Liste und die Fragen gehen an den Fachbereich, SCHARF erst nach der Antwort.
> 2. Ja, mit den heutigen Annahmen: Pfad wörtlich, „enthalten in“-Zeilen leer, G-BL und Twin übernehmen. Abweichungen werden später von Hand gepflegt.

### S-7 / F-16 Blechdicke an IDEAL: Rechte und Feld belegen

**Problem.** Die Blechteile-Felder (darunter `USER_OSBlechdicke`) liest die Anwendung über SData-Ressourcenfelder
(`SdataArtikelReader.BlechteileFelderLesenAsync`, Z. 340-370). Der Feldname kommt aus `Sync:Userfeld:Blechfertigung:USER_OSBlechdicke`
(ADR 0033). Auf DEV IDEAL ist er gesetzt (lesend geprüft). Auf DEV IDEAL scheitert aber schon der vorherige SQL-Lesezugriff:
„Die von der Anmeldung angeforderte IDEAL_TEST_2026_05_03-Datenbank kann nicht geöffnet werden … AKENB\svc_idealakewms“. Quelle ist die
Lauf-Notiz Z. 136. Das ist Fehler 4060: Der Login hat keinen Benutzer in der DB, **oder die DB heißt anders**.

**Unstimmigkeit gefunden:**
- Die SageConnection auf DEV IDEAL zeigt auf `IDEAL_TEST_2026_05_03` (`D:\Dev\IDEAL\Service\appsettings.DEV.json`).
- Das SData-Dataset in `IDEAL_WMS_DEV.ServiceSettings` heißt `IDEAL_TEST_2026_03_05;1`.
- Ein Datum ist vertauscht. Vor jedem GRANT klären, welche DB existiert (P-7).

**Welche Rechte das DEV-Dienstkonto braucht:** nur lesen. Laut Fehlermeldung ist das Konto `AKENB\svc_idealakewms`. Ob die Instanz
im IDEAL-Netz AKENB-Konten annimmt, ist offen (Domänen-Vertrauen). Auch Geralds AKENB-Konto wird dort abgewiesen.

GRANT-Vorschlag für den DBA (**nicht ausgeführt**, nur Test-Instanz `idealsql2\SAGE100TEST`, DB-Name nach P-7):

```sql
-- auf idealsql2\SAGE100TEST, als DBA; NUR Testinstanz
IF SUSER_ID(N'AKENB\svc_idealakewms') IS NULL
    CREATE LOGIN [AKENB\svc_idealakewms] FROM WINDOWS;      -- scheitert, wenn kein Vertrauen zu AKENB besteht -> Option B
GO
USE [IDEAL_TEST_2026_05_03];   -- bzw. der Name aus P-7
GO
IF USER_ID(N'AKENB\svc_idealakewms') IS NULL
    CREATE USER [AKENB\svc_idealakewms] FOR LOGIN [AKENB\svc_idealakewms];
ALTER ROLE db_datareader ADD MEMBER [AKENB\svc_idealakewms];
-- enger statt db_datareader: GRANT SELECT nur auf die gelesenen Views/Tabellen (Liste aus dem Sage-Lesecode des Dienstes)
```

| | Option | Aufwand | Risiko | Test |
|---|---|---|---|---|
| A | P-7 ausführen und den DB-Namen bzw. das Dataset auf DEV IDEAL korrigieren. Danach den GRANT oben durch den DBA. | S (DBA) | Ein fremdes Domänenkonto liest Sage IDEAL; Vertrauen offen | Dienst-Neustart DEV IDEAL, Lauf „Artikel“ ohne Fehler 4060 |
| B | **Kein GRANT.** P-6 und Punkt #29 am **Testsystem IDEAL `:88`** ausführen. Dessen Dienstkonto liest `IDEAL_TEST_2026_05_03` schon (Spec Lagerplatz-Anlage V1, Z. 734). | S | keins | Am Testsystem `Sync:BlechteileFelderNachlesen = true`, ein Zyklus, Spalte Blechdicke |

**Empfehlung: B.** Ohne neue Rechte und ohne Domänenfrage. Der Nachweis entsteht dort, wo IDEAL ohnehin testet. A lohnt sich erst,
wenn DEV IDEAL dauerhaft gegen Sage IDEAL lesen soll. Den Namensdreher (P-7) trotzdem aufklären, er ist ein eigener Befund.
`Sync:BlechteileFelderNachlesen` bleibt an IDEAL-Prod aus, bis P-6 an Prod das Feld belegt. Die Prüfung
`Pruefung/BlechteileNachleseAus` (`nachlauf.psd1:322-325`) meldet das.

> [!question] Frage an Gerald
> Wo soll die Blechdicke aus Sage IDEAL nachgewiesen werden?
> 1. **(empfohlen)** Am Testsystem IDEAL (`:88`). Dessen Dienstkonto liest Sage IDEAL schon, es braucht keine neuen Rechte. DEV IDEAL bleibt ohne Sage-Lesezugriff.
> 2. Auf DEV IDEAL. Der DBA vergibt dem Konto `AKENB\svc_idealakewms` Leserechte auf der IDEAL-Testdatenbank (GRANT oben), vorher den Datenbanknamen klären.

### S-8 Rückfall nach SQL/169

**Problem.** Die Down-Migration `20261125191311_…` legt den alten UNIQUE-Index (eine Meldung je Schlüssel) wieder an. Das scheitert,
sobald es Teilfertig-Meldungen gibt (Kommentar in `Down`, Z. 63). `docs/betrieb/03` §5.2a bzw. Z. 204 und §6 sagen dasselbe.

**Empfehlung:** kein Entscheid nötig, wird so gebaut. Vor jedem Prod-Update ist das DB-Backup Pflicht, das ist Schritt 2 der
Checkliste unten. Der Rückfall nach Prod geht nur über das Backup **plus** die alten Programmordner. Das Skript sichert keine
Programmdateien (docs/betrieb/03 §6), also werden `Web` und `Dienst` vorher als ZIP gesichert.

---

## HINWEIS und F-Punkte

### H-3 Vorbelegung in SQL/169 ohne Audit-Felder

**Problem.** `SQL/169_*.sql:43` setzt `BdeStandardsichtTeile = 1` ohne `ModifiedAt` und `ModifiedBy`. `ProductionWorkplace` erbt
`AuditableEntity` (`Models/ProductionWorkplace.cs:5`). Es gibt ein Vorbild, das die Felder setzt: `SQL/110:79-81`.

| | Option | Aufwand | Risiko |
|---|---|---|---|
| A | So lassen und in `fallstricke.md` festhalten: Eine Schema-Vorbelegung im selben Batch wie `ADD COLUMN` ist keine fachliche Änderung. `ModifiedAt` zeigt weiter die letzte echte Pflege. | S | keins |
| B | Die drei Felder im UPDATE ergänzen, wie in SQL/110 | S | Der Zweig läuft nur, wenn die Spalte fehlt. Auf DEV ist er schon gelaufen, die Änderung wäre dort ungetestet. |

**Empfehlung: A.** Ein Skript, das nach der Abnahme noch geändert wird, ist das größere Risiko.

> [!question] Frage an Gerald
> Die Vorbelegung der neuen Werkbank-Spalte in SQL/169 setzt keine Änderungsfelder. Was gilt?
> 1. **(empfohlen)** So lassen und als bewusste Ausnahme in den Fallstricken festhalten.
> 2. Vor Prod ergänzen (Muster SQL/110), dafür das Skript erneut testen.

### H-7 / F-3 Nachtrag-Marker im SyncLog, Totalausfall

**Problem.**
- Der Marker ist `MAX(SyncLogs.Timestamp)` mit „Run gestartet“.
- Wenn das Schreiben scheitert, protokolliert `SyncRun.cs:91-95` nur eine Warnung ins Datei-Log. Der Marker fehlt dann, und der Lauf
  wiederholt sich jeden Zyklus. Das ist idempotent, aber teuer.
- Dieser Fall tritt praktisch nur ein, wenn die WMS-DB selbst nicht erreichbar ist. Dann scheitert auch der Lauf.
- Bei einem Totalausfall schreibt `FinishFailed` den Fehler und wirft weiter, `RunResilientAsync` behandelt ihn. Der Tag ist dann
  verpasst, aber sichtbar.
- Teilfehler stehen nur als Zähler `fehler` in der Erfolgsmeldung.

| | Option | Aufwand | Risiko |
|---|---|---|---|
| A | So lassen (Besprechung F-3) und zusätzlich bei `fehler > 0` eine Warnung ins Protokoll schreiben, also ein `LogWarningAsync` vor `FinishSuccess` | S (1 Zeile + Test) | keins |
| B | So lassen, ohne Warnung. Der Zähler steht schon in der Meldung. | – | Ein Teilfehler fällt nicht auf (Pflicht „melden“) |

**Empfehlung: A**, aber **nach** Prod (v1.99.82). Der Zähler ist schon sichtbar, die Warnung macht ihn nur auffälliger.

> [!question] Frage an Gerald
> Soll der nächtliche Nachtrag bei Teilfehlern zusätzlich eine Warnung ins Aktivitäts-Protokoll schreiben?
> 1. **(empfohlen)** Ja, im nächsten Lauf nach dem Prod-Update. Bis dahin prüft die IT den Zähler „fehler“ am Morgen nach dem ersten Lauf.
> 2. Ja, noch vor Prod in v1.99.81.
> 3. Nein, der Zähler in der Meldung reicht.

### H-9 Manifest-Handschritt `BdeTerminalMaxHauptFa`

**Problem.** `nachlauf.psd1:453-454` (`v1.64.0-MaxHauptFa`, Typ Einstellung) verlangt weiter „BdeTerminalMaxHauptFa = 5“. Das
widerspricht `:706-708` (entfällt seit 1.99.78). Das Skript zeigt den Eintrag nur, wenn die installierte Version unter 1.64.0 liegt oder
es eine Neuinstallation ist (`Update-IdealAkeWms.ps1:907, 527, 268`). AKE-Prod (1.99.64) sieht ihn also nicht, IDEAL-Prod nur bei einem
Stand unter 1.64. Der Drift-Test prüft nur Schema- und Skript-Einträge (`NachlaufManifestDriftTests.cs:66-104`), eine Textänderung
ist deshalb frei.

**Empfehlung:** kein Entscheid nötig, wird so gebaut (v1.99.81, eine Zeile). Der Text wird zu
„BdeTerminalMaxHauptFa entfällt seit 1.99.78 (in /Settings als verwaist sichtbar) – nichts einstellen.“ Der Eintrag bleibt stehen,
damit die Versionsgeschichte vollständig ist.

### F-4 Nachtrag auf DEV beobachten

**Problem.** Die DEV-Absicherung `D:\Dev\scripts\dev-absicherung-{ake,ideal}.sql` liegt außerhalb des Repos. Z. 10 setzt jedes
`Sync:%Enabled` auf `false`, also auch den Nachtrag. Die Besprechung hat schon entschieden: einmal von Hand einschalten.

**Anleitung (DEV AKE):**
1. `UPDATE dbo.ServiceSettings SET [Value]=N'true' WHERE [Key]=N'Sync:KommissionierNachtragEnabled'` auf `AKE_WMS_DEV`.
2. Ohne Marker ist der Lauf sofort fällig, die Uhrzeit muss also nicht verstellt werden. Es reicht, den nächsten Zyklus abzuwarten.
3. Im Aktivitäts-Protokoll den Eintrag „Kommissionier-Nachtrag“ mit Zählern prüfen.
4. Einen Zyklus später darf **kein** zweiter Lauf kommen (Marker gesetzt).
5. Danach wieder `false` setzen.

Das nächste DEV-Deploy setzt den Wert über die Absicherung ohnehin zurück.

**Empfehlung:** kein Entscheid nötig (schon entschieden). Eine Ausnahme in der Absicherung
(`AND [Key] <> N'Sync:KommissionierNachtragEnabled'`) bleibt bewusst weg, weil die Absicherung DEV vor allen Schreibläufen schützen soll.

### F-11 Glasbestellungen in der Teileliste der Fräse

**Problem.**
- `BdeTeilelisteService.LagerZeilenAsync` (`IdealAkeWms/Services/BdeTeilelisteService.cs:334-345`) filtert nur nach dem Status
  Submitted oder PartiallyDelivered, **nicht nach der Bestellart**.
- Die Werte sind `Lager = 1` und `Glas = 2` (`IdealAkeWms/Models/WarehouseRequisitionType.cs:9-10`). Default in der Spalte ist 1
  (`SQL/00_FreshInstall.sql:1823`, eingeführt mit `SQL/77`).
- Glas ist **nicht** gleich Lagerbestellung: Es ist dieselbe Tabelle mit eigenem Typ, eigener Empfängergruppe und eigenen
  Artikelgruppen (Kommentar im Enum).

**DEV-Zahlen** (`WarehouseRequisitions` × Positionen, lesend):

| DB | Typ 1 Lager | Typ 2 Glas | davon abgeschickt/teilgeliefert (würde erscheinen) |
|---|---|---|---|
| AKE_WMS_DEV | 62 Bestellungen / 144 Positionen | 6 / 3 | Glas: 2 Bestellungen, 2 Positionen |
| IDEAL_WMS_DEV | 5 / 6 | 1 / 1 | Glas: 1 Bestellung, 1 Position |

Nebenbefund: In `IDEAL_WMS_DEV` gibt es eine Bestellung `Id 1` mit `Type = 0`, das ist kein gültiger Enum-Wert. Sie hat Status
Closed und wurde am 2026-11-23 vom QA-Agent angelegt, ist also ein Testdaten-Artefakt. Sie ist nicht abgeschickt und erscheint
deshalb nicht. Ein Filter `Type == Lager` schließt sie mit aus.

**Empfehlung:** kein Entscheid nötig (Besprechung F-11), wird so gebaut. Es kommt eine Bedingung
`&& i.WarehouseRequisition.Type == WarehouseRequisitionType.Lager` in `LagerZeilenAsync` dazu, dieselbe Bedingung in die
Meldeprüfung (`:597-613`), damit eine Glas-Position auch nicht per manipuliertem Request gemeldet werden kann. Dazu ein Unit-Test mit
einer Glas-Bestellung. Das blockiert Prod nicht (Werkbank-Option Default 0, `SQL/165`), ist aber klein genug für v1.99.81.

---

## Deploy-Checkliste Prod AKE und IDEAL

**Vorbedingung:** Schranke 2 (UAT und Merge) ist genommen, B-2/H-A ist entschieden (anderer Lauf), v1.99.81 ist gebaut und durch die
QA. Die Mail geht **nach** dem Deploy raus.

**Vorab (Tage vorher, je Standort):**
1. Den Stand belegen: P-1. Für AKE wird 1.99.64 erwartet, für IDEAL der Stand laut Abfrage.
2. `Update-IdealAkeWms.ps1 -NurPruefen` gegen Prod ausführen. Plan und Vorher-Prüfungen lesen. Jede Prüfung mit einem Wert über 0
   (BdeNurFaMeldung, FreshInstallIndizesDuplikate, MailPickup, BenutzerOhneKennwort, FaStrukturSchluesselDoppelt) vor dem Fenster
   klären (B-3).
3. Nur wenn v1.99.81 **ohne** B-1 Option B ausgeliefert wird: P-3 ausführen, also den Nachtrag-Schalter per SQL auf `false` setzen.
4. IDEAL: P-5 (SQL-Version, Mandant). AKE: P-5b (Mandant). Beides ist nur für den späteren Sage-Schalter nötig.
5. Lager (1.99.65), Leitstand (Nachtrag am Abend) und Werker (neue BDE-Seite) vorab informieren.

**Im Wartungsfenster (je Standort, das Skript führt durch):**
1. Web- und Dienst-Ordner als ZIP sichern, weil das Skript keine Programmdateien sichert (S-8).
2. DB-Backup ist **Pflicht**, sonst gibt es keinen Rückfall nach SQL/169.
3. Das Skript stoppt Web und Dienst und spielt die Schemata automatisch in Manifest-Reihenfolge ein (AKE: 167, 168, 164, 165, 169;
   IDEAL laut P-1). Danach kopiert es Web und Dienst und startet sie.
4. Haltepunkt: `Pruefung/RolleFraesenpfadPflegen` als Reparatur bestätigen (`nachlauf.psd1:310`).
5. IDEAL: Das Einmal-Skript Fräsen-Pfad läuft **nur als Trockenlauf**, die Ausgabe sichern (S-6).

**Nachher (gleicher Tag):**
1. Die Nachher-Liste des Skripts abarbeiten: Infos 1.99.66 bis 1.99.78, BDE-Standardsicht je Werkbank (IDEAL),
   Terminal-Zuordnungen je buchendem Konto, Handy-Konten anlegen.
2. Bleiben **aus:** `SageLagerplatzAnlageAktiv`, `SageLagerAnlageLagertechniken`, `FraesenPfadAktiv`,
   `Sync:BlechteileFelderNachlesen` (IDEAL) und die Werkbank-Option `BdeTeilelisteLagerbestellungen`.
3. **Abends:** `Sync:KommissionierNachtragEnabled = true`. Der erste Lauf startet sofort. Im Aktivitäts-Protokoll
   „Kommissionier-Nachtrag“ die Zähler `fehler`, `altbestand_pruefen` und `stueckliste_kleiner` prüfen, im Leitstand die neuen
   Aufträge ansehen.
4. Nach dem ersten Wareneingang den Beleg in Sage ansehen: Referenz und Memo (1.99.68, P-2 offen).
5. Die Mail mit den Texten aus S-2 versenden und das Datum eintragen.

**Später (je Schalter, eigene Freigabe):**
- Lagerplatz-Anlage: P-4 und P-5, dann DBA-GRANT nach der Spec (Z. 863-884), dann P-4 aus dem Dienstkontext, Sage-Backup, Schalter.
- Fräsen-Pfad: Antwort 15/16, SCHARF, `FraesenPfadAktiv`.
- Blechteile: P-6 an IDEAL-Prod, dann `Sync:BlechteileFelderNachlesen`.
- Lagerbestellungen an der Fräse erst nach F-11.

---

## Schnitt: v1.99.81 (Pflicht vor Prod) und später

| v1.99.81 – Pflicht vor Prod | Später (nach Prod) |
|---|---|
| B-1 Option B: Default `false` plus Nachher-Prüfung plus Texte (wenn A gewählt wird: P-3 als Pflichtschritt statt Code) | F-3/H-7: Warnung bei `fehler > 0` (v1.99.82) |
| S-2: Mailtexte (nur Brain, kein Code) | S-3 Option B: nur wenn P-5 an IDEAL eine Version unter 2017 zeigt (vor dem Einschalten des Sage-Schalters) |
| H-9: Manifest-Text `:453-454` | H-3: Eintrag in `fallstricke.md` |
| `docs/betrieb/03`: Zitat Z. 215 korrigieren, in §8 eine Zeile „Nachtrag abends einschalten“, Backup der Programmordner | S-7: P-7 Namensdreher DEV IDEAL aufklären |
| S-5: je ein Satz in `docs/anwender/19` und `docs/betrieb/07` | |
| F-11: Filter Bestellart Lager plus Test. Klein, blockiert aber nicht: Option Default 0. Darf notfalls nach hinten rutschen. | |
| Dazu, aus dem anderen Lauf: B-2/H-A je nach Entscheid | |

---

## Prüfabfragen für Prod (Gerald)

> [!warning] Nicht ausgeführt
> Alles lesend, außer **P-3**: Das ist ein Schreibzugriff und nur nötig, wenn B-1 nicht per Code gelöst wird. Das Ziel ist jeweils die
> **WMS-Prod-DB** des Standorts bzw. die Sage-DB des Standorts.

**P-1 Stand (WMS-Prod, beide):**
```sql
SELECT TOP 8 MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId DESC;
-- zusaetzlich: deploy-info.txt im Web-Ordner ansehen
```

**P-2 BdeNurFaMeldung (WMS-Prod, beide). Besser ist `-NurPruefen`, das alle Vorher-Prüfungen zeigt:**
```sql
SELECT [Key], [Value], ModifiedAt, ModifiedBy FROM dbo.AppSettings WHERE [Key] = N'BdeNurFaMeldung';
```

**P-3 Nachtrag vor dem Update aus (WMS-Prod, beide). SCHREIBT, nur bei B-1 Option A:**
```sql
IF NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N'Sync:KommissionierNachtragEnabled')
    INSERT INTO dbo.ServiceSettings ([Key], [Value], [Category], [Description])
    VALUES (N'Sync:KommissionierNachtragEnabled', N'false', N'Sync', N'Vor Update 1.99.8x aus; abends einschalten');
ELSE
    UPDATE dbo.ServiceSettings SET [Value] = N'false' WHERE [Key] = N'Sync:KommissionierNachtragEnabled';
```

**P-4 Outbox vor dem Einschalten der Lagerplatz-Anlage (WMS-Prod, beide; erwartet Sendbar = 0):**
```sql
SELECT o.EntityType, o.[Status], COUNT(*) AS Anzahl,
       SUM(CASE WHEN o.EntityType = N'StorageLocation' AND l.Source = N'Manual'
                 AND l.StorageAreaId IS NOT NULL AND l.IsActive = 1 THEN 1 ELSE 0 END) AS Sendbar
FROM dbo.SageStammdatenOutbox o
LEFT JOIN dbo.StorageLocations l ON o.EntityType = N'StorageLocation' AND l.Id = TRY_CONVERT(int, o.EntityKey)
GROUP BY o.EntityType, o.[Status];
-- Status: 0 Offen, 1 Gesendet, 2 Bestaetigt, 3 Fehler
```

**P-5 SQL-Version und Mandant (Sage IDEAL Prod und Test, als Konto mit Login):**
```sql
SELECT @@VERSION;                                                     -- STRING_AGG braucht 2017 (14.x) oder neuer
SELECT name, compatibility_level FROM sys.databases WHERE name = DB_NAME();   -- >= 140
SELECT Mandant, COUNT(*) FROM dbo.KHKLagerplaetze GROUP BY Mandant;   -- erwartet nur 1
SELECT Mandant, COUNT(*) FROM dbo.KHKLagerorte    GROUP BY Mandant;   -- erwartet nur 1
```
**P-5b Mandant (Sage AKE Prod `ake` auf AKESQL20; Version ist über `ake_TEST2026` schon belegt, 2019):** die beiden `GROUP BY Mandant`
aus P-5.

**P-6 Benutzerfeld Blechdicke (Sage IDEAL Prod bzw. Test). Die Bedeutung nicht aus dem Namen ableiten, Werte ansehen:**
```sql
SELECT OBJECT_SCHEMA_NAME(c.object_id) + N'.' + OBJECT_NAME(c.object_id) AS Tabelle, c.name, TYPE_NAME(c.user_type_id) AS Typ
FROM sys.columns c WHERE c.name LIKE N'USER_OS%' ORDER BY 1, 2;
-- dann mit der gefundenen Tabelle (z. B. dbo.KHKArtikel):
-- SELECT TOP 20 Artikelnummer, USER_OSBlechdicke, USER_OSMaterial, USER_OSArbeitsschritte FROM <Tabelle> WHERE USER_OSBlechdicke IS NOT NULL;
```
Zum Vergleich: In `ake_TEST2026` gibt es kein `USER_OS*`-Feld, die AKE-USER-Felder liegen an `KHKArtikelVarianten`.

**P-7 Name der IDEAL-Test-DB (auf `idealsql2\SAGE100TEST`, Konto mit Login):**
```sql
SELECT name, compatibility_level FROM sys.databases WHERE name LIKE N'IDEAL_TEST%';
-- vergleichen mit DEV IDEAL: SageConnection = IDEAL_TEST_2026_05_03, SData:Dataset = IDEAL_TEST_2026_03_05;1
```
