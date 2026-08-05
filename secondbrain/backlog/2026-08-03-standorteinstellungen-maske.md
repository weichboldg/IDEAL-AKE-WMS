---
type: backlog
title: Standorteinstellungen-Maske (Firmenname, Adresse, Mandant, FA-Logik)
status: neu
created: 2026-08-03
herkunft: "Schranke-1-Antwort 5 zu [[2026-07-29-sage-lagerbuchungen-spec]] (dort bewusst out-of-scope, S5)"
---

# Standorteinstellungen-Maske

Aus der Schranke-1-Antwort 5 zur Sage-Lagerbuchungs-Spec: Idee einer eigenen
**Settings-Maske „Standorteinstellungen"** mit Firmenname, Adresse, **Mandant/dataset**,
Fertigungsauftragslogik u. a. — als zentrale Stelle fuer standortspezifische Konfiguration
(AKE vs. IDEAL, getrennte Deployments).

**Bewusst NICHT Teil von v1.28.0** (Sage-Lagerbuchungen). Dort bleibt `SData:Dataset` ein einfacher
ServiceSetting; ein groesseres Standorteinstellungs-Vorhaben waere eigenes Scope-Creep gewesen (S5).

## Zu klaeren, falls aufgegriffen
- Verhaeltnis zu bestehenden `ServiceSettings` (DB-first, [[0008-servicesettings-db-first-mit-typisiertem-katalog]])
  und `AppSettings` — neue Kategorie/Maske oder eigene Tabelle?
- Welche Werte gehoeren wirklich „pro Standort" (Mandant, Firmenname, Adresse) vs. bestehende Keys?
- Beruehrungspunkte mit den IDEAL-Anpassungen (Sub-FA-Granularitaet,
  `backlog/2026-07-27-ideal-anpassungen-neu-nachbilden.md`).
