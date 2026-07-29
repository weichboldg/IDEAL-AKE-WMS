---
typ: feature
anhaenge:
  - anhaenge/2026-07-29-Postman-Lagerbuchungen/AKE - Ideal - CWS Samples.postman_collection.json
---
# Sage-100-Lagerbuchungen ueber SData-API

## Worum geht es

Wir haben eine **API fuer Lagerbuchungen** (Sage 100) erhalten und wollen sie
ins WMS integrieren: Lagerbuchungen (Zugang/Entnahme) aus dem WMS an Sage
uebergeben.

- **Aktivierung und alle Parameter** (URL, Service-Benutzer, ...) muessen ueber
  unsere Einstellungen konfigurierbar sein.
- Die Logik „Buchungen an Sage erlauben" soll **pro Lager aktivierbar** sein.

## Was die Postman-Collection zeigt (technische Basis)

Anhang: `AKE - Ideal - CWS Samples.postman_collection.json`. Es ist eine
**Sage 100 SData-Schnittstelle** (Common Wawi Services, OL-Endpunkt):

- **Basis-URL:** `https://<FQDN>:5493/sdata/ol/{servicecontract}/{dataset}/...`
  Variablen: `sdata_base_url`, `sdata_servicecontract` (= `CommonWawiServices`),
  `sdata_contract` (= `CommonWawiServicesResources`), `dataset` (Mandant, leer
  in der Sample-Collection -> muss gesetzt werden).
- **Auth:** **Basic-Auth** (Beispiel-User `sage`). -> Credentials sind ein
  Secret, gehoeren NICHT in AppSettings-Klartext.
- **Buchung (Schreiben):** `POST .../$service/LagerbuchungService`
  - **Zugang:** `Lagerbewegungsart: "Zugang"`, Ziel-Lager gesetzt, Herkunft leer.
  - **Abgang:** `Lagerbewegungsart: "Entnahme"`, Herkunft-Lager gesetzt, Ziel leer.
  - Payload je Buchung u. a.: `Artikelnummer`, `MengeLager`,
    `HerkunftLagerkennung`/`HerkunftLagerplatzId`,
    `ZielLagerkennung`/`ZielLagerplatzId`, optional `Seriennummern`, `Chargen`
    (mit `Verfallsdatum`), plus `Memo` und `Standardtext` je Buchungskopf.
  - Lagerkennung-Format: `"Haupt01;0;0;0"` (Kennung + drei Ebenen).
  - Mehrere Buchungen pro Request moeglich (Array `Lagerbuchungen`).
- **Lesen (Stammdaten):** `GET Adressen`, `GET Artikel` (auch `include=$children`),
  `$schema`. Fuer Buchungen sekundaer, aber nuetzlich zum Abgleich
  (Artikelnummer-Mapping WMS <-> Sage).

Damit sind die frueheren Auth-/Endpunkt-/Payload-Fragen beantwortet. Die
verbleibenden offenen Punkte sind fachlich/architektonisch.

## Geklaert (aus den Rueckfragen)

- **Mandant (`dataset`):** konfigurierbar (pro Instanz).
- **Credentials:** in der **Datenbank** ablegen (geschuetzt, nicht Klartext) —
  passt zum bestehenden DB-first-ServiceSettings-Ansatz.
- **Verarbeitung: async, aber FAST LIVE.** Queue fuer Robustheit/Retry, aber der
  Service muss sie nahezu sofort abarbeiten (kein Minutentakt). -> Design-
  Kriterium: kurze Poll-/Trigger-Latenz bzw. ereignisgetriebenes Abarbeiten.
- **Pro-Lager-Flag:** existiert vermutlich bereits an der Lager-Entitaet —
  beim Spezifizieren pruefen und wiederverwenden statt neu anlegen.
- **Serien/Chargen:** NICHT jetzt — kommen in einem spaeteren Step.
- **Protokollierung:** ja, jede Buchung nachvollziehbar (wer/wann/was +
  API-Antwort).
- **Test:** eigenes Sage-Testsystem vorhanden (dort wird beim manuellen Test
  real gebucht).
- **BDE-Abgrenzung:** BDE wird SEPARAT gebucht (eigener Kanal) — auf eine
  Schnittstellendoku dafuer wird noch gewartet. Diese Lagerbuchungs-Integration
  ist also bewusst NUR Material Zugang/Entnahme, nicht FA-Rueckmeldung.

## Offene Fragen / Unklarheiten

Nur noch Detailpunkte fuer die Spec-Rueckfragen:

1. **Fast-live-Mechanik konkret:** Wie triggert der Service das sofortige
   Abarbeiten (kurzes Poll-Intervall, DB-Trigger, In-Memory-Signal)? ->
   Umsetzungsdetail fuer die Spec.
2. **Idempotenz / Doppelbuchung:** Buchungsstatus im WMS fuehren
   (offen/gesendet/bestaetigt/fehler); bei Timeout nach dem Senden nicht blind
   neu senden. Bei Buchungen geschaeftskritisch — muss die Spec loesen.
3. **Artikelnummer-Mapping:** WMS-Artikelnummer == Sage `Artikelnummer`, oder
   Mapping noetig (Artikelnummer- vs. Ressourcenummer-Fallstrick)? `GET Artikel`
   kann zum Abgleich dienen.
4. **Pro-Lager-Flag verifizieren:** existierendes Flag identifizieren und
   dessen Semantik pruefen (passt „Buchungen an Sage erlauben" dort hinein?).

## Vorgeschlagene Integration (erste Skizze)

- **`ISageLagerbuchungClient`** — kapselt SData-Aufruf (Basic-Auth, URL/Mandant
  aus Konfig, JSON-Payload Zugang/Entnahme). HttpClient via `IHttpClientFactory`.
- **Buchungs-Queue + Verarbeitung im Windows-Service:** WMS schreibt Buchung
  mit Status (offen/gesendet/bestaetigt/fehler) in eine Tabelle; der Service
  arbeitet sie **fast live** ab (kurze Latenz, ereignisnah statt Minutentakt) und
  aktualisiert den Status, Retry bei Fehler.
- **Konfiguration in der DB:** URL/Mandant/Aktivierung + Credentials geschuetzt
  (DB-first, analog ServiceSettings). Pro-Lager-Flag: bestehendes Flag
  wiederverwenden (verifizieren).
- **Feature-Toggle:** `SageLagerbuchungAktiv` (Default `false`), plus das
  bestehende Pro-Lager-Flag.
- **Scope Step 1:** nur Material Zugang/Entnahme, ohne Serien/Chargen (die
  kommen spaeter), ohne BDE (separater Kanal).

## Reif fuer den Backlog?

**Ja, im Wesentlichen.** Die API ist geklaert (SData, Basic-Auth,
Zugang/Entnahme), die Architektur-Entscheidungen sind getroffen (DB-Konfig,
async/fast-live, nur Material, Serien/Chargen spaeter, BDE getrennt). Die vier
verbliebenen Punkte sind Spec-Detailfragen, kein Blocker.

Vor dem Umzug nach `backlog/`:
1. Anhang mitnehmen nach
   `backlog/anhaenge/2026-07-29-postman-lagerbuchungen/`.
2. Spezifizierung **interaktiv** starten (Anhang liegt als JSON vor).

Groesse: **mittleres Feature, kein Epic.** Ein Stueck genuegt (Serien/Chargen
und Stammdaten-Sync sind ohnehin spaetere Steps) — `split` nicht noetig.

> Reihenfolge (entschieden): **Diese Postman-/Lagerbuchungs-Integration wird
> ZUERST umgesetzt, danach erst die IDEAL-Loesung.** Gut als Generalprobe fuer
> die Pipeline mit Anhang + interactive-Modus.
