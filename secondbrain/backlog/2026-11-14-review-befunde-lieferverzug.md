---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: neu
modul: Lieferverzug-Mail (LieferverzugPlaner, LieferverzugMailService, LieferverzugMailHtml, MailService; v1.99.10/v1.99.19)
stand: "Bündel v1.99.22, 90bbbbd7, feature/2026-08-07-ideal-teile-1-5"
---
# Review-Befunde Lieferverzug-Mail (Teil A2, lesend)

Specs: [[2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec]], [[2026-11-13-lieferverzug-nachbesserung-keyuser-spec]]. Pfade relativ zum
Worktree, Datei `IDEALAKEWMSService/Services/LieferverzugMailService.cs`, sofern nicht anders genannt.

## LV-1 · mittel · Die Tagessperre hängt an jeder Protokollzeile: Ein Fehler der Sammelliste wird am selben Tag nicht nachgeholt

- **Fundstelle:** `:278` (einmal am Tag = irgendeine `PurchaseOrderReminders`-Zeile von heute), `:349` (Protokoll sofort nach jeder Kreditor-Mail),
  `:361-383` (Sammelliste: Fehler nur als Warnung), `:386` (`FinishSuccessAsync`, auch wenn `fehler > 0`).
- **Befund:** Die Sperre „heute schon gelaufen“ greift, sobald eine einzige Kreditor-Mail protokolliert ist, auch wenn die Sammelliste an den
  Einkauf danach scheitert. Das gilt für einen SMTP-Fehler (Exception) ebenso wie für das stille Überspringen ohne Host. Dasselbe passiert, wenn
  der Lauf nach der ersten Kreditor-Mail abbricht (Dienst-Stopp, Abbruch): Die restlichen Kreditor-Mails und die Sammelliste fehlen bis morgen.
  Der Lauf endet trotzdem als „Erfolg“, mit Count `fehler`/`nicht_gesendet` und einer Warnung. Eine Fehlermail gibt es nicht.
- **Szenario:** Echtbetrieb, 5 Kreditor-Mails gehen raus, beim Senden der Sammelliste ist der SMTP-Relay kurz weg. Der Einkauf erhält an diesem
  Tag keine Liste und erfährt nicht, dass die Lieferanten angeschrieben wurden. Das sieht nur, wer ins Aktivitätsprotokoll schaut.
- **Vorschlag:** Die Tagessperre je Art prüfen (Sammelliste: `Art = Einkauf` heute vorhanden, Kreditor-Mails: Wiederholregel je Position, besteht
  schon). Den Lauf bei `fehler > 0` als Warnung/Fehler beenden und `_errorNotifier` benachrichtigen. Test: Sammelliste wirft → nächster Zyklus
  sendet nur die Sammelliste, keine Kreditor-Mail doppelt.
- **DEV-Nachmessung 2026-11-14:** nicht reproduzierbar ohne Eingriff (SMTP-Fehler nur beim Senden der Sammelliste lässt sich auf DEV mit
  PickupDirectory nicht erzeugen). Beobachtet, was den Befund stützt: Nach Löschen der heutigen Zeilen schrieb der Lauf 20:42 erst 999 Zeilen
  `Art 1` (Kreditor, Testbetrieb) und 1119 Zeilen `Art 0` (Sammelliste) – 91 `.eml`. Der Folgezyklus 21:02 startete gar keinen Lauf
  „LieferverzugMail“ (keine Protokollzeile, keine neue Datei) – die Sperre hängt also an „irgendeine Zeile von heute“, unabhängig von der Art.
  Bericht [[02-wareneingang-einkauf]].

## LV-2 · niedrig · Mail gesendet, Protokoll scheitert: Die Kreditor-Mail geht im nächsten Zyklus erneut raus

- **Fundstelle:** `:339-358`. Der `try` umfasst `SendMitOptionenAsync` **und** `ProtokollAsync` (`:349`, `:416-428`).
- **Befund:** Wirft `SaveChangesAsync` im Protokoll, nachdem die Mail schon gesendet ist, zählt der `catch` das als „Fehler beim Versand“ und
  schreibt keine Zeile. Gibt es heute sonst keine Zeile, läuft der ganze Lauf im nächsten SyncWorker-Zyklus (Default 15 min) erneut, und die
  externe Mail geht ein zweites Mal raus. Die Statuszeile sagt außerdem fälschlich „Fehler beim Versand“.
- **Szenario:** DB kurz nicht erreichbar oder voll, während die Mails laufen → Lieferanten bekommen dieselbe Erinnerung mehrfach.
- **Vorschlag:** Protokollfehler getrennt fangen, als „gesendet, Protokoll fehlgeschlagen“ melden und den Lauf abbrechen, statt weitere Mails
  zu senden. Benachrichtigung über `_errorNotifier`.

## LV-3 · niedrig · Grenze überschritten ohne Protokollzeile: Fehlermail in jedem Zyklus

- **Fundstelle:** `:309`, `:319-325`.
- **Befund:** Ist `KreditorMaxJeLauf` überschritten und entsteht keine Protokollzeile, wiederholt sich der ganze Lauf in jedem Zyklus samt
  `_errorNotifier.NotifyAsync`. Das tritt ein, wenn der Einkauf aus ist, die Empfängerliste leer ist oder die Sammelliste scheitert. Bei
  15-Minuten-Takt sind das bis zu ~68 Fehlermails pro Tag ab der eingestellten Uhrzeit. Die Spec (AK 8) akzeptiert nur die wiederholte
  **Warnung** bei leerer Empfängerliste, nicht die Fehlermail.
- **Vorschlag:** Die Fehlermail höchstens einmal je Tag senden, z. B. mit einem Marker im Protokoll oder der Tagessperre aus LV-1 je Art.
- **DEV-Nachmessung 2026-11-14:** auf DEV nicht beobachtbar: `Lieferverzug:Testbetrieb = true`, die Grenze `KreditorMaxJeLauf` (30) gilt nur im
  Echtbetrieb; Lauf 20:42 mit 90 Testbetrieb-Mails ohne Grenzen-Warnung (`ausgesetzt=0`). Echtbetrieb mit Test-Postfächern wäre nötig.
  Bericht [[02-wareneingang-einkauf]].

## Geprüft ohne Befund

- **Kein Sage-Schreibweg:** Der Lauf liest nur den Cache `SagePurchaseOrderLines`.
- **Mail-Schutz:** Gate `Lieferverzug:MailAktiv` (Default aus), `KreditorAktiv` Default aus, `Testbetrieb` Default an. Im Testbetrieb gehen
  Kreditor-Mails nur an `EinkaufEmpfaenger`, die externe Adresse steht nur im Text. Die Grenze je Lauf gilt nur für echte Mails. Ein Stand älter
  als 24 h setzt die Kreditor-Mails aus. Adressen werden mit `MailAdressen.IstGueltig` vor dem Versand geprüft. DEV legt über `PickupDirectory`
  `.eml` ab (`MailService.cs`), `false` bei stillem Überspringen wird nicht protokolliert.
- HTML: alle Werte über `HtmlEncode` (`LieferverzugMailHtml.cs`), multipart mit Text-Teil.
- Rest-Regel: dieselbe Funktion wie in der Maske (`ErfasstJeBelPos.Rest`). Wiederholung je (BelPosId, Liefertermin) und Modus. Schwellen-Maximum
  (F1), Altersgrenze (F2), Rahmenabruf-Schalter (F3) wie in der Spec. Zeitfenster Mo–Fr. Lokalzeit.
- Einstellungen: alle 15 `Lieferverzug:*` im Katalog `ServiceSettingDefinitions`, Grenzen beim Lesen geklemmt. `ISyncLogger` als letzter
  Parameter, deutsche Counts-Keys. Migration 145 und das Protokoll ohne Audit sind bewusst so (Spec F7). Tests: `LieferverzugMailTests`.
- Bekannt, nicht neu: Q5 der Nachbesserungs-Spec (Einleitungssatz bei `KreditorAktiv = false`, `LieferverzugMailHtml.cs:98`).
- Tippfehler: keine gefunden.
