---
type: changelog
version: 1.99.19
date: 2026-11-13
---
# v1.99.19 — Lieferverzug-Mail: Kreditor-Schwelle, Altersgrenze, Abschnitt „Zu klären“, Schalter für Rahmenabrufe

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `c31c06b0` (feature-complete), `def97b36` (Freigabe-Nachtrag 2 + QA Q2/Q5), QA `65496165`. **Testbereit** (Nach-QA 2026-11-13, DEV AKE+IDEAL). Spec [[2026-11-13-lieferverzug-nachbesserung-keyuser-spec]]
(Nachbesserung zu [[2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec]] nach den Keyuser-Antworten). Nicht gemergt;
/goal-Lauf [[2026-11-13-buendel-v1-99-19-goal-lauf]].

## Kernpunkte

- **Zwei Schwellen:** `Lieferverzug:TageNachTermin` (jetzt „Sammelliste: Tage nach Liefertermin“) und neu `Lieferverzug:KreditorTageNachTermin`
  (Default 3); an den Kreditor geht eine Position erst nach dem **Maximum** beider Schwellen (Freigabe-Antwort 2).
- **Altersgrenze:** neu `Lieferverzug:MaxAlterTage` (Default 180, EBB und EBZ, 0 = keine Grenze). Ältere Positionen gehen nicht an den Kreditor,
  sondern in den neuen Abschnitt **„Zu klären“** der Sammelliste (HTML + Text, Spalte Art, Betreff „davon k zu klären“). Die Sammelliste geht
  auch dann raus, wenn nur Positionen „zu klären“ anstehen. Parallel bereinigt der Einkauf alte Abrufe in Sage (Freigabe-Antwort 1).
- **Rahmenabrufe:** neu `Lieferverzug:KreditorMitRahmenabrufen` (Default **aus**): EBZ-Positionen stehen weiter in der Sammelliste, gehen aber
  erst mit dem Schalter an den Kreditor; ersetzt insoweit Antwort 1 der [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]]. Einschalten erst
  nach P-L4 (Freigabe-Antwort 3).
- Counts `zu_klaeren`, `vorlauf`, `rahmenabruf_ausgenommen`; Protokollzeile `Art = Einkauf` auch für „Zu klären“.
- **Lieferanten-Artikelnummer** (Freigabe-Nachtrag 2, 16:42): Spalte „Lief.-Art.-Nr.“ (EN „Supplier item no.“) in Kreditor-Mail und Sammelliste,
  HTML + Text, aus `ArticleSupplier.SageBestellnummer` je Artikel + Kreditor der Bestellung (`EinkaufPositionenInfo` hat keine eigene – lesender
  GET 2026-11-13, 91 Felder). Fehlt sie: leer. **Ohne `Sync:ArtikelLieferantEnabled` bleibt die Spalte leer** (AKE-DEV: aus). Ersetzt R11 aus v1.99.10.
- Ein Lieferantenname je Konto in der ganzen Mail (WMS-Kreditor → Sage-Name → Konto); Kreditor-Schwelle in der Einleitung nur bei `KreditorAktiv`.
- Spalte/Filter „bestätigt“: **nicht** hier, Folge-Spec nach P-L3 (Freigabe-Antworten 4/5).

## Befund P-L1 (AKE_WMS_DEV, lesend, Cache 13.11.2026 12:44)

Erste Welle Kreditor-Mail trotz 180 Tagen **99 Kreditoren** (ohne Abrufe 96) bei `KreditorMaxJeLauf` 30; EBZ „zu klären“ 73 Positionen
(älteste 03.07.2019). Laut Freigabe-Nachtrag kein Filter-Umbau – **Entscheidung über das Limit: Gerald.**

## Migration / SQL

Keine.

## Deploy-Risiken

Web (Einstellungskatalog, Hilfe) + Dienst (Planer/Mail). Fünf Manifest-Einträge v1.99.19 in `scripts/deploy/nachlauf.psd1` (Schwellen-Info,
Prüfungen Sammellisten-Tage und Adressen je Standort, Info). Der ältere Eintrag `v1.99.15-Rahmenabrufe` („Abrufe auch in den
Lieferverzug-Mails“) gilt seitdem nur noch für die Sammelliste.
