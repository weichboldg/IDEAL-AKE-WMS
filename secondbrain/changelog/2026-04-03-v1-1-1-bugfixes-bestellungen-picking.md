---
type: changelog
version: 1.1.1
date: 2026-04-03
---
# v1.1.1 — Bugfixes Bestellungen und Kommissionierung

- **Picking erst beim Umbuchen:** die Checkboxen in der Stueckliste sind rein clientseitig; erst der
  Klick auf „Gepickte Artikel umbuchen" sendet alle Auswahlen mit Quell-Lagerplatz in **einer**
  Transaktion.
- Empfaengergruppen: Redirect nach Create direkt auf Edit (damit sofort Empfaenger erfasst werden).
- Bedarfsmeldungen: alle aktiven Empfaenger der zugeordneten Gruppe sind im Modal vorausgewaehlt;
  `ArticleGroup`, `OrderRecipientGroupId` und `SentToEmails` werden korrekt gespeichert.
- **Zwei Parsing-Fallstricke behoben**, die bis heute dokumentiert sind: Em-Dash statt Hyphen im
  Artikel-Split (Wareneingang zeigte offene Bedarfsmeldungen nicht), und die Artikelgruppe muss als
  reiner Code gesendet werden (`940`, nicht `940 - Kleinmaterial Allgemein`).
- Footer-Version: `v@(...)` statt `v@...` — Razor parst `v@Namespace.Class` sonst als E-Mail.
