---
typ: feature
---
# Bereinigungen nach dem Bündel 2026-11-07 (Restbefunde, Zusatzfunde, Aufräumen)

**Aufgenommen 2026-11-07** (Gerald: „Bitte trotzdem die Bereinigungen planen“). Sammelt alle kleinen Restpunkte aus dem /goal-Lauf
[[2026-11-07-buendel-2026-11-07-goal-lauf]] und der [[2026-11-07-buendel-uebersicht]] für EINE Spec im nächsten Bündel. Vor dem Spezifizieren
je Punkt prüfen, ob er im Lauf schon behoben wurde (Commit `bd178dc4` behebt laut Aufgabe B1–B5 des Einstellungen-Epics; die Spec
nannte B2–B5 noch „offen, Hinweis“) – nur Offenes übernehmen.

## A. Einstellungen ([[2026-11-07-einstellungen-auswahlfelder-und-beschreibung-spec]], QA 1)
- B2 `NegativeBuchungLagerplatz`: fehlende Zeile + kein Lagerplatz `NAN` → Seite zeigt „ungültig“, Speichern nicht blockiert für andere Werte.
- B3 Lesestellen trimmen wie `SettingValidator`/`BoolWirksam` (z. B. `RequireLagerbestellungAktivAttribute.cs:34`); Bestandswerte mit
  Leerzeichen per Startprüfung melden.
- B4 Auswahlliste vergleicht ohne Groß/Klein wie der Validator; abweichende Schreibweise als Hinweis statt „ungültig“.
- B5 Drift-Guard: Service-Key ohne eigene Metadaten (Fallback Key/„Dienst“) lässt den Test scheitern.
- B6 `feature-map.md` v1.76.0/v1.77.0 (falls beim Bündel-Abschluss nicht nachgezogen).
- `OseonRueckmeldungAktiv` / `SageRueckmeldungAktiv` ohne Leser (Freigabe-Antwort 3: „ohne Wirkung“ führen, Aufräumen als Aufgabe):
  entfernen oder an den späteren Leser (Rückmelde-Epic) binden – Entscheidung in der Spec mit Empfehlung.

## B. Tests
- Flaky (Altbestand, zeitabhängig, 3 Ausfälle in 8 Läufen): `SyncWorkerTests.SyncWorker_IgnoresIConfigurationDisableSeed_ForBlockGates`,
  `NotificationWorkerTests.NotificationWorker_PositiveLowStock_UsesOrangeColor` → deterministisch machen (Zeitquelle injizieren o. ä.).

## C. Zusatzfunde aus den Specs vom 07.11.
- Fehlteile: Werkbank-Dropdown im Modus „Nur meine Werkbänke“ gesperrt und listet alle Werkbänke (`MissingPartsController.cs:134`,
  `Views/MissingParts/Index.cshtml:70`).
- **Nicht hier** (eigene Backlog-Notizen, vom Bündel-Lauf angelegt): [[2026-11-07-fehlteile-karte-meine-fehlteile-lager-glas]] und
  [[2026-11-07-oseon-teileverfolgung-doppelter-klickhandler]] – im selben Bündel mitnehmen, nicht doppelt spezifizieren.

## D. SData-Epic ([[2026-11-06-sage-sdata-stammdaten-sync-spec]])
- QA-Hinweis Q2 (Listenanzeige Herkunft Sage/WMS leicht unterschiedlich) und Q3 (Doku-Drift, z. B. `SdataArtikelReader.cs:32-35`),
  falls noch offen.

## E. Befunde der Koordination auf DEV (v1.78.0, 2026-11-07, nur angesehen)
- **`/ServiceSettings`: Sperr-Hinweis an falscher Stelle.** Der gelbe Kasten „SData ist hier gesperrt bis P-R1 …“ gehört zu
  `Sync:Quelle:Artikel`, steht aber direkt über „Quelle FA-Struktur“ und wirkt wie deren Überschrift; ebenso P-F2 über „Quelle Werkbänke und
  Arbeitsgänge“ (IDEAL-DEV `localhost:8082`). Hinweis innerhalb des eigenen Eintrags anzeigen.
- **Lagerplätze: Spalte „Übergabe an Sage“ leer** – auch bei manuell angelegten Plätzen (`1;1;1`, `asdfasdf`, AKE-DEV `localhost:8081`).
  Laut SData-Spec TS-106.11 sollten Bestandsplätze „nicht nötig“ und neue WMS-Plätze „offen“ zeigen. Klären: Anzeige leer bei `NichtNoetig`
  gewollt, oder Migration/Anzeige fehlerhaft? Liste muss den Status lesbar zeigen.
- **Altwert `LagerbestellungAktiv = "1"`** auf AKE-DEV (Einstellungsprüfung 10:52: „„1“ ist nicht erlaubt … Wirksam: „true““). Zu B3/B4:
  Bestandswerte wie `1`/`0`/Leerzeichen einmalig auf `true`/`false` normalisieren (Migration oder Startprüfung mit Korrektur), damit
  Anzeige und Wirkung übereinstimmen. Vor dem produktiven Deploy AKE und IDEAL auf solche Werte prüfen:
  `SELECT [Key],[Value] FROM dbo.AppSettings WHERE [Value] IN ('1','0') OR [Value] LIKE ' %' OR [Value] LIKE '% ';`
  (Spaltennamen laut ADR 0011 Key/Value – beim /spec gegen das Modell prüfen).

Grenzen: kein neues Verhalten außer den genannten Korrekturen; Schalter-Defaults unverändert.
