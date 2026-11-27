---
typ: feature
status: offen
created: 2026-11-13
quelle: "Benjamin Andl-Braun, über Gerald 2026-11-13 13:57 (Chat Koordination)"
standort: IDEAL (Werkbank-Option, Default aus; AKE unverändert)
epic: true
---
# BDE „vereinfacht“: Teileliste nach Matchcode für die Blechfertigung, Fertigmeldung einzeln oder als Block

**Gerald:** Alles muss wie bisher konfigurierbar sein.

## Anforderung (Wortlaut sinngemäß, Benjamin Andl-Braun)
- **Einstellung je Terminal bzw. Werkbank**, ob die „vereinfachte“ BDE aktiv ist.
- Heute gruppiert das BDE nach **HauptFA**. Das passt für Bereiche, in denen das Gerät zusammengebaut wird. In der
  **Blechfertigung** werden aber **Einzelteile** bearbeitet und rückgemeldet → es braucht eine Sicht, die die **Matchcodes** auflistet.
- Damit die Werker das abarbeiten können, wie in den **Vormontage-Listen**:
  - eine **Wochenvorauswahl**,
  - eine **Filterleiste**,
  - eine Funktion, die **summiert**,
  - **einblendbare Inhalte** wie dort, inklusive **„Abteilung“** und **„Beschichtet“**.
- Die so zurechtgefilterten **Arbeitsblöcke** sollen **einzeln oder als Block fertiggemeldet** werden können.
- Die **Filterauswahl bleibt gespeichert**, damit der Werker sie am nächsten Tag nicht neu setzen muss.

## Belege im Code (Bündel-Worktree `2026-08-07-ideal-teile-1-5`, Stand v1.99.15, nur gelesen – die Spec prüft neu)
- **Vorbild Vormontage-Listen:** `IdealAkeWms/Controllers/FaHierarchyVormontageController.cs` – Sicht 1 `Index` (gruppiert nach HauptFA)
  und Sicht 2 `Summiert` (nach Matchcode aggregiert), Wochenwahl `kwVon`/`kwBis` über `IsoWeekRange.Resolve` auf `FE_Termin`,
  Server-Spaltenfilter `ColumnFilterHelper.ReadFromQuery`, Reiter je `VMBedarf`. Gate `RequireFaHierarchyVormontageAktiv` + `RequireVorbauAccess`.
- **Spalten der Vormontage** (`Models/ViewModels/ColumnDefinitions.cs`, `FaHierarchyVormontageEinzeln`/`…Summiert`): Abteilung
  (`montage-abteilung`, v1.99.3) ist da, **„Beschichtet“ fehlt dort**. „Beschichtet“ gibt es als Spalte in
  `FaHierarchyKommissionierListen` (DefaultHidden) und `FaHierarchyBeschichtung` – Datenquelle laut Spec prüfen, nicht aus dem Namen ableiten.
- **Werkbank-Optionen heute** (`Models/ProductionWorkplace.cs`): `BdeAktiv`, `BdeNurAgFertigmeldung` (v1.63.0, Fertigmeldung ohne
  Start/Stop, Buchung Start = Ende), `ArbeitsschrittCode` (Kürzel-Familie). Globale Schalter u. a. `BdeNurFaMeldung`, `BdeTerminalMaxHauptFa` (v1.64.0).
- **Terminal heute:** gruppiert nach Haupt-FA, Prio/Termin ([[2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-spec]], v1.64.0);
  offene AGs über die Kürzel-Familie ([[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec]], v1.63.0).
- **Kernbefund – Buchungsziel je Einzelteil fehlt:** Arbeitsgänge (`WorkOperation`) gibt es nur je Sub-FA. Kürzel von **Blatt-Kindern**
  (`SubFA == 0`, typisch Blechteile mit `LS KA`) zählen beim **Vater-Sub-FA** ([[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]],
  AK 13 und TS-77.18: „Korpus `PG LS KA`“). Ein einzelnes Blechteil (Matchcode) hat also heute **keinen eigenen Arbeitsgang**, gegen den
  man fertigmelden könnte. Eine Matchcode-Liste ist leicht zu bauen, die Rückmeldung je Teil braucht eine Entscheidung zum Datenmodell.
- **Gespeicherte Anzeige je Benutzer:** `Models/UserViewPreference.cs` (`UserId`, `ViewKey`, `SettingsJson`) trägt heute die
  Spaltenauswahl. **Ungeprüft:** ob dort oder anderswo schon Filterwerte gespeichert werden – Spec prüft (`wwwroot/js/column-preferences.js`, `table-filter.js`).

## Empfehlungen der Koordination (für die Spec)
1. **Eine Werkbank-Option** „BDE-Teileliste (vereinfacht)“, Default aus, in Stammdaten › Werkbänke neben `BdeNurAgFertigmeldung`.
   Wirkung: Das Terminal dieser Werkbank zeigt statt der HauptFA-Gruppen die Teileliste. Die Teileliste meldet **nur fertig** (kein
   Start/Stop) – das ist dieselbe Semantik wie `BdeNurAgFertigmeldung` und wird wiederverwendet, nicht neu erfunden.
2. **Datenbasis der Teileliste:** Blattpositionen der FA-Struktur (`FaHierarchyNode`, `SubFA = 0`), deren Arbeitsschritte ein Kürzel der
   Kürzel-Familie der Terminal-Werkbank enthalten; je Zeile Matchcode, Menge, HauptFA, Sub-FA, Termin. Muster und Bausteine der
   Vormontage wiederverwenden (`VormontageService`, `IsoWeekRange`, Spaltenfilter, Zahnrad, Summiert-Aggregation) statt neu zu bauen.
3. **Buchungsziel (Kern-Entscheidung, Variante in der Spec):**
   - **A (Empfehlung): eigene, schlanke Teil-Rückmeldung** je Strukturposition + Kürzel (wer, wann, Werkbank, Menge) mit **stabilem
     fachlichen Schlüssel** (HauptFA/Sub-FA/Position, nicht die Node-Id – Spec prüft, ob `FaHierarchyNode` bei jedem Sync neu angelegt wird).
     Der Vater-Sub-FA-AG bleibt unverändert; optional wird er fertig, wenn alle seine Teile mit diesem Kürzel gemeldet sind (Rückfrage 3).
   - **B:** `WorkOperation` auch für Blatt-Kinder anlegen. Greift in die Struktur-Erkennung, ins Terminal und in alle AG-Zählungen ein
     (AK 13 der v1.47-Spec) – größerer Umbau, nur wenn die Teile auch in Auswertungen als echte Arbeitsgänge zählen sollen.
4. **Block-Fertigmeldung:** über die aktuell gefilterte Auswahl (Checkboxen + „alle sichtbaren“), mit Bestätigungsdialog, der Anzahl Zeilen
   und Summe nennt; serverseitig je Zeile Familien-Prüfung (wie FA-a9) und Schutz gegen Doppelmeldung. Summiert-Zeile meldet alle
   beitragenden Positionen.
5. **Filter speichern** je Benutzer **und** Werkbank (ein Terminal-PC kann mehrere Werkbänke bedienen) im vorhandenen
   `UserViewPreference.SettingsJson` bzw. einem eigenen ViewKey, inklusive Wochenauswahl als **relative** Angabe („aktuelle KW + n“),
   damit sie am Folgetag/in der Folgewoche sinnvoll bleibt. Knopf „Filter zurücksetzen“.
6. **Konfigurierbarkeit:** Werkbank-Option (Stammdaten), Spaltenauswahl per Zahnrad, Default der Wochenauswahl als AppSetting nach
   ADR 0020 (Auswahlliste, Wirkung, Standort). Keine fest verdrahteten Kürzel (`LS`, `KA` …) – die Kürzel-Familie der Werkbank entscheidet.

## Rückfragen (an Benjamin, über Gerald – vor der Freigabe klären)
1. **„Vereinfacht“ = nur Fertigmeldung** ohne Start/Stop (wer, wann, wo, Menge)? Oder braucht die Blechfertigung auch Zeiten?
2. **Was ist eine Zeile:** jedes Blechteil (Blattposition, Matchcode) einer Sub-FA – oder auch Sub-FAs, die selbst Blechteile sind?
3. **Wirkung der Teil-Meldung:** Soll der Arbeitsgang der Vater-Sub-FA (z. B. `KA` am Korpus) automatisch fertig werden, wenn alle
   seine Teile gemeldet sind? Oder bleibt das getrennt?
4. **Menge:** immer die volle Sollmenge, oder Teilmengen/Ausschuss je Zeile? Beim Block: immer volle Menge?
5. **„Wochenvorauswahl“** auf welchem Termin – FE-Termin (wie Vormontage) oder ein eigener Blech-/AG-Termin?
6. **„Beschichtet“:** dasselbe Feld wie in Kommissionierliste/Beschichtung (Ja/Nein bzw. RAL)? Weitere gewünschte Spalten?
7. **Filter speichern:** je Werker (persönliche Anmeldung am Terminal) oder je Terminal/Werkbank (Sammelanmeldung)? Wie melden sich die
   Werker in der Blechfertigung heute an?
8. **Welche Werkbänke** (Kürzel) betrifft es zum Start – z. B. Laser `LS`, Kanten `KA`, Entgraten `EG`?
9. **Rücknahme:** Darf ein Werker eine Fertigmeldung zurücknehmen (z. B. Block falsch gemeldet), oder nur ein BDE-Admin?

## Zusammenhang
- Verwandt: [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv]] (BF-Aufträge aus dem Warenkorb, Fertigmeldung nur am Schluss) – andere
  Datenquelle (Baugruppe ohne Teile), aber dieselbe Abteilung. In der Spec abgrenzen, nicht verschmelzen.
- Lastenheft BDE: [[2026-10-19-bde-lastenheft-abgleich]] (Nr. 12 Rückmelden).

## Antworten Fachbereich (Mail Benjamin Andl-Braun 2026-11-16, Antworten von Walter Kronberger, Andi Peric, Hannes Kronberger; über Gerald 14:23)
Frage 1 („vereinfacht“ = nur Fertigmeldung ohne Zeiten) war vorab geklärt; die Mail vom 13.11. stellte die übrigen 8 Fragen.
1. **Zeile:** Blechteil **und/oder Baugruppe** – jeder Artikel mit dem Arbeitsgang wird angezeigt. **Unteraufträge aufklappbar**, damit der
   Werker sieht, aus welchen Teilen die Baugruppe besteht. Haben Baugruppe und enthaltenes Teil denselben Arbeitsschritt, erscheinen
   **beide als einzelne Zeilen** (Hinweis auf Mehraufwand/Platzwechsel) – „Aspekt, den man auch später noch implementieren kann“.
2. **Keine Rückmeldung nach oben.** „Die Arbeitsgänge sind mittlerweile korrekt jeweils am richtigen Teil angeschrieben.“
3. **Menge:** passt (Vorbelegung volle Sollmenge, je Zeile änderbar, Ausschuss optional; Block = volle Sollmenge je Zeile).
4. **Wochenauswahl:** FE-Termin.
5. **Spalten:** passt (Beschichtet = Feld wie Kommissionier-/Beschichtungsliste; Standard sichtbar Matchcode, Menge, Hauptauftrag,
   Abteilung, Beschichtet, FE-Termin, Material; Rest per Zahnrad).
6. **Anmeldung persönlich.** Filter je Anmeldung und Werkbank, Wochenauswahl **relativ**, Knopf „Filter zurücksetzen“ – passt.
7. **Werkbänke zum Start:** alle, die bisher in der Vormontage waren: **Stanzen, Kanten KA, Schweißen (Laser und Manuell), Punkten PU,
   FR-Zuschnitt ZU, Isolierung IF**.
8. **Rücknahme:** eigene Meldung bis Schichtende, danach nur BDE-Admin über die BDE-Buchungen – passt.

## Einordnung Koordination (2026-11-16)
- **Zu 2 – Widerspruch zum Code prüfen:** Die Struktur-Erkennung legt Kürzel von Blatt-Kindern heute am **Vater-Sub-FA** als `WorkOperation`
  an (`IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:105-106` nimmt Blatt-Kinder `SubFA == 0` in den Scope des Vaters; [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] AK 13, TS-77.18). „Am richtigen Teil angeschrieben“ beschreibt die Sage-Daten,
  nicht diese WMS-Regel. Ohne Rückmeldung nach oben blieben solche Vater-AGs (z. B. `KA` am Korpus aus Blechteilen) für Teileliste-Werkbänke
  **dauerhaft offen** (Terminal, Cockpit, Auswertungen). Spec muss entscheiden: Struktur-Erkennung legt für Kürzel von Teileliste-Werkbänke
  keine Vater-AGs mehr aus Blatt-Kindern an (Empfehlung), oder die Vater-AGs werden dort ausgeblendet.
- **Zu 1 – zwei Buchungsziele:** Baugruppen-Zeilen (Sub-FA mit eigenem `WorkOperation`) vs. Blechteil-Zeilen (Blatt, kein AG) →
  Variante A (eigene Teil-Rückmeldung) für Blätter; für Sub-FA-Zeilen Empfehlung: dieselbe Teil-Rückmeldung **und** den eigenen AG fertig
  melden (gleiche Semantik wie `BdeNurAgFertigmeldung`), damit Terminal/Cockpit stimmen. Spec legt das fest.
- **Zu 1 – „beide Zeilen bei gleichem Schritt“** als eigene Etappe/später, wie vom Fachbereich angeboten.
- **Zu 7 – Rückfrage:** Laserschneiden (`LS`) steht nicht in der Liste („Schweißen (Laser …)“ ist Laserschweißen). Gewollt? Kürzel für
  „Stanzen“ und „Schweißen“ aus den Werkbank-Stammdaten ableiten, nicht raten. Wirkung per Werkbank-Option, also ohnehin je Werkbank schaltbar.
- Abgrenzung zu [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv]] bleibt (BF-Aufträge, Antworten auf 13 Fragen stehen aus).

## Antwort Benjamin 2026-11-16 (Frage LS, über Gerald 16:22)
„Die Arbeitsgänge haben das Kürzel **SL** und **SM**.“ → Schweißen Laser = SL (Sage-AG 161), Schweißen Manuell = SM (162); **kein LS**.
Start-Werkbänke damit (Kürzel laut Sage `USER_PNKZ`): **ST** Stanzen (120), **KA** Kanten (150), **SL** (161), **SM** (162), **PU** Punkten (180),
**ZU** FR-Zuschnitt (130), **IF** Isolierfräsen (220). Werkbänke im WMS über `ArbeitsschrittCode` zuordnen (Spec belegt).
