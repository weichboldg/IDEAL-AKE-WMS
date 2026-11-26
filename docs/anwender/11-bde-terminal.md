# 11 – BDE: Arbeitsgänge buchen

Stand: Version 1.99.81 (Fortsetzen oder Abschließen, Fertig mit Restmenge, Kopf einklappbar, Gruppen-Abschluss je Werkbank, Prüfung aller Buchungen; Text, Fotos von 1.99.78), 1.99.79 (Doku-Nachzug 1.99.80: Meldung „Terminal nicht zugeordnet“; Scan „läuft bereits“, Sub-FA und Laufzeit in „Meine Buchungen“, Tageshistorie nach Anmeldung; Text), 1.99.78 (Terminal-Seite abgelöst, Kapitel umgeschrieben, Tageshistorie neu; alle Fotos 1.99.78), 1.99.77
(Meine Buchungen, Scan, Tätigkeit), 1.99.76 (FA-Sicht) · Standorte AKE und IDEAL · Bilder aus dem IDEAL-Testsystem

## Zweck

Auf der **BDE-Seite** (Betriebsdatenerfassung) melden Werker in der Fertigung, woran sie arbeiten: Rüsten, Produktion, Pause, Teil- und
Fertigmeldung mit Gut- und Ausschussmenge, dazu ungeplante Tätigkeiten wie Wartung oder Reinigung. Daraus entstehen die Buchungen, die der
Schichtleiter im Cockpit und in der Buchungsliste sieht, und der Status der Arbeitsgänge in der FA-Struktur.

Seit Version 1.99.78 gibt es die frühere Terminal-Seite (Karten „1. Operator“ und „2. Arbeitsgang wählen“) nicht mehr. **BDE → Terminal**, die
Kachel auf der Startseite und alte Lesezeichen öffnen an beiden Standorten die BDE-Seite der Terminal-Werkbank.

## Wer darf

| Aufgabe | Rolle (wie unter *Stammdaten → Rollen*) |
|---|---|
| BDE-Seite bedienen (anmelden, starten, pausieren, melden, Tageshistorie) | **BDE-Mitarbeiter** |
| zusätzlich Cockpit, Buchungsliste, BDE-Stammdaten pflegen | **BDE-Schichtleiter** |
| zusätzlich Buchungen korrigieren und stornieren, Terminals einrichten | **BDE-Admin** |
| alles | **Administrator** |

**Voraussetzungen** (richtet die WMS-Betreuung bzw. der BDE-Admin ein):

- *Stammdaten → Einstellungen*: **BDE (Betriebsdatenerfassung)** an. Sonst fehlt das Menü **BDE**.
- Für den angemeldeten WMS-Benutzer (meist ein Terminal-Benutzer je Arbeitsplatz, am Handy ein eigenes WMS-Konto je Gerät) ist unter
  *BDE → Stammdaten → Terminals* ein **Terminal** mit Werkbank angelegt. Sonst kommt „Für diesen Benutzer ist kein Terminal konfiguriert.
  Bitte an den BDE-Admin wenden.“
- Die Werkbank ist unter *Stammdaten → Werkbänke* auf **BDE aktiv** geschaltet. Sonst zeigt die Seite „Werkbank ist nicht BDE-aktiv“ mit
  einer Auswahl der BDE-aktiven Werkbänke.
- Jeder Werker ist unter *BDE → Stammdaten → Operatoren* mit seiner **Personalnummer** angelegt.

## Schritt für Schritt

### 1. Öffnen und anmelden

Menü **BDE → Terminal**.

![BDE-Seite: Anmelden mit Personalnummer](bilder/11-01-terminal-operator-angemeldet.jpg)

1. Die Seite öffnet die **Werkbank des Terminals**. Eine andere BDE-aktive Werkbank wählen kann seit 1.99.81 nur der **BDE-Admin** (oben
   rechts; der Werker bleibt dabei angemeldet).
2. **Personalnummer** scannen (Ausweis, auch mit dem Handscanner ohne Antippen) oder eintippen und **Anmelden** drücken.
3. Der Werker-Balken zeigt Name und Personalnummer, rechts **Tageshistorie** (Abschnitt 4) und **Werker wechseln** (meldet ab, der nächste
   meldet sich an).
4. Welche Ansicht kommt, legt die Werkbank fest (*BDE-Standardsicht*): an IDEAL-Werkbänken mit BDE-Teileliste die Reiter **Teile Einzeln** /
   **Teile Summiert** (Abschnitt 5), sonst der Reiter **FA** (Abschnitt 2). An AKE gibt es nur den Reiter **FA**.
5. **Kopf einklappen** (seit 1.99.81): Werker, Werkbank und „Meine Buchungen“ lassen sich mit dem Knopf über dem Kopf einklappen, damit mehr
   Platz für die Liste bleibt. Eingeklappt zeigt eine Kurzzeile Werker und laufende Buchung; Meldungen bleiben sichtbar. Das Gerät merkt sich die Wahl.

### 2. Reiter FA: Arbeitsgänge als Tabelle (seit Version 1.99.76)

![FA-Sicht: Tabelle mit Haupt-FA, Sub-FA, Arbeitsgang, AG-Status, Status und den Knöpfen Rüsten und Produktion](bilder/11-05-fa-sicht.jpg)

1. **Spalten:** Haupt-FA, Sub-FA, Arbeitsgang, **AG-Status** (alle Arbeitsgänge des Auftrags als Zeichen wie in der FA-Struktur, der Arbeitsgang
   der Zeile ist **umrahmt**), Bezeichnung, Artikelnummer, Matchcode, Sollmenge, FE-Termin, Soll-Termin AG, Prio, Kunde, Abteilung, Status.
   Weitere (Material, Arbeitsschritte, Bemerkung PN, Sage-Status …) über das **Zahnrad**. Jede Spalte hat einen **Spaltenfilter**; lange Listen
   werden **geblättert**. Reihenfolge: Priorität, dann früherer Fertigungstermin.
2. **Status:** *offen*, *überfällig*, *läuft* (ein anderer Werker arbeitet daran) oder **läuft bei Ihnen** (eigener laufender Arbeitsgang) –
   immer als Text, nicht nur als Farbe.
3. **Anstempeln:** in der Zeile **Rüsten** oder **Produktion** antippen. Danach grüne Meldung „Produktion gestartet.“ bzw. „Rüsten gestartet.“.
   Läuft der Arbeitsgang schon bei einem anderen Werker, öffnet sich das Fenster **„Arbeitsgang bereits in Arbeit“** mit
   „AG ist bereits in Arbeit durch <Name> an <Werkbank> seit hh:mm.“ – **OK** schliesst, gebucht wird nichts.
4. **Werkbank „nur AG-Fertigmeldung“** (z. B. Entgraten): je Zeile nur der Knopf **Fertig melden**. Das Fenster „Mengen melden“ ist mit der
   **Sollmenge** vorbelegt, Ausschuss 0. Liegt die Gutmenge unter der Sollmenge, fragt ein Fenster nach. Gespeichert werden Werker, Werkbank,
   Zeitpunkt und Menge, aber keine Arbeitszeit. Ein zweites Melden lehnt das WMS ab: „Arbeitsgang ist bereits fertig gemeldet.“

Mengen gibt man im Fenster **Mengen melden** ein:

![Fenster „Mengen melden“ mit Gutmenge, Ausschuss, Abbrechen und Bestätigen](bilder/11-04-mengen-melden.jpg)

**Bestätigen** einmal antippen und warten, bis das Fenster zu ist und oben die grüne Bestätigung erscheint.

### 3. Meine Buchungen, Scan und Tätigkeit (seit Version 1.99.77)

![Oben „Meine Buchungen“ mit einer laufenden Produktion (Pause, Teilfertig, Fertig) und einer pausierten (Fortsetzen), darunter das Feld „FA scannen“](bilder/11-06-kopf-buchungen.jpg)

1. **Meine Buchungen** (unter dem Werker-Namen): alle **laufenden** und **pausierten** Buchungen des Werkers – laufende mit
   durchgezogenem Balken und „… läuft“, pausierte mit gestricheltem Balken und „… pausiert“. Die Knöpfe je Zeile:
   - Rüsten läuft: **Pause**, **Rüsten fertig**, **Produktion** (Wechsel vom Rüsten in die Produktion),
   - Produktion läuft: **Pause**, **Teilfertig**, **Fertig** (jeweils mit dem Fenster „Mengen melden“; Gut ist Pflicht; bei Fertig unter oder über
     der Sollmenge kommt eine Rückfrage),
   - Tätigkeit läuft: **Beenden**,
   - pausiert: **Fortsetzen** (setzt als Produktion fort). Die Zeile zeigt immer Werkbank und Pausenbeginn („Kanten, seit 25.11. 14:10“).
     Geht Fortsetzen nicht mehr (z. B. FA in Sage abgeholt, Arbeitsgang gehört nicht zu dieser Werkbank), steht **„Nicht fortsetzbar: <Grund>“**
     dabei und statt Fortsetzen der Knopf **Abschließen** (seit 1.99.81): nach einer Rückfrage wird die Buchung beendet, die Zeit bis zur Pause
     bleibt gebucht, eine Menge wird nicht gemeldet.
   **Fertig nach Teilmengen** (seit 1.99.81): Wurden schon Teilmengen gemeldet, ist Gut mit dem **Rest** (Sollmenge minus bisher Gut) vorbelegt;
   das Fenster nennt Soll, bisher und Rest. Ausschuss kann dazukommen. Fertig mit **0 Gut und 0 Ausschuss** geht nur nach Teilmengen (der Rest
   ist dann schon gemeldet); ohne Teilmenge lehnt das WMS 0/0 ab. Erreicht eine Teilmenge die Sollmenge, sagt die Meldung „Sollmenge erreicht –
   bitte Fertig melden“.
   Nach jeder Buchung lädt die Seite neu und zeigt oben eine grüne Meldung. Seit 1.99.79 steht bei Arbeitsgängen eines Sub-FA
   „<FA> · Sub-FA <Sub> / <AG>“, und die Laufzeit zählt ohne Neuladen weiter (alle 15 Sekunden).
2. **Zeiten und Details** (Schalter rechts): blendet je Buchung Startzeit, Laufzeit, Werkbank, Terminal und Personalnummer ein. Die Wahl
   merkt sich der Browser **je Werker**; sie ändert nur die Anzeige.
3. **Fertig mit weiteren Werkern:** Arbeiten andere noch auf demselben Arbeitsgang (Einstellung Mehrfachbuchung je Arbeitsgang), fragt das
   Fenster **„Weitere aktive Buchungen“**: **Ja, alle beenden** beendet sie („<n> weitere Buchungen beendet.“), **Nein, nur meine** lässt sie laufen.
4. **Mehrere Produktionen gleichzeitig** (Einstellung gleichzeitiger Abschluss): **Fertig** öffnet **„Alle Produktionsaufträge abschliessen“**
   mit „Haupt-FA · Sub-FA / AG“, Werkbank, Sollmenge, Gut, Ausschuss und Haken *Abschl.* – seit 1.99.81 nur die Produktionen **derselben
   Werkbank** wie die angeklickte. Jede Zeile braucht eine Menge (ganze Zahlen); liegt Gut unter Soll, fragt **ein** Fenster für alle Zeilen nach.
   **Alle fertig melden** bucht alle („<n> Buchungen fertig gemeldet.“), **Abbrechen** bucht nichts.
5. **FA scannen** (Feld über der Tabelle, auch mit dem Handscanner ohne Antippen): `FA` oder `FA/AG` (bzw. `FA,AG`). Eindeutig: die Zeile wird
   **markiert** (Rahmen und Balken), die Seite blättert zu ihr und der Knopf **Rüsten** bzw. **Fertig melden** ist gewählt. Mehrere offene
   Arbeitsgänge: Fenster **„Arbeitsgang wählen“**. Gelbe Meldung (seit 1.99.79 getrennt): **„nicht gefunden“** bei unbekannter Nummer,
   **„läuft bereits (offene Buchung)“**, wenn der Arbeitsgang schon gebucht wird, sonst **„hat an dieser Werkbank keinen offenen Arbeitsgang“**. Ist die Zeile durch einen
   Spaltenfilter ausgeblendet, sagt das eine Meldung – Spaltenfilter zurücksetzen.
6. **Tätigkeit starten** (Knopf rechts oben): Auswahl der ungeplanten Tätigkeiten (z. B. Reinigung); die Tätigkeit erscheint dann unter
   „Meine Buchungen“ mit **Beenden**.
7. Jeder Knopf bucht **einmal** – ein zweites Tippen während der Buchung wird ignoriert. Im Mengen-Fenster bestätigt auch die Eingabetaste.
8. **Zweiter Arbeitsgang, während eine Produktion läuft** (seit 1.99.81): Das WMS bucht nichts und sagt „Es läuft noch Ihre Produktion
   <FA/AG> (seit hh:mm). Solange sie läuft, kann kein zweiter Arbeitsgang starten …“. Die laufende Zeile unter „Meine Buchungen“ ist markiert;
   dort **Pause** oder **Fertig** melden, dann neu starten.

### 4. Tageshistorie (seit Version 1.99.78)

Knopf **Tageshistorie** im Werker-Balken oder Menü **BDE → Tageshistorie**.

![Tageshistorie: Tabelle mit Zeit, Art, Typ, Auftrag / Tätigkeit, Werkbank, Dauer, Gut, Ausschuss und Status](bilder/11-07-tageshistorie.jpg)

1. Die Seite zeigt **Ihre Buchungen und Teilmeldungen von heute**, neueste zuerst, an allen Werkbänken.
2. **Art** *Buchung*: Typ Rüsten, Produktion oder Tätigkeit, Zeit von–bis, Dauer in Minuten, Gut und Ausschuss, Status *läuft*, *pausiert*,
   *fortgesetzt* oder *beendet*. Stornierte Buchungen fehlen. Eine Buchung über Mitternacht (z. B. 22:00–06:00) steht seit 1.99.81 an
   **beiden** Tagen, dann mit Datum („25.11. 22:00–26.11. 06:00“); die Spalte Auftrag nennt den Sub-FA.
3. **Art** *Teilmeldung* (Teileliste, IDEAL): Typ **Teilfertig** oder **Fertig**, Auftrag als „FA … / Sub-FA … · Pos. … · Matchcode“, Gut und
   Ausschuss. Meldungen auf Lagerbestell-Zeilen beginnen mit „Lagerbestellung · “. Fehlt FA oder Sub-FA (Wert 0), entfällt der Teil (seit 1.99.79). Die Fertigmeldung einer Baugruppe steht nur einmal da.
4. Jede Spalte hat einen **Spaltenfilter**, lange Listen werden geblättert. **Spaltenfilter zurücksetzen** hebt alle Filter auf.
5. **Zurück zur Liste** führt auf die BDE-Seite der Werkbank.
6. Über den Menüpunkt ohne angemeldeten Werker kommt zuerst die Anmeldung; danach öffnet direkt die Tageshistorie (seit 1.99.79).

### 5. Werkbank mit BDE-Teileliste (IDEAL, seit Version 1.99.32)

Ist an der Werkbank **BDE-Teileliste (vereinfacht)** an (z. B. *Kanten*), zeigt die Seite die Reiter **Teile Einzeln** und **Teile Summiert**:
Teile und Baugruppen nach Matchcode, teilfertig oder fertig melden, einzeln oder als Block. Anleitung: [Kapitel 17](17-bde-teileliste-werkbank.md).

## Unterschiede AKE und IDEAL

| | AKE | IDEAL |
|---|---|---|
| Reiter | nur **FA**; Spalten Sub-FA, Abteilung, Material leer | **FA** neben **Teile Einzeln** / **Teile Summiert**; Startreiter je Werkbank (BDE-Standardsicht) |
| Fertigungstermin | Termin des Auftrags | FE-Termin der Struktur |
| Werkbänke mit Kürzel (z. B. EG) | keine | mehrere Werkbänke mit demselben Kürzel sehen und buchen dieselben Arbeitsgänge |
| BDE-Teileliste (vereinfachte Fertigmeldung) | – | je Werkbank einschaltbar, siehe [Kapitel 17](17-bde-teileliste-werkbank.md) |
| Tageshistorie | nur Buchungen | Buchungen und Teilmeldungen |

## Hinweise und Stolperfallen

- **Doppelte Fertigmeldung** ist seit Version 1.99.23 ausgeschlossen (auch bei zwei Geräten gleichzeitig). Jeder Knopf bucht einmal; ist trotzdem
  etwas doppelt: dem Schichtleiter melden, er storniert die zweite Buchung unter *BDE → Buchungen*.
- **Doppelte Buchungen schwer zu erkennen (IDEAL).** In *BDE → Buchungen* zeigt die Spalte „Ziel“ nur Haupt-FA und Arbeitsgang (z. B.
  `1059463/EG`), nicht die Sub-FA. Zur Prüfung Zeit und Werker vergleichen.
- **Erst den Werker anmelden, dann scannen.** Vor der Anmeldung landet ein Scan im Feld „Personalnummer“, danach im Feld „FA scannen“.
- „FA … ist bereits verpackt/abgeholt. Keine BDE-Buchung mehr möglich.“: Der Auftrag ist in Sage abgeschlossen; nur noch laufende Buchungen
  beenden, eine pausierte **abschließen**.
- Mengen dürfen nicht negativ sein; Gut- oder Ausschussmenge muss angegeben werden.
- Ist der Schichtkalender eingeschaltet, pausiert das WMS laufende Buchungen am Schichtende selbst („auto-pausiert“); am nächsten Tag
  **Fortsetzen** unter „Meine Buchungen“.
- **Alle Buchungen** (Pause, Fortsetzen, Fertig, Teilfertig, Gruppe, Abschließen) prüfen seit 1.99.81 Terminal, Werker und ob die Buchung Ihnen
  gehört. Eine an einem Terminal gestartete Buchung können Sie an einem anderen Terminal beenden. Mögliche Meldungen: „Diese Buchung gehört nicht
  dem angemeldeten Werker – Seite neu laden.“, „Dieser Werker ist nicht (mehr) aktiv – bitte neu anmelden.“ und bei einem Fehler im WMS
  „Serverfehler – nichts sicher gebucht. Seite neu laden und Buchung prüfen …“ (dann unter „Meine Buchungen“ nachsehen).
- „Dieses Terminal ist nicht dem angemeldeten Benutzer zugeordnet – Seite neu laden. Nichts gebucht.“ (seit 1.99.79): Die Seite war
  noch unter einem anderen Konto offen. Seite neu laden (F5) und die Buchung wiederholen; bleibt die Meldung, die WMS-Betreuung
  informieren (Terminal-Zuordnung prüfen).
- **Handy (Android):** eigenes lokales WMS-Konto je Gerät mit Formular-Login und eine Terminal-Zuordnung – richtet die WMS-Betreuung ein. Auf
  schmalen Bildschirmen erscheinen die Tabellen als Karten.

## Häufige Fragen

**Ich sehe meinen Arbeitsgang nicht.**
Richtige Werkbank gewählt (oben rechts)? Spaltenfilter zurücksetzen oder den FA scannen. Erledigte Arbeitsgänge stehen nicht mehr in der Liste.

**Wie korrigiere ich eine falsche Menge?**
Auf der BDE-Seite nicht. Der BDE-Admin korrigiert oder storniert die Buchung unter *BDE → Buchungen*; Teilmeldungen siehe Kapitel 17.

**Ich habe vergessen, auf Pause zu drücken.**
Dem Schichtleiter melden; vergessene Buchungen kann ein Admin in der Buchungsliste schließen.

**Was heißt das rote Warndreieck?**
Der Arbeitsgang ist überfällig: Der Fertigungstermin plus der für den Arbeitsgang eingestellte Versatz in Arbeitstagen (*BDE → Stammdaten → Arbeitsgänge*) liegt vor heute.
