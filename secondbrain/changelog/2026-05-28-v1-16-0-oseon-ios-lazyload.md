---
type: changelog
version: 1.16.0
date: 2026-05-28
---
# v1.16.0 — OSEON-Tracking iOS-Fix + Lazy-Load-Refactor

Anlass: die OSEON-Tracking-Seite war auf iOS Safari praktisch unbedienbar (Traegheit, Input-Lock,
kaputter QR-Button).

- **Lazy-Load:** die Seite rendert nur Top-Level-Gruppen; Subauftraege und Arbeitsgaenge kommen per
  AJAX (`/Tracking/OseonGroupDetails`). ~50x weniger initiales DOM.
- Baumaufbau in `OseonGroupViewModelBuilder` extrahiert.
- **Wichtig:** `GetSubOrdersForCustomerOrderAsync` darf WorkOperations **nicht** auf
  `relevantOperationNames` filtern — der Builder braucht alle Ops, um „nur nicht-relevante Ops =
  Fertig" zu erkennen (war ein Bug im Verlauf, `fee19b7`).
- Sub-Row-Handler nur per **Event-Delegation** — direkt gebundene Handler erreichen AJAX-Zeilen nicht.
- QR-Scanner: Permission-Pre-Warm im synchronen Click-Stack (iOS verweigert die Kamera nach einem
  `await`); `html5-qrcode` lokal gehostet statt per CDN.
