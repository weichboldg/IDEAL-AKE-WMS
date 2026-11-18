---
type: adr
id: 0033
title: Sage-Benutzerfelder (USER_*) als Service-Einstellung mit Default = heutiger Name
status: accepted
date: 2026-11-18
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-18-sage-userfelder-als-einstellung-spec]] (v1.99.40, TS-161, keine Migration). Gerald 2026-11-18 09:15: „bei den Userfeldern
> nicht hardcodiert, immer als Einstellung“. Ergänzt [[0008-servicesettings-db-first-mit-typisiertem-katalog]] und
> [[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]]. Bündel `feature/2026-08-07-ideal-teile-1-5`, Commit `127f7e3f`.

## Kontext und Problem

- Benutzerfelder legt jeder Sage-Mandant selbst an. AKE und IDEAL haben verschiedene Felder; IDEAL kennt z. B. `USER_EKMindestbestellwert`
  nicht. Namen, die fest im Code standen, brachen am IDEAL-Testsystem ganze Abgleiche ab („Ungültiger Spaltenname“).

## Entscheidung

- **Eine Einstellung je gelesenem Feld:** Key `Sync:Userfeld:<Abgleich>:<Standardname>`, Kategorie „Sage-Benutzerfelder: <Abgleich>“. Der Katalog
  `IdealAkeWms/Services/SageSdata/SageUserfelder.cs` ist für Web und Dienst dieselbe Quelle und landet automatisch in `ServiceSettingDefinitions.All`.
- **Lesart:**
  - Keine Zeile → Default (= heutiger Name, AKE unverändert).
  - Leer → nicht angefragt, Anzeige „nicht konfiguriert“.
  - Ungültig (nicht `USER_[A-Za-z0-9_]`) → wie leer, mit Warnung.

  Beim Speichern wird das Muster geprüft. Die Präfixe (`KHKEKBelege_`, `KHKPpsFaBelege_`) bleiben im Code.
- **Logischer Name = Standardname:** Merkmale (Blechfertigung), Wörterbuch-Schlüssel und Code arbeiten weiter mit ihm; nur die Sage-Abfrage
  nimmt den konfigurierten Namen. Eine Umbenennung lässt so keine verwaisten Merkmale zurück.
- **Fehlerbilder:**
  - „Ungültiger Spaltenname“ → `SageUserfelder.Erklaeren` nennt Anzeigename und Key.
  - HTTP 404 einer Resource → verständliche Meldung, der Schritt wird 24 h pausiert (`SyncWorker`).

## Konsequenzen

- Ein neues USER-Feld im Code heißt: neuer Katalog-Eintrag, nie ein String-Literal in einer Abfrage (Fallstrick 51).
- Der Einstellungswert wird in Abfragen eingesetzt. Die Sicherheit hängt deshalb am Muster (keine Leerzeichen, Kommas, Klammern).
- `Sync:ArtikelBenutzerfelder` (Freitext-Liste, v1.73) bleibt, wie sie ist.
