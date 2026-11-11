---
type: adr
id: 0020
title: Einstellungen mit festem Wertebereich sind Auswahlfelder und beschreiben Wirkung und Verwendung - fuer AppSettings und ServiceSettings
status: accepted
date: 2026-11-07
supersedes: ""
superseded_by: ""
---

> Vorgabe Gerald 2026-11-07: "Die Einstellungen gilt fuer Service und normale Einstellungen immer.
> Sollen bitte als Dropdown-Werte sein - verringert Risiko von Tippfehlern. Und etwas sauberer
> strukturieren, uebersichtlicher machen und mehr Informationen, was das Setting bewirkt und wo es
> verwendet wird." Ergaenzt [[0008-servicesettings-db-first-mit-typisiertem-katalog]] und
> [[0011-feature-toggles-ueber-appsettings]]; Umsetzung fuer den Bestand:
> Backlog `2026-11-07-einstellungen-auswahlfelder-und-beschreibung`.

## Kontext und Problem

Es gibt zwei Einstellungsorte: `/Settings` (Tabelle `AppSettings`, Key/Value/Description, ADR 0011)
und `/ServiceSettings` (typisierter Katalog `ServiceSettingDefinitions`, Typen Bool/Int/String, ADR 0008),
dazu kuratierte Masken wie `/StandortEinstellungen`. Werte mit festem Wertebereich werden heute
teils als Freitext gepflegt - mit v1.73.0 z. B. die Quellenschalter `Sync:Quelle:*`
(`View` | `SData` | `Vergleich`, [[0019-quellen-weiche-view-sdata-je-abgleich]]). Ein Tippfehler
("Sdata", "view ") faellt erst zur Laufzeit auf oder wird still als Default gelesen. Ausserdem
sagt die Oberflaeche zu wenig darueber, was ein Schalter bewirkt, wo er wirkt und was beim
Umschalten passiert (z. B. "sendet Buchungen an Sage").

## Betrachtete Optionen

- **Freitext lassen, nur Beschreibung verbessern** - billig, Tippfehlerrisiko bleibt.
- **Auswahlwerte nur in der Oberflaeche** - Tippfehler in der Maske weg, aber Direktschreibwege
  (SQL-Skripte, Seeds, API) bleiben ungeprueft.
- **Wertebereich im Katalog, Oberflaeche und Server pruefen beide dagegen** - gewaehlt.

## Entscheidung

Gilt fuer **alle** Einstellungen - AppSettings (`/Settings`), ServiceSettings (`/ServiceSettings`)
und kuratierte Masken - fuer den Bestand und fuer jede neue Einstellung:

1. **Typ bestimmt das Bedienelement.**
   - Bool -> Schalter.
   - Fester Wertebereich (Enum-artig, z. B. `View|SData|Vergleich`, Standort, Modus) -> **Auswahlliste
     (Dropdown)**, nie Freitext. Die erlaubten Werte stehen im Katalog, je Wert mit Anzeigetext.
   - Verweis auf Stammdaten (Lagerplatz, Mitarbeiter, Arbeitsgang, Kategorie ...) -> Auswahl aus
     den Stammdaten, gespeichert wird der Schluessel.
   - Zahl -> Zahlenfeld mit Grenzen (min/max) und Einheit.
   - Freitext nur, wo der Wert wirklich frei ist (Namen, Pfade, Views), mit Pruefung, wo moeglich.
2. **Server prueft mit.** Beim Speichern wird gegen den Katalog validiert (erlaubter Wert, Grenzen);
   ein ungueltiger Wert wird abgelehnt, nicht still auf Default gesetzt. Beim Lesen eines
   ungueltigen Bestandswerts: Default verwenden und im Protokoll warnen.
3. **Jede Einstellung beschreibt sich selbst.** Pflichtangaben im Katalog:
   - Anzeigename (deutsch, ohne Key-Syntax), Gruppe/Kategorie
   - **Was bewirkt sie** (ein bis drei Saetze, inkl. Wirkung jedes Auswahlwerts)
   - **Wo wird sie verwendet** (Modul, Seite, Dienst-Lauf)
   - Default, Standortbezug (AKE / IDEAL / beide)
   - Warnhinweis, wenn das Umschalten nach aussen wirkt (schreibt an Sage, versendet Mails,
     loest Buchungen aus) oder nur einmalig/einwegs moeglich ist
   - Abhaengigkeiten (wirkt nur, wenn Schalter X an ist)
   Der technische Key bleibt sichtbar, aber zweitrangig.
4. **Uebersichtliche Struktur.** Gruppiert nach Fachbereich, einheitlicher Aufbau auf beiden Seiten,
   Suche/Filter, geaenderte Werte gegenueber Default erkennbar.
5. **Drift-Guard erweitert.** Tests schlagen fehl, wenn ein Key ohne Beschreibung/Verwendung im
   Katalog steht oder ein Auswahl-Key keine erlaubten Werte hat.
6. **Specs:** Jede Spec, die eine Einstellung einfuehrt oder aendert, nennt Typ, erlaubte Werte,
   Default, Wirkung, Verwendung und Standortbezug. Ohne diese Angaben ist sie nicht freigabereif.

## Konsequenzen

**Positiv**
- Keine Tippfehler bei Auswahlwerten, ungueltige Werte werden beim Speichern abgewiesen.
- Admins sehen ohne Code-Lesen, was ein Schalter tut, wo er wirkt und was riskant ist.
- Eine Regel fuer beide Einstellungsorte.

**Negativ / Risiken**
- AppSettings sind heute untypisiert (Key/Value/Description); sie brauchen einen Katalog analog
  ADR 0008 - Umbau des Bestands.
- Bestandswerte koennen ausserhalb des neuen Wertebereichs liegen -> Migration/Bereinigung noetig.
- Jeder neue Key kostet mehr Pflichtangaben.
