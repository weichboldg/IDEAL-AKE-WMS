---
type: testbericht
title: "Browser-Testbericht TS-79, Teil-C-Schritte mit Datenänderung (v1.47.2)"
protokoll: "[[2026-10-06-ts-79-browser-testprotokoll]]"
spec: "[[2026-09-25-kommissionierung-nur-hauptfa-spec]]"
vorlauf: "[[2026-10-06-ts-79-browser-testbericht-2]]"
system: "http://idealweb01.ideal.ideal-ake.at:88/ (v1.47.2)"
ausfuehrung: Claude Cowork (Abnahme)
datum: 2026-10-06
---

# Browser-Testbericht TS-79, Teil C — 2026-10-06

Gerald hat drei datenverändernde Schritte ausdrücklich im Chat freigegeben:

- Profil-Standardfilter
- Freigabe im Leitstand
- Glas/Fremdbezug umschalten

**Ergebnis: 4 OK, 0 FEHLER, 0 nicht prüfbar, 0 Protokoll passt nicht.**

## Rahmen
- **Benutzer:** `admin`, von Gerald angemeldet.
- **Profil:** Vorher waren alle Standardfilter leer und die Seitengröße stand auf 50. Ich habe nur das
  Feld „Standard-Filter Kommissionierziel (Stückliste)“ geändert, das Kennwort-Feld nicht berührt.
  Am Ende ist das Feld wieder leer; alle anderen Profilfelder habe ich vorher und nachher verglichen,
  sie sind identisch.
- **Stückliste:** `/Picking/Bom/24` (HauptFA 1035235, 73 von 166 Positionen mit Ziel). Neue
  Sitzungen habe ich jeweils in einem neuen Tab geöffnet, weil der `sessionStorage` je Tab gilt.

## Ergebnisse

| Schritt | Ergebnis | Beobachtung | Screenshot |
|---|---|---|---|
| TS-79.5/.7/.21 Profil-Standardfilter | OK | **1.** `!(leer),KA-02` gespeichert → gespeichert `!(leer)`, gelber Hinweis „Alle Ziele schließt Einzelziele aus — gespeichert wurde nur 'Alle Ziele'.“ und „Profil gespeichert.“ **2.** `!(LEER)` → gespeichert `!(leer)`, nur „Profil gespeichert.“, kein Hinweis. **3.** Stückliste in neuem Tab: „Alle Ziele“ vorgewählt, Badge, 73 Zeilen. **4.** `KA'02` → gespeichert `KA'02`. Die Stückliste lädt ohne Konsolenfehler, Badge „Gefiltert: Komm.-Ziel = KA'02“, 0 Zeilen, Hinweis „Standardfilter Kommissionierziel „KA'02“ aktiv — keine Treffer in dieser Stückliste.“ **5.** Feld wieder geleert. | nein |
| TS-79.6 c Sitzung vor Profil | OK | Profil `!(leer)`, Stückliste zeigt „Alle Ziele“. Umgestellt auf KOM, F5: KOM bleibt, 54 Zeilen, Badge „Komm.-Ziel = KOM“. Neuer Tab (neue Sitzung): wieder „Alle Ziele“ aus dem Profil, 73 Zeilen. | nein |
| TS-79.13 Mehrfach-Freigabe | OK | Leitstand: Checkboxen der HauptFA-Zeilen 1037045 und 1039413 gesetzt, „Markierte freigeben“. Meldung „2 Auftrag/Aufträge freigegeben.“ / „Davon 0 mit offener Buchung.“ Beide zeigen danach „Freigabe zurücknehmen“. Warteschlange `/Picking`: 3 Gruppen (1035235, 1037045, 1039413), je genau 1 Zeile mit der HauptFA-Nummer. | nein |
| TS-79.18 Glas/Fremdbezug an Sub-FA | OK | Leitstand, Sub-FA 1043421 (id 101): Glas an → nach Neuladen an; Glas aus und Zukauf an → nach Neuladen Glas aus, Zukauf an; Zukauf aus → nach Neuladen beide aus (Ausgangszustand). | nein |

## Bleibende Datenänderung
- **Freigaben:** 1037045 und 1039413 sind jetzt zur Kommissionierung **freigegeben**. Wie vereinbart
  habe ich das nicht zurückgenommen; die Warteschlange enthält damit 3 statt 1 HauptFA. Bitte bei
  Bedarf im Leitstand zurücknehmen.
- **Profil, Glas und Zukauf:** wieder im Ausgangszustand.

## Hinweis zur Durchführung
Speichern im Profil, die Leitstand-Checkboxen und die Schalter Glas/Zukauf habe ich per Skript
ausgelöst. Sie werden damit über ihre eigenen Klick-Handler bedient; vorher habe ich geprüft, dass
kein Bestätigungsdialog folgt. Den Grund kennt der Bericht schon: Der Chrome-Tab lief im Hintergrund,
und Mausklicks kamen dort unzuverlässig an. Die Dropdown-Auswahl in der Stückliste lief per Maus.
