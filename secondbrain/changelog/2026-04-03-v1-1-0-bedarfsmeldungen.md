---
type: changelog
version: 1.1.0
date: 2026-04-03
---
# v1.1.0 — Bedarfsmeldungen

- Fehlteile koennen direkt aus der Stueckliste als interne Bedarfsmeldung erfasst werden (Einzel-
  oder Sammelbestellung) mit Prioritaeten Normal/Dringend/Eilt.
- **E-Mail-Benachrichtigung** an konfigurierbare Empfaengergruppen (Versand ueber den
  Windows-Service).
- Bestelluebersicht mit Status-Badges, Prioritaet, Filter und Pagination.
- **Empfaenger-Verwaltung:** Empfaengergruppen mit Empfaengern und N:M-Zuordnung zu Artikelgruppen.
- Wareneingang-Integration: offene Bedarfsmeldungen erscheinen bei der Einbuchung und koennen mit
  der Buchung verknuepft werden.
- Neue Tabellen `PartRequisitions`, `OrderRecipientGroups`, `OrderRecipients`,
  `ArticleGroupRecipientMappings` (`SQL/36`). Modul-Gate `BestellungenAktiv`.
