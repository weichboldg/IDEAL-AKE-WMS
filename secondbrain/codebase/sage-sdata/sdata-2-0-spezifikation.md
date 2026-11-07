---
type: codebase-karte
updated: 2026-11-03
quelle: "SData-Spezifikation https://sage.github.io/SData-2.0/ – vollständig gelesen 2026-11-03 (Core 1.1 Kap. 1–14 + Anhänge, Linking & Synchronisation, SData-2.0-Teil); zuerst Kapitel 6 Queries (6.1–6.10) plus die dort verwiesenen Abschnitte 2.11 Query-Parameter, 2.12 Query Language, 3.10 Error Payload, 8.4/11.6 Reliable Posting, 11.4 Service Operations, 12.2 Named Queries, SData 2.0 JSON (04-0400, 04-0500). Link von DPS (Michael Wiesner, Ticket#7954779, 11.06.2026)."
---
# SData-Spezifikation – was für das WMS zählt

Geschwister: [[sage-sdata-uebersicht]] · [[sage-sdata-realtimedata]] · [[sage-sdata-masterdata]] · [[sage-sdata-commonwawiservices]]
Befund am Server: [[sdata-befund-ake-test-2026-10-30]]. Die Spezifikation ist der Standard; **was Sage 100 davon umsetzt, steht nur im
Befund**. Aussagen unten ohne Server-Beleg sind Spezifikation, nicht geprüftes Sage-Verhalten.

## Abfrage-Parameter (2.11, Kap. 6)

| Parameter | Wirkung laut Spezifikation | Sage 100 (AKE-Test) |
|---|---|---|
| `where` | Filter, Query Language (unten) | geht; Strings nur mit `'…'` (ohne → 500) |
| `orderBy` | `feld asc,feld2 desc`; nicht unterstützte Kombination → kein Fehler, nur Warn-Diagnose | ungeprüft |
| `startIndex` / `count` | 1-basiert; Provider darf `count` kappen, muss dann `itemsPerPage` setzen | `count` von `dtsTeileverwendungsnachweis` ignoriert (237.122 Zeilen) |
| `select` | Felder wählen, Pfade `a/b`, `a/*`; **überschreibt** `include` und `precedence` | geht |
| `include` | verknüpfte Ressourcen; `$children` rekursiv; `$descriptors` nur für Menschen (teuer) | ungeprüft |
| `precedence` | nur Felder mit Priorität ≤ n; `0` = keine Payload (billige Zählung/Existenzprüfung) | ungeprüft |
| `format` | MIME-Typ, überschreibt `Accept`; JSON: `application/json;vnd.sage=sdata` | `format=json` geht im Browser **nur zusammen mit** `Accept: application/json`, sonst Atom |
| `returnDelta` | bei POST/PUT nur geänderte Felder zurück (MAY) | ungeprüft |
| `trackingID` | UUID für Reliable Posting (MAY) | ungeprüft |
| `language`, `version`, `includeSchema`, `search` | optional | – |

**Unbekannte Parameter muss der Provider ignorieren, ohne Fehler.** Darum lieferte `dtsFaBelegStruktur?StrukturID=835` am 30.10. kein
Fehler, sondern leer: Der Parameter wurde schlicht nicht ausgewertet.

## Query Language (2.12)

- **Basic (Pflicht):** `eq ne lt le gt ge`, `and`, `or`, Klammern, Literale.
- **Weitere (nur Intermediate/Complete, Provider-abhängig):** `not`, `between … and …`, `in (…)`, `like '%x%'`, Arithmetik `mul div mod + -`,
  Member-Zugriff `a.b`, Funktionen (String: `concat left right substring lower upper replace length locate lpad rpad trim`; Zahl: `abs round trunc
  floor ceil pow`; Datum: `currentDate() year() month() day() dateAdd() dateSub()` …).
- Präzedenz: Vergleich vor `and` vor `or`.
- Literale: `17`, `17.0`, `'GB'`/`"GB"` (Escape `'Maxim''s'`), Datum `@2008-05-19@`, Zeitstempel `@2008-05-19T18:41:00@` / `…+02:00@` / `…Z@`.
- **Protokollfilter:** `$updated`, `$published`, `$key`, `$uuid`, `$title` – nur, wenn der Provider sie per `sme:protocolFilters` ankündigt;
  mit normalen Feldern nur per `and` kombinierbar. `$updated gt @…@` wäre der Standardweg für **Delta-Abfragen** → für Sage ungeprüft.
- Sage AKE-Test: `eq`, `like`, `and`, `or` belegt (Übersicht); `in`, `between`, Funktionen ungeprüft.

## Antworten und Fehler (3.10, 6.10, JSON 04-0500)

- JSON-Feed: `$resources` (Pflicht), dazu `$totalResults`, `$startIndex`, `$itemsPerPage`, `$url`, `$updated`. Entry: `$key`, `$url`, `$uuid`,
  `$etag`, `$updated`, Felder.
- **Treffer null = 200 mit leerem Feed, nie 404.** Ein leeres Ergebnis sagt also nichts darüber, ob ein Filter überhaupt gegriffen hat.
- Statuscodes: 400 Anfrage falsch (ändern) · 401 Anmeldung · **403 Consumer darf den Feed nicht lesen** · 404 URL-Teil existiert nicht
  (Vertrag, Ressource) · **500 schwerer Providerfehler, nicht wiederholen** · 501 nicht implementiert · **503 vorübergehend, Wiederholung
  erlaubt (`Retry-After`)**.
- Diagnose `$diagnoses[]`: `$severity` (`info`, `warning`, **`transient`** = später erneut versuchen, z. B. Datensatz gesperrt; `error` = Anfrage
  ändern; `fatal` = nicht wiederholen), `$sdataCode` (`BadUrlSyntax`, `BadQueryParameter`, `DatasetNotFound`, `ResourceKindNotFound`,
  `BadWhereSyntax`, **`ApplicationDiagnosis`** = anwendungsspezifisch, Details in `applicationCode`), `$message`, `$payloadPath`.
  → **Für die Queue:** Retry-Entscheidung an `severity`/HTTP-Status festmachen (transient/503 → Retry mit Backoff; error/400/500/fatal → Fehler,
  manuelle Klärung). Das deckt Zusatzfund Z-3 aus [[2026-11-02-sage-pps-abgleich]] ab.

## Service Operations und Reliable Posting (11.4, 8.4, 11.6)

- Synchron: `POST …/{Ressource}/$service/{op}` bzw. `…/{Dataset}/$service/{op}`, Body = Entry mit `request`-Element, Antwort 200 mit
  `response`-Element. So arbeitet heute `LagerbuchungService`.
- **Reliable Posting:** Consumer erzeugt eine UUID und hängt sie als `?trackingID=<uuid>` an den POST. Provider antwortet 202 + `Location`
  auf `…/$post('<uuid>')`, Consumer pollt per GET bis ≠ 202, räumt per DELETE auf. Doppelte `trackingID` → Provider erkennt das Duplikat
  (empfohlen: normale 202). Alternativ auf Anwendungsebene: UUID (`sdata:uuid`) im Datensatz. **Beides ist für den Provider optional.**
  → Wäre die saubere Idempotenz für Rückmeldungen. Ob Sage 100/DPS `trackingID` unterstützt, ist ungeprüft (Frage an DPS).

## Named Queries und Intermediate URLs (12.2, 6.9)

- `…/{Ressource}/$queries/{name}`; nehmen dieselben Parameter (where, orderBy, select …). Eigene Parameter beschreibt die Spezifikation nicht.
- Sage AKE-Test: `$queries` ist bei allen geprüften PPS-Quellen **leer** ([[sdata-befund-ake-test-2026-10-30]] §6, §8).
- Property-URL: `…/salesOrders('43660')/orderLines` liefert die Kind-Collection; gilt für Relationen mit `sme:relationship`. Für
  `strFaBeleg(…)/FaPositionen` wäre das der Weg – am AKE-Test aber 403 auf ganz `MasterData`.

## Caching (6.6)

`ETag` + `If-None-Match` → 304, oder `Expires`. Optional für den Provider; für Sage ungeprüft. Für einen Polling-Dienst nützlich, falls vorhanden.

## Folgerungen für das WMS

1. Die **403 auf MasterData** ist laut Spezifikation ein Leseverbot für den Consumer. Die Sage-Meldung („Referenz zwischen Datensatz und
   Element stimmt nicht überein“, `ApplicationDiagnosis`) nennt den Grund nicht; Rechte, Lizenz oder Konfiguration bleiben offen (DPS).
2. Die **500 auf `dtsVerursacherAnzeige`** ist laut Spezifikation „nicht wiederholen“ – kein vorübergehender Fehler.
3. **Leere Struktur-Quellen** sind spezifikationskonform (200, leerer Feed); ein fehlender Kontext erzeugt keinen Fehler.
4. Idempotenz: `trackingID` vor eigenem Memo-/GUID-Feld bei DPS anfragen.
5. Delta-Lesen: `$updated` als Protokollfilter am Server prüfen (Schema-Attribut `sme:protocolFilters`), bevor ein Dienst Vollabzüge zieht.

---

# Ergänzung 2026-11-03: übrige Kapitel, Synchronisation, SData 2.0

Gelesen per WebFetch (Zusammenfassungen, kein Volltext); MUST/SHOULD sinngemäß. Kurzform: `core/NNNN` = `https://sage.github.io/SData-2.0/pages/core/NNNN/`,
`sync/…`, `20/…` entsprechend. Die Seiten unter `core/` sind laut Breadcrumb **SData Core 1.1**; der 2.0-Teil ist ein „Werkzeugkasten“, der 1.1
weitergelten lässt und viele MUST zu MAY lockert (`20/01-0000`). Alles Spezifikation, **nicht** geprüftes Sage-100-Verhalten.

## URL-Aufbau und Metadaten (Kap. 2–4)

- `https://host[:port]/sdata/{application}/{contract}/{dataset}/{resourceKind}`; `-` = Default-Dataset; mehrteilige Datasets sind ein Segment
  (Sage: `ake_TEST2026;1`) (`core/0201`).
- Einzelsatz: `kind('KEY')` (MUST) oder `kind(feld eq 'x')` (SHOULD; mehr als ein Treffer → 400). Zusammengesetzte Schlüssel als ein Wert,
  Trennzeichen anwendungsspezifisch (`core/0202`).
- Property-URL `kind('k')/kinder` nur für Relationen; POST/PUT/DELETE nur bei `child`-Relationen (`core/0203`, `core/0404`).
- `$template` liefert Vorbelegungen für ein Anlegen, legt nichts an; Pflicht bei `canPost` (`core/0206`, `core/0802`).
- `$schema` je Vertrag und je Ressource; Intermediate URLs (`/sdata`, `/sdata/ol`, `…/{dataset}/$service`) listen die nächste Ebene (`core/0207`, `core/0209`).
- Eigene Parameter sollen mit `_` beginnen (`core/0215`).
- **Schema-Attribute, an denen man ablesen kann, was der Server kann** (`core/0402`, `core/0403`, `core/0408`, Anhang A):

| Attribut | Bedeutung |
|---|---|
| `sme:canGet/Post/Put/Delete` | Operation **im Vertrag** möglich – nicht die Rechte des Benutzers; Zugriffsregeln können weiter einschränken (→ 403) |
| `sme:supportsETag` | ETags vorhanden; dann ist `If-Match` bei PUT Pflicht (sonst 400, Abweichung 412) |
| `sme:hasUuid` | Einträge tragen `sdata:uuid` |
| `sme:protocolFilters` | welche von `$updated`, `$uuid`, `$key` … im `where` gehen (Delta-Lesen) |
| `sme:invocationMode` | Service Operation `sync` / `async` / `syncOrAsync` |
| `sme:batchingMode` | `$batch` möglich (`none` Default) |
| `sme:hasTemplate` | `$template` vorhanden |
| `sme:isMandatory`, `isReadOnly`, `isUniqueKey`, `maxLength`, `totalDigits/fractionDigits` | Feldregeln; zu lange Werte kürzt/rundet der Provider |
| `sme:relationship` | `parent` / `child` / `reference` / `association`; Kind-Graph ist ein Baum |
| `sme:isSyncSource/isSyncTarget` | Synchronisation angeboten |

- Null wird als `xsi:nil="true"` übertragen; ein **fehlendes** Element heißt bei Updates „unverändert“ (`core/0308`, `core/0902`).
- Sicherheit: HTTPS SHOULD, Basic/Digest SHOULD (Basic nicht ohne HTTPS), falsche Anmeldung 401 (`core/0501`, `core/0502`). SageID (OAuth2
  Bearer) nur für Cloud-Szenarien (`20/06-0000`).

## Schreiben (Kap. 7–11, 13)

- **Lesen eines Satzes:** `GET kind('k')` → 200 + ETag (falls `supportsETag`) (`core/0701`).
- **Anlegen:** `POST kind` → 201 + `Location`. Der Consumer SOLL eine `sdata:uuid` mitgeben. Kommt dieselbe UUID nochmals, SOLL der Provider
  **nicht** 409 liefern, sondern 201 mit dem vorhandenen Satz – aber nur, solange der Satz seither nicht geändert wurde (`core/0805`). Unique-Konflikt
  sonst 409. `returnDelta=true` optional (`core/0801`).
- **Ändern:** `PUT kind('k')` mit `If-Match: <ETag>` → 200 + neuer ETag; Teil-Update erlaubt (fehlende Felder = unverändert); Kind-Listen als Delta
  oder voll mit `sdata:deleteMissing`; ohne `If-Match` bei `supportsETag` → 400, falscher ETag → **412** mit aktuellem Satz; ohne ETag-Unterstützung
  „wer zuletzt schreibt, gewinnt“ (`core/0901`–`0905`). SData 2.0: Teil-Update per **PATCH**, PUT dafür veraltet (`20/03-0600`).
- **Löschen:** `DELETE kind('k')` → 200; `410 Gone` = schon gelöscht, als Erfolg werten (`core/1001`–`1004`).
- **Service Operations:** immer `POST …/$service/{op}`; synchron 200; asynchron (`invocationMode=async` oder `syncOrAsync` + `trackingID`) →
  202 + `Location` (Tracking-URL, nicht selbst bauen) → GET pollen im Takt `pollingMillis` bis 200 → **DELETE** auf die Tracking-URL (Consumer MUST).
  Kommt auf den ersten POST kein 202, gilt die Operation als synchron erledigt. Schema eines Requests: `GET …/$service/{op}/$schema`; Vorbelegung
  optional `…/$template` (`core/1101`–`1108`).
- **Batch** `POST kind/$batch` (optional): Feed mit je Eintrag `http:httpMethod` (+ `ifMatch`); Antwort hat genauso viele Einträge mit eigenem
  `httpStatus`. **Global 200 sagt nichts**; bricht der Provider ab, tragen unverarbeitete Einträge 5xx. **Nicht als atomar spezifiziert** – kein
  Rollback zugesichert (`core/1303`–`1306`).
- **Named Queries:** `GET kind/$queries/{name}?_param=…` (Parameter = Request-Felder mit Unterstrich) oder POST; Antwort immer Feed; ab 1.1 auch
  mit `where/orderBy/select` (`core/1201`, `core/1203`).
- Wiederholungsregel durchgängig: **400 → Anfrage ändern; 500 → nicht wiederholen; 503 → wiederholen erlaubt (`Retry-After`)**; Diagnosen bei
  4xx/5xx SOLLEN mitkommen, im Batch MÜSSEN sie (`core/0705`, `core/1306`).

## SData 2.0 / JSON (`20/…`)

- JSON: `Accept: application/json;vnd.sage=sdata` oder `?format=…`; ohne Angabe Atom (`20/03-0600`). Das JSON-Kapitel nennt sich selbst
  „nicht genehmigte Vorschau“ (`20/04-0000`).
- **Typen:** `sdata/decimal` kommt als **String** mit Punkt (`"1.2990"`), Datum `"YYYY-MM-DD"`, Zeitstempel ISO 8601 mit Zeitzone (`20/05-0700`).
  → Der .NET-Client muss Dezimalwerte als String lesen können. (Sage AKE-Test liefert z. B. `IstEingelastet` als Zahl `-1`; Dezimalfelder ungeprüft.)
- Metadaten in JSON optional: `?includePrototype=true`, `…/$prototypes/{kind}` mit `$properties`, `$capabilities`, `$protocolFilters`
  (`20/05-0400`, Anhang A).
- Diagnosen `$diagnoses[]` mit `$severity` `info/warning/transient/error/fatal`.

## Linking & Synchronisation (`sync/…`)

- **Linking:** Anwendungen ordnen Sätze über eine **UUID** zu; jede Seite führt ihre Zuordnung lokale ID ↔ UUID selbst. Provider stellt
  `kind/$linked` (GET/POST) und `$linked('uuid')` (GET/PUT/DELETE) bereit; Abfragen mit `includeUuid=true` (`sync/0101`–`0104`).
- **Synchronisation:** Vektoruhr je Endpunkt (Ticks), Digest je Collection, Ressourcen-`syncState` (Endpunkt, Tick, Stamp). Ablauf eines Laufs:
  `GET $syncDigest` (Ziel) → `POST $syncSource?trackingID=…&runName=…&runStamp=…` mit Ziel-Digest (asynchron, Feed nur mit Änderungen, je
  Endpunkt nach Tick aufsteigend, Löschungen als `isDeleted`) → `POST $syncTarget` → Digest fortschreiben; Konflikt: niedrigere Priorität gewinnt,
  dann jüngerer Zeitstempel. Referenzierte Sätze vor referenzierenden (`sync/0201`–`0601`).
- **Nur relevant, wenn der Provider es anbietet** – Digest und Ticks führt der Server; nachbauen kann ein Client das nicht. Erkennbar an
  `sme:isSyncSource/isSyncTarget` im Schema oder einer Antwort auf `GET kind/$syncDigest` bzw. `kind/$linked` (Bewertung, ungeprüft für Sage).
- Übertragbare Muster auch ohne Sync: Merkpunkt (Zeitstempel/Tick) + aufsteigende Sortierung, damit ein abgebrochener Lesellauf fortsetzen kann;
  Mapping-Tabelle WMS-ID ↔ Sage-Schlüssel; „Referenzen zuerst“ (≈ Unter-FA vor Haupt-FA); Lauf-ID je Abgleich im Protokoll.

## Was das für die Rückmeldungen heißt

1. **Idempotenz:** Die Spezifikation bietet zwei Wege – `trackingID` am POST oder eine vom WMS vergebene `sdata:uuid`. Beides ist für den Server
   optional, und die UUID-Regel schützt nur, solange der Satz seither unverändert ist. Wenn DPS keines davon unterstützt, bleibt die eigene ID im
   Memo/USER-Feld mit Lookup vor dem Retry.
2. **Retry in der Queue:** 503 / `severity = transient` → automatisch mit Backoff wiederholen; 400 / 409 / 412 / `error` → Fehler, Klärung;
   500 / `fatal` → nicht wiederholen. 202 → Tracking pollen, danach DELETE.
3. **Mehrere Rückmeldungen eines FA nicht als `$batch` verlassen,** solange DPS keine Atomarität zusichert; je Eintrag Status auswerten.
4. **Am Server prüfbar (GET, Schema schon im Browser-Cache möglich):** `supportsETag`, `hasUuid`, `protocolFilters`, `invocationMode`,
   `batchingMode`, `isSyncSource` der genutzten Ressourcen/Operationen sowie `GET …/$syncDigest` / `…/$linked` auf einer PPS-Quelle.

**Geprüft am AKE-Test 2026-11-03:** siehe [[sdata-befund-ake-test-2026-10-30]] §9 – kein Sync/Linking, keine UUIDs/ETags in RealTimeData/CommonWawiServices, `$updated` = Abfragezeit; Delta nur über Quellfelder wie `GeaendertAm`; `LagerbuchungService` und `strFaBeleg … Execute` sind `syncOrAsync` (trackingID laut Schema zulässig, nicht getestet); MasterData hätte ETag + UUID, ist aber 403.
