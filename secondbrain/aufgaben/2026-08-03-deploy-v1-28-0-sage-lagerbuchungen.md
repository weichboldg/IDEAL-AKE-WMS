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

## Risiken
- **Ein-Instanz-Voraussetzung:** genau **ein** laufender `SageBookingWorker` (kein Doppel-Deploy/Failover
  auf derselben Queue) — sonst Doppelbuchung.
- Nicht daten-destruktiv; DB-Backup wie ueblich empfohlen.
