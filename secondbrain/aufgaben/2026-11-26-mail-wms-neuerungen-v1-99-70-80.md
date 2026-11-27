---
type: aufgabe
status: entwurf
date: 2026-11-25
version: 1.99.81
source: "[[2026-11-25-kommission-lagerplatz-bde-goal-lauf]]"
---
# Mail-Entwurf: WMS-Neuerungen v1.99.70 bis v1.99.80

> [!warning] Nur Entwurf – nicht gesendet
> Versand durch einen Menschen. Vorher: Versionen auf den Zielsystemen eingespielt? Confluence-Seiten sind seit 2026-11-26 übernommen (IDs und Versionen in
> [[2026-11-25-kommission-lagerplatz-bde-goal-lauf]], Abschnitt „Doku-Nachzug v1.99.80“); die Links unten sind eingesetzt.
> Stand des Codes: v1.99.70–80 **Testbereit**, v1.99.81 feature-complete (QA offen), noch nicht gemergt und nicht ausgerollt – das Datum „ab …“ erst nach dem Deploy eintragen.

**Betreff:** WMS – Neuerungen ab Version 1.99.81: BDE-Seite, Teilfertigmeldung, Kommissionier-Nachtrag, Lagerplätze in Sage

---

Hallo zusammen,

mit dem Update auf WMS-Version **1.99.81** (ab [Datum]) ändern sich einige Abläufe. Unten steht je Bereich, was neu ist und was Sie tun
müssen. Was nur für einen Standort gilt, ist mit **(AKE)** oder **(IDEAL)** markiert; alles andere gilt an beiden Standorten.

## Werker / BDE

- **Eine BDE-Seite für alles.** *BDE → Terminal* öffnet jetzt immer die neue BDE-Seite mit den Reitern *Teile Einzeln*, *Teile Summiert*
  und *FA*. Die alte Terminal-Seite gibt es nicht mehr. Anmelden wie gewohnt mit Ihrer Personalnummer.
  WMS-Anleitung 11 – BDE: Arbeitsgänge buchen: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/681017386
- **Reiter FA mit „Meine Buchungen“.** Dort sehen Sie Ihre laufenden und pausierten Arbeitsgänge und können Pause, Teilfertig, Fertig und
  Fortsetzen direkt drücken. Mit „FA scannen“ springt die Liste zum gescannten Auftrag.
- **Teilfertig melden (IDEAL).** In der Teileliste können Sie eine Teilmenge melden; die Zeile bleibt offen und zeigt „gemeldet X von Soll“.
  „Fertig melden“ schließt die Zeile ab. Eine eigene Meldung lässt sich zurücknehmen.
  WMS-Anleitung 17 – BDE-Teileliste an der Werkbank: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/688160769
- **Tageshistorie.** Der Knopf im Werker-Balken (oder Menü *BDE → Tageshistorie*) zeigt alle Ihre Buchungen und Meldungen von heute.
- **Alle Buchungen nur am eigenen Terminal und vom eigenen Werker.** Pause, Teilfertig, Fertig, Fortsetzen und Abschließen nimmt das WMS
  nur an, wenn das Terminal Ihrem Benutzer zugeordnet ist und die Buchung dem angemeldeten Werker gehört. Kommt die Meldung „Dieses Terminal
  ist nicht dem angemeldeten Benutzer zugeordnet“ oder „Diese Buchung gehört nicht dem angemeldeten Werker“: Seite neu laden, es wurde nichts
  gebucht. Bleibt die Meldung, bitte beim BDE-Admin melden.
- **Fertig nach Teilmengen (seit 1.99.81).** Nach einer Teilfertigmeldung schlägt „Fertig“ die Restmenge vor; ein Teil davon kann als
  Ausschuss eingetragen werden. Ist die Sollmenge schon gemeldet, darf „Fertig“ mit 0 / 0 abschließen.
- **Pause nicht mehr fortsetzbar (seit 1.99.81).** Ist der Auftrag inzwischen abgeholt oder abgeschlossen, zeigt „Meine Buchungen“ den Grund
  und den Knopf „Abschließen“. Der Kopf der BDE-Seite lässt sich einklappen; als BDE-Admin wechseln Sie die Werkbank, ohne den Werker neu anzumelden.
- **Blechdicke und Fräsen-Pfad (IDEAL).** Die Teileliste zeigt die Blechdicke (fehlt der Wert, steht „–“). Den Fräsen-Pfad können Sie über
  das Zahnrad einblenden und mit „Pfad kopieren“ übernehmen.

## Lager / Kommissionierung

- **Fehlende Kommissionieraufträge kommen nachts von selbst.** Ein nächtlicher Lauf legt für freigegebene Fertigungsaufträge die noch
  fehlenden Kommissionieraufträge an. Sie müssen dafür nichts mehr drücken. **Wichtig:** Der Lauf startet beim ersten Dienststart nach dem
  Update sofort und legt dann auf einen Schlag alle bisher fehlenden Aufträge an – bitte am Update-Tag mit mehr neuen Aufträgen rechnen.
  WMS-Anleitung 01 – Kommissionierung: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/681082881
- **Hinweis „⚠ Stückliste kleiner“.** Wurde die Stückliste in Sage verkleinert, zeigt das WMS diesen Hinweis. Bereits offene Zeilen werden
  nicht gekürzt – bitte die Menge prüfen.
- **Bestandsübersicht.** Die Spalte „Aktueller Bestand“ bleibt immer sichtbar.
  WMS-Anleitung 10 – Bestände und Spaltenfilter-Suche: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/681017366
- **Lagerplätze in Sage anlegen.** Neue Lagerplätze können im WMS erfasst werden; das WMS legt sie dann in Sage an. Diese Funktion schaltet
  die IT erst nach Abnahme frei – bis dahin bleibt alles wie bisher.
  WMS-Anleitung 19 – Lagerplätze in Sage anlegen: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/701005871

## Leitstand

- **„Altbestand geprüft“ legt die Aufträge sofort an.** Klappt das Anlegen einmal nicht, erscheint eine gelbe Meldung; der nächtliche Lauf
  holt die Aufträge dann nach.
- **Knöpfe „Fehlende Kommissionieraufträge …“** sind nur noch für Administratoren sichtbar – der nächtliche Lauf erledigt das.
- **Werkbank-Einstellung „BDE-Standardsicht“ (IDEAL).** Je Werkbank lässt sich festlegen, ob die BDE-Seite mit den Teilen oder mit den
  FA startet. WMS-Anleitung 17 – BDE-Teileliste an der Werkbank: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/688160769

## Einkauf

- **Lagerbestellungen an der Isolierungsfräse (IDEAL).** Abgeschickte Lagerbestellungen mit dem Kürzel der Werkbank erscheinen in deren
  Teileliste. Der Termin ist Abschicken plus 10 Arbeitstage.
  WMS-Anleitung 03 – Lagerbestellung und Glasbestellung: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/681115649
- **Fräsen-Pfad je Artikel (IDEAL).** Der Pfad steht in Artikel-Liste, Bestandsübersicht und Lagerbestellung (über das Zahnrad
  einblendbar). Pflegen dürfen ihn Personen mit der Rolle „Fräsen-Pfad pflegen“.
- **Artikelsuche in der Lagerbestellung.** Bezeichnungen mit Sonderzeichen wie `&` werden jetzt richtig angezeigt.

## IT-Admin

- **Update mit dem Update-Skript.** Datenbank-Änderungen: SQL/168 (Kommissionierung) und SQL/169 (Teilmeldungen), beide automatisch. Neue
  Prüfungen und Hinweise stehen im Handbuch. WMS-Betrieb 03 – Installation, Update und Rückfall: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/680820744
- **Schalter, die aus bleiben, bis die Abnahme erfolgt ist:** `SageLagerplatzAnlageAktiv` und `SageLagerAnlageLagertechniken` (schreiben
  per SQL in Sage, vorher Rechte und Sage-Backup), `FraesenPfadAktiv` (IDEAL, erst nach dem Einmal-Skript Fräsen-Pfad).
  WMS-Betrieb 04 – Konfiguration: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/680787999 · WMS-Betrieb 05 – Schnittstellen zu Fremdsystemen: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/681181205
- **BDE am Handy und Terminals.** Jedes Konto, das bucht, braucht eine eigene Terminal-Zuordnung; seit 1.99.79 nimmt das WMS nur Buchungen
  vom eigenen Terminal an. Handy-Konten (lokales Konto, Formular-Anmeldung) bitte anlegen.
  WMS-Betrieb 09 – Sicherheit, Konten und Rechte: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/680722455
- **Neue Protokoll-Einträge:** „Kommissionier-Nachtrag“ und „Lagerplätze → Sage“ im Aktivitäts-Protokoll; Fehlerbilder und Prüfabfragen
  im Handbuch. WMS-Betrieb 06 – Logs und Überwachung: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/681050153 · WMS-Betrieb 07 – Troubleshooting: https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/680722435 ·
  WMS-Betrieb 08 – Diagnose-Abfragen (nur lesend): https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/681246721
- **Einstellung „Nur FA-Meldung“** (`BdeNurFaMeldung`) ist eine Altlast und sollte aus sein; das Update-Skript meldet es.

## Fragen und Fehler

Bitte Fehler und Wünsche über die Seite „WMS – Fehler melden und Wünsche einreichen“ (https://ideal-ake.atlassian.net/wiki/spaces/AKEDOKUMEN/pages/683933697/WMS+Fehler+melden+und+W+nsche+einreichen) melden – mit Bildschirmfoto,
Uhrzeit und Standort.

Danke und viele Grüße
[Absender]

---

## Belege (nicht in die Mail übernehmen)

- Standorte: Specs `standort:` – Nachtrag beide, Lagerplatz-Anlage beide, Isolierungsfräse/Fräsen-Pfad IDEAL, BDE-Masken beide; Manifest
  `scripts/deploy/nachlauf.psd1:667-711` (Firma je Eintrag; Teilfertig/Standardsicht/Summiert IDEAL).
- 10 Arbeitstage = Default `BdeTeilelisteLagerbestellungArbeitstage` (`IdealAkeWms/Models/AppSettingDefinitions.cs:237`), einstellbar 0–60.
- Terminal-Meldung `IdealAkeWms/Controllers/BdeTerminalController.cs:273-280`; „Altbestand geprüft“ `PickingLeitstandController.cs:571-586`.
- Rollen-Anzeigename „Fräsen-Pfad pflegen“ = Schlüssel `fraesenpfad_pflegen` (`IdealAkeWms/Views/Users/RoleOverview.cshtml:248-249`).
- Quellen je Punkt: Brain-Changelogs v1.99.70 bis v1.99.80 ([[2026-11-25-v1-99-80-doku-nachzug]]).
