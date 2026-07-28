# OverridePrePickingDays wirkungslos

Beim Nacherfassen aufgefallen: `ProductionWorkplace.OverridePrePickingDays`
wird in den Werkbank-Stammdaten gepflegt und angezeigt, aber von keiner
Terminberechnung gelesen (grep-verifiziert, nur CRUD-/Anzeige-Treffer).
Ein Admin setzt pro Werkbank einen Override und erwartet Wirkung auf den
Kommissionier-Termin — es passiert nichts, ohne Rueckmeldung.

Das soll bereinigt werden. Unklar ist, in welche Richtung:
entweder das Feld tatsaechlich in die Terminberechnung einbinden
(wo genau? Prioritaet gegenueber dem globalen Wert?) oder das Feld
samt UI entfernen, damit keine falsche Erwartung entsteht.

Kontext: siehe secondbrain/architektur/fallstricke.md (Eintrag dazu)
und der offene Punkt in secondbrain/feature-map.md.