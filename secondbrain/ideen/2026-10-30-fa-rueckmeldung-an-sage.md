---
typ: feature
epic: true
anhaenge:
  - anhaenge/2026-10-30-fa-rueckmeldung-an-sage/sdata-befund-ake-test-2026-10-30.md
  - anhaenge/2026-10-30-fa-rueckmeldung-an-sage/2026-11-03-dps-anfrage-produktion-sdata.md
---
# Fertigungsaufträge über das WMS an Sage zurückmelden (BDE → Sage, FA-Hierarchie)

**Aufgenommen 2026-10-30 (Gerald), Analyse Koordination.** Nur gelesen: Hauptcheckout + Bündel-Worktree; SData am AKE-Test
nur per GET ([[sdata-befund-ake-test-2026-10-30]]).
Ziel Gerald: FAs im WMS per BDE buchen, die Information an Sage liefern (Bestandsbuchungen, ggf. Stückliste anpassen,
FA abschließen) und die Abgleiche über die FA-Hierarchie konsistenter machen.
Bezug: [[2026-10-19-bde-lastenheft-abgleich]] (S1 „BDE ↔ Sage“), [[bde_ideen]], [[2026-07-29-sage-lagerbuchungen-spec]],
[[sage-views-ideal]].

## Ist-Stand WMS (belegt)

- **Einziger Schreibweg an Sage:** SData `POST …/CommonWawiServices/{Dataset}/$service/LagerbuchungService`, Basic-Auth,
  Queue `SageBookingQueueItems` + `SageBookingWorker` (`IDEALAKEWMSService/Services/SageLagerbuchungClient.cs`,
  [[integrationen]] „Sage-Lagerbuchung“). Bewegungsarten Zugang/Entnahme/Umbuchung. **Kein FA-Bezug im Payload**
  (`SageLagerbuchungPayload.cs`).
- `SageRueckmeldungAktiv` wird angelegt (`IdealAkeWms/Program.cs`), laut [[2026-10-19-bde-lastenheft-abgleich]] ohne
  Verwendung im Code (nicht erneut über alle Wege geprüft).
- FA-Hierarchie: `FaHierarchyNode` = Full-Refresh-Projektion der View `FAListe` (`Models/FaHierarchyNode.cs`, Worktree),
  Schlüssel `HauptFA` (StrukturID), `VaterFA`/`SubFA` (BelIDs, `SubFA = 0` = Blatt); View liest nur `RessourceTyp = 'MA'`.
- WMS-Arbeitsgänge kommen aus dem Freitextfeld `Arbeitsschritte`, nicht aus Sage-PPS-Arbeitsgängen.

## Befund SData AKE-Test 2026-10-30 (Details im Anhang)

| Bedarf | Sage-SData-Angebot | Stand |
|---|---|---|
| FA-Status / Rückmeldestand **lesen** | `RealTimeData`: `dtsFaUebersicht`, `dtsFertigmeldungen`, `dtsRueckKorrektur` (= Rückmeldungstabelle), `dtsFaBelegStruktur`, `dtsFaFortschritt`, `dtsBDE*` – JSON, Lesetest OK | **vorhanden** |
| Fertigmeldung / Zugang mit FA-Bezug | kein Service in `CommonWawiServices`; nur interne Struktur `strFaBeleg.Sage.PPS` (PUT/Execute, `MethodName` undokumentiert) → **HTTP 403** für den SData-Benutzer | **fehlt (dokumentiert)** |
| Materialentnahme zum FA | wie oben | fehlt |
| AG-Zeiten | wie oben; Felder existieren (`POS_IstRuestzeit/IstStueckzeit`, `dtsRueckKorrektur.Typ/Start/Ende`) | fehlt |
| FA abschließen | `strFaFertigungsprojektBeenden.Sage.PPS` (Fertigungsauftrag, UnterbaugruppenMitBeenden) – nur Get, Aufruf unklar | fehlt |
| Stückliste ändern | `strRessourcenlisteKopf` (Artikel-Stückliste) PUT, `strFaRessourcenlisteAktualisieren` – nicht aus dem WMS (s. u.) | bewusst nicht |

Sage 100 hat ein **eigenes BDE-Modul** (Rückmeldung mit Typ, Mitarbeiter, Arbeitsplatz, Terminal, Stempelkorrektur). Der
Sage-Partner (Nr. 100036671) betreibt auf dem Server eigene Pakete (`AKE_InterneFA`, `DPSBS_MDE*`, `AKE_OseonDataImport`).

## Kernbefund

1. **FA-Buchungen nicht über den LagerbuchungService nachbilden.** Zugang/Entnahme ohne FA-Bezug lassen den FA in Sage offen,
   Bedarf und Zugang stehen doppelt in der Dispo, Nachkalkulation bleibt leer (Einschätzung, mit Partner bestätigen).
2. **Schreibweg Produktion existiert nur intern.** `strFaBeleg.Sage.PPS` ist die Datenstruktur des Sage-Clients; ohne Rechte
   und ohne Doku der `MethodName`s kein tragfähiger Integrationsweg.
3. **Adresse jeder Rückmeldung ist der Sub-FA (BelID), nicht der HauptFA.** `strFaPosition.POS_BelegHandleNachfolger` /
   `dtsFaBelegStruktur.BelIDNachfolger` sind die Sage-Gegenstücke zu `SubFA`.

## Wege

| Weg | Was | Bewertung |
|---|---|---|
| **A** Partner-Service | Sage-Partner liefert Service(s) im Stil des `LagerbuchungService`: Rückmeldung (Menge/Zeit je BelID/BelPosID), Materialentnahme zum FA, Fertigmeldung, FA beenden | **Empfohlen.** Queue/Worker/Idempotenz wiederverwendbar. Partner kennt die Struktur (eigene Pakete vorhanden) |
| **B** `strFaBeleg` direkt | Rechte für den SData-Benutzer + Doku der `Execute`-Methoden | Rückfall; versionsabhängig, undokumentiert |
| **C** OL Business Objects | eigener 32-bit-Dienst | nur wenn A und B scheitern |
| **D** KHK-Tabellen schreiben | – | **Nein** |

## Fachliche Empfehlung zum Umfang

- **Stückliste nicht im WMS ändern.** Mehr-/ungeplanter Verbrauch als Materialentnahme zum FA melden (Lastenheft Nr. 22).
- **Reihenfolge risiko-aufsteigend:**
  0. Partner anfragen (Weg A); parallel Prüfabfragen unten.
  1. **Abgleich lesend** (jetzt möglich): `RealTimeData`-Quellen per Dienst lesen, Sage-Rückmeldestand je BelID neben dem
     WMS-Stand anzeigen. Kein Schreibrisiko, Prüfwerkzeug für alle weiteren Stufen.
  2. Fertigmeldung Sub-FA → Sage (Menge, Gut/Ausschuss aus `BdeBookingQuantity`) über die Queue.
  3. Materialentnahme zum FA (retrograd oder explizit – Gerald/Sage entscheiden).
  4. AG-Zeiten – erst wenn die FAs in Sage AG-Positionen haben.
  5. FA abschließen – bottom-up, Invariante melden statt still abschließen.
- AKE: BDE derzeit nicht im Einsatz → zuerst IDEAL (hierarchisch); **IDEAL-Sage-Server noch nicht geprüft** (eigener SData-Host?).

## Prüfabfragen für Gerald (ungeprüft)

**Sage-Partner:** Gibt es (oder liefern sie) SData-Services für FA-Rückmeldung, Materialentnahme zum FA, Fertigmeldung,
FA beenden? Alternativ: Rechte des SData-Benutzers auf `MasterData/strFaBeleg.Sage.PPS` und Doku der `Execute`-MethodNames.
**IDEAL:** SData-Host und Dataset des IDEAL-Sage-Servers (`idealsql2…`?) – gleicher Vertragsstand?

**SQL (Sage-Test-DBs, nur lesen):**
```sql
-- Gibt es Arbeitsgang-Positionen in den FAs?
SELECT RessourceTyp, COUNT(*) AS Anzahl FROM dbo.KHKPpsFaBelegePositionen GROUP BY RessourceTyp;
-- Rueckmeldungen vorhanden? (Tabelle laut dtsRueckKorrektur)
SELECT name FROM sys.tables WHERE name LIKE 'KHKPps%Rueck%' OR name LIKE 'KHKPpsFertig%' ORDER BY name;
```

## Offen (Gerald)

- **2026-11-03:** DPS-Anfrage formuliert ([[2026-11-03-dps-anfrage-produktion-sdata]], Ticket#7954779), Stand Entwurf. Ergebnis der
  Schnittstellenprüfung: [[sdata-befund-ake-test-2026-10-30]] §7–§9, Spezifikation: [[sdata-2-0-spezifikation]]. Abgleich/Entscheidungen:
  [[2026-11-02-sage-pps-abgleich]] Abschnitt F.

- Antwort Sage-Partner (Weg A) bzw. Rechte für Weg B.
- Materialentnahme retrograd oder explizit?
- Bestätigung: Stückliste bleibt in Sage, WMS meldet nur Mehrverbrauch.
- Soll Stufe 1 (Abgleich lesend) schon jetzt als Backlog-Notiz starten? Sie hängt nicht am Partner.
