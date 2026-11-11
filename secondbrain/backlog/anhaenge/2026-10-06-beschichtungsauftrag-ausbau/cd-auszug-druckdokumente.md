---
typ: anhang
quelle: "CD-Manual IDEAL-AKE, Stand 01/23 (cd_manual_2022.md im claude.ai-Projekt „IDEAL-AKE WMS“, von Gerald bereitgestellt 2026-10-06)"
bezug: "[[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]]"
---
# Corporate Design – Auszug für Druckdokumente (Beschichtungsauftrag)

Auszug aus dem CD-Manual (Stand 01/23), nur was für ein A4-Druckdokument mit Briefkopf zählt.
Die Werte sind wörtlich übernommen; Auslegung für den Druck steht getrennt unter „Hinweise“.

## Farben

| Name | HEX | RGB | Verwendung laut Manual |
|---|---|---|---|
| IDEAL Blau | `#053153` | 5 / 49 / 83 | Hausfarbe IDEAL, Überschrift 1, Firmenbezeichnung/Icons in IDEAL-Adressen |
| AKE Blau | `#43a6e2` | 67 / 166 / 226 | Hausfarbe AKE, Überschrift 2, Pfeile/Markierungen |
| Grau 85 | `#4a494a` | 74 / 74 / 74 | Lauftext |
| Icon Blau | `#a1d9f7` | 161 / 218 / 248 | Icons (neutral) |
| Icon Grün | `#82bf6e` | 131 / 192 / 111 | Icons (ökologische Themen) |
| Orange | `#ef7d00` | 239 / 125 / 0 | (im Manual ohne Verwendungsangabe) |

## Typografie

- Überschrift 1: Nimbus Sans Novus Semi Bold, Großbuchstaben, Laufweite 20, IDEAL Blau.
- Überschrift 2: Nimbus Sans Novus Semi Bold, AKE Blau.
- Lauftext: Nimbus Sans Novus Regular, Grau 85.

## Satzspiegel DIN A4 Hochformat

- 12er-Raster; a = 17,5 mm, b = 24,75 mm.
- Ränder: oben 37,125 mm (Logozone = 1,5 b, Sperrzone für andere Elemente), unten 24,75 mm,
  links 17,5 mm, rechts 17,5 mm.
- Logo: Höhe = a = 17,5 mm, vertikal mittig in der Logozone.
- Helle Seiten: Balken unten (Visitenkarten/Folder/Inserate: Balkenhöhe 3 mm, Winkel 45°).

## Adressblock (Manual, Abschnitt „Adressen“)

- Firmenbezeichnung und Icons in der Hausfarbe (IDEAL → IDEAL Blau, AKE → AKE Blau).
- Nur eine Adresse: gemeinsam mit den Kontaktdetails (z. B. Briefpapier).
- Beispiel im Manual:
  - IDEAL Kältetechnik GmbH · In der Schörihub 28, 4810 Gmunden · Österreich ·
    +43 7612 660 61 · office@ideal-ake.at · www.ideal-ake.at
  - AKE Ausseer Kälte- und Edelstahltechnik GmbH · Pichl 66, 8984 Bad Mitterndorf · Österreich ·
    +43 3624 211 00 · office@ake.at · www.ideal-ake.at

## Logos (Anhang, gleicher Ordner)

- `IDEAL_AKE_Logo_rgb.jpg` – 2379 × 945 px, 300 dpi (Druck).
- `IDEAL_AKE_Logo_rgb_klein.jpg` – 854 × 339 px, 300 dpi.
- Gemeinsames Logo IDEAL/AKE (Schriftzug IDEAL in IDEAL Blau, AKE in AKE Blau).

## Hinweise für die Umsetzung (Koordination, ungeprüft)

- **Abweichung Adresse:** Die Standorteinstellungen am Testsystem IDEAL enthalten
  „IDEAL Kältetechnik“ / „In der Schörihub 38, 4840 Gmunden“ (Screenshot Gerald 2026-10-06).
  Das Manual nennt „IDEAL Kältetechnik GmbH“ / „In der Schörihub **28**, **4810** Gmunden“.
  Gerald 2026-10-06: Tippfehler in den Einstellungen, das Manual stimmt. Der Briefkopf druckt, was in den
  Einstellungen steht – dort korrigieren.
- **Schrift:** Nimbus Sans Novus ist eine lizenzpflichtige Schrift; ob sie auf dem Server
  (Headless Edge) installiert werden darf, ist offen. Ersatz bis dahin: eine serifenlose Systemschrift.
- **Kontaktdaten** (Telefon, E-Mail, Web) gibt es in den Standorteinstellungen heute nicht.
- Konsistenz vor Eigenständigkeit (CLAUDE.md): Briefkopf ja, aber kein Umbau der übrigen Druckansicht.
