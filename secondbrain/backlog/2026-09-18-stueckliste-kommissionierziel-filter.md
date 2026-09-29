---
typ: feature
---
# Stueckliste: Kommissionierziel als Spalte + gespeicherter Standardfilter (Dropdown)

**Aufgenommen 2026-09-18.**

## Worum es geht

In der **Stueckliste** (BOM-Ansicht) soll das **Kommissionierziel** analog zur Kommissionierliste:
1. als **Spalte** erscheinen, die sich ueber das Zahnrad **ein- und ausblenden** laesst,
2. per **Dropdown** gefiltert werden koennen (wie in der Liste oben: Werteauswahl statt Freitext),
3. und diese Filterauswahl soll sich in den **Benutzereinstellungen als Standardfilter** speichern
   lassen - beim naechsten Oeffnen ist sie wieder aktiv.

## Warum das gerade jetzt Sinn ergibt

Das ist der natuerliche Begleiter zu [[2026-09-13-kommissionierung-nur-hauptfa]]: Wird nur noch auf
dem HauptFA kommissioniert und zeigt dessen Stueckliste die **Vollstruktur** (Ruling 4 bleibt), wird
sie lang. Ein Kommissionierziel-Filter laesst jeden Kommissionierer **nur seinen Bereich** sehen - und
als gespeicherter Standard muss er das nicht bei jedem Aufruf neu einstellen.

## ZUERST PRUEFEN - womoeglich ist ein Teil schon da

Der Abschlussbericht der BOM-Bridge (v1.36) nennt ausdruecklich "fuenf hierarchische Spalten" in
der Stueckliste, **darunter Kommissionier-Ziel und Ebene** - die Druck-Whitelist in
`PrintBom.cshtml` kenne sie nur nicht. Das deutet darauf hin, dass die **Spalte in der
Bildschirmansicht bereits existiert**.
**Vor der Spezifizierung am Code klaeren**, was davon schon steht:
- Existiert die Spalte in der Stueckliste? (vermutlich ja)
- Ist sie ueber das Zahnrad ein-/ausblendbar - also in **`ColumnDefinitions` UND `#column-config`**
  registriert? (Fallstricke Abschn. 10: Genau hier liefen die beiden schon einmal auseinander.)
- Gibt es bereits einen Filter darauf, und in welcher Form?
Sonst wird gebaut, was schon da ist.

## Der Standard-Filter: KEIN neuer Mechanismus - die Vorlage existiert bereits [KORRIGIERT]

**Meine erste Einschaetzung ("vermutlich groesser, als es klingt") war falsch** - am Code geprueft
(2026-09-18). Standard-Filter je Benutzer sind **eine etablierte Mechanik**: Das `User`-Modell fuehrt
mehrere `DefaultFilter*`-Felder (`DefaultFilterBeschaffung`, `DefaultFilterArtikelgruppe`,
`DefaultFilterFaWorklistDescription1`, `DefaultWorkbenches` ...).

**Und fuer genau diese Ansicht gibt es bereits einen:**

```csharp
/// Standard-Filterwert fuer die Spalte "Bezeichnung 1" in der Stueckliste (BOM,
/// Client-Mode-Spaltenfilter). NULL/leer = kein Default. Darf die Spaltenfilter-Mini-Syntax
/// nutzen (OR mit `,`).
[StringLength(200)]
[Display(Name = "Standard-Filter Bezeichnung 1 (Stueckliste)")]
public string? DefaultFilterBomDescription1 { get; set; }
```

**Damit ist das die exakte Vorlage** (ponytail, Sprosse 2): ein neues Feld
**`DefaultFilterBomKommissionierziel`** direkt daneben, gleicher Typ (`string?`, `[StringLength(200)]`),
gleiche Mini-Syntax, gleiche Stelle in den Benutzereinstellungen, gleicher Anwendungsweg in der
Stueckliste. Wo `DefaultFilterBomDescription1` gelesen, gesetzt und angewendet wird, kommt das neue
Feld dazu. **Kein neuer Mechanismus, keine Aenderung an den Spaltenpraeferenzen.**

**Folge:** eine kleine Migration (eine nullable Spalte an `Users`). Sonst reine Nachbildung.

Damit erledigen sich die bisherigen Fragen 2 und 3: nicht "allgemein vs. nur Stueckliste" (es ist
das bestehende Je-Feld-Muster) und nicht "wo" (in den Benutzereinstellungen, neben dem
Bezeichnungs-Filter).

### Eine Designfrage bleibt: Dropdown vs. Mini-Syntax

Der **bestehende** Stuecklisten-Filter ist ein **Textfeld mit OR-Syntax** (`KA-02,S-01`). Gewuenscht
ist fuer das Kommissionierziel ein **Dropdown** wie in der Liste oben.
- Ein **einfaches Dropdown** erlaubt **einen** Wert - kein OR.
- Die Mini-Syntax erlaubt **mehrere** - passt zum gespeicherten Feld und zu `DefaultWorkbenches`
  (ebenfalls kommasepariert).
**Zu entscheiden:** Einzelauswahl reicht (ein Kommissionierer, ein Ziel), oder Mehrfachauswahl
(jemand betreut zwei Bereiche)? Mehrfachauswahl im Dropdown + kommasepariert gespeichert waere
konsistent mit dem bestehenden Feldformat.

## Fallstricke, die jetzt schon absehbar sind

**F1 - Ein gespeicherter Filter kann unsichtbar wirken.** Wer einen Standardfilter gesetzt hat und
es vergisst, sieht beim naechsten Mal eine **unvollstaendige Stueckliste** und haelt sie fuer
vollstaendig. Das ist dieselbe Gefahr wie beim gefilterten PDF.
**Pflicht:** Ein aktiver Standardfilter muss **sichtbar** sein (Hinweis/Badge "gefiltert:
Kommissionierziel X") und **mit einem Klick aufhebbar**. Hausregel: sichtbar machen statt still
filtern.

**F2 - Dropdown-Werte: fest oder aus den Daten?** Eine feste Liste veraltet; eine Liste aus den
tatsaechlich vorkommenden Werten (`SELECT DISTINCT`) ist immer aktuell, kann aber bei einer leeren
Stueckliste leer sein. Und: **Was passiert, wenn der gespeicherte Standardwert in der aktuellen
Stueckliste gar nicht vorkommt?** Dann darf nicht still eine leere Liste erscheinen - Filter
ignorieren und darauf hinweisen, oder leere Liste mit klarem Hinweis.

**F3 - AKE hat kein Kommissionierziel.** Die AKE-Stueckliste kommt aus der AKE-View, die kein solches
Feld fuehrt. Im flachen Modus bleibt die Spalte leer. Entweder dort ausblenden (`defaultHidden` im
flachen Modus) oder hausweit sichtbar lassen und leer akzeptieren - dieselbe Frage wie beim Matchcode,
dort wurde "hausweit sichtbar" entschieden. **Die Bedingung bei AKE aber: Ein gespeicherter Filter
auf ein leeres Feld darf die AKE-Stueckliste nicht leerfiltern.**

**F4 - Druck und PDF.** Die Druck-Whitelist kennt das Kommissionierziel noch nicht (bekannter Befund
aus der BOM-Bridge). Soll der aktive Filter in den Ausdruck durchschlagen? Wenn ja, braucht der Druck
denselben "gefilterte Ansicht"-Hinweis wie das PDF - sonst haelt jemand eine gefilterte Liste auf
Papier fuer vollstaendig.

## Offene Fragen

1. **Existiert die Spalte in der Stueckliste schon** und ist sie ueber das Zahnrad ausblendbar
   (`ColumnDefinitions` UND `#column-config`)? - am Code pruefen, vor allem anderen.
2. **Dropdown: Einzel- oder Mehrfachauswahl?** Mehrfach + kommasepariert gespeichert waere
   konsistent zu `DefaultFilterBomDescription1` und `DefaultWorkbenches`.
3. **Dropdown-Werte fest oder aus den Daten?** (vgl. F2)
4. **Verhalten, wenn der gespeicherte Wert in der aktuellen Stueckliste nicht vorkommt?**
5. **AKE: Spalte ausblenden oder leer sichtbar?**
6. **Soll der Filter in Druck/PDF durchschlagen?**
7. **Zeigt die Stueckliste heute schon sichtbar an, dass `DefaultFilterBomDescription1` aktiv ist?**
   Falls nein, ist F1 eine **vorbestehende** Luecke - dann fuer beide Felder zugleich schliessen,
   nicht nur fuer das neue.

## Bezug

[[2026-09-13-kommissionierung-nur-hauptfa]] (Vollstruktur macht die Liste lang - daher der Filter),
[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]] (hierarchische Spalten, Druck-Whitelist),
[[2026-08-12-listen-spaltenauswahl-spec]] (Spaltenpraeferenzen, `supportsSortDefault`),
Fallstricke Abschn. 10 (`ColumnDefinitions` vs. `#column-config`)
