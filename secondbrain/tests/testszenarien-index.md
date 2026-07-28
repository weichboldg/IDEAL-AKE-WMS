---
type: test-index
updated: 2026-07-27
---
# Testszenarien-Index

Single Source of Truth der manuellen Abnahme ist `../../docs/TESTSZENARIEN.md` (Stand 2026-07-22,
v1.26.0). Dieser Index verlinkt Kapitel ↔ Feature/Spec, damit Agenten und Menschen schnell finden,
welches Szenario zu welcher Aenderung gehoert. Feature-Kontext: [[feature-map]].

**Pflicht:** Bei jedem neuen Feature **und** jedem Bugfix wird `TESTSZENARIEN.md` ergaenzt und
dieser Index nachgezogen.

Die Kapitel 1–26 sind nach **Fachbereich** gegliedert, ab Kapitel 27 nach **Release** — historisch
gewachsen, bewusst nicht umsortiert (die TS-Ids sind in Abnahmeprotokollen referenziert).

## Fachbereiche (Kapitel 1–26)

| Kapitel | Szenarien | Feature / Spec |
|---|---|---|
| 1. Authentifizierung & Zugriff | TS-1.1 – 1.7 | Login, Session-Timeout, Rollen-Zugriff, Admin-Wildcard, BDE-Gate → [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]], [[0011-feature-toggles-ueber-appsettings]] |
| 2. Lager | TS-2.1 – 2.21 | Ein-/Aus-/Umbuchung, Lagerplatz en bloc, Bestand, Historie, Artikelinfo, Meldebestand, QR-Scan, FA-Lagerplatz-Hinweis (`einbuchung-fa-autofill`) |
| 3. Stammdaten | TS-3.1 – 3.16 | Benutzer, Rollen, Werkbank, Lagerplatz, Artikel/Kategorien/Merkmale, Settings-Toggles, Empfaengergruppen, BDE-Stammdaten, View-Reset |
| 4. Fertigungsauftraege | TS-4.1 – 4.36 | FA-Liste, KW-Filter, Flags, enaio-Dokumente, Leitstand-Freigabe/Bulk/Prioritaet, Baugruppen-Flags, ProductionOrder-Split (Migration + AgentJob), Slim-Index, Compat-Redirects, FA-Vervollstaendigung |
| 5. Stueckliste (BOM) | TS-5.1 – 5.9 | BOM aufrufen, Fehlteil-Filter, Druck, Picking-Start, Bedarfsmeldung, Sammelbestellung, Foto, BOM-Cache in der Artikelinfo |
| 6. Kommissionierung / Picking | TS-6.1 – 6.10 | Kommissionierliste mit/ohne Leitstand, Picker-Zuweisung, Menue-Badge, Status, Druck, Spaltenfilter |
| 7. OSEON Teileverfolgung | TS-7.1 – 7.10 | Auftragsliste, 3-Ebenen-Baum, Ampel, Filter, Werkbank-Filter, Artikelsuche + QR |
| 8. BDE Phase 1 | TS-8.1 – 8.15 | `2026-04-14-bde-phase-1-design.md` |
| 9. BDE Phase 2.1 — Werkbank-Erweiterungen | TS-9.1 – 9.5 | `2026-04-20-bde-phase-2-1-werkbank-erweiterungen-design.md` |
| 10. BDE Phase 2.2 — Mehrfachanmeldung + Zeit-Split | TS-10.1 – 10.15 | `2026-04-21-bde-phase-2-2-mehrfachanmeldung-zeit-split-design.md` |
| 11. Bestellungen / Bedarfsmeldungen | TS-11.1 – 11.7 | `2026-04-02-bedarfsmeldungen-design.md` |
| 12. Print + OSEON-Tracking-Verbesserungen | TS-12.1 – 12.6 | `2026-04-17-print-tracking-improvements-design.md` |
| 13. Spalten-Konfiguration & Filter | TS-13.1 – 13.7 | `2026-04-10-customizable-view-preferences-design.md` |
| 14. Service / Sync (read-only Verifikation) | TS-14.1 – 14.4 | Windows-Service, Sync-Bloecke → [[services]] |
| 15. BDE Phase 2.3 — Schichtkalender + Auto-Pause | TS-15.1 – 15.12 | `2026-04-27-bde-phase-2-3-schichtkalender-auto-pause-design.md` |
| 16. OSEON Reporting — AG-Uebersicht | TS-16.1 – 16.6 | `2026-04-30-oseon-reporting-ag-uebersicht-design.md` |
| 17. OSEON Tracking — Artikel-Filter | TS-17.1 – 17.4 | `2026-04-30-oseon-tracking-article-filter-fix-design.md` |
| 18. Lagerbestellung aus der Produktion | TS-18.1 – 18.9 | `2026-04-30-lagerbestellung-aus-produktion-design.md` |
| 19. Listen-Pagination & User-Default | TS-19.1 – 19.6 | v1.14.0 → [[0005-listen-view-pattern-mit-server-side-spaltenfilter]] |
| 20. Server-Side Spaltenfilter | TS-20.1 – 20.7 | v1.14.0 → [[0005-listen-view-pattern-mit-server-side-spaltenfilter]] |
| 21. Leitstand als eigenes Hauptmenue | TS-21.1 – 21.2 | v1.14.0, `2026-04-03-navigation-restructuring-design.md` |
| 22. Lagerbestellungen — Notiz + INT-Mengen | TS-22.1 – 22.6 | v1.14.0; Culture-Fallstrick in [[fallstricke]] |
| 23. FA-Vervollstaendigung als Feature-Toggle | TS-23.1 – 23.3 | v1.14.0, Gate `FaCompletionAktiv` |
| 24. StorageLocation-Code 50 Zeichen | TS-24.1 – 24.3 | v1.14.0 |
| 25. OSEON-Tabellen-Hover | TS-25.1 | v1.14.0 |
| 26. Bestand: Source-Lagerplatz-Vorschlag bei Sage-Stock | TS-26.1 – 26.2 | v1.14.0; Fallstrick „Picking Source-Fallback ist NAN" |

## Nach Release (Kapitel 27–55)

| Kapitel | Version | Feature / Spec |
|---|---|---|
| 27. SyncLog-Pflicht fuer alle Sync-Services | v1.15.0 | `2026-05-26-synclog-pflicht-alle-syncs-design.md` → [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] |
| 28. Activity-Log fuer Non-Sync-Services | v1.15.1 | `2026-05-27-activity-log-non-sync-services-design.md` |
| 29. OSEON Stammdaten-Imports im Aktivitaets-Protokoll | v1.15.2 | Workplaces + ArticleCategories |
| 30. OSEON-Tracking iOS-Fix + Lazy-Load | v1.16.0 | `2026-05-28-oseon-tracking-ios-fix-design.md` |
| 31. Artikel-Sync-Erweiterung | v1.17.0 | `2026-05-28-article-sync-erweiterung-design.md` |
| 32. Lagerbestellungen Teilgeliefert + Fehlteile | v1.18.0 | `2026-05-29-teilgeliefert-fehlteile-design.md`, `-missingparts-include-partially-delivered-` |
| 33. ShortageStatus 3-State + 2-Tab Fehlteile | v1.19.0 | `2026-05-29-shortage-status-3state-design.md`, `-note-einkauf-mineonly-lager-view-` |
| 34. Feingranulare Berechtigungen | v1.20.0 | `2026-06-03-finegrained-permissions-design.md` |
| 35. v1.20.0-Bugfixes (Post-Initial-Release) | v1.20.0 | Model-Binder `int[]`, Date-Picker-Event, Server-Filter-Init → [[fallstricke]] |
| 36. Universal-Filter-Rollout | v1.21.0 | `2026-06-10-universal-filter-rollout-design.md` |
| 37. FA-Abschliessen | v1.21.1 | Lese-Seite `IsDone \|\| IsDonePicking` → [[0009-app-status-in-satelliten-tabellen-neben-sage-master]] |
| 38. FA-Vorbau | v1.22.0 | `2026-06-12-fa-vervollstaendigung-erweiterung-design.md`; **cutover-kritisch** (AgentJob) |
| 39. UI-Nachzuegler | v1.22.0 Folge-Fixes | enaio-Badges, Werkbank-/AG-Defaults, Text-Merkmal |
| 40. Windows-Authentifizierung & AD-Benutzer | v1.23.0 | `2026-06-18-windows-auth-ad-users-design.md` + `2026-07-09-windows-auth-ua-gate-design.md`. **Der zentrale Manual-UAT-Block** — SSO, LDAP, Negotiate, POSTs/Antiforgery, Logout Desktop+Mobile → [[0002-dual-auth-session-login-plus-windows-sso]] |
| 41. Rolle „Lagerbestellung" + Artikelinfo fuer Stammdaten-ansehen | v1.23.0 | `2026-06-19-lagerbestellung-rolle-artikelinfo-design.md` |
| 42. Lagerbestellungs-Druck spiegelt GUI | v1.23.0 | `2026-06-19-warehousepicking-print-spalten-sort-design.md` |
| 43. FA-Abarbeitungsliste — Komma-Werkbank-Filter + Bezeichnung | v1.23.0 | `2026-06-19-faworklist-werkbankfilter-bezeichnung-design.md` |
| 44. FA-AG-Erkennung + BOM-Cache — Protokoll-Aufgliederung | v1.23.0 | `2026-06-25-fa-detection-protokoll-aufgliederung-design.md` |
| 45. FA-Vorbau 3-Wert-Status + Beschichtungstermin + ENTER-Spaltenfilter | v1.24.0 | `2026-06-25-faworkstep-3state-coating-filter-design.md`; Migration 76 **daten-konvertierend** |
| 46. Glas-Bestellung (Bestelltyp Lager/Glas) | v1.25.0 | `2026-07-03-glas-bestellung-design.md` |
| 47. Service-Resilienz + Fehlermail + ProductionOrders-515-Fix | v1.25.0 | `RunResilientAsync`, `SyncErrorNotifier`, `SubOrderNumber`-`COL_LENGTH`-Check |
| 48. Rolle `stock_read` + FA-Liste/Stueckliste fuer Vorbau | v1.25.0 | `2026-07-07-vorbau-bom-button-stock-read-role-design.md` |
| 49. Hauptlagerplatz am Artikel | v1.25.0 | `2026-07-07-hauptlagerplatz-design.md`; Sync-Regeln sind Manual-UAT (raw SQL) |
| 50. FA-Reconciliation (verwaiste FAs stornieren) | v1.25.0 | `2026-07-07-fa-reconciliation-design.md`; **Manual-UAT** — Reconcile-UPDATE ist raw SQL |
| 51. Typisierte, vollstaendige Service-Einstellungen | v1.25.0 | `2026-07-08-service-settings-typed-catalog-design.md`. **Deploy-Pflicht:** jeder Sync muss hier einmalig aktiviert werden → [[0008-servicesettings-db-first-mit-typisiertem-katalog]] |
| 52. Lagerbestellung aus der Stueckliste + Master-Schalter | v1.25.0 | `2026-07-09-lagerbestellung-aus-bom-design.md` |
| 53. Lagerbestand-Nullsetzen verwaister Paare | v1.25.0 | `2026-07-09-lagerbestand-nullsetzen-verwaist-design.md` |
| 54. Aktivitaets-Protokoll-Bereinigung | v1.25.0 | `2026-07-15-cleanup-jobs-service-design.md` (TS-54.1 – 54.4) |
| 55. FA-Zusatzinfos (Sage) | v1.26.0 | `2026-07-22-pa-zusatzinfos-design.md` (TS-55.1 – 55.13). **Erstlauf-Pflicht:** DryRun fahren und `erledigt-gesetzt` kontrollieren (TS-55.1, 55.10–55.13); Recovery-SQL steht im Kapitel |

## Kapitel mit besonderem Gewicht

Diese Kapitel deckt **kein** automatisierter Test ab — sie sind der einzige Nachweis:

| Kapitel | Warum nur manuell pruefbar |
|---|---|
| 40 (Windows-Auth) | Negotiate-Handshake + LDAP brauchen Domaene und IIS |
| 49, 50, 53 (Hauptlagerplatz, Reconciliation, Nullsetzen) | Schreibpfade sind raw SQL — nur die Planner/Reconciler sind unit-getestet |
| 51 (Service-Einstellungen) | Die wertabhaengige „laeuft-wenn-in-DB-enabled"-Wirkung; Tests sichern nur die Invariante |
| 55 (FA-Zusatzinfos) | Sage-View-Read + automatisches Erledigt-Setzen mit Cap |

Begruendungen im Detail: [[fallstricke]] Abschnitt 8.
