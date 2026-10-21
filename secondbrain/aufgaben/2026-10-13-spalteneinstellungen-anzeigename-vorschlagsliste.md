---
typ: aufgabe
status: offen
quelle: "[[2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen-spec]]"
---
# Spalten-Einstellungen: Anzeigename der Vorschlagsliste Beschichtung angleichen

QA-Befund M2 (2026-10-13, v1.56.0): `ColumnDefinitions.cs:377` und `:402` führen als Anzeigenamen „Beschichtungsauftrag (IDEAL)“
bzw. „Beschichtungsauftrag — Summiert (IDEAL)“. Sichtbar in den gespeicherten Spalteneinstellungen des Benutzers
(`Views/Users/Edit.cshtml:196`). Seit v1.56.0 heißt die Seite „Vorschlagsliste Beschichtung (IDEAL)“ — Vorschlag: Anzeigenamen
angleichen (nur Text, `ViewKey` bleibt, sonst gehen gespeicherte Einstellungen verloren). Außerhalb des Umfangs der Spec.

> Umgesetzt in [[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]] (v1.65.0, Bündel, 2026-10-21).
