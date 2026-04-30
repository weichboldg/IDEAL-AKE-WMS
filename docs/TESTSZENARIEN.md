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
1. Direkter URL-Aufruf /Reporting/OseonOperations.

**Erwartet:** Redirect auf AccessDenied.
