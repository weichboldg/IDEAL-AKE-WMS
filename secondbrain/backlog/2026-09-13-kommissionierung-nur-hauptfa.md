---
typ: feature
---
# IDEAL: Kommissionierung findet nur auf dem HauptFA statt

**Aufgenommen 2026-09-13.** Herausgeloest aus
[[2026-09-13-matchcode-artikelstamm-kommissionierung-hauptfa]], damit beide Themen ihren eigenen
Takt haben. Der Matchcode laeuft zuerst.

Kommissioniert wird ausschliesslich auf Ebene des **HauptFA**, nicht je Sub-FA.

## Das ist keine Detailaenderung — es loest drei offene Punkte auf einmal

**(1) Die offene Scope-Frage B-0 ist damit beantwortet.**
Die BOM-Bridge liess offen, ob die Vorbau-Stueckliste eines HauptFA die **Vollstruktur** zeigen soll
(Ruling 4, bis zur Abnahme geparkt). Die Pruefregel dazu lautete: *"Erscheint dasselbe Material
dadurch auf ZWEI Kommissionierlisten?"*
**Wenn nur der HauptFA kommissioniert wird, gibt es keine zweite Liste.** Die Vollstruktur ist damit
nicht nur zulaessig, sondern **notwendig** - sonst fehlte Material, das niemand sonst holt.
**Folge:** Der Rueckbau von Ruling 4 entfaellt; stattdessen wird die **Druck-Whitelist in
`PrintBom.cshtml` um Ebene und Komm.-Ziel ergaenzt** - das war die andere Haelfte der
Entweder-oder-Entscheidung und ist **vor dem Merge** faellig.

**(2) Die Freigabe-Kaskade wird gegenstandslos.**
[[2026-09-10-fa-liste-ausbau-matchcode-spec]] (freigegeben) baut eine Kaskade
"HauptFA freigeben -> alle Sub-FAs zur Kommissionierung freigeben", samt Pflicht-Picker,
Bestaetigungsdialog und Atomaritaet.
**Findet Kommissionierung nur auf dem HauptFA statt, gibt es an den Sub-FAs nichts freizugeben.**
Die Kaskade loest ein Problem, das dann nicht mehr existiert - und sie ist der aufwendigste Teil
jener Spec.
**Zu entscheiden: Block 4 (Tasks 12-14) dort streichen oder umwidmen?** Laeuft der Dev-Lauf bereits
daran, sofort melden - je frueher, desto billiger.

**(3) Der doppelte Materialzug ist ausgeschlossen** - die Sorge, die hinter B-0 stand.

## Was ausdruecklich NICHT betroffen ist

**Die BDE-Rueckmeldung bleibt auf Sub-FA-Ebene.** Teil 8 ist gebaut und abnahmebereit: Der Werker
meldet **seinen** Arbeitsgang an **seinem** Sub-FA zurueck.
**Rueckmeldung ist Arbeitsnachweis, Kommissionierung ist Materialbereitstellung** - zwei
verschiedene Dinge. Wer sie zusammenwirft, baut Teil 8 versehentlich mit zurueck.

Ebenso unberuehrt: Beschichtungsauftrag und Vormontage-Listen - sie gruppieren nach
**Arbeitsbereich**, nicht nach FA-Ebene.

## Offene Fragen

1. **Was passiert mit den Sub-FAs im Picking-Pfad?** Bekommen sie gar keine
   `ProductionOrderPickingStatus`-Zeile mehr, oder existiert sie und bleibt unbenutzt?
   **Achtung:** Der F6-Fix legt sie gerade erst an (siehe
   [[2026-09-09-materialisierung-ohne-statuszeilen-bug]]) - nicht versehentlich zurueckbauen. Die
   Zeile wird auch fuer andere Zwecke gebraucht (Glas, Fremdbezug, Lackierung).
   *Einschaetzung:* Zeile bleibt, nur der Freigabe-/Picking-Weg wird auf den HauptFA beschraenkt.
2. **Verschwindet der Freigabe-Knopf an Sub-FA-Zeilen**, oder bleibt er sichtbar und wirkungslos?
   *Einschaetzung:* verschwinden - ein Bedienelement, das nichts tut, ist schlimmer als keines.
3. **Gilt das auch fuer AKE?** Dort ist `OrderNumber == SubOrderNumber`, die Regel waere folgenlos -
   aber sie sollte am **Master-Schalter** haengen, nicht global gelten.
4. **Was ist mit bereits freigegebenen Sub-FAs** im Testsystem? Stehen lassen oder zuruecksetzen?
5. **Und die Kommissionierlisten aus Teil 3?** Sie gruppieren nach HauptFA und Arbeitsbereich -
   zu pruefen, ob sie von dieser Regel beruehrt sind oder unveraendert bleiben.

## ERWEITERUNG 2026-09-23 — jetzt beauftragt

**Bestaetigt vom Menschen:** Kommissioniert wird **nur der HauptFA, inklusive aller in der Stueckliste
enthaltenen Teile** — also die Vollstruktur (Ruling 4 bleibt, siehe oben). Die fruehere Festlegung
"Kommissionierung auf Sub-FA-Ebene" gilt nicht mehr.

### Neu: Die Filterregeln der Kommissionierliste werden Einstellung und wirken auch in der Stueckliste

Die **Kommissionierliste** (IDEAL Teil 3) filtert heute bereits nach festen Parametern, welche
Positionen kommissionier-relevant sind. Diese Parameter sollen:
1. **in die IDEAL-Standorteinstellungen** wandern (Teil 6), damit sie bei Bedarf angepasst werden koennen,
   statt im Code zu stehen;
2. **zusaetzlich in der Stuecklistenkommissionierung** (`/Picking` Stueckliste) anwendbar sein — per
   **Checkbox** ein- und ausschaltbar;
3. vom **"Filter loeschen"-Knopf** der Stueckliste **mit zurueckgesetzt** werden.

**Der Kerngedanke, der den Entwurf leiten muss: EINE Regel, zwei Verbraucher.** Kommissionierliste
und Stueckliste muessen **dieselbe** Filterlogik aus **denselben** Einstellungen lesen — nicht zwei
Implementierungen, die mit der Zeit auseinanderlaufen. Heute steht die Logik in der
Kommissionierliste; sie wird herausgeloest und von beiden benutzt. Sonst zeigt die Stueckliste eine
andere "kommissionier-relevante" Menge als die Liste, und niemand weiss, welche stimmt.

**Am Code zu erheben, NICHT zu raten:** Welche Parameter filtert die Kommissionierliste heute genau
(`SubFA`, Beschaffungsartikel, Kommissionier-Ziel, Artikelgruppe, ...)? Die Liste dieser Parameter ist
der Umfang der neuen Einstellungen.

### Zusammenspiel mit den bestehenden Standardfiltern der Stueckliste

Die Stueckliste hat seit [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] vier gespeicherte
Standardfilter, **jeder mit sichtbarem Badge und Ein-Klick-Reset** (Hausregel: sichtbar machen statt
still filtern). Der neue Kommissionierlisten-Filter ist ein **fuenfter** Filter derselben Art:
- **Aktiv muss er sichtbar sein** — derselbe Badge, nicht nur ein Haken in der Checkbox. Eine
  Stueckliste, die still auf "kommissionier-relevant" eingeschraenkt ist, sieht vollstaendig aus.
- **"Filter loeschen" setzt ihn zurueck** — wie gewuenscht, und konsistent mit den vier anderen.
- **Zu entscheiden:** Ist die Checkbox beim Oeffnen standardmaessig **an** (weil der Kommissionierer
  meist nur die relevanten Teile will) oder **aus** (Vollstruktur wie bisher)? Falls standardmaessig
  an: pro Benutzer speicherbar wie die anderen Standardfilter?

### Zu pruefen: Was ist mit der Freigabe-Kaskade?

[[2026-09-10-fa-liste-ausbau-matchcode-spec]] plante in Block 4 eine Kaskade "HauptFA freigeben ->
alle Sub-FAs freigeben". Laut letztem Stand fehlte Block 4 noch. **Am Code pruefen, ob sie inzwischen
gebaut ist.** Falls ja: zurueckbauen — sie gibt Sub-FAs frei, die nie kommissioniert werden, und
erzeugt damit einen Zustand ohne Bedeutung. Falls nein: in der FA-Listen-Spec als ueberholt
kennzeichnen.

### Weiterhin gueltig aus dem Abschnitt oben

BDE-Rueckmeldung bleibt auf Sub-FA-Ebene; die `PickingStatus`-Zeile der Sub-FAs wird nicht
zurueckgebaut (sie traegt auch Glas/Fremdbezug/Lackierung); die Druck-Whitelist braucht Ebene und
Komm.-Ziel (pruefen, ob mit der Kommissionierziel-Spec bereits erledigt).

## Bezug

[[2026-09-10-fa-liste-ausbau-matchcode-spec]] (Kaskade - betroffen) ·
[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]] (B-0/Ruling 4, Druck-Whitelist) ·
[[2026-07-29-standort-ideal-teil-8-spec]] (BDE bleibt auf Sub-FA-Ebene) ·
[[2026-09-09-materialisierung-ohne-statuszeilen-bug]] (PickingStatus-Zeile)
