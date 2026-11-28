---
type: aufgabe
status: erledigt
created: 2026-11-28
quelle: geplanter Spec-Lauf
---
# Spec-Lauf Backlog 2026-11-28 10:08

Drei Kandidaten. Alle sind Review-Befunde vom 2026-11-14 und damit Bugfixes. Seit der Sichtung am 2026-11-28 stehen sie auf `offen` (siehe [[2026-11-28-buendel-wird-neuer-main]]). Bei gleichem Datum habe ich nach Schwere gewählt: Sicherheit zuerst, dann die Befunde der Stufe mittel. Keine der Notizen hat `jira:`, deshalb **wurde in Jira nichts geändert**. Die Fragen stehen nur in den Specs. Der Code wurde im Hauptcheckout (main, v1.99.89) gelesen.

## Backlog → Spec

| Backlog (jetzt in-spec) | Spec (Entwurf) | Jira | Fragen |
|---|---|---|---|
| [[2026-11-14-review-befunde-sdata]] | [[2026-11-28-sdata-geheimnisse-review-befunde-spec]] | – | 2 (Gerald) |
| [[2026-11-14-review-befunde-lieferverzug]] | [[2026-11-28-lieferverzug-mail-tagessperre-review-befunde-spec]] | – | 6 (5 Gerald, 1 Fachbereich: Till Zöhrer) |
| [[2026-11-14-review-befunde-update-skript]] | [[2026-11-28-update-skript-review-befunde-spec]] | – | 3 (Gerald) |

Zusätzlich hat der spec-agent einen Bug-Record angelegt: [[2026-11-28-sdata-geheimnisse-review-befunde-bug]].

Vorläufige Nummern: v1.99.93/TS-214 (SData), v1.99.94/TS-215 (Lieferverzug), v1.99.95/TS-216 (Update-Skript). Keine SQL-Nummer, keine Migration.

### SData / Geheimnisse (SD-1, SD-2, SD-3, SD-4, SD-6)
- SD-1: Der Verbindungstest nutzt die gespeicherten Zugangsdaten nur unter allen diesen Bedingungen: Schema, Host und Port sind gleich `SData:BaseUrl`, die Verbindung läuft über https, die Adresse enthält kein `@`, die TLS-Prüfung ist an.
- SD-2: Die Antwort wird als Stream nach 50.000 Zeichen abgebrochen. `& ? #` in orderBy/include werden abgelehnt.
- SD-3: Geheim-Keys werden ohne Unterscheidung von Groß-/Kleinschreibung erkannt. Dabei ist eine zweite Lücke aufgefallen: Der Einwegtor-Key in `HierarchischeStrukturKeys.cs` wird ordinal verglichen. Das ist aus dem Code abgeleitet und nicht ausgeführt.
- SD-4: Ein Manifest-Eintrag `Ort='Web'` kommt dazu. SD-6: zwei getrennte try-Blöcke im `SageBookingWorker`.
- Fragen:
  1. SD-3: Nur `IstGeheim` ändern, oder allgemein jeden Key ablehnen, der sich nur in der Schreibweise von einem Katalog-Key unterscheidet? Empfehlung: allgemein. (Gerald)
  2. SD-1: Gespeicherte Zugangsdaten nie über http senden, auch wenn `SData:BaseUrl` selbst http ist? Vorher Prüfabfrage P-3 auf Prod. Empfehlung: ja. (Gerald)

### Lieferverzug-Mail (LV-1, LV-2, LV-3)
- Die Tagessperre gilt nur noch je Sammelliste. Jeder Lieferant bekommt höchstens eine Mail am Tag.
- Ein Lauf mit Fehlern endet als „fehlgeschlagen“ und löst eine Fehlermail aus, höchstens einmal am Tag. Merker ist der Eintrag im Aktivitätsprotokoll.
- Versand und Protokoll werden getrennt gefangen. Scheitert das Protokoll, bricht der Lauf ab.
- Fragen:
  1. Die Spec vom 2026-11-12 hatte die Sperre „irgendeine Zeile von heute“ akzeptiert. Diese Spec kehrt das um. Bestätigt? (Gerald)
  2. Nach einer Störung kommt die Sammelliste im nächsten Zyklus, unter Umständen Stunden später. Gewünscht, oder erst am nächsten Morgen? (Fachbereich: Till Zöhrer)
  3. Ist die Sammelliste aus oder ohne Empfänger, schließt der Tag nie. Ein Lieferant kann dann noch am Nachmittag seine erste Mail bekommen, aber nie mehr als eine. Einverstanden? (Gerald)
  4. LV-2: Variante A „abbrechen und melden“ (empfohlen) oder B „Protokollzeile vor dem Versand“? (Gerald)
  5. Die Grenzen-Meldung wird ein Fehler statt einer Warnung und dient als Tagesmerker. Einverstanden? (Gerald)
  6. Fehler im Lauf ergeben „Run fehlgeschlagen“ plus Fehlermail. Ist nur der Mailversand nicht konfiguriert, bleibt es bei der Warnung. Einverstanden? (Gerald)

### Update-Skript (UP-1 bis UP-4)
- UP-1 betrifft mehr als die Notiz sagt: Auch Einstellung-, Info- und Leitstand-Einträge verschwinden. Lösung: `deploy-info.txt` erst am Laufende schreiben. Als installierte Version gilt die kleinere Angabe aus Fußzeile und Datei.
- UP-2: Ein pauschales COMMIT-Verbot geht nicht, sechs Manifest-Skripte enthalten ein gepaartes BEGIN/COMMIT. Stattdessen wird ungepaartes COMMIT abgelehnt, ebenso `USE`. Zur Laufzeit prüft das Skript `@@TRANCOUNT`.
- UP-3: Der Marker bekommt `@Jetzt = Get-Date`. UP-4: Aus der Prüfung wird ein Hinweis nur für AKE, der Default 3 bleibt.
- Die Lieferverzug-Spec könnte dieselben Zeilen in `nachlauf.psd1` berühren, beim Merge auf die Reihenfolge achten.
- Fragen:
  1. Soll der Versionsfilter für Daten-Einträge zusätzlich entfallen? Das kehrt TS-126.22 um. Empfehlung: ja. (Gerald)
  2. `@Jetzt = Get-Date` statt einer ADR-0025-Ausnahme, und `Set-Startkennwort.ps1` gleich mit umstellen? Empfehlung: zweimal ja. (Gerald)
  3. Den Text von `v1.99.19-LieferverzugSchwellen` auf „AKE: 1, IDEAL: Standard 3 lassen“ ändern? Empfehlung: ja. (Gerald)

## Ausgelassen

| Backlog | Grund |
|---|---|
| [[2026-11-14-review-befunde-bde]] | kein Hindernis, nur Limit 3 → nächster Lauf (offen: N-B3-5, bde-cockpit.js unmaskiert) |
| [[2026-11-14-review-befunde-lokalzeit]] | kein Hindernis, nur Limit 3 → nächster Lauf (LZ-1, LZ-2) |
| [[2026-11-14-review-befunde-kommissionierbereiche]] | B-1 wartet auf eine Entscheidung von Gerald (Standardwechsel in einer Transaktion bzw. letzten Standard sperren) |
| [[2026-11-14-review-befunde-wareneingang]] | offene Frage N-7 (Glasbestellung als Lagerbestellung) |
| [[2026-11-14-review-befunde-lager]] | offene Fragen an Gerald L-1, L-4 |
| [[2026-11-14-review-befunde-ideal-hierarchie]] | offene Frage N-B3-3 (Briefkopf-Anschrift), sonst nur DEV-Daten |
| [[2026-11-14-review-befunde-tabellen]] | offene Frage Sage-Spalte Fertigungmenge |
| [[2026-11-14-review-befunde-einstellungen]] | offene Frage Sage-View-Spalte Fertigungmenge |
| [[2026-11-14-review-befunde-offene-bestellungen]] | offene Frage N-10 (IDEAL-DEV Wareneingang an) |
| [[2026-11-14-review-befunde-kommissionierung]] | hat schon eine Spec ([[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]]); Rest mit offenen Fragen K-3/K-8/K-13 |
| [[2026-11-26-bde-api-endpunkte-nach-iis-log]] | wartet auf 30 Tage IIS-Log nach dem Deploy von v1.99.81 |
| [[2026-11-26-bde-u1-gefuehrter-ag-wechsel]] | offene Frage an Gerald (Wechsel mit Pause oder nur mit Teilfertig) |
| [[2026-11-18-ideal-bedarfsmeldung-zukaufteile-wie-ake]] | offene Fragen an Gerald |
| [[2026-11-17-beschichtung-kistenzettel]] | wartet auf das Beispiel von Till |
| [[2026-11-12-lagerbestellungen-und-bedarfsmeldungen-gespraech]] | wartet auf einen Gesprächstermin |
| [[2026-11-13-sonderbedarfe-erfassen]] | Felder und Ablauf zuerst bei Gerald bzw. Till erfragen |
| [[2026-11-16-enaio-rest-statt-csv-wareneingang]] | zuerst mit Gerald bzw. dem enaio-Betreuer klären |
| [[2026-11-17-ideal-fa-listen-und-views-anzeige-durchgang]] | setzt einen Durchgang am IDEAL-Testsystem voraus |

**Status nachziehen, nicht geändert:** Wie in [[2026-11-27-spec-lauf-backlog-0810]] stehen weiterhin etwa 23 Notizen auf `offen`, obwohl es eine Spec mit `source_backlog` gibt. Beispiele sind [[2026-11-12-befunde-ake-regression-dev]] und [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu]].
