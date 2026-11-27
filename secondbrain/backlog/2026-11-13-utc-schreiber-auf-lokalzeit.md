---
typ: backlog
status: offen
created: 2026-11-13
herkunft: "Rückfrage 6 in [[2026-11-12-befunde-ake-regression-dev-spec]] (Gerald 2026-11-13: eigener Eintrag)"
standort: beide
---
# Weitere Schreiber mit UTC statt Lokalzeit

Konvention: Zeitstempel in Lokalzeit (ADR [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]], [[fallstricke]] „SyncLog.Timestamp ist
DateTime.Now“). Die Bugfix-Spec behebt nur die Bedarfsmeldungen. Weitere Schreiber mit `DateTime.UtcNow`/`GETUTCDATE()` laut Spec-Frontmatter
(Z. 35), u. a. `ProductionOrderPickingStatusRepository.cs:78/88/246`, `UsersController`, `RolesController`, `OrderRecipientsController`,
`TrackingController.cs:255`, Service-Syncs mit `ModifiedAt`. Vor einer Spec: vollständig erfassen (alle Wege), je Feld prüfen, ob die Anzeige
umrechnet, und Altdaten-Frage je Tabelle.
