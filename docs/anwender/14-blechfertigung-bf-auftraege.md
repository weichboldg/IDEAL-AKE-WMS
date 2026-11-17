# 14 Blechfertigung – BF-Aufträge (IDEAL)

## Zweck

Mit BF-Aufträgen bestellt die Produktion Blechteile in der eigenen Blechfertigung. Der Schachtler prüft die Bestellung, gibt sie frei,
und das WMS legt sie als Datei in den WiCAM-Auftragspool. WiCAM macht daraus einen Auftrag. Wenn die Teile gefertigt sind, meldet der
Schachtler den Auftrag fertig.

In dieser ersten Stufe schreibt das WMS **nichts an Sage** und bucht **nichts ein**. Die Teile werden wie bisher eingebucht.

## Wer darf

| Was | Rolle (*Stammdaten → Rollen*) | Einstellung |
|---|---|---|
| Warenkorb füllen, abschicken, eigenen Auftrag bis zur Freigabe stornieren | Lagerbestellung (wer Lagerbestellungen anlegen darf) **oder** `schachtler` | *Blechfertigung aktiv* an |
| Alle Aufträge sehen, Menge und Lagerplatz ändern, Positionen streichen, freigeben, fertig melden, bis zur Fertigmeldung stornieren, „CSV erneut schreiben“ | `schachtler` „Schachtler (Blechfertigung)“ | wie oben |

Ohne *Blechfertigung aktiv* fehlt das Menü, auch für Administratoren. Abschicken geht erst, wenn unter *Einstellungen → Blechfertigung* eine
**Empfängergruppe BF-Aufträge** gewählt ist.

## Schritt für Schritt

### 1. Bestellen (Besteller)

1. Menü **Blechfertigung → BF-Warenkorb**.
2. Im Suchfeld **Artikelnummer, Matchcode oder Bezeichnung** einen Begriff eingeben, **Suchen**. Gefunden werden nur Sage-Lagerartikel mit
   Artikeltyp **BG** (Baugruppe) oder **FT** (Fertigteil).
3. In der Trefferzeile **Menge** eingeben, wenn nötig einen **Status** (z. B. *EILT*) und einen anderen **Hauptlagerplatz** wählen,
   **Hinzufügen**.
   - Hat der Artikel in Sage eine **Bestellmenge** und dieselbe Einheit, muss die Menge ein ganzes Vielfaches davon sein
     (Bestellmenge 5: 5, 10, 15 …). Bestellmenge 0 oder leer = frei.
   - Fehlt die **Materialnummer** (Sage-Feld *Materialnummer PN*), erscheint ein Hinweis: Die Position wird **nicht an WiCAM übergeben**.
     Bitte die Materialnummer in Sage nachtragen lassen.
4. Positionen können bis zum Abschicken geändert oder entfernt werden.
5. **Abschicken**. Der Auftrag bekommt eine Nummer wie **BF00000042**, und die Empfängergruppe erhält eine Mail.

### 2. Prüfen und freigeben (Schachtler)

1. Menü **Blechfertigung → BF-Aufträge**, Status *bestellt* filtern, Auftrag öffnen.
2. Menge oder Hauptlagerplatz ändern bzw. Positionen **Streichen** (gestrichene bleiben sichtbar, gehen aber nicht an WiCAM).
3. Steht über dem Knopf **Freigabe noch nicht möglich**, nennt die Liste Position und Grund, z. B. „Pos 3: Menge muss ein ganzes Vielfaches
   der Bestellmenge 5 sein“ oder „Pos 2 Matchcode: Semikolon“. Erst korrigieren (bzw. in Sage pflegen), dann freigeben.
4. **Freigeben**. Das WMS rechnet ab heute:
   - **Schachteltermin** = heute + 1 Arbeitstag,
   - **FE-Termin** = heute + 10 Arbeitstage,
   jeweils ohne Wochenenden und Feiertage. Die **Bemerkung** jeder Position wird festgeschrieben:
   `[Status - ][Fertigungsinfo PN - ]TT.MM[ - Position]` (die Position nur bei Baugruppen), z. B. `EILT - Laser - 17.11 - 4`.
5. Die Meldung nennt Positionen ohne Materialnummer, die nicht übergeben werden.

### 3. Datei an WiCAM

Der Dienst schreibt die Datei **BF00000042.csv** innerhalb weniger Minuten in den WiCAM-Pool. In der Liste zeigt die Spalte **CSV**:
*offen*, *geschrieben* oder *Fehler* (Mauszeiger auf die Zelle zeigt den Fehlertext).

Bei **„Datei existiert bereits im Pool – nicht überschrieben.“** wiederholt das WMS nicht von selbst. In WiCAM prüfen, ob der Auftrag
schon angelegt ist; wenn nicht, die Datei im Pool löschen und im Auftrag **CSV erneut schreiben** drücken.

### 4. Fertig melden (Schachtler)

- In der Liste die Aufträge anhaken (nur freigegebene mit geschriebener CSV haben ein Kästchen) und **Auswahl fertig melden**, oder
- im Auftrag **Fertig melden**.

Das setzt nur den Status. Es wird nichts eingebucht.

### 5. Stornieren

Im Auftrag unten **Auftrag stornieren** aufklappen, optional einen Grund eingeben, **Stornieren**.

- Der Besteller kann seinen Auftrag stornieren, solange er *bestellt* ist.
- Der Schachtler kann bis zur Fertigmeldung stornieren. Ist der Auftrag schon freigegeben, liegt die Datei ggf. im WiCAM-Pool. Die Meldung
  sagt dann „Die CSV liegt bereits im Pool … – den Auftrag in WiCAM löschen.“ Das WMS löscht nichts in WiCAM.

Den Warenkorb storniert man nicht – Positionen einfach entfernen.

## Hinweise und Stolperfallen

- **Platzhalter-Format:** Das Dateiformat entspricht der Vorlage aus der Spezifikation. Vor dem ersten echten Einsatz wird es mit einer
  Musterdatei aus dem WiCAM-Pool abgeglichen.
- Zeichen wie `;` oder `"` in Matchcode, Lagerplatz oder Fertigungsinfo sperren die Freigabe, weil WiCAM die Datei sonst falsch liest.
- Der Bezugstag der Termine ist der **Freigabetag**, nicht der Bestelltag.
- Feiertage kommen aus *Stammdaten → Feiertage*. Fehlen sie für das Jahr, rechnen die Termine nur ohne Wochenenden.

## Häufige Fragen

**Warum finde ich meinen Artikel nicht?** Nur Sage-Lagerartikel mit Artikeltyp BG oder FT werden angeboten.

**Warum bekommt der Besteller keine Mail bei Freigabe?** Gewollt: Mail gibt es nur beim Abschicken an die Empfängergruppe.

**Kann ich eine Liste exportieren?** Ja, über das Zahnrad bzw. den CSV-Knopf über der Tabelle (Liste und Positionen im Auftrag).
