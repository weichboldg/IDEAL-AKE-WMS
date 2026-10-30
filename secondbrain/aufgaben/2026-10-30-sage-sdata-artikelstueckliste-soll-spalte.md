---
type: aufgabe
title: Sage live – Artikelstückliste (Herkunft 1) als Soll-Spalte
status: NEU
spec: "[[2026-10-30-sage-sdata-fa-ansicht-spec]]"
assignee: agent
blocked_by: []
created: 2026-10-30
updated: 2026-10-30
---

Folgeaufgabe aus Freigabe-Antwort 2 der Spec [[2026-10-30-sage-sdata-fa-ansicht-spec]] (v1.71.0): Die Ansicht „FA-Struktur (Sage live)“
zeigt nur den FA-Beleg (`dtsTeileverwendungsnachweis`, Herkunft 2). Gewünscht ist später ein Soll/Ist-Vergleich mit der
Artikelstückliste (`where=KopfArtikelnummer eq '<Artikel>'`, nur Herkunft 1, BelID null).

Offen vor einer Spec:
- Es gibt keinen gemeinsamen Positionsschlüssel (keine BelPosID in der Quelle) – Abgleich nur über Ressourcenummer, bei Mehrfach-
  vorkommen mehrdeutig (melden, nicht still zuordnen).
- Artikelnummer als String-`where` in `'…'`; Werte nur aus der vorherigen Sage-Antwort verwenden, nie Nutzereingabe.
- +1 Aufruf je Artikel bzw. ein `or`-Aufruf je Block (wie v1.71.0).

## Verlauf
- 2026-10-30: angelegt beim Dev-Lauf v1.71.0 (nicht umgesetzt).
