---
type: changelog
version: 1.99.14
date: 2026-11-13
---
# v1.99.14 — SData Teil 2: Register „Sage-Test“, Lesetest Benutzerfelder, Stückliste live, SData-Kennwort verschlüsselt

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `7f3b1a66`, `cd38fbaa` (feature-complete), `0b1cc9a2` (Test Schlüsselring Dienst), `7048071a` (QA-Befunde Q-1 Einrückung de-Kultur, Q-2 Zahleneingabe, Q-3 Protokoll je Zustandswechsel). **Testbereit** (QA 2026-11-13).
Spec [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]], ADR [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]],
Lesetest [[sage-sdata-benutzerfelder-lesetest]]. Nicht gemergt, nicht gepusht; /goal-Lauf [[2026-11-13-buendel-v1-99-13-goal-lauf]].

## Kernpunkte

- **Register „Sage-Test“** (nur Admin, `RequireAdminAccess`, keine eigene Rolle): Reiter auf `/Settings`, `/ServiceSettings`,
  `/StandortEinstellungen`, `/SageTest`. Inhalt: Sage-Verbindung testen (von `/ServiceSettings` umgezogen), Feldliste je Ressource (Schema +
  eine Datenseite ohne select, Benutzerfelder gekennzeichnet; Rückfall aufs Vertrags-Schema, weil `{Ressource}/$schema` am AKE-Test 401 liefert),
  Stückliste live (Artikelnummer → Ressourcenliste; FA-Nummer → FA-Positionen über `BelIDNachfolger`), FA-Struktur live (Umzug, jetzt Admin).
- **Gate `SageSdataFaAnsichtAktiv` entfällt** (Katalog, Seed, Standort-Maske, Menüpunkt); DB-Zeile per SQL/147.
- **SData-Zugangsdaten** `SData:Username`/`SData:Password` in den Service-Einstellungen, Kennwort als `dp1:` + DataProtection-Schutztext
  (Schlüsselring der Web-App, der Dienst liest ihn mit). Neuer Einstellungstyp `Geheim`: nie Klartext in Oberfläche, Orphan-Liste,
  Einstellungsprüfung, Protokoll. Eine Auflösungsstelle `SdataZugangsdaten` für Dienst (`SageQuelleSchalter`, `SageBookingWorker`) und Web.
  Nicht entschlüsselbar → kein Aufruf an Sage, kein Rückfall. Leerer `SData:Username` → appsettings-Rückfall mit genau einer Warnung je Lauf.
- **Lese-Client** `SageSdataReadClient`: `include`, `QueryPageAsync`, `GetSchemaAsync` – weiterhin nur GET.
- **Lesetest am AKE-Test** (nur GET, über DEV): FA-Kopf 67 Felder/3 USER_, FA-Positionen 120/3 USER_, Arbeitsgänge ohne USER_;
  `SpecificUserfields`/`CustomFields` nur als `include`; Stückliste 86003871 mit Benutzerfeldern je Position; PPS-Ressourcenliste über SData
  nicht schreibbar; AKE-FA-USER-Felder (`KHKPpsFaBelege`) nur über RealTimeData; Werkstattauftrag = `KHKIFAuftraege` (in keinem Vertrag).
  9 Fragen an DPS (U1–U9). IDEAL (L10) offen – kein Endpunkt.

## Migration / SQL

Keine Schema-Änderung, keine EF-Migration. **SQL/147_RemoveAppSettingSageSdataFaAnsichtAktiv.sql** (Daten, Transaktion, Muster SQL/108;
Manifest Typ Daten, Phase VorStart).

## Deploy-Risiken

Web + Dienst. Manifest: Ordner `{DataProtection}` (Dienstkonto Lesen, Pflicht, herstellen; neuer Platzhalter im Deploy-Modul),
`Pruefung/SDataKennwortAppsettings` (Phase Nachher – Vorher hätte jedes Prod-Update abgebrochen, solange das Kennwort planmäßig noch in der
Datei steht), Einstellungs- und Info-Hinweis. **Dienst braucht `DataProtection:KeysPath` = Pfad der Web-App** und Leserecht darauf, sonst
„nicht entschlüsselbar“ → keine Sage-Aufrufe, sobald `SData:Username` gesetzt ist. Solange leer: Verhalten wie bisher (appsettings) + Warnung.

## Folgearbeit

[[2026-11-06-sage-sdata-folgearbeit]] (Rückfall entfernen, DPAPI, DPS-Fragen U1–U9, P-W1-Viewtexte).
