---
type: changelog
version: 1.8.6
date: 2026-05-05
---
# v1.8.6 — Bugfix: Einstellungen mit leerem Text-Feld speichern

- Behebt einen 500-Fehler beim Speichern der Einstellungen, wenn ein Text-Setting (z. B.
  `LackierteilKategorieName`) leer ist. Leere Werte werden jetzt als leerer String uebernommen.
- Relevant, weil „leer" bei mehreren Settings die dokumentierte Bedeutung *inaktiv* hat →
  [[0011-feature-toggles-ueber-appsettings]].
