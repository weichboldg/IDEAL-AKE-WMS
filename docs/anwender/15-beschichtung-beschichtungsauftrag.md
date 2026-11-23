# 15 Beschichtung – Vorschlagsliste und Beschichtungsauftrag

Stand: Version 1.99.56 · Standorte AKE und IDEAL · Bilder aus dem IDEAL-Testsystem

## Zweck

Teile, die beschichtet (lackiert, pulverbeschichtet) werden, gehen zu einem Lackierer. Das WMS zeigt in der **Vorschlagsliste**, welche Teile
eines Auftrags beschichtet werden müssen. Daraus legt man einen **Beschichtungsauftrag** an (Nummer wie *BA-2026-00007*). Der Auftrag wird
gedruckt und geht mit den Teilen zum Lackierer. Am Auftrag sieht man jederzeit, wo er steht: **Entwurf → Übergeben → Versendet → Erledigt**.

## Wer darf

| Was | Rolle (*Stammdaten → Rollen*) | Einstellung |
|---|---|---|
| Vorschlagsliste und Aufträge ansehen, drucken, PDF | „Beschichtungsauftrag“ (Schlüssel `beschichtungsauftrag`) | **Beschichtungsauftrag aktiv** |
| Zusätzlich Aufträge anlegen, bearbeiten, Status setzen, stornieren, Reklamation | „Beschichtungsaufträge (Schreibrecht)“ (Schlüssel `beschichtungsauftrag_schreiben`) | wie oben |
| Lackierer als „Beschichter“ kennzeichnen (*Stammdaten → Kreditoren*) | Stammdaten | – |

Administratoren dürfen alles. Ohne die Einstellung **Beschichtungsauftrag aktiv** fehlt das Menü **Beschichtung** für alle.

## Schritt für Schritt

### 1. Vorschlagsliste öffnen

Menü **Beschichtung → Vorschlagsliste**. Die Seite öffnet in der Ansicht **Summiert**: je Auftrag (HauptFA) eine Zeile pro Artikel mit
der Summe der Sollmenge und der Zahl der Positionen. Der Umschalter **Liste** zeigt jede Position einzeln.

![Vorschlagsliste Beschichtung, Ansicht Summiert](bilder/15-01-vorschlagsliste-summiert.jpg)

Im grauen Gruppenkopf stehen je Auftrag: Dienstleister, RAL, Start, Retour, AB-Nummer und Abteilung, rechts die schon angelegten
Aufträge (z. B. **Aufträge: BA-2026-00006**, als Link) und wie viele Positionen noch frei sind („(12 Positionen frei)“).

Den Auftrag findet man am schnellsten über den Spaltenfilter **HauptFA**.

### 2. Teile anhaken – Spalte „Verpackt“

In der letzten Spalte **Verpackt** die Teile anhaken, die jetzt zum Lackierer gehen.

- Ein Haken in der Ansicht *Summiert* nimmt **alle freien** Positionen dieses Artikels im Auftrag.
- „vergeben“ und kein Kästchen: Die Teile stecken schon in einem Beschichtungsauftrag.
- „2 von 3 frei“: Ein Teil ist vergeben, der Rest ist noch frei.
- Der Haken in der Vorschlagsliste wird **nicht gespeichert**. Neu laden setzt ihn zurück.

### 3. Teile ohne Fertigungsauftrag dazunehmen

Im Gruppenkopf **+ Position ohne FA** klicken. Es erscheint eine Zeile mit **Artikel suchen** (mindestens 3 Zeichen) und **Menge**.

![Position ohne FA: Artikel suchen und Menge eingeben](bilder/15-02-position-ohne-fa.jpg)

### 4. Auftrag anlegen

Im Gruppenkopf **Aus Vorschlagsliste erstellen** klicken. Der neue Auftrag öffnet sich als **Entwurf** – mit genau den angehakten
Positionen, alle als *verpackt* markiert. Was nicht angehakt war, bleibt in der Vorschlagsliste frei.

- Den Knopf sieht nur, wer Schreibrecht hat, und nur solange noch Positionen frei sind.
- Nichts angehakt: „Keine Position angehakt — nichts angelegt.“

### 5. Entwurf ausfüllen

Im Auftrag oben links den **Kopf** ausfüllen:

- **Lackierer (Kreditor)**: aus der Liste wählen. Dort stehen die Kreditoren mit dem Kennzeichen *Beschichter* („Kurzname — Name, Ort“).
  Der gewählte Kreditor ist später die **Anschrift auf Druck und PDF**. Fehlt der Lackierer in der Liste, über den Link
  **Beschichter pflegen** (Stammdatenrecht) nachtragen.
- Kein Kreditor? **— kein Kreditor, Freitext unten —** wählen und **Dienstleister (Freitext, nur ohne Kreditor)** ausfüllen.
- **RAL**, **Start Beschichtung**, **Retour** prüfen.
- Mengen, Haken **Verpackt** und **Entfernen** in der Positionstabelle anpassen, dann **Speichern**.
- Unter **Positionen hinzufügen** stehen die noch freien Positionen dieses Auftrags, darunter **Positionen ohne FA**.

Die **Bemerkung** rechts hat einen eigenen Knopf **Bemerkung speichern**. Ist sie geändert, aber nicht gespeichert, steht
„⚠ nicht gespeichert“ daneben. Drucken, PDF, Speichern und die Statusknöpfe fragen dann: „Bemerkung ist nicht gespeichert — trotzdem
fortfahren?“

Seit 1.99.56 **speichern die Statusknöpfe** (außer Stornieren) geänderte Mengen und Haken **mit**, bevor sie den Status setzen – ein
eigenes **Speichern** davor ist nicht mehr nötig. Lehnt der Status ab (z. B. noch nicht alles verpackt), ist trotzdem gespeichert:
Meldung „Gespeichert.“ plus gelbe Warnung. **Drucken** und **PDF** fragen bei ungespeicherten Positionen nach, weil der Druck den
gespeicherten Stand zeigt.

![Entwurf mit Statusknöpfen; „Auf Übergeben setzen“ speichert die Positionen, die Warnung nennt den fehlenden Dienstleister](bilder/15-07-entwurf-status-knoepfe.jpg)

### 6. Übergeben, drucken, versenden, erledigen

Oben im Auftrag zeigt der **Statusverlauf** jeden Schritt mit Zeit und Benutzer. Darunter steht der Knopf für den nächsten Schritt.

![Auftrag im Status Übergeben: Statusverlauf, Kopf, Positionen](bilder/15-04-auftrag-uebergeben.jpg)

1. **Auf „Übergeben“ setzen** – geht nur mit Dienstleister (Kreditor oder Freitext). Sonst: „Dienstleister fehlt — zum Übergeben
   erforderlich.“ Ab jetzt ist der Kopf fest; Positionen (Menge, Haken, Entfernen, Hinzufügen) lassen sich noch ändern.
2. **Drucken** oder **PDF** – das Blatt geht mit den Teilen zum Lackierer (siehe Abschnitt 7).
3. **Auf „Versendet“ setzen** – geht nur, wenn **alle** Positionen verpackt sind. Sonst nennt die Meldung die offenen Positionen
   („Versendet nur, wenn alle Positionen verpackt sind — nicht verpackt (2): …“). Haken setzen, **Speichern**, erneut versenden.
   Ab jetzt sind nur noch Status und Bemerkung änderbar.
4. Sind die Teile zurück: **Auf „Erledigt“ setzen**.

![Erledigter Auftrag mit vollständigem Statusverlauf und Reklamation](bilder/15-05-auftrag-erledigt-statusverlauf.jpg)

**Stornieren** geht nur im Entwurf. Die Positionen sind danach in der Vorschlagsliste wieder frei.

**Reklamation:** An einem erledigten Auftrag **Reklamation** klicken und die betroffenen Teile wählen. Es entsteht ein neuer Entwurf mit
denselben Kopfdaten. Der alte Auftrag zeigt „Reklamiert → BA-…“.

### 7. Druck und PDF

Drucken und PDF gehen ab **Übergeben**. Das Blatt enthält:

- oben den Briefkopf, darunter den **Empfänger** als Brief-Anschrift aus dem gewählten Kreditor (beim Drucken frisch gelesen); ohne Kreditor
  den Dienstleister-Text,
- Auftragsnummer als Barcode rechts neben dem Titel, HauptFA, RAL, Start und Retour, die Bemerkung,
- **eine Zeile je Position** mit laufender Nummer *Pos.*, Menge ohne unnötige Nachkommastellen (*5* statt *5,000*) und als letzte Spalte
  **Verpackt** (☑/☐),
- unten rechts „Seite x von y“.

Spalten, die man über das Zahnrad ausblendet, fehlen auch im Druck.

![Druckansicht eines Beschichtungsauftrags](bilder/15-06-druck.jpg)

Der gelbe Kasten „Briefkopf: …“ im Bild ist ein Hinweis aus dem Testsystem (Vorlage fehlt dort) und erscheint nur am Bildschirm.

### 8. Auftragsliste

Menü **Beschichtung → Aufträge** zeigt alle Beschichtungsaufträge mit Status-Filter (Alle, Entwurf, Übergeben, Versendet, Erledigt,
Storniert) und der Spalte **Versendet am**. **Zur Vorschlagsliste** führt zurück.

![Liste der Beschichtungsaufträge](bilder/15-03-auftraege-liste.jpg)

## Hinweise und Stolperfallen

- **Ältere Aufträge ohne Kreditor:** Ist ein Auftrag schon *Übergeben*, hat aber keinen Kreditor, steht im Kopf
  **Lackierer (Kreditor) nachtragen** („Für Anschrift auf Druck und PDF. Danach nicht mehr änderbar.“). Einmal wählen und speichern.
- **Lackierer-Liste leer:** Kein aktiver Kreditor ist als *Beschichter* gekennzeichnet – über **Beschichter pflegen** nachtragen lassen.
- **Gelber Hinweis „Anschrift: …“ im Druck:** Kein Kreditor gewählt, oder am Kreditor fehlen Straße, PLZ oder Ort. Der Hinweis steht nur
  am Bildschirm.
- **„Im Entwurf nicht druckbar — erst übergeben.“** – zuerst auf *Übergeben* setzen.
- **Übergeben → Erledigt direkt geht nicht mehr** (seit Version 1.99.41). Erlaubt ist nur Entwurf → Übergeben → Versendet → Erledigt
  sowie Entwurf → Storniert.
- **„… wurde inzwischen von jemand anderem geändert — bitte neu laden“:** Nichts wurde gespeichert. Seite neu laden und wiederholen.
- **Positionen ohne FA** zählen nie als „vergeben“ – derselbe Artikel darf in mehreren Aufträgen stehen.
- **„in Sage beendet“ / „inaktiv“ / „ungeklärt“** an einer Zeile (seit Version 1.99.37): Die Position ist in Sage nicht mehr aktiv. Bitte
  mit der Arbeitsvorbereitung klären.
- **Kein PDF-Knopf:** Auf dem Server fehlt die PDF-Erzeugung – den Bildschirmdruck nutzen.

## Häufige Fragen

**Ich habe einen Teil vergessen. Muss ich einen neuen Auftrag anlegen?** Nein, solange der Auftrag *Entwurf* oder *Übergeben* ist: unten
unter **Positionen hinzufügen** anhaken und **Speichern**.

**Warum lässt sich der Auftrag nicht auf „Versendet“ setzen?** Mindestens eine Position hat keinen Haken *Verpackt*. Die Meldung nennt sie.

**Welche Anschrift steht auf dem Blatt?** Die des gewählten Kreditors, so wie sie beim Drucken in den Stammdaten steht.

**Kann ich einen erledigten Auftrag wieder öffnen?** Nein. Für Nacharbeit gibt es die **Reklamation**.

## Unterschiede AKE und IDEAL

| Thema | AKE | IDEAL |
|---|---|---|
| Woher kommen die Positionen? | Aus der **Stückliste des WA**: alle Zeilen der Artikelkategorie *Lackierteil*, für jeden offenen WA mit Beschichtungskennzeichen. HauptFA = WA-Nummer. | Aus der **FA-Struktur**: alle Positionen mit *Beschichtet*, alle Ebenen. |
| Kommissionierte Aufträge | ausgeblendet; Schalter **auch kommissionierte zeigen** holt sie zurück | – |
| Maße (Breite, Höhe, Tiefe) | aus dem Sage-Artikel (mm) | aus der FA-Struktur; bei **Position ohne FA** steht 0 |
| Lackierer vorbelegt | nein | ja, wenn der Dienstleister aus Sage genau dem Kurznamen eines aktiven Beschichters entspricht; sonst Hinweis „Kein Beschichter-Kreditor zu „…“ — Kreditor wählen oder Kurzname pflegen.“ |
| Kopf beim Anlegen | Start = berechneter Beschichtungstermin des WA; Dienstleister, RAL, Retour leer | Dienstleister, RAL, Start, Retour aus Sage |

Ablauf, Status, Druck und Rechte sind an beiden Standorten gleich.
