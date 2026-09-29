---
typ: bug
---
# AKE (main): Stuecklisten-Druck scheitert bei grossen Stuecklisten + Lagerbestellung IST nicht vorbefuellen

**Aufgenommen 2026-09-28.** Zielzweig: **`main`** (AKE-Betrieb) - **nicht** der IDEAL-Buendel-Zweig.
Zwei voneinander unabhaengige Teile, beide klein, gleicher Zweig und gleicher Deploy.

---

## Teil 1 - Stuecklisten-Druck: HTTP-Fehler 404.15

### Befund (Screenshot vom 2026-09-28, akenet01, Port 4443)

```
GET /Picking/PrintBom/3664?visiblePositions=1,1.1,1.1.1,1.1.1.1, ... ,2.22.11&visibleColumns=pick-control,...
-> HTTP 404.15 - RequestFilteringModule: Abfragezeichenfolge zu lang
   (requestFiltering/requestLimits@maxQueryString, IIS-Standard 2048 Zeichen)
```

**Ursache:** Der Druck uebergibt die Liste **aller sichtbaren Positionsnummern** im Query-String. Bei grossen
Stuecklisten - hier mehrere hundert Positionen - sprengt das die Grenze. "Manchmal" heisst: abhaengig allein
von der Groesse der Stueckliste, nicht vom Anwender oder vom Filter.

**Auffaellig:** In diesem Fall stehen offenbar **alle** Positionen der Stueckliste in der Liste - es war kein
Filter aktiv. Uebergeben wurde die vollstaendige Liste trotzdem.

### Loesungswege

- **(a) `maxQueryString` in der `web.config` hochsetzen** - *nicht als alleinige Loesung.* Verschiebt nur die
  Grenze; die naechstgroessere Stueckliste scheitert wieder, und dahinter warten weitere Grenzen (`maxUrl`,
  http.sys). Allenfalls als Sofortmassnahme bis zum eigentlichen Fix.
- **(b) Druck per POST statt GET** - Positions- und Spaltenliste im Formular-Body, Ziel ein neues Fenster
  (`<form method="post" target="_blank">`). Keine praktische Laengengrenze. Die Antiforgery-Konvention des
  Hauses beachten (POST-Actions tragen das Token). **Empfohlen.**
- **(c) Ergaenzend: Sind alle Positionen sichtbar, gar keine Liste senden.** Dieselbe Konvention wie bei
  `visibleColumns` (leer = alle). Behebt den haeufigsten Fall - ungefilterter Druck - ohne Umbau, aber nicht
  den Fall "grosse Stueckliste mit Filter, der nur wenige Zeilen ausblendet". **Nur als Ergaenzung zu (b).**
- **(d) Filterkriterien statt Positionsliste senden** - geht nicht. Die Filterlogik laeuft ausschliesslich
  im Browser (`bomMatchesFilter`); der Server kann sie nicht nachbilden. Siehe
  [[2026-09-23-suchsyntax-spaltenfilter-erweitern]] - genau diese Verstreuung der Filterlogik.

**Empfehlung: (b), mit (c) als kostenlose Ergaenzung.**

### Uebertrag ins IDEAL-Buendel - per VORWAERTS-MERGE, nicht doppelt implementieren (Entscheidung 2026-09-28)

`Bom.cshtml` und `PrintBom` sind im Buendel-Zweig **stark veraendert** (Kommissionierziel-Filter, Badges,
Leerzustand, "kein Druck bei null sichtbaren Zeilen" aus [[2026-09-25-kommissionierung-nur-hauptfa-spec]]).
Die Ursache besteht dort ebenfalls.

**Vorgehen:**
1. Fix **einmal** in `main` umsetzen und auf AKE ausliefern.
2. **Direkt danach `main` in den Buendel-Zweig `feature/2026-08-07-ideal-teile-1-5` mergen** und die
   Konflikte in `Bom.cshtml`/`PrintBom` dort aufloesen - der neue POST-Ausloeser muss mit der
   Leerzustand-Sperre des Buendels zusammenspielen (bei null sichtbaren Zeilen weiterhin kein Druck).
3. Buendel-Tests laufen danach gruen; der Druck funktioniert im Buendel mit einer grossen Stueckliste.

**Warum nicht in beiden Zweigen getrennt umsetzen:** Zwei leicht verschiedene Fassungen desselben Fixes
stuenden sich beim spaeteren Buendel-Merge im Konflikt gegenueber - der Konflikt waere nicht weg, sondern
verdoppelt. Der Vorwaerts-Merge loest ihn **einmal**, jetzt, solange der Fix frisch ist; das Buendel traegt
danach **denselben** Fix, und beim grossen Merge gibt es an dieser Stelle nichts mehr zu entscheiden.

**Zeitpunkt:** nicht, waehrend ein `/dev`-Lauf im Buendel-Worktree laeuft - erst dessen Ende abwarten.

### Erhebung

Wo sonst werden **Listen** per GET-Query an Druck oder PDF uebergeben? Spaltenlisten (`visibleColumns`) sind
kurz und unkritisch; **Positions- oder Zeilenlisten** sind das Problem. Jede Fundstelle mit demselben Muster
gehoert mitbehoben oder benannt.

---

## Teil 2 - Lagerbestellung: IST nicht mehr mit "Bestellt" vorbefuellen

### Anforderung

Das Feld **IST** wird nicht mehr mit der bestellten Menge vorbefuellt.

*Einordnung:* Ein vorbefuelltes IST laedt dazu ein, ohne Zaehlen zu bestaetigen - Differenzen fallen dann nie
auf. Ein leeres Feld erzwingt eine bewusste Eingabe.

### Zu klaeren

1. **Leer oder 0?** *Empfehlung: leer.* Eine vorbefuellte `0` saehe aus wie "nichts entnommen" und wuerde beim
   Bestaetigen als echte Menge gebucht - dieselbe Falle wie die Vorbefuellung, nur in die andere Richtung.
2. **Was passiert beim Speichern oder Abschliessen mit leerem IST?** Pflichtfeld mit sichtbarer Meldung - oder
   "leer = Zeile noch nicht bearbeitet"? **Ein leeres IST darf nie still als 0 gebucht werden.**
3. **Folgewirkung am Code pruefen - der wichtigste Punkt:** Verlaesst sich irgendetwas darauf, dass IST
   gefuellt ist? Buchung, Bestandsaenderung, Statuswechsel, Rueckschreibung nach Sage, Druck, Summen. Ein
   Feld, das bisher **nie** leer war, kann jetzt an Stellen `null` sein, die das nicht erwarten.
4. **Datentyp:** Ist das IST-Feld heute `NOT NULL` mit Standardwert? Dann braucht "leer" eine Migration
   (nullable). Am Code pruefen.
5. **Bestehende offene Lagerbestellungen:** behalten ihre vorbefuellten Werte, oder zuruecksetzen?
   *Empfehlung:* unveraendert lassen - nur das Verhalten fuer neue Zeilen aendert sich.
6. **Komfort-Schaltflaeche "IST = Bestellt" je Zeile?** Vorsicht: Sie braechte das Bestaetigen ohne Zaehlen
   zurueck, nur einen Klick weiter entfernt. Nur, wenn fachlich ausdruecklich gewollt.

---

## Umsetzung - fuer BEIDE Zweige (Entscheidung 2026-09-28)

Beide Teile gelten fuer **AKE (`main`) und IDEAL (Buendel)**. Umgesetzt wird **einmal** in `main`; der
**Vorwaerts-Merge** `main` -> `feature/2026-08-07-ideal-teile-1-5` bringt **beide** Teile ins Buendel (siehe
Teil 1, Abschnitt Uebertrag). Keine zweite Umsetzung.

**Was "beide Zweige" fuer Teil 2 zusaetzlich bedeutet:**
- **Die Folgewirkungs-Analyse (Frage 3) in BEIDEN Zweigen fuehren.** Hat das Buendel seit August eigenen
  Code hinzugefuegt, der IST liest, ist der in `main` nicht sichtbar - und bekommt nach dem Merge ploetzlich
  `null`. Jede Lesestelle im Buendel gehoert mit bewertet.
- **Nach dem Vorwaerts-Merge im Buendel pruefen**, dass die Lagerbestellung dort ebenfalls ohne
  Vorbefuellung laeuft und kein IDEAL-spezifischer Pfad ein leeres IST falsch behandelt.

**Migrationsfalle, falls IST nullable werden muss (Frage 4):**
- **SQL-Skriptnummer:** Die Nummern sind fortlaufend, und das Buendel ist weit vor `main` (dort bereits
  `SQL/93` und hoeher). Die Nummer fuer `main` muss in **beiden** Zweigen frei sein - also **hinter** der
  hoechsten Buendel-Nummer, nicht die naechste freie in `main`. Sonst tragen nach dem Merge zwei
  verschiedene Skripte dieselbe Nummer.
- **EF-Modell-Snapshot:** Tragen beide Zweige Migrationen, entsteht beim Vorwaerts-Merge ein sicherer
  Konflikt in `ApplicationDbContextModelSnapshot.cs`. Aufloesen, indem **beide** Aenderungen erhalten
  bleiben; danach `dotnet ef migrations list` und ein Build pruefen, dass die Kette stimmt.
- **Zeitstempel:** Die neue `main`-Migration ist juenger als die Buendel-Migrationen und reiht sich damit
  korrekt hinten ein - hier droht kein Problem, aber am Ende nachpruefen.

**Deploy:** web auf AKE; Migration nur, falls Teil 2 sie braucht. Im Buendel wirkt alles erst mit dessen
eigenem Deploy.
