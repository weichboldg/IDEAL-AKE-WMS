---
typ: feature
status: offen
created: 2026-11-26
herkunft: "Anforderung Gerald 2026-11-26 (Chat)"
standort: IDEAL
split: true
---
# Beschichtung Summe im Druck, Leitstand nur Haupt-FA, Kommissionierauftrag mit Masken-Umschaltung Stückliste/Kommissionierliste

Originaltext der Anforderung:

> **Beschichtung**
> - Ansicht im Beschichtungsauftrag immer Summiert (im Drucklayout)
> - Am Ende der Tabelle eine Summenzeile: Summe --> wie viele Teile waren in Auftrag drauf
>
> **Kommissionierung**
> - Leitstand
>   - Nur Haupt FA Anzeigen
>     - Checkbox auch auf Haupt FA zum freigeben
> - Kommissionierlisten (wenn dieses feature aktiv, soll präferiert das verwendet werden)
>   die Kommissionieraufträge sollen auch für die Kommissionierliste verwendet werden... eigentlich sollte kommissionierung (stückliste) und kommissionierung kommissionierliste im aufruf aus dem kommissionierauftrag auswählbar sein.
>   zb. kommissionierauftrag KO-2026-00004 am idealweb01:88 - wenn ich diesen öffne sollte die möglichkeit bestehen maske 1 (Stückliste) und maske 2 kommissionierliste umgeschaltet werden können. (wenn das feature aktiv -> standard "Liste")
>   - features wie
>     - checkboxen zur auswahl
>     - menge / soll - ist eingabe
>     - umbuchung auf / Ziellagerplatz zb. kommissionierwagen
>     - Auftrag Drucken / Abschließen
>     - Fotos hinzufügen

## Zerlegung (split)

1. **Beschichtungsauftrag Druck**: Drucklayout immer summiert, Summenzeile „Summe Teile im Auftrag“.
2. **Leitstand nur Haupt-FA**: Leitstand zeigt nur Haupt-FA; Freigabe-Checkbox am Haupt-FA.
3. **Kommissionierauftrag Masken-Umschaltung**: Kommissionierauftrag (z. B. KO-2026-00004) öffnet wahlweise Stückliste oder Kommissionierliste; bei aktivem Kommissionierliste-Feature Standard „Liste“; Liste braucht Auswahl-Checkboxen, Soll/Ist-Menge, Umbuchung auf Ziellagerplatz (Kommissionierwagen), Drucken/Abschließen, Fotos.

Bezug (zu prüfen): [[2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand]], [[2026-11-07-kommissionierung-auftraege-kopf-zeilen]], [[2026-11-25-kommissionierauftraege-nachtrag-automatisch-knopf-nur-admin]], [[2026-09-25-leitstand-subfa-readonly-stueckliste]].
