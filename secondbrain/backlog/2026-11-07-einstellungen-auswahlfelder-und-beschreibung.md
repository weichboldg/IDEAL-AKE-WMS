---
typ: feature
---
# Einstellungen: Auswahlfelder statt Freitext, klarere Struktur, Wirkung und Verwendung je Einstellung

**Aufgenommen 2026-11-07** (Gerald, ueber die Koordination). Verbindliche Regel: [[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]].

Gerald: "Die Einstellungen gilt fuer Service und normale Einstellungen immer. Sollen bitte als Dropdown-Werte sein -
verringert Risiko von Tippfehlern. Und etwas sauberer strukturieren, uebersichtlicher machen und mehr Informationen,
was das Setting bewirkt und wo es verwendet wird."

## Umfang
- `/Settings` (AppSettings, ADR 0011) und `/ServiceSettings` (ServiceSettings-Katalog, ADR 0008); kuratierte Masken
  wie `/StandortEinstellungen` auf dieselbe Darstellung pruefen.
- Bestand inventarisieren: jede Einstellung mit Ist-Typ, tatsaechlich verwendeten Werten (beide Standorte),
  Lesestellen im Code (Datei:Zeile) -> daraus Typ, erlaubte Werte, Wirkung, Verwendung.
- Auswahllisten fuer alle Werte mit festem Wertebereich, mindestens die Quellenschalter `Sync:Quelle:*`
  (`View` | `SData` | `Vergleich`, v1.73.0) und alle Modus-/Standort-artigen Keys. Stammdaten-Verweise
  (z. B. `DefaultLagerbestellempfaengerId`, `BdeDefaultArbeitsgang`, `LackierteilKategorieName`) als Auswahl aus den Stammdaten.
- Serverseitige Validierung beim Speichern; ungueltige Bestandswerte beim Lesen -> Default + Warnung im Protokoll.
- AppSettings bekommen einen typisierten Katalog analog `ServiceSettingDefinitions` (Seed idempotent, Drift-Guard).
- Darstellung: gruppiert nach Fachbereich, Anzeigename statt Key, Beschreibung "bewirkt" + "verwendet in",
  Default, Standortbezug, Warnhinweis bei Aussenwirkung (Sage-Schreiben, Mails, Buchungen), Abhaengigkeiten,
  Suche/Filter, Kennzeichnung "weicht vom Default ab". Einheitlich auf beiden Seiten.
- Drift-Guard-Tests: Beschreibung/Verwendung Pflicht, Auswahl-Keys brauchen erlaubte Werte.
- Spec-Vorlage `_templates/spec.md`: Abschnitt fuer neue/geaenderte Einstellungen (Typ, Werte, Default, Wirkung, Verwendung, Standort).

## Grenzen
- Kein Verhalten aendern: gleiche Keys, gleiche Defaults, gleiche Lesesemantik (z. B. `LagerbestellungAktiv` "nur false sperrt").
- Einweg-Master `ProduktionsauftragHierarchisch` bleibt read-only (ADR 0012).
- Geheimnisse (`ConnectionStrings`, `MailSettings`, `Security:AdDomain`) bleiben in appsettings (ADR 0008).

## Hinweis Pipeline
Gerald will kuenftig moeglichst in /goal-Laeufen mit mehreren Specs buendeln - Kandidat fuer das naechste Buendel.
