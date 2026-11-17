---
type: aufgabe
status: offen
created: 2026-11-17
quelle: "[[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]]"
---
# Kontrast: weißer Text auf `.btn-success` / `.btn-secondary` unter WCAG AA

QA-Befund F2 (BDE-Teileliste, v1.99.32): Markenfarben in `IdealAkeWms/wwwroot/css/site.css:6,8,151-170` ergeben ca. 2,2:1 (`.btn-success`)
bzw. 2,7:1 (`.btn-secondary`) – Ziel 4,5:1 (Text) / 3:1 (Bedienelement). Global und schon vor dem Bündel vorhanden; betrifft alle Terminals.
Vorschlag: Farbtöne abdunkeln oder dunklen Text; einmal zentral in `site.css`, mit Screenshot-Abgleich Terminal/Listen.
