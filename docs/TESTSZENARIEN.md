# IDEAL-AKE-WMS — Testszenarien

Dieses Dokument enthält manuelle Testszenarien für die Hauptfunktionen des IDEAL-AKE-WMS. Jeder Bereich hat fortlaufend nummerierte Testfälle (TS-Bereich.Nummer) mit Vorbedingungen, Schritten und erwartetem Ergebnis.

## 1. OSEON Reporting — AG-Übersicht

### TS-1.1 — KPI-Cards zeigen korrekte Counts
**Vorbedingungen:** Reporting-Rolle, mindestens 1 AG je Bereich (Überfällig / Heute / Zukunft) im OSEON-Mirror.
**Schritte:**
1. Menü Reporting -> OSEON AG-Übersicht öffnen.

**Erwartet:** 4 KPI-Cards zeigen plausible Counts; Summe Heute geplant ≥ Heute erledigt.

### TS-1.2 — Tab-Wechsel filtert Tabelle
**Vorbedingungen:** Daten in allen 3 Slices vorhanden.
**Schritte:**
1. Tabs reihum klicken: Heute / Überfällig / Zukunft / Alle.

**Erwartet:** Tabelle zeigt nur Zeilen des aktiven Slice. Default-Tab ist Heute.

### TS-1.3 — Filter Werkbank + AG-Name greifen
**Vorbedingungen:** Mehrere Werkbänke, mehrere AG-Namen.
**Schritte:**
1. Werkbank-Dropdown auf "WB-A1" stellen, AG-CSV "B" eintragen, Anwenden klicken.

**Erwartet:** Nur AGs vom Typ B an WB-A1 sichtbar. KPI-Counts spiegeln Filter.

### TS-1.4 — Auftragsnummer-Link öffnet Tracking
**Schritte:**
1. In der Tabelle auf eine FA-Nummer klicken.

**Erwartet:** OSEON-Tracking-Seite öffnet in neuem Tab, vorgefilltert mit der FA-Nummer.

### TS-1.5 — Banner für ungepflegte Configs
**Vorbedingungen:** AG-Name "FOO" in OSEON-Auftrag, KEIN Eintrag in OseonOperationConfig.
**Schritte:**
1. Reporting öffnen.

**Erwartet:** Gelber Banner "X AG(s) ohne Config-Eintrag".

### TS-1.6 — Berechtigungs-Block
**Vorbedingungen:** User OHNE Rolle reporting.
**Schritte:**
1. Direkter URL-Aufruf /OseonReporting/OperationsOverview.

**Erwartet:** Redirect auf AccessDenied.

## 2. OSEON Tracking — Artikel-Filter

### TS-2.1 — Artikelnummer-Filter via Form-Submit
**Vorbedingungen:** Tracking-Rolle, mindestens 3 OSEON-Auftraege mit unterschiedlichen Artikelnummern, davon mindestens einer mit Artikelnummer-Pattern "ART-100*".
**Schritte:**
1. /Tracking/OseonIndex oeffnen.
2. In das Feld "Artikelnummer" `100` eingeben.
3. Auf "Filtern" klicken.
**Erwartet:** Ergebnis enthaelt nur Customer-Order-Gruppen mit mindestens einem Sub-Auftrag dessen ArticleNumber `100` enthaelt. Innerhalb einer matchenden Gruppe werden ALLE Sub-Auftraege angezeigt (auch nicht-matchende — siehe Spec §4 Group-Pagination). Filter-Wert bleibt im Input. Pagination wirkt auf gefiltertes Group-Ergebnis.

### TS-2.2 — Kombinierter Filter (Artikel + Werkbank + Auftrag)
**Vorbedingungen:** Daten mit verschiedenen Werkbaenken + Artikelnummern.
**Schritte:**
1. Artikelnummer eingeben + Werkbank waehlen + Auftragsnummer-Suchterm eingeben.
2. "Filtern" klicken.
**Erwartet:** Schnittmenge aller Filter (konjunktiv auf Group-Ebene). Reset-Link sichtbar.

### TS-2.3 — QR-Scan triggert Form-Submit
**Schritte:**
1. Auf den QR-Button neben dem Artikelnummer-Input klicken.
2. Artikel-QR-Code scannen.
**Erwartet:** Input wird mit dem gescannten Wert befuellt UND Form wird automatisch submitted (Page-Reload mit Filter aktiv).

### TS-2.4 — Reset entfernt alle Filter inkl. Artikel
**Vorbedingungen:** Mindestens ein Filter ist aktiv (Artikel, Auftrag, Werkbank, ShowFinished oder useRelevanceFilter=false).
**Schritte:**
1. "Zuruecksetzen"-Link klicken.
**Erwartet:** Alle Filter zurueckgesetzt (Artikel-Input leer), volle Liste sichtbar.
