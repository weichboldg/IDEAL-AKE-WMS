---
typ: feature
---
# Kombinationsgeraete: Trennung nach Montage-Abteilung

Nachtrag aus der IDEAL-Spec-Runde (2026-08-06). In den Teilen 1, 3 und 4 wurden
Kombinationsgeraete **bewusst out of scope** gestellt \u2014 hier steht, was spaeter nachzuholen ist
und warum.

## Worum geht es

Bei Kombinationsgeraeten teilen sich **zwei fachlich verschiedene Auftraege denselben `HauptFA`**.
Unterschieden werden sie erst ueber `[Montage-Abteilung]` aus der View `FAInfos`. Die View
`FAListe` (Positionen/Stueckliste) kann sie **nicht** unterscheiden \u2014 die Montage-Abteilung ist
auftragsbezogen und liegt bewusst nicht auf Positionsebene.

## Was heute gilt (Zwischenstand, nicht Endzustand)

- **Keine Trennung.** Positionen werden nicht nach Montage-Abteilung getrennt; die Abteilung
  erscheint nur im Klartext-Kopf.
- **Kein Fan-out-Join.** `FaHierarchyOrderInfo` wird nie per `INNER JOIN ... ON HauptFA` an die
  Positionszeilen gehaengt \u2014 das erzeugte sonst ein kartesisches Produkt und verdoppelte jede
  Position. Kopfdaten kommen aus einer eigenen Abfrage.
- **Mehrdeutigkeit wird sichtbar gemacht, nicht aufgeloest.** Gibt es zu einem `HauptFA` mehrere
  FAInfos-Zeilen, werden **alle** Kopfvarianten im Klartext aufgefuehrt, das Dokument wird als
  mehrdeutig gekennzeichnet und ein Eintrag ins Aktivitaets-/SyncLog geschrieben.
- **Bekannte Einschraenkung im Druck:** Der Barcode traegt `HauptFA` \u2014 zwei Kombigeraete erhalten
  damit **denselben Barcode**. Die Montage-Abteilung im Klartext-Kopf ist derzeit das einzige
  Unterscheidungsmerkmal fuer den Menschen.

## Warum das nur ein Zwischenstand sein kann

Beim **Beschichtungsauftrag** wiegt es am schwersten: Haben zwei Kopfzeilen unterschiedliche
`Dienstleister` oder `RAL`, waere ein stillschweigend gewaehlter Kopf fachlich falsch \u2014 Teile
fuehren physisch zum falschen Beschichter. Deshalb heute die Kennzeichnung statt einer Auswahl.

## Was nachzuholen ist

1. **Trennschluessel auf Positionsebene** \u2014 entweder liefert Sage ihn (View-Erweiterung), oder es
   gibt eine fachliche Zuordnungsregel Position -> Montage-Abteilung. **Ohne das ist keine echte
   Trennung moeglich** (die FAListe gibt sie nicht her).
2. **Getrennte Dokumente je Montage-Abteilung** in Kommissionierliste, Beschichtungsauftrag und
   Vormontage.
3. **Eindeutiger Barcode** je Auftrag statt je `HauptFA`.
4. **Gruppierung/Anzeige** in den Listen entsprechend erweitern.

## Vorbedingung

Punkt 1 ist die Wurzel \u2014 alles andere haengt daran. Vor der Aufnahme klaeren, ob die Sage-Views
den Schluessel liefern koennen oder ob eine fachliche Regel definiert werden muss.

## Bezug

[[2026-07-29-standort-ideal-teil-1-spec]] (Befund B-1),
[[2026-07-29-standort-ideal-teil-3-spec]] (T3-B3),
[[2026-07-29-standort-ideal-teil-4-spec]] (B-2),
Anhang [[sage-views-ideal]] (Befund B3 der Ideen-Notiz).
