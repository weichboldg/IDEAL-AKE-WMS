---
type: spec
title: "Lieferverzug-Mail: Nachbesserung nach Keyuser-Antworten (Kreditor-Schwelle, Altersgrenze, Feld „bestätigt“)"
slug: 2026-11-13-lieferverzug-nachbesserung-keyuser-spec
status: InUmsetzung
created: 2026-11-13
updated: 2026-11-13
source_backlog: "[[2026-11-13-lieferverzug-nachbesserung-keyuser]]"
depends_on: "[[2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec]]"
verwandt: "[[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]], [[2026-11-12-keyuser-fragen-einkauf]]"
task: "[[2026-11-13-buendel-v1-99-19-goal-lauf]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.19 (1.99.16–1.99.18 vergibt der laufende goal-Lauf [[2026-11-13-buendel-v1-99-16-goal-lauf]]; Dev-Lauf prüft)"
sql_nummer_vorschlag: "keine (keine Migration)"
testszenario: "TS-128 erweitert um 128.16 – 128.23"
affected_code:
  - "IDEALAKEWMSService/Services/LieferverzugPlaner.cs:8-11 (Record LieferverzugEinstellungen + KreditorTageNachTermin, MaxAlterTage, ggf. KreditorMitRahmenabrufen R 3), :20-24 (Grund Vorlauf, ggf. Rahmenabruf), :41-45 (Plan + ZuKlaeren, AnzahlFaellig), :74-76 (Aufteilung fällig / zu klären), :109-140 (Entscheiden: Kreditor-Schwelle vor der Wiederholung)"
  - "IDEALAKEWMSService/Services/LieferverzugMailService.cs:44-56 (LesenAsync), :87 (Abbruch nur wenn auch ZuKlaeren leer), :146 (Protokoll Sammelliste inkl. ZuKlaeren), :164-181 (Counts zu_klaeren, vorlauf)"
  - "IDEALAKEWMSService/Services/LieferverzugMailHtml.cs:90-141 (Sammelliste: Einleitungssatz mit beiden Schwellen, Abschnitt „Zu klären“ in HTML und Text)"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs:141-152 (Meta), :174-177 (IntMeta), :310-325 (Basis; Text TageNachTermin :311 geändert)"
  - "Tests: IDEALAKEWMSService.Tests/Services/LieferverzugMailTests.cs, IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs:141-152 (Drift-Liste), IdealAkeWms.Tests/Services/SettingValidatorTests.cs:183-184 (Grenzen)"
  - "docs/TESTSZENARIEN.md (TS-128, Z. 11647-11674), Views/Help/Index.cshtml:128-156, README.md:955, Views/Help/Changelog.cshtml, beide AppVersion.cs, scripts/deploy/nachlauf.psd1"
open_questions:
  - "1 Altersgrenze: Einstellung Lieferverzug:MaxAlterTage (Default 180, gilt für EBB und EBZ, 0 = keine Grenze) – oder bereinigt der Einkauf alte Positionen in Sage (dann Default 0) – oder beides?"
  - "2 Kreditor-Schwelle: Default Lieferverzug:KreditorTageNachTermin = 3, wirksam ist das Maximum aus Sammellisten- und Kreditor-Schwelle?"
  - "3 Rahmenabrufe in der Kreditor-Mail: Alle vor v1.99.15 gelieferten Abrufe sind zwangsläufig direkt in Sage gebucht und bleiben im WMS offen, solange der Rahmen offen ist. Jetzt Schalter Lieferverzug:KreditorMitRahmenabrufen (Default aus) mitbauen, oder Teil-3-Antwort 1 unverändert lassen, bis Till die Frage beantwortet hat?"
  - "4 Feld „bestätigt“ als Spalte in Sammelliste und Kreditor-Mail: eigene Folge-Spec nach den Prüfschritten P-L3?"
  - "5 Einstellung „nur bestätigte Positionen an Kreditoren“: eigene Folge-Spec nach P-L3 (Werteverteilung), nicht hier?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-13"
---

# Lieferverzug-Mail: Nachbesserung nach Keyuser-Antworten

Folgeänderung zu v1.99.10 ([[2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec]], Changelog
[[2026-11-12-v1-99-10-lieferverzug-mail]]) nach den Antworten von Till Zöhrer ([[2026-11-12-keyuser-fragen-einkauf]], Antworten 9, 10, 12, 14)
und dem Befund „107 von 108 Abrufen überfällig“ aus [[2026-11-13-buendel-v1-99-13-goal-lauf]]. Alle Zeilennummern beziehen sich auf den
Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Stand v1.99.15, `e92894c9`). In `main` gibt es diesen Code noch nicht.

**Begriffe.** „Sammelliste“ ist die tägliche Einkauf-Mail (`LieferverzugMailHtml.Einkauf`, `:90`). Das ist die „Einkauf-Liste“ des Backlogs.
Die Maske „Offene Bestellungen“ ist davon getrennt: Dort heißt „überfällig“ `Liefertermin < heute` (`PurchaseOrdersController.cs:145`), und
keine `Lieferverzug:*`-Einstellung wirkt darauf (Grep `TageNachTermin` über `*.cs`/`*.cshtml`: Treffer nur in `LieferverzugPlaner.cs`,
`LieferverzugMailService.cs`, `ServiceSettingDefinitions.cs` und Tests). Diese Spec ändert die Maske nicht.

## Ziel / Nutzen (das Warum)

- Till will die Sammelliste schon ab dem ersten Tag haben. Der Einkauf spricht dann zuerst mit dem Lager und schreibt erst danach den Lieferanten
  an (Antwort 10). Heute steuert **eine** Schwelle beide Mails. Mit `TageNachTermin = 1` bekäme also auch der Lieferant schon nach einem Tag eine
  Erinnerung.
- Rahmenabrufe stehen seit v1.99.15 im Cache. Am AKE-Test sind 107 von 108 überfällig, der älteste seit dem 03.07.2019. Ohne Altersgrenze
  würden in der ersten echten Welle Lieferanten an Abrufe von 2019 erinnert.
- Till nennt ein Sage-Feld „bestätigt“ (Antwort 9). Diese Spec benennt es, zeigt, was dem Cache dafür fehlt, und legt die Prüfschritte fest.
  Gebaut wird dazu noch nichts.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- F1 neue Einstellung `Lieferverzug:KreditorTageNachTermin` (eigene Schwelle für die Kreditor-Mail).
- F2 neue Einstellung `Lieferverzug:MaxAlterTage`: Ältere Positionen erscheinen nur im Abschnitt „Zu klären“ der Sammelliste und nie in einer
  Kreditor-Mail (Rückfrage 1).
- F3 nur bei Rückfrage 3 = ja: Schalter `Lieferverzug:KreditorMitRahmenabrufen`.
- F4 P-M1 „bestätigt“: Befund und Prüfschritte, **kein Code**.
- F5 Adressen je Standort: nur Einstellungswerte, kein Code (Deploy/Manifest).

**Out-of-Scope**
- Lesen des Feldes „bestätigt“, eine Spalte dafür und die Einstellung „nur bestätigte“ (Rückfragen 4/5, gegebenenfalls Folge-Spec).
- Maske „Offene Bestellungen“ (z. B. ein eigener Filter „zu klären“). Wenn der Einkauf das braucht, wird es eine eigene Spec.
- Abzug von Abruf-Lieferungen, die direkt in Sage gebucht wurden (akzeptierte Grenze aus Teil 3, Entscheidung 2).
- Feiertage, Wiederholungsregel, Testbetrieb, Grenze je Lauf: bleiben unverändert.

## Fachliche Anforderungen

### Belegter Ausgangsstand

- **Schwelle heute:** `LesenAsync` liest `Lieferverzug:TageNachTermin` (Default 3, geklemmt auf 0–60, `LieferverzugMailService.cs:49`). Der Wert
  wirkt an drei Stellen:
  1. Vorfilter der Kandidaten (`LieferverzugMailService.cs:68`),
  2. Fälligkeit im Planer (`LieferverzugPlaner.cs:75` über `TerminUeberschritten`, `:61-62`),
  3. Einleitungssatz der Sammelliste (`LieferverzugMailHtml.cs:103`, übergeben in `LieferverzugMailService.cs:141`).

  Die Kreditor-Mail nimmt dieselben fälligen Positionen. Die Gruppen entstehen aus `faellig` (`LieferverzugPlaner.cs:78-94`), und `Entscheiden`
  wählt nur nach der Wiederholung aus (`:125-135`). Eine zweite Schwelle gibt es nicht.
- **Termin und Alter:** Der Planer rechnet nur mit `SagePurchaseOrderLine.Liefertermin` (`TerminUeberschritten` `:61-62`,
  `TageUeberfaellig = (heute − Liefertermin.Date).Days` `:76`). Geschrieben wird das Feld nur vom Abgleich, und zwar als Positions-Liefertermin
  oder, wenn der fehlt, als Beleg-Liefertermin, ohne Uhrzeit (`EinkaufBestellungenSyncService.cs:141`, `:155-163`, `Datum()` `:225-227`). Ein
  Belegdatum oder Bestelldatum steht **nicht** im Cache (vollständige Feldliste `SagePurchaseOrderLine.cs:15-54`). Ein anderes „Alter“ als
  „Tage seit Liefertermin“ ginge deshalb nur mit Abgleich-Erweiterung und Migration.
- **Rahmenabrufe im Cache und im Planer:**
  - Der Abgleich liest seit v1.99.15 zwei Belegarten (`Belegarten = [EBB, EBZ]`, `EinkaufBestellungenSyncService.cs:42`) mit je einer Abfrage
    (`:74-81`). Bei EBZ gilt `MengeBestellt = MengeOffen = MengeBasis` und `MengeGeliefert = 0` (`:145-146`, `:160`). Der Termin wird wie bei
    EBB bestimmt (`:141`).
  - Der Lauf liest **alle** Cache-Zeilen ohne Belegart-Filter (`LieferverzugMailService.cs:66`). Im Planer wertet nur `Rest > 0` und den Termin
    aus (`LieferverzugPlaner.cs:74-76`). Kein Treffer `EBZ|EBB|Belegkennzeichen|IstRahmenabruf` in `LieferverzugPlaner.cs` und
    `LieferverzugMailService.cs`. Nur die HTML-Ausgabe kennt Abrufe, und zwar für „Geliefert –“ (`LieferverzugMailHtml.cs:145`).
  - Rest EBZ = Abrufmenge − **alle** WMS-Wareneingänge (`ErfasstJeBelPos.Abzuziehen`, `PurchaseOrderReceiptRepository.cs:22-23`; Aufruf
    `LieferverzugMailService.cs:73`).
  - **Folgerung für die erste Welle (P-M3):** Bis v1.99.15 standen Abrufe nicht im Cache. Am WMS konnte deshalb kein Abruf gebucht werden
    ([[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]] „Ziel“, ehemals `EinkaufBestellungenSyncService.cs:32` nur `EBB`). Jede Abruf-Lieferung
    vor dem Deploy von v1.99.15 ist also entweder gar nicht erfolgt oder direkt in Sage gebucht. Die Sage-View zeigt einen Abruf, solange der
    **Rahmen** offen ist, auch wenn der Abruf selbst voll geliefert ist (Teil 3, „Sichtbarkeit“, P-W4). Deshalb ist jeder Abruf mit Termin vor dem
    Stichtag im WMS „offen mit voller Abrufmenge“. Ob das auch auf die Produktion zutrifft, hängt an Sage-Daten (P-L2, P-L4).
  - Gemessen auf DEV AKE mit den Daten des AKE-Tests: `faellig` stieg von 1014 auf 1120, also +106 Abrufe, mit 96 Testbetrieb-Kreditor-Mails
    (Teil-3-Spec, Nachtrag TS-132.6, SyncLog 5661675).
- **Grenze je Lauf:** Wären mehr als `KreditorMaxJeLauf` (Default 30) echte Kreditor-Mails fällig, geht **keine** raus
  (`LieferverzugPlaner.cs:100-104`). Mit den DEV-Zahlen (96 Kreditoren) stünde der Echtbetrieb jeden Tag still, mit Warnung und Fehlermail.
  F1 und F2 verkleinern genau diese Menge.

### F1 Eigene Schwelle für die Kreditor-Mail

- Neue Einstellung `Lieferverzug:KreditorTageNachTermin` (Default 3, Rückfrage 2).
- **Sammelliste:** unverändert `heute ≥ Liefertermin + TageNachTermin` und `Rest > 0`. Till hat dafür den Wert 1 vorgesehen, das ist reine
  Einstellung (Manifest).
- **Kreditor-Mail:** nur Positionen der Sammelliste mit `heute ≥ Liefertermin + max(TageNachTermin, KreditorTageNachTermin)`. Das Maximum hält die
  Regel ein, dass jede Position einer Kreditor-Mail auch in der Sammelliste steht. Eine feldübergreifende Prüfung beim Speichern ist damit
  nicht nötig. Die Beschreibung der Einstellung nennt das ausdrücklich.
- **Reihenfolge in `Entscheiden`:** Sonderfälle wie bisher (`:115-122`). Danach werden die Positionen unter der Kreditor-Schwelle aussortiert.
  Bleibt keine übrig, wird der neue Grund `Vorlauf` gesetzt, Status „Kreditor-Mail ab <frühestes Datum> (<n> Tage nach Liefertermin)“. Sonst
  gilt die Wiederholung (`:125-136`) nur für die verbliebenen Positionen. Liegt ein Teil der Positionen noch unter der Schwelle, ergänzt die
  Statuszeile „– <k> Position(en) erst ab <Datum>“ (melden statt still).
- Die Wiederholung, ihr Schlüssel `(BelPosId, Liefertermin)` und der Testbetrieb bleiben unverändert. Testbetrieb-Mails folgen derselben Schwelle.
  Der Einkauf sieht im Testbetrieb also genau das, was später echt rausginge.

### F2 Altersgrenze und Abschnitt „Zu klären“ (Rückfrage 1)

- Neue Einstellung `Lieferverzug:MaxAlterTage` (Vorschlag Default 180, 0 = keine Grenze).
- **Alter** = Tage seit dem Liefertermin des Caches, also dieselbe Größe wie „Tage überfällig“ (`LieferverzugPlaner.cs:76`). Eine fällige
  Position ist **zu klären**, wenn `MaxAlterTage > 0` und `TageUeberfaellig > MaxAlterTage` gilt, also `Liefertermin < heute − MaxAlterTage`.
  Das gilt für EBB und EBZ gleich (Rückfrage 1).
- Positionen, die zu klären sind, kommen **nie** in eine Kreditor-Mail, auch nicht im Testbetrieb. Sie zählen nicht für die Grenze je Lauf und
  nicht für die Wiederholung. Sie bilden keine Kreditor-Gruppe.
- Die Sammelliste bekommt nach den Kreditor-Gruppen einen eigenen Abschnitt **„Zu klären – Liefertermin älter als <N> Tage (keine Erinnerung an
  Lieferanten)“**. Er ist **eine** Tabelle, sortiert nach Lieferant und Termin, mit den Spalten Lieferant (Name + Konto), Bestellung, Artikel,
  Bezeichnung, Bestellt, Geliefert („–“ bei Abrufen wie bisher), Rest, Liefertermin, Tage überfällig, Art („Rahmenabruf“ bzw. „Bestellung“, als
  Text). Dazu kommt ein Satz: „Bitte in Sage prüfen: geliefert, storniert oder Termin anpassen.“ Im Text-Teil steht dasselbe ohne Markup.
- Der Betreff nennt die Zahl: „Lieferverzug: n Bestellposition(en) bei m Lieferant(en), davon k zu klären – <Datum>“ (nur wenn k > 0).
- Sind nur Positionen zu klären und keine sonst fällig, geht die Sammelliste trotzdem raus. Der Abbruch `plan.AnzahlFaellig == 0`
  (`LieferverzugMailService.cs:87`) zählt die Positionen zu klären mit.
- Protokoll: Die Sammelliste schreibt wie bisher eine Zeile `Art = Einkauf` je Position, jetzt auch für die Positionen zu klären
  (`LieferverzugMailService.cs:146`).
- Count `zu_klaeren` im Aktivitäts-Protokoll.

### F3 Rahmenabrufe in der Kreditor-Mail (nur bei Rückfrage 3 = ja)

- Schalter `Lieferverzug:KreditorMitRahmenabrufen` (Default aus). Steht er auf aus, wird eine EBZ-Position (`IstRahmenabruf`,
  `SagePurchaseOrderLine.cs:23`) nie in eine Kreditor-Mail aufgenommen. Sie bleibt in der Sammelliste, und zwar als „Rahmenabruf“ in der Gruppe ihres
  Lieferanten. Die Statuszeile meldet „<k> Rahmenabruf/-abrufe nicht an Lieferanten (Einstellung)“. Eine Gruppe ganz aus Abrufen bekommt den Grund
  `Rahmenabruf` mit demselben Text.
- Bei Rückfrage 3 = nein entfällt F3 vollständig. Dann gilt Teil-3-Antwort 1 unverändert, und die Abrufe stehen in beiden Mails. F2 schützt in diesem
  Fall nur vor Abrufen, die älter sind als `MaxAlterTage`.

### F4 Feld „bestätigt“ (P-M1) – Befund, kein Code

- **Beantwortet dem Namen nach (AKE):** Till sagt, `Liefertermin` sei zuerst der Wunschtermin und werde bei abweichender AB in der Regel angepasst.
  Der Cache-Termin passt also „in der Regel“, und Rückfrage 1 der Mutter-Spec bleibt „wie empfohlen“. Das Feld „bestätigt“ ist im Vertrags-`$schema`
  des AKE-Testservers belegt ([[sage-sdata-api-vertraege]] Z. 53-54, gelesen 2026-10-30):
  - `eptEKBelegPosition.udf` → `KHKEKBelegePositionen_USER_bestaetigt` [Liefertermin bestätigt],
  - auf Kopfebene `eptEKBeleg.udf` → `KHKEKBelege_USER_ABErhalten` [AB Erhalten] (Z. 50).
- **Unbelegt:** Datentyp und Werte (Z. 53-54 ohne Typ), das IDEAL-Schema (nie gelesen) und ob der Einkauf das Feld durchgängig pflegt.
  Der Vertrag `apiBelegerfassung.Sage.API` wurde bisher nur als Schema gelesen, nie als Daten (Z. 12-13). Ob der SData-Servicebenutzer ihn lesen
  darf, ist ungeprüft. Achtung: Das Ressourcen-`$schema` liefert am AKE-Test 401, nur das Vertrags-`$schema` liefert 200 ([[fallstricke]] Z. 1846).
- **Der Cache führt das Feld nicht.** Weder `Select` (`EinkaufBestellungenSyncService.cs:36-38`) noch der Record `SdataEinkaufPositionZeile`
  (`:11-15`) noch `SagePurchaseOrderLine` (`:15-54`) enthalten es. Grep `bestaetigt|USER_` in der Abgleich- und Lieferverzug-Strecke findet nichts:
  Die einzigen `bestaetigt`-Treffer in `*.cs` sind fachfremd (`SageBookingWorker`, Kommissionierung). Die Quelle `EinkaufPositionenInfo`
  (Vertrag `CommonWawiServicesResources`) hat laut Lesetest **0 USER_-Felder** und 24 Property-Platzhalter ([[sage-sdata-benutzerfelder-lesetest]]
  Z. 398). Das Feld einfach ins `select` aufzunehmen ginge deshalb nicht: Ein unbekannter Feldname führt dort zu HTTP 500 ([[fallstricke]] Z. 1786).
- **Was es bräuchte (Folge-Spec, Rückfragen 4/5):** eine zweite Lesequelle je Position, z. B. `apiBelegerfassung … eptEKBelegPosition.udf`, oder
  DPS ordnet `USER_bestaetigt` einem `PositionProperty` von `EinkaufPositionenInfo` zu. Dazu kämen eine Cache-Spalte (Migration) und die Anzeige
  bzw. die Regel. Welcher Weg trägt, zeigen die Prüfschritte P-L3.

### F5 Adressen (keine Code-Änderung)

`Lieferverzug:KreditorAbsender` und `Lieferverzug:AntwortAdresse` gibt es seit v1.99.10 (`ServiceSettingDefinitions.cs:320-323`). Werte laut Till
(Antwort 12): AKE `Einkauf@ideal-ake.at`, IDEAL `einkauf_ideal@ideal-ake.at`. Sie werden je Installation gesetzt (Manifest-Prüfung unten). Vor
dem Echtbetrieb muss die IT das Relay für den Absender an externe Empfänger freigeben (P-M4).

## Technischer Loesungsentwurf

Keine neue Klasse, kein neuer Lauf, keine Migration. Die Änderungen liegen im reinen Planer (Muster „Entscheidung im Helper“, [[services]]), im Leser
der Einstellungen und in der HTML-Funktion.

1. `LieferverzugEinstellungen` (`LieferverzugPlaner.cs:8-11`) bekommt `int KreditorTageNachTermin`, `int MaxAlterTage` und bei R 3 = ja
   `bool KreditorMitRahmenabrufen`. `LesenAsync` (`LieferverzugMailService.cs:42-56`) liest sie mit `Int(key, def, min, max)` in den Grenzen des
   Katalogs (3 / 0–60, 180 / 0–3650) bzw. `GetBoolSafeAsync(…, false)`.
2. `Planen` (`:67-107`): `faellig` teilt sich auf in `ZuKlaeren` (Alter > Grenze) und den Rest. Gruppen entstehen nur aus dem Rest.
   `LieferverzugPlan` bekommt `IReadOnlyList<LieferverzugPosition> ZuKlaeren`, und `AnzahlFaellig` zählt beide Teile.
3. `Entscheiden` (`:109-140`): nach `standAlt` (`:122`) die Kandidaten der Kreditor-Mail bestimmen, also Positionen ab
   `max(TageNachTermin, KreditorTageNachTermin)` und, bei R 3 = ja mit Schalter aus, ohne EBZ. Sind keine übrig, `Vorlauf` bzw. `Rahmenabruf` setzen.
   Danach folgt die vorhandene Wiederholung über die Kandidaten. Neue Enum-Werte in `LieferverzugGrund` (`:20-24`).
4. `LieferverzugMailHtml.Einkauf` (`:90-141`): Einleitungssatz `:103` nennt beide Schwellen und die Altersgrenze. Der Abschnitt „Zu klären“ steht
   vor dem Link, in HTML und Text, mit den vorhandenen Bausteinen `TabelleStart`/`Zeile`/`TabelleEnde`. Die Signatur bekommt die Einstellungen statt
   nur `tageNachTermin`.
5. Counts (`LieferverzugMailService.cs:164-181`) ergänzen um `zu_klaeren` (Positionen), `vorlauf` (Gruppen mit `Vorlauf`) und bei R 3
   `rahmenabruf_ausgenommen` (Positionen).
6. Katalog: zwei bzw. drei `new(...)` in `Basis()` (`:310-325`), Einträge in `Meta` (`:141-152`) und `IntMeta` (`:174-177`). Der Text von
   `TageNachTermin` (`:311`) wird angepasst. Die Drift-Liste `ServiceSettingDefinitionsTests.cs:141-152` wird ergänzt. Den Seed übernimmt
   `Program.cs` aus dem Katalog (ADR 0008), deshalb ist kein SQL nötig.
7. Hilfe (`Views/Help/Index.cshtml:128-156`) und README (`:955`) beschreiben beide Schwellen, die Altersgrenze und den Abschnitt „Zu klären“.

Rollen: unverändert (`/ServiceSettings` `[RequireAdminAccess]`). Listen-View-Pattern: entfällt, weil es keine neue Tabellenansicht gibt.
Oberfläche: keine View-Änderung außer dem Hilfetext. Gilt der Mail-Abschnitt als Oberfläche, wird `frontend-design` aufgerufen; Kontrast wie in
v1.99.10 (Tabellenkopf `#053153` mit weißer Schrift).

## Migrations-/SQL-Auswirkungen

Keine. Es kommt kein Feld an einer Entität dazu (alles läuft über ServiceSettings und den Planer), deshalb gibt es kein SQL-Skript und keinen
Nachtrag in FreshInstall. `SQL/AgentJobs/` ist nicht betroffen. Die neuen ServiceSettings-Zeilen legt der Seed aus dem Katalog an. Nicht
daten-destruktiv.

## Einstellungen

Alle Einstellungen liegen in `/ServiceSettings`, Kategorie „Lieferverzug-Mail“ (`LvKategorie`), DB-first (ADR 0008). Der Server prüft die Grenzen
beim Speichern über `IntMeta` (ADR 0020). Verwendung jeweils `LvVerwendung` („Dienst: Lauf „Lieferverzug-Mails“ nach „Offene Bestellungen (Sage)““).
Für alle gilt Standort „beide“ (Wert je Installation).

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Lieferverzug:KreditorTageNachTermin` (neu), Anzeigename „Kreditor-Mail: Tage nach Liefertermin“ | Int | 0–60, Einheit „Tage“ | `3` (R 2) | Eine Position kommt frühestens so viele Kalendertage nach ihrem Liefertermin in die Kreditor-Mail. Wirksam ist der größere Wert aus dieser Einstellung und „Tage nach Liefertermin“, damit jede Kreditor-Position auch in der Sammelliste steht. Bis dahin zeigt die Sammelliste „Kreditor-Mail ab <Datum>“. | Dienst | beide | „Ein kleinerer Wert erinnert Lieferanten früher (externe Mails, sofern der Testbetrieb aus ist).“ |
| `Lieferverzug:MaxAlterTage` (neu), Anzeigename „Altersgrenze (Tage nach Liefertermin)“ | Int | 0–3650, Einheit „Tage“; 0 = keine Grenze | `180` (R 1) | Positionen, deren Liefertermin mehr als so viele Tage zurückliegt, stehen nur im Abschnitt „Zu klären“ der Sammelliste und gehen nie an Lieferanten. Gilt für Bestellungen und Rahmenabrufe. | Dienst | beide | „0 schaltet die Grenze ab: dann können auch sehr alte Positionen (z. B. Rahmenabrufe seit 2019) an Lieferanten gehen.“ |
| `Lieferverzug:TageNachTermin` (geändert: nur Text und Anzeigename „Sammelliste: Tage nach Liefertermin“) | Int | 0–60 (unverändert) | `3` (unverändert; Betriebswert laut Till **1**, Manifest) | Ab so vielen Kalendertagen nach dem Liefertermin steht eine Position mit Rest > 0 in der Sammelliste an den Einkauf. Untergrenze auch für die Kreditor-Mail. | Dienst | beide | – |
| `Lieferverzug:KreditorMitRahmenabrufen` (neu, **nur bei R 3 = ja**), Anzeigename „Kreditor-Mail auch für Rahmenabrufe“ | Bool | Schalter | `false` | Aus: Rahmenabrufe (EBZ) stehen nur in der Sammelliste, nie in einer Kreditor-Mail. An: wie Bestellungen. | Dienst | beide | „Abruf-Lieferungen, die direkt in Sage gebucht wurden, zieht das WMS nicht ab. Einschalten erinnert Lieferanten möglicherweise an bereits Geliefertes.“ |
| `Lieferverzug:KreditorAbsender`, `Lieferverzug:AntwortAdresse` (vorhanden, **nur Werte**) | String (eine Adresse, Server prüft) | unverändert | unverändert `""` | unverändert; Werte AKE `Einkauf@ideal-ake.at`, IDEAL `einkauf_ideal@ideal-ake.at` | Dienst | AKE bzw. IDEAL je Wert | vorhanden (Relay, IT) |

## Audit-Feld-Auswirkungen

Keine neue Entität und kein neuer Schreibweg. `PurchaseOrderReminders` bleibt bewusst ohne `AuditableEntity` (Antwort 13 der Mutter-Spec). Der
einzige zusätzliche Inhalt sind Zeilen `Art = Einkauf` für die Positionen zu klären. ServiceSettings speichern über den vorhandenen Pfad.

## Akzeptanzkriterien

1. `/ServiceSettings` zeigt in „Lieferverzug-Mail“ die Einträge „Kreditor-Mail: Tage nach Liefertermin“ (3) und „Altersgrenze“ (180), jeweils mit
   Anzeigename, Wirkung, Verwendung, Standort „beide“ und Warnhinweis. Die Werte 61 bzw. 3651 und −1 werden beim Speichern abgelehnt.
2. `TageNachTermin = 1`, `KreditorTageNachTermin = 3`, eine Position mit Termin heute−1: Sie steht in der Sammelliste, aber nicht in einer
   Kreditor-Mail (auch nicht im Testbetrieb). Die Statuszeile lautet „Kreditor-Mail ab <Termin+3>“. Am Tag Termin+3 kommt sie in die Kreditor-Mail.
3. `TageNachTermin = 5`, `KreditorTageNachTermin = 3`: Eine Position mit Termin heute−4 steht in keiner der beiden Mails, eine mit heute−5 in beiden
   (Maximum-Regel).
4. Eine Gruppe mit einer Position bei Tag 1 und einer bei Tag 10 (Kreditor-Schwelle 3) bekommt eine Kreditor-Mail mit genau der Tag-10-Position.
   Die Statuszeile nennt „1 Position(en) erst ab <Datum>“.
5. `MaxAlterTage = 180`: Eine Position mit Termin heute−181 steht nur im Abschnitt „Zu klären“, in keiner Kreditor-Mail und in keiner Gruppe. Eine mit
   heute−180 wird normal behandelt.
6. `MaxAlterTage = 0`: Es gibt keinen Abschnitt „Zu klären“. Alte Positionen werden wie bisher behandelt.
7. Sind nur Positionen zu klären und keine sonst fällig, geht die Sammelliste trotzdem raus. Der Betreff nennt „davon k zu klären“, und je
   Position gibt es eine Protokollzeile `Art = Einkauf`.
8. Positionen zu klären zählen nicht für `KreditorMaxJeLauf`. 31 Kreditoren nur mit alten Positionen und Grenze 30 lösen im Echtbetrieb keine
   Grenzen-Warnung aus.
9. Das Aktivitäts-Protokoll zeigt `zu_klaeren` und `vorlauf` (bei R 3 = ja auch `rahmenabruf_ausgenommen`), DryRun mit „[DryRun]“.
10. (nur R 3 = ja) Schalter aus: Ein fälliger Abruf (EBZ) steht in der Sammelliste, nie in einer Kreditor-Mail, und die Statuszeile meldet es. Schalter
    an: Er wird behandelt wie eine Bestellung.
11. Die bisherigen TS-128-Fälle (128.1 – 128.15) verhalten sich mit `KreditorTageNachTermin = TageNachTermin` und `MaxAlterTage = 0` unverändert
    (Regressionstests grün).
12. Build und alle Tests sind grün, einschließlich Drift-Guard `ServiceSettingDefinitionsTests`, `SettingValidatorTests` und neuer Planer-Tests für
    AK 2–8 (und 10).

## Test-Szenarien

TS-128 in `docs/TESTSZENARIEN.md` (Z. 11647) wird um 128.16 – 128.23 erweitert, danach `secondbrain/tests/testszenarien-index.md` nachziehen.
Vorbedingungen wie TS-128: DEV mit `MailSettings:PickupDirectory` (nur DEV), Testbetrieb an, Einkauf-Empfänger `.invalid`, synthetische Cache-Zeilen
(BelPosId 990001…) mit gesetzten Terminen.

- **128.16 Zwei Schwellen:** `TageNachTermin = 1`, `KreditorTageNachTermin = 3`. Termine heute−1, −2, −3, −10. Erwartet: In der Sammelliste stehen
  alle vier, in der Testbetrieb-Kreditor-Mail nur −3 und −10. Die Statuszeile nennt „2 Position(en) erst ab …“.
- **128.17 Nur Vorlauf:** Ein Lieferant hat nur eine Position heute−1. Erwartet: keine Kreditor-Mail, Status „Kreditor-Mail ab <Datum>“, Count `vorlauf = 1`.
- **128.18 Maximum-Regel (negativ):** `TageNachTermin = 5`, `KreditorTageNachTermin = 3`, Termin heute−4. Erwartet: in keiner Mail.
- **128.19 Altersgrenze:** `MaxAlterTage = 180`, Termine heute−181 (EBB) und heute−400 (EBZ) sowie heute−180. Erwartet: Die ersten beiden stehen
  im Abschnitt „Zu klären“ mit Art „Bestellung“/„Rahmenabruf“ und in keiner Kreditor-Mail, heute−180 normal. Der Betreff nennt „davon 2 zu klären“,
  Count `zu_klaeren = 2`.
- **128.20 Nur alte Positionen:** Alle fälligen Positionen sind älter als 180 Tage. Erwartet: Die Sammelliste kommt nur mit dem Abschnitt „Zu klären“,
  keine Kreditor-Mail, kein Grenzen-Hinweis.
- **128.21 Grenze 0 (negativ):** `MaxAlterTage = 0`. Erwartet: kein Abschnitt „Zu klären“, die heute−400-Position ist normal fällig.
- **128.22 Einstellungen (negativ):** `KreditorTageNachTermin = 61`, `MaxAlterTage = 3651` bzw. `-1`. Erwartet: Speichern wird mit Meldung abgelehnt.
- **128.23 (nur R 3 = ja) Rahmenabrufe:** Schalter aus, fälliger Abruf im Alter von 10 Tagen. Erwartet: Er steht in der Sammelliste, nicht in der
  Kreditor-Mail, mit Statuszeile. Schalter an: Er steht in der Kreditor-Mail.
- **Daten-Durchsicht AKE (Mensch, nach P-L1):** Den Abschnitt „Zu klären“ der echten Sammelliste mit Till durchgehen und drei Positionen in Sage
  gegenprüfen (geliefert, storniert, Termin).

Testbar ohne Mailserver: Planer und HTML (Unit). Nur Manual-UAT: Darstellung des neuen Abschnitts im Mailclient, fachliche Sichtung durch den Einkauf.

## Etappen (nur bei epic: true)

Kein Epic. Die Umsetzung erfolgt in einem Commit im Bündel-Worktree.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

*Vorläufig (Spec-Agent). Der qa-agent bestätigt gegen den echten Diff.*

- **Web-App:** ja (Katalog `ServiceSettingDefinitions` und Validierung liegen im Web-Projekt, dazu Hilfe und Changelog).
- **Service:** ja (Planer, Lauf, Mail-HTML).
- **Migration:** nein.
- **Reihenfolge:** Update-Skript, Web, Dienst. Danach `/ServiceSettings` > „Lieferverzug-Mail“ die Werte setzen (Manifest).
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023, ASCII, Version = vergebene Version, Vorschlag 1.99.19):
  - `@{ Id='v1.99.19-LieferverzugSchwellen'; Version='1.99.19'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Text='/ServiceSettings > Lieferverzug-Mail: Sammelliste Tage nach Liefertermin = 1 (Einkauf, Till), Kreditor-Mail Tage nach Liefertermin = 3 (neu),
    Altersgrenze = 180 Tage (neu; aeltere Positionen nur unter Zu klaeren, nie an Lieferanten).'; Quelle='<changelog-slug>' }`
  - `@{ Id='Pruefung/LieferverzugSammellisteTage'; Version='1.99.19'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Lieferverzug:TageNachTermin'' AND LTRIM(RTRIM([Value])) = N''1'') THEN 0 ELSE 1 END';
    Text='Lieferverzug: Sammelliste steht nicht auf 1 Tag nach Liefertermin (Vorgabe Einkauf). /ServiceSettings > Lieferverzug-Mail pruefen.' }`
  - `@{ Id='Pruefung/LieferverzugAdressenAKE'; Version='1.99.19'; Typ='Pruefung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Pruefung='SELECT CASE WHEN (SELECT COUNT(*) FROM dbo.ServiceSettings WHERE [Key] IN (N''Lieferverzug:KreditorAbsender'', N''Lieferverzug:AntwortAdresse'') AND LOWER(LTRIM(RTRIM([Value]))) = N''einkauf@ideal-ake.at'') = 2 THEN 0 ELSE 1 END';
    Text='Lieferverzug AKE: Absender und Antwortadresse der Kreditor-Mail nicht Einkauf@ideal-ake.at. Setzen in /ServiceSettings; Relay fuer externe Empfaenger mit der IT klaeren (P-M4).' }`
  - `@{ Id='Pruefung/LieferverzugAdressenIDEAL'; Version='1.99.19'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Pruefung='… wie AKE mit N''einkauf_ideal@ideal-ake.at''';
    Text='Lieferverzug IDEAL: Absender und Antwortadresse der Kreditor-Mail nicht einkauf_ideal@ideal-ake.at. Setzen in /ServiceSettings; Relay P-M4.' }`
  - `@{ Id='v1.99.19-LieferverzugInfo'; Version='1.99.19'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Text='Einkauf informieren: Sammelliste ab 1 Tag, Lieferanten erst ab 3 Tagen; Positionen mit Liefertermin aelter als 180 Tage stehen unter Zu klaeren
    und gehen nie an Lieferanten - bitte in Sage bereinigen.'; Quelle='<changelog-slug>' }`
  - Keine Schema-, Daten- oder Einmal-Einträge.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

### Prüfabfragen für den Menschen (vom Agenten nicht ausgeführt)

**P-L1 (WMS-DB, wo v1.99.15 läuft: AKE_WMS_DEV jetzt, Produktion nach dem Deploy) – erste Welle mit den neuen Regeln (ersetzt P-M3).**
Näherung: Abzug der WMS-Erfassung, Adressen, Sperren und Wiederholung fehlen. Die genaue Zahl liefert der Testbetrieb.
```sql
DECLARE @heute date = CAST(GETDATE() AS date), @nEinkauf int = 1, @nKreditor int = 3, @maxAlter int = 180;
SELECT l.Belegkennzeichen, k.Klasse, COUNT(*) AS Positionen, COUNT(DISTINCT l.LieferantKto) AS Kreditoren,
       MIN(l.Liefertermin) AS AeltesterTermin
FROM dbo.SagePurchaseOrderLines l
CROSS APPLY (SELECT DATEDIFF(day, l.Liefertermin, @heute) AS Tage) t
CROSS APPLY (SELECT CASE WHEN l.Liefertermin IS NULL THEN '0 ohne Termin'
                         WHEN @maxAlter > 0 AND t.Tage > @maxAlter THEN '4 zu klaeren'
                         WHEN t.Tage >= @nKreditor THEN '3 Kreditor-Mail'
                         WHEN t.Tage >= @nEinkauf THEN '2 nur Sammelliste'
                         ELSE '1 nicht faellig' END AS Klasse) k
WHERE l.MengeOffen > 0
GROUP BY l.Belegkennzeichen, k.Klasse
ORDER BY k.Klasse, l.Belegkennzeichen;

-- Wichtig fuer die Grenze je Lauf (KreditorMaxJeLauf = 30, alle oder keine):
SELECT COUNT(DISTINCT l.LieferantKto) AS KreditorenKlasse3
FROM dbo.SagePurchaseOrderLines l
WHERE l.MengeOffen > 0 AND l.Liefertermin IS NOT NULL
  AND DATEDIFF(day, l.Liefertermin, CAST(GETDATE() AS date)) BETWEEN 3 AND 180;
```
Ergibt `KreditorenKlasse3 > 30`, gehen im Echtbetrieb an keinem Tag Kreditor-Mails raus, bis die Zahl darunter fällt oder die Grenze angepasst wird.

**P-L2 (Sage-SQL AKE Produktion, nur lesend – vor dem Deploy von v1.99.15, solange der WMS-Cache keine Abrufe hat).** Der View-Name stammt aus
P-M1. Die Spaltennamen sind aus dem SData-Select übernommen und **in der View ungeprüft**. Bei einem Fehler erst
`SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'vwDPSBS_CWS_Mde_EinkaufpositionenInfo'` ausführen.
```sql
SELECT v.Belegkennzeichen, k.Klasse, COUNT(*) AS Positionen, COUNT(DISTINCT v.A0Empfaenger) AS Kreditoren, MIN(t.Termin) AS AeltesterTermin
FROM dbo.vwDPSBS_CWS_Mde_EinkaufpositionenInfo v
CROSS APPLY (SELECT CAST(COALESCE(v.Liefertermin, v.BelegLiefertermin) AS date) AS Termin) t
CROSS APPLY (SELECT CASE WHEN t.Termin IS NULL THEN '0 ohne Termin'
                         WHEN DATEDIFF(day, t.Termin, GETDATE()) > 180 THEN '4 zu klaeren'
                         WHEN DATEDIFF(day, t.Termin, GETDATE()) >= 3 THEN '3 Kreditor-Mail'
                         WHEN DATEDIFF(day, t.Termin, GETDATE()) >= 1 THEN '2 nur Sammelliste'
                         ELSE '1 nicht faellig' END AS Klasse) k
WHERE v.Belegkennzeichen IN ('EBB', 'EBZ')
GROUP BY v.Belegkennzeichen, k.Klasse
ORDER BY k.Klasse, v.Belegkennzeichen;
```
Aussagekräftig ist die Zeile EBZ / „3 Kreditor-Mail“. Diese Abrufe erinnert der Echtbetrieb ohne F3, und sie sind wahrscheinlich längst geliefert.

**P-L3 („bestätigt“, nur lesend, P-M1-Rest).**
- (a) Vertrags-Schema, SData mit dem Servicebenutzer des DEV-Ziels, je Standort: `GET {SData:BaseUrl}/apiBelegerfassung.Sage.API/{Dataset}/$schema`
  (das Ressourcen-`$schema` liefert 401). Im Element `eptEKBelegPosition.udf` Typ und Länge von `KHKEKBelegePositionen_USER_bestaetigt` notieren,
  im Element `eptEKBeleg.udf` die von `KHKEKBelege_USER_ABErhalten`. **IDEAL:** erst mit eingetragenem IDEAL-Endpunkt
  ([[sage-sdata-benutzerfelder-lesetest]] L10 offen). Bis dahin entfällt der Schritt und wird so vermerkt.
- (b) Werte: `GET {SData:BaseUrl}/apiBelegerfassung.Sage.API/{Dataset}/eptEKBelegPosition.udf?format=json&count=20&orderBy=<Schlüsselfeld laut
  Schema>`. Notieren: HTTP-Status (Leserecht des Servicebenutzers), vorkommende Werte von `…USER_bestaetigt` und wie sich eine Position einer
  `BelPosId` des Caches zuordnen lässt.
- (c) Quelle des Caches: WMS `/SageTest` > Feldliste, Vertrag `CommonWawiServicesResources`, Ressource `EinkaufPositionenInfo`,
  Where `Belegkennzeichen eq 'EBB'`, OrderBy `BelPosId`, Anzahl 5. Prüfen, ob ein USER_-Feld oder ein `PositionProperty1–4` mit Inhalt
  „bestätigt“ vorkommt. Laut Lesetest AKE: 0 USER_.
- (d) Alternativ Sage-SQL (nur lesend, Tabellenname aus dem Präfix `KHKEKBelegePositionen_USER_`; Spalten `BelID`/`Belegkennzeichen` **ungeprüft**):
  ```sql
  SELECT c.name, ty.name AS Typ, c.max_length
  FROM sys.columns c JOIN sys.types ty ON ty.user_type_id = c.user_type_id
  WHERE c.object_id = OBJECT_ID('dbo.KHKEKBelegePositionen') AND c.name LIKE 'USER[_]%best%';

  SELECT b.Belegkennzeichen, p.USER_bestaetigt, COUNT(*) AS Positionen
  FROM dbo.KHKEKBelegePositionen p JOIN dbo.KHKEKBelege b ON b.BelID = p.BelID
  WHERE b.Belegkennzeichen IN ('EBB', 'EBZ')
  GROUP BY b.Belegkennzeichen, p.USER_bestaetigt;
  ```

**P-L4 (Sage-Oberfläche AKE, vor Rückfrage 3):** Drei alte Abrufe öffnen, z. B. `2023-147973` (Termin 09.01.2023, am AKE-Test im Cache). Ist die
Abrufmenge geliefert, gibt es also einen Wareneingang bzw. Lieferschein zum Abruf? Bei „ja“ ist belegt, dass Abruf-Lieferungen direkt in Sage gebucht
werden. Das ist die Frage, die Teil-3-Antwort 1 an Till stellt.

## Offene Rueckfragen

1. **Altersgrenze oder Bereinigung in Sage?** Ohne Grenze kämen am AKE-Test 107 Abrufe ab 2019 in die erste Welle. *Empfehlung:* Einstellung
   `Lieferverzug:MaxAlterTage` mit Default 180, gültig für EBB **und** EBZ, 0 = aus. Der Abschnitt „Zu klären“ ist zugleich die Arbeitsliste,
   mit der der Einkauf in Sage bereinigt. Die Alternative „nur Bereinigung in Sage“ braucht keinen Code, schützt aber erst, wenn die Bereinigung
   fertig ist, und nicht gegen neue Altfälle.
2. **Default der Kreditor-Schwelle.** *Empfehlung:* `KreditorTageNachTermin = 3` (Vorschlag der Koordination, Till hat nur die Sammelliste mit 1
   genannt). Wirksam ist das Maximum beider Schwellen, sodass jede Kreditor-Position auch in der Sammelliste steht und keine feldübergreifende
   Prüfung nötig ist.
3. **Rahmenabrufe in der Kreditor-Mail (Konflikt mit Teil-3-Antwort 1).** Belegt ist, dass vor v1.99.15 kein Abruf am WMS gebucht werden konnte. Jede
   bisherige Abruf-Lieferung ist also direkt in Sage gebucht, und solche Lieferungen zieht das WMS nie ab (akzeptierte Grenze). Antwort 1 der
   Teil-3-Spec sieht die Bedingung „nur Sammelliste“ als Nachtrag vor, falls Till direkte Sage-Buchungen bestätigt. Diese Antwort steht noch aus
   (Till, Antwort 14, sagt nur „sollen erinnert werden“). *Empfehlung:* Schalter `Lieferverzug:KreditorMitRahmenabrufen` (Default aus) in dieser
   Spec mitbauen (F3, eine Bedingung im Planer). Der Einkauf schaltet ihn ein, sobald P-L4 und die Bereinigung das erlauben, ohne neues Deploy.
   Bei „nein“ entfallen F3, AK 10 und TS-128.23.
4. **Spalte „bestätigt“ in Sammelliste und Kreditor-Mail.** Der Cache hat das Feld nicht, und die Quelle `EinkaufPositionenInfo` liefert keine
   USER_-Felder (F4). *Empfehlung:* nicht hier, sondern eine Folge-Spec nach P-L3. Sie umfasst Lesequelle, Cache-Spalte (Migration) und Anzeige.
5. **Einstellung „nur bestätigte Positionen an Kreditoren“.** *Empfehlung:* erst entscheiden, wenn P-L3 (b)/(d) zeigt, ob der Einkauf das Feld bei
   offenen Bestellungen durchgängig pflegt. Bei lückenhafter Pflege würde der Filter verzögerte, aber unbestätigte Positionen still verschweigen.
   Gegebenenfalls gehört sie in dieselbe Folge-Spec wie Rückfrage 4.

## Freigabe-Nachtrag 2026-11-13 16:27 (Gerald, geht dem Rumpf vor)
- Gemeinsamer /goal-Lauf mit [[2026-11-08-einkauf-bestellvorschlaege-spec]] auf dem Bündel (Start v1.99.18 `d9e4c89e`). Versions-/TS-/SQL-Nummern
  vergibt der Lauf in Lauf-Reihenfolge (Lieferverzug zuerst; Version ab 1.99.19, TS ab 136, SQL ab 148). P-L1–P-L4 sind keine Blocker.
- Liegt die erste Welle (P-L1 auf DEV) trotz 180 Tagen und ohne Abrufe über `KreditorMaxJeLauf`: als Befund melden – angepasst wird dann das
  Limit, nicht der Filter (Entscheidung Gerald danach).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Altersgrenze `MaxAlterTage` (Default 180, EBB+EBZ, 0 = aus) oder nur Bereinigung in Sage oder beides → **Beides: MaxAlterTage Default 180; parallel bereinigt der Einkauf alte Abrufe in Sage (kein Warten darauf) (Gerald 2026-11-13 16:27).**
2. Kreditor-Schwelle Default 3, wirksam Maximum beider Schwellen → **Ja (Gerald 2026-11-13 16:27).**
3. Schalter „Kreditor-Mail auch für Rahmenabrufe“ (Default aus) jetzt mitbauen oder Teil-3-Antwort 1 unverändert → **Jetzt mitbauen, Default aus; ersetzt insoweit Antwort 1 der Teil-3-Spec. Einschalten erst nach P-L4 (Gerald 2026-11-13 16:27).**
4. Spalte „bestätigt“ als Folge-Spec nach P-L3 → **Ja, Folge-Spec (Gerald 2026-11-13 16:27).**
5. Einstellung „nur bestätigte an Kreditoren“ als Folge-Spec nach P-L3 → **Ja, Folge-Spec (zusammen mit 4) (Gerald 2026-11-13 16:27).**
