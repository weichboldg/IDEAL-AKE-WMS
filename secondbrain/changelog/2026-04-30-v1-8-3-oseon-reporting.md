---
type: changelog
version: 1.8.3
date: 2026-04-30
---
# v1.8.3 — OSEON Reporting (AG-Uebersicht) + Tracking-Artikel-Filter-Fix

- **Neuer Reporting-Bereich** mit KPI-Karten (Ueberfaellig / Heute geplant / Heute erledigt /
  Zukunft) und filter-/sortierbarer AG-Liste; Tabs Heute/Ueberfaellig/Zukunft/Alle. Rolle
  `reporting`.
- Banner fuer AGs **ohne** `OseonOperationConfig`-Eintrag — diese werden im Reporting ignoriert, was
  sonst unerklaerlich waere.
- Werktag-/Offset-Berechnung in `OseonDueDateCalculator` extrahiert (von Tracking und Reporting
  gemeinsam genutzt).
- **Fix Tracking-Artikelfilter:** vom Browser-Live-Filter (verursachte App-Freeze beim Tippen) auf
  einen server-seitigen Filter umgestellt; QR-Scan eines Artikelcodes loest den Submit aus.
- Neue AppSetting `OseonReportingHorizonDays` (10), neuer Index auf `OseonProductionOrders.ArticleNumber`.
