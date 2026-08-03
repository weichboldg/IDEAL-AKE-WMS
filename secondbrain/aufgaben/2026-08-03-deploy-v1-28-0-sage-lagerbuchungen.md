---
type: aufgabe
title: Deploy v1.28.0 — Sage-Lagerbuchungen
status: offen
created: 2026-08-03
spec: "[[2026-07-29-sage-lagerbuchungen-spec]]"
---

# Deploy v1.28.0 — Sage-Lagerbuchungen (ausgehend)

Offen nach dem Dev-Lauf (Reihenfolge; Schranke 2 = Mensch: manueller Test → Merge → Deploy).

## Vor dem Merge (Schranke 2)
- [ ] Manual-UAT am **Sage-Testsystem** (nicht InMemory-testbar): TS-56.3/56.4 (echte Zugang/Entnahme),
      TS-56.6 (Fehlerpfad), TS-56.7 (Timeout/Doppelbuchungs-Schutz), TS-56.8 (Requeue).
- [ ] **Dev-Lauf-Verifikationen** am Testsystem bestaetigen:
  - Korrelationsspalte fuer den Memo-Lookup: `Memo` (Annahme) vs. `Referenz` — ggf. eine Zeile in
    `IDEALAKEWMSService/Services/SageBuchungLookupReader.cs` umstellen.
  - `KHKLagerplaetze.PlatzID` als Quelle fuer `SageLagerplatzId` bestaetigen (H1).
  - `Article.ArticleNumber == Sage-Artikelnummer` (Frage 4).
  - Reales Antwortformat des `LagerbuchungService` (Erfolg/Fehler) — ggf. Erfolgskriterium im
    `SageLagerbuchungClient` verfeinern (aktuell: HTTP 2xx = Erfolg).

## Deploy-Reihenfolge
1. DB-Migration zuerst (additiv, unkritisch): `SQL/82_*` + `SQL/83_*` (oder EF-Migrate).
2. Web-App neu deployen.
3. Service stoppen → Binaries + `IDEALAKEWMSService/appsettings.json`-Block
   `SageLagerbuchung:Username/Password` (echte Credentials) einspielen → Service starten.

## Nach dem Deploy (Aktivierung, Default alles aus)
- [ ] `/ServiceSettings`: `SData:BaseUrl`, `SData:Dataset` setzen; `SageLagerbuchungAktiv` = true.
- [ ] Je gewuenschtem Lagerplatz `SageBuchungErlaubt` aktivieren (Stammdaten → Lagerplaetze).
- [ ] Lagerplatz-Sync mind. einmal laufen lassen (befuellt `SageLagerkennung`/`SageLagerplatzId`).
- [ ] Mit **einem** Testartikel + Testlagerplatz beginnen, `/SageBookingQueue` beobachten, dann breiter.

## Troubleshooting (aus UAT 2026-08-03)

Die tatsaechlich gesendete URL steht im Service-Log (`System.Net.Http.HttpClient.ISageLagerbuchungClient`).

- **URL enthaelt `/sdata/sdata/`:** `SData:BaseUrl` wurde inkl. `/sdata` eingetragen. Seit dem Fix
  wird die Wurzel nicht mehr verdoppelt (BaseUrl mit oder ohne `/sdata` funktioniert). Sauber:
  `https://sagetest01.ake.at:5493` (ohne `/sdata`).
- **Dataset-Segment falsch (z.B. nur `1`):** `SData:Dataset` muss den **vollen** Mandant-Wert tragen,
  z.B. `ake_TEST2026;1` (Semikolon **literal**, nicht kodieren — uebersteht das Speichern in
  `/ServiceSettings` unveraendert). `1` allein ist falsch.
- **HTTP 401 Unauthorized:** Basic-Auth-Credentials fehlen/falsch. `SageLagerbuchung:Username` und
  `:Password` im **Service**-`appsettings.json` am Zielserver setzen (appsettings-only, nicht in der
  DB) und den Dienst neu starten.
- **TLS-/Zertifikatsfehler:** `SageLagerbuchungSslZertifikatPruefen=false` fuer das Testsystem
  (`sagetest01` hat `PartialChain`) — vor Produktivgang wieder `true`.

## Risiken
- **Ein-Instanz-Voraussetzung:** genau **ein** laufender `SageBookingWorker` (kein Doppel-Deploy/Failover
  auf derselben Queue) — sonst Doppelbuchung.
- **TLS-Schalter:** `SageLagerbuchungSslZertifikatPruefen` (ServiceSetting, Default `true`) darf am
  Testsystem (`sagetest01.ake.at`, aktuell `PartialChain`) auf `false` — **vor Produktivgang zwingend
  auf `true` zuruecksetzen** (Warnhinweis erscheint im Worker-Log, `/ServiceSettings` und
  `/SageBookingQueue`, solange aus).
- Nicht daten-destruktiv; DB-Backup wie ueblich empfohlen.
