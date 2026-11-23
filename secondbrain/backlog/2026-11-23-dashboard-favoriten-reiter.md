---
type: backlog
typ: feature
created: 2026-11-23
status: erledigt
spec: "[[2026-11-23-dashboard-favoriten-reiter-spec]]"
quelle: Gerald 2026-11-23 (Sammelauftrag „bitte folgende features planen“, Punkt 1)
modul: Dashboard / Startseite
bezug: "[[2026-11-19-dashboard-kacheln-nach-kategorie-und-berechtigung-spec]]"
---
# Dashboard: Reiter „Favoriten“

Wortlaut: *Dashboard „FAVORITEN-Reiter“ – User sollen sich Favoriten setzen können.*

Hinweise für die Spec:
- Die Dashboard-Spec v1.99.45/46 (Kacheln nach Kategorie und Berechtigung, Quelle `NavigationKatalog`, ADR 0034)
  hat „Benutzerdefinierte Startseite oder Favoriten“ ausdrücklich ausgeschlossen (Spec Z. 110) – das hier ist der Folgeschritt.
- Favoriten nur aus Kacheln, die der Benutzer laut Berechtigung ohnehin sieht; entzogene Rechte = Favorit verschwindet (melden, nicht still).
- Speicherort je Benutzer (DB, nicht Browser – gleiche Favoriten am Terminal und am PC) – Spec soll prüfen, ob es schon
  eine Benutzer-Einstellungsablage gibt (z. B. Profil/Scanner-Schalter) und die wiederverwenden.
- Offen für Rückfrage: Reiter als Standard beim Öffnen, wenn Favoriten vorhanden? Reihenfolge frei sortierbar oder Katalog-Reihenfolge?
  Favoriten auch im Menü?
