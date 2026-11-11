---
typ: abgleich
datum: 2026-11-02
thema: Recherche Sage 100 PPS (FA, Baugruppen, BDE) gegen WMS-Code und Sage-Bestand
recherche: "[[2026-11-02-sage100-pps-fa-bde-recherche]]"
pruefabfragen: "[[2026-11-02-sage-pps-pruefabfragen]]"
spec: "[[2026-11-02-sage-pps-fa-bde-abgleich-spec]]"
phase2: nur Abfragedatei – keine DB-Verbindung
---
# Abgleich Recherche „Sage 100 PPS – FA, Baugruppen, BDE“ ↔ WMS-Code ↔ Sage-Bestand

**Grundlage:** [[2026-11-02-sage100-pps-fa-bde-recherche]] (Kennzeichnung [B]/[A]/[U]).
**Code-Stand:** Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.71.0, **W**) und Hauptcheckout `main` (v1.30.1, **M**).
Alle Fundstellen ohne Präfix beziehen sich auf W. Nur gelesen, keine Git-Befehle.
**Sage-Seite:** Es wurde **keine** Verbindung zur Sage-DB hergestellt. Keine Verbindung ist nachweislich nur lesend: Die DEV-`SageConnection` zeigt auf
die Test-Sage, ihre Rechte sind nicht geprüft ([[2026-10-29-dev-umgebung-lokal]]). Die Abfragen liegen fertig in
[[2026-11-02-sage-pps-pruefabfragen]] (Q0–Q14, je Installation AKE/IDEAL getrennt). Als einziger Sage-Beleg dient der dokumentierte
**SData-Lesebefund am AKE-Test vom 2026-10-30** ([[sdata-befund-ake-test-2026-10-30]], nur GET). IDEAL-Sage ist ungeprüft.

## Ergebnis in Zahlen

| | Recherche-Aussagen (R) | WMS-Annahmen (W) |
|---|---|---|
| bestätigt | **8** | 0 |
| widerlegt | **0** | 0 |
| ungeprüft | **25** | 5 |
| **Summe** | 33 | 5 |

Dazu kommen ein widerlegter Brain-Eintrag (B-1) und vier Zusatzfunde (Z-1 bis Z-4).

> [!warning] Die wichtigsten Abweichungen zur Recherche
> 1. **Hierarchie-Verknüpfung (R2):** Das WMS baut die FA-Hierarchie **nicht** über „Verursacher“ (Verursachertyp 70). Es liest nur
>    `HauptFA`/`VaterFA`/`SubFA` aus der View `FAListe`. **Laut View-Doku** stammen sie aus `StrukturID` und
>    `KHKPpsFaBelegePositionen.BelID`/`BelIDNachfolger`. Der Viewtext fehlt im Repo, deshalb ist die Herkunft ungeprüft (W2). Sage stellt in
>    SData `POS_BelegHandleNachfolger` und `BelIDNachfolger` bereit. Ob Verursacher-Kanten dieselbe Struktur liefern, ist ungeprüft (Q4b gegen Q5).
> 2. **Reconciliation am falschen Ort vermutet (R9):** Die Storno-Reconciliation aus Migration 80 läuft **nur im flachen AKE-Pfad**
>    (WAListe, `OrderNumber`). Verschwundene **Unter-FAs** behandelt die IDEAL-Materialisierung: Sie setzt nur `SageMissingSince` und
>    storniert nicht. Die Unterscheidung „in Material umgewandelt“ gehört deshalb in die Materialisierung, nicht (nur) in Migration 80.
> 3. **SData und Produktion (R11):** Die Aussage „SData deckt Produktionsrückmeldungen nicht ab“ ist in dieser Absolutheit nicht bestätigt, aber auch **nicht widerlegt** (Korrektur 2026-11-02, s. Abschnitt F).
>    `MasterData/strFaBeleg.Sage.PPS` bietet PUT/POST/`Execute`. Nutzbar ist das trotzdem nicht: Die MethodNames sind undokumentiert, und der
>    SData-Benutzer bekommt HTTP 403. `CommonWawiServices` hat tatsächlich keinen Produktions-Service.
> 4. **Weg „SageBridge“ (R12) widerspricht der Vor-Analyse:** [[2026-10-30-fa-rueckmeldung-an-sage]] empfiehlt **Weg A**, einen
>    Partner-Service im Stil des `LagerbuchungService`. OL Business Objects sind dort nur Weg C („nur wenn A und B scheitern“). Die Recherche
>    setzt dagegen auf die SageBridge (= Weg C). Diese Entscheidung ist offen und gehört zu Schranke 1.
> 5. **Doppel-Entnahme-Risiko im Ist-Stand (R19):** WMS-Ausbuchungen gehen schon heute als SData-„Entnahme“ **ohne FA-Bezug** an Sage
>    (`SageLagerbuchungPayloadBuilder.cs:34-54` ignoriert `StockMovement.ProductionOrder`). Kommt eine Material-RM zum FA hinzu, wird dasselbe
>    Material doppelt entnommen, solange FA-Material weiter als freie Ausbuchung läuft.
> 6. **Kein Statusfilter (R7):** Kein WMS-Lesepfad filtert auf eingelastet, begonnen oder beendet (`SageImportService.cs:58`,
>    `FaHierarchySql.cs:139-152`). Die Viewtexte liegen nicht im Repo (`SQL/sage-views/*.sql` mit TODO). Ob die Views selbst filtern, zeigt erst Q11.

## A) Recherche-Aussagen

Spalten: Aussage · Kennz. · Befund WMS-Code · Befund Sage · Fundstelle · **Status**

| # | Aussage Recherche | Kennz. | Befund WMS-Code | Befund Sage | Fundstelle | Status |
|---|---|---|---|---|---|---|
| R1 | Unterbaugruppen bekommen eigene Unter-FAs (eigene BelID, gemeinsame Struktur) | B | IDEAL-Struktur: `HauptFA` = StrukturID, `SubFA` = BelID des Unter-FA, materialisiert als `ProductionOrder.SubOrderNumber` | SData AKE-Test: `strFaBeleg` mit `IstUnterfertigungsauftrag`, `HatUnterbaugruppen`, `StrukturHandle`; `dtsFaUebersicht` mit `BelID`, `StrukturID` | `Models/FaHierarchyNode.cs:17-25`; `FaMaterializationSyncService.cs:144-156`; [[sdata-befund-ake-test-2026-10-30]] §3/§4 | **bestätigt** |
| R2 | Verknüpfung über „Verursacher“, Hierarchie vermutlich StrukturID + Verursachertyp 70 | U/A | **Abweichung:** WMS-Kanten aus `KHKPpsFaBelegePositionen.BelID` (Vater) → `BelIDNachfolger` (Kind); `VerursacherTyp` nur Anzeige, bewusst kein Wurzelkriterium | SData: `POS_BelegHandleNachfolger`, `dtsFaBelegStruktur.BelIDNachfolger` vorhanden; Verursacher-Tabelle ungeprüft | `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:23-27`; `SageSdataFaTreeBuilder.cs:34-35`; Q4b/Q5 | **ungeprüft** |
| R3 | Unter-FA vor Haupt-FA fertigmelden (bottom-up) | A | Kein Schreibweg vorhanden, Regel nicht implementiert | – | – | **ungeprüft** |
| R4 | Kopf-Status über Kennzeichen Eingelastet / Begonnen / Beendet / Reaktiviert / Aktiv | B | SData-Ansicht zeigt `IstEingelastet/IstBegonnen/IstBeendet/IstGesperrt` nur an | SData: Felder vorhanden, Sage-Boolean (FA 835 `IstEingelastet = -1`); `strFaBeleg.IstReaktiviert` | `SageSdataFaModels.cs:70-71`; `Views/SageSdataFa/Index.cshtml:15-21`; [[sdata-befund-ake-test-2026-10-30]] §6 | **bestätigt** (in SData; Recherche nennt „Datum“, SData liefert Bool – Tabellenspalten R5) |
| R5 | Echte Spaltennamen der Statusfelder in `KHKPpsFaBelege` | U | Kein WMS-Code liest sie per SQL | ungeprüft | Q3, Q3b | **ungeprüft** |
| R6 | Keine Status-Enumeration, Status aus Kennzeichen ableiten | B/A | – | SData `dtsFaNachStatus`: abgeleitete Zählung Neu 20 / Eingelastet 7 / Begonnen 0 | [[sdata-befund-ake-test-2026-10-30]] §4/§6 | **bestätigt** |
| R7 | Nur eingelastete FAs als arbeitsbereit synchronisieren | A | **Kein** Statusfilter in irgendeinem Lesepfad: WAListe nur `[WA Nummer] IS NOT NULL`; FAListe nur `EXISTS` gegen FAInfos; FAInfos filtert nur PPS-Status ≠ leer | Viewtexte nicht im Repo | `SageImportService.cs:47-59`; `FaHierarchySql.cs:139-163`; `SQL/sage-views/…FAInfos.sql:41-44`, `…:50` (TODO); Q11 | **ungeprüft** |
| R8 | Bedarfsprüfung „beim Einlasten“ löscht Unter-FAs mit ausreichendem Bestand | B (DPS-Hb.) | Kein Code kennt den Fall (Suche `Bedarfspr\|umgewandelt\|Einlast\|in Material\|aufgel[oö]st`: nur SData-Anzeige) | Einstellung und Ablage ungeprüft | Q7, Q12 | **ungeprüft** |
| R9 | Reconciliation (Migration 80) storniert gelöschte FAs, unterscheidet „in Material umgewandelt“ nicht | A | **Bestätigt, aber mit Ortsabweichung:** Reconciler arbeitet nur flach per `OrderNumber` (WAListe); verschwundene Unter-FA im IDEAL-Pfad → nur `SageMissingSince`, kein Storno, bleibt in offenen Sichten (Badge „vermisst seit …“); ein Grund wird nirgends erfasst | – | `ProductionOrderReconciler.cs:36-68`; `SageImportService.cs:215-327`; `FaMaterializationPlanner.cs:68-86`; `FaMaterializationSyncService.cs:161-182`; `_ProductionOrderRow.cshtml:53-59` | **bestätigt** |
| R10 | Rückmeldungen nie per SQL-INSERT in KHK-Tabellen | B | WMS schreibt **nichts** per SQL nach Sage; alle DML zielen auf WMS-Tabellen; Sage-Lookup ist reines SELECT | – | `SageBuchungLookupReader.cs:14-15`; DML-Treffer `SageImportService.cs:161/171/282/318`, `BomCacheSyncService.cs:509-526` (WMS-Verbindung) | **bestätigt** |
| R11 | SData/REST deckt Produktionsrückmeldungen nicht ab | B (TerraCom) | – | `CommonWawiServices` (25 Ops): kein PPS-Service ✔; **aber** `MasterData/strFaBeleg.Sage.PPS` mit PUT/POST/`Execute` (MethodNames undokumentiert, **403**) | [[sdata-befund-ake-test-2026-10-30]] §2/§3 | **ungeprüft** (Korrektur 2026-11-02: `strFaBeleg` ist beschreibbar, ein Rückmeldeweg ist nicht belegt) |
| R12 | Eigene .NET-4.8-x86-Komponente („SageBridge“) nötig | A | Keine Sage-Assembly, kein COM in irgendeinem `*.csproj` | – | Suche `Sagede\|OfficeLine\|RueckmeldungEngine\|Interop\|ComImport` ohne Code-Treffer; **Abweichung zu** [[2026-10-30-fa-rueckmeldung-an-sage]] (Weg A empfohlen) | **ungeprüft** |
| R13 | Partner-Lösungen brauchen Third-Party-Lizenz | B (TerraCom) | – | ungeprüft (DPS/Sage) | – | **ungeprüft** |
| R14 | Signaturen der RueckmeldungEngine öffentlich nicht dokumentiert, SDK nur Tech-Partner | U | – | ungeprüft (DPS) | – | **ungeprüft** |
| R15 | Assemblies `Sagede.OfficeLine.Pps.*`, `ApplicationEngine.CreateSession`, Dispose, 32 Bit | B | nicht vorhanden | Installation auf Sage-Server ungeprüft | – | **ungeprüft** |
| R16 | Empfohlene Grundlageneinstellungen (Entnahme manuell/Gesamt-FM, Verf. Mengen = Keine, Rest zu Soll) und DPS-Soll-Werte | A/U | – | Ablage und Ist-Werte ungeprüft | Q7 | **ungeprüft** |
| R17 | Je Charge eine Material-RM; Chargen erzwingen „manuell bzw. Gesamtfertigmeldung“ | B | WMS hat **keine** Charge-/SN-Felder; Lagerbuchung schickt leere Charge/SN | ungeprüft (Zusatzpaket lizenziert?) | `SageLagerbuchungPayloadBuilder.cs:44-46`; Suche `Charge\|Serien\|SerialNumber\|Batch\|Lot` in `Models`: 0 Treffer | **ungeprüft** |
| R18 | Rundung nach Lager-ME (Entnahme auf-, Zugang abrunden) | B | Keine Einheit im Payload, keine Rundung; Menge `decimal(18,3)`; implizit Lager-ME (`Article.Unit` = `KHKArtikel.Lagermengeneinheit`, beim Buchen nicht gelesen) | Dezimalstellen je ME ungeprüft | `SageLagerbuchungPayloadBuilder.cs:43`; `SageArticleSql.cs:16,32`; `ApplicationDbContext.cs:335` | **ungeprüft** |
| R19 | WMS-Umbuchung auf Kommissionierwagen = Lagerumbuchung; Verbrauch = Material-RM | A | **Risiko:** Queue kennt nur Zugang/Entnahme; Ausbuchung → Entnahme **ohne FA-Bezug**; `StockMovement.ProductionOrder` wird gespeichert, aber nicht gesendet | ungeprüft, ob Sage-Dispo diese Entnahmen dem FA zuordnet (laut Vor-Analyse: nein) | `SageBookingEnqueueDecision.cs:14-15`; `SageLagerbuchungPayloadBuilder.cs:34-54`; `StockMovementsController.cs:130` | **ungeprüft** |
| R20 | `KHKPpsRueckmeldungen` (+Chargen/SerienNr/Lager/Fibu), Kinder per RueckmeldungID/BatchID | B/U | – | SData `dtsRueckKorrektur`: RueckmeldungID, BelID, BelPosID, Typ, Buchungstyp, Menge, MengeGut, Schwund, Start/Ende, Mitarbeiter, Arbeitsplatz, LagerBuchID, LagerJobID, Status; Tabelle ungeprüft | [[sdata-befund-ake-test-2026-10-30]] §4; Q1/Q2/Q6/Q13 | **ungeprüft** |
| R21 | Gut/Ausschuss getrennt, Ausschussfeld ungeprüft | U | WMS trennt `GoodQuantity`/`ScrapQuantity` je Buchung | SData zeigt `Menge`, `MengeGut`, `Schwund`; ein eigenes Ausschussfeld ist nicht sichtbar | `Models/BdeBookingQuantity.cs:5-26`; Q1 | **ungeprüft** |
| R22 | Sage-Boolean −1/0 | B | WMS filtert bereits `= -1` | SData `IstEingelastet = -1` | [[fallstricke]] (Boolean −1); [[sdata-befund-ake-test-2026-10-30]] §6 | **bestätigt** |
| R23 | Alle PPS-Tabellen führen `Mandant` | B | FA-Lesepfade ohne Mandantfilter; `Mandant = 1` hart nur in Bestand-/Lagerplatz-Lesern | PPS-Tabellen ungeprüft | `SageBestandReader.cs:33`; `SageLagerplatzReader.cs:27`; Q0/Q1 | **ungeprüft** |
| R24 | AKE und IDEAL getrennte Sage-Installationen; Schlüssel (Standort, Mandant, BelID) | B/A | Getrennte Installationen ✔ (AKE `ake` auf AKESQL20, IDEAL `idealsql2…\SAGE100TEST`); WMS hat **keine** Standort-/Mandant-Spalte, Eindeutigkeit über `SubOrderNumber`, Standort nur über Deployment/Konfiguration | – | `IDEALAKEWMSService/appsettings.json:3-6`; `StandortSettingsCatalog.cs:29,48-55`; `ApplicationDbContext.cs:430`; [[2026-10-29-dev-umgebung-lokal]] | **bestätigt** (Trennung; die Schlüssel-Empfehlung geht als Entscheidung in die Spec) |
| R25 | Idempotenz über GUID, ggf. im Memofeld | U | Muster existiert schon für Lagerbuchungen: Memo `IdealAkeWms Lagerbuchung SM#<id>#`, Lookup `Memo LIKE` in `KHKLagerplatzbuchungen`; laut Code-Kommentar selbst **unbestätigt** | ungeprüft | `SageBookingCorrelation.cs:13-15`; `SageBuchungLookupReader.cs:10-15` | **ungeprüft** |
| R26 | Reihenfolge seriell je StrukturID; Zeit → Material → Fertigmeldung | A | Ist-Queue: ein Worker, `ORDER BY Id`, keine Schlüssel-Serialisierung, kein DB-Claim, **kein Auto-Retry**; nur eine Service-Instanz zulässig | – | `SageBookingWorker.cs:14-15,146`; `SageBookingQueueRepository.cs:48-49` | **ungeprüft** (Entwurfsentscheidung) |
| R27 | Lastenheft: AKE ohne Arbeitszeiten, IDEAL ohne Arbeitsgänge in Stücklisten | B | WMS-Arbeitsgänge stammen aus dem Freitext `Arbeitsschritte`/`USER_ArbeitsSchritt` (Kürzel); **kein** Bezug zu einer Sage-AG-Position | Verteilung der Positionstypen ungeprüft | `WorkOperationStructureDetectionService.cs:20-21,108-127`; `Models/WorkOperation.cs:5-50`; Q6b | **ungeprüft** |
| R28 | (implizit) Heute fließt nichts aus dem BDE nach Sage | – | Bestätigt über alle Wege: einziger POST = Lagerbuchung; `SageRueckmeldungAktiv` wird nur angelegt/angezeigt, **nie gelesen** | – | `SageLagerbuchungClient.cs:45`; `Program.cs:291`; `Views/Settings/Index.cshtml:39`; Suchmuster s. Anhang | **bestätigt** |
| R29 | Gesamtfertigmeldung „Rest zu Soll“ + Schwund → „zu viel zurückgemeldet“; 9.0 bucht Netto + Schwund | B (Forum/Release) | – | Version/Einstellung ungeprüft | Q0, Q7 | **ungeprüft** |
| R30 | Fix „Als Unterbaugruppe = Nein“ ab 9.0, Build-Version prüfen | U | – | ungeprüft | Q0, Q8 | **ungeprüft** |
| R31 | Kein Phantom-Positionstyp in Sage 100 DACH | U | – | ungeprüft | Q6b, Q9 | **ungeprüft** |
| R32 | Trigger auf KHKPps*-Tabellen | U | – | ungeprüft | Q10 | **ungeprüft** |
| R33 | DPS: Unterbaugruppen-RM „entfällt, da Nutzung BDE“ – wird heute schon zurückgemeldet (DPS-MDE)? | B/U | – | SData: Partner-Felder `USER_DPSBSMDEFreigabe`, `USER_DPSBSBereitstellungBeendet`, Pakete `DPSBS_MDE*`; `dtsFertigmeldungen`/`dtsRueckKorrektur` am AKE-Test **leer** | [[sdata-befund-ake-test-2026-10-30]] §1/§3/§4; Q6, Q14 | **ungeprüft** |

## B) WMS-Annahmen, die der Abgleich mitprüft

| # | Annahme im WMS | Fundstelle | Prüfung | Status |
|---|---|---|---|---|
| W1 | Wurzel einer Struktur: `SubFA = HauptFA`, also BelID des Haupt-FA = StrukturID | `SQL/sage-views/…FAListe.sql:50-53` | Q4 (`RolleAnnahme`) | **ungeprüft** |
| W2 | `SubFA = KHKPpsFaBelegePositionen.BelIDNachfolger`, `0` = Blatt | `…FAListe.sql:23-27` (nur Doku, Viewtext fehlt) | Q4b, Q11 | **ungeprüft** |
| W3 | WAListe liefert „alle offenen FAs“ | `docs/superpowers/specs/2026-07-07-fa-reconciliation-design.md:126` | Q11 | **ungeprüft** |
| W4 | Korrelation der Lagerbuchung über `KHKLagerplatzbuchungen.Memo` | `SageBuchungLookupReader.cs:10-13` (Kommentar „am Testsystem zu bestätigen“) | Q1 (Spalten `KHKLagerplatzbuchungen`) | **ungeprüft** |
| W5 | Sage-Mandant ist an beiden Installationen `1` | `SageBestandReader.cs:33`, `SageLagerplatzReader.cs:27` | Q0 | **ungeprüft** |

## C) Brain-Eintrag widerlegt

- **B-1:** [[integrationen]] Zeile 40: „Was zurückgeschrieben wird: nur Rückmeldungen, und nur wenn `SageRueckmeldungAktiv` gesetzt ist.“
  Das ist **widerlegt**. Zurückgeschrieben werden nur Lagerbuchungen (SData `LagerbuchungService`, Gate ServiceSetting `SageLagerbuchungAktiv`,
  `SageBookingWorker.cs:66`). Rückmeldungen schreibt kein Code, und `SageRueckmeldungAktiv` liest niemand. In [[integrationen]] ist
  deshalb eine Korrekturnotiz ergänzt (additiv).

## D) Zusatzfunde (melden, nicht mitbauen)

- **Z-1 Reconciliation per `OrderNumber` im hierarchischen Schema.** Storno, Reaktivieren und Upsert arbeiten mit `WHERE OrderNumber = @x`
  (`SageImportService.cs:284,320`). Im IDEAL-Schema teilen Haupt-FA und alle Unter-FAs dieselbe `OrderNumber`, ein Treffer erwischt also die
  ganze Gruppe. Davor schützt nur die Betriebsvorgabe `Sync:ProductionOrdersEnabled = false` an IDEAL (`docs/TESTSZENARIEN.md:7017-7018`). Einen
  Code-Guard gegen den Master `ProduktionsauftragHierarchisch` gibt es nicht (`SyncWorker.cs:43`). **Verschärfung:** Der Default ist `true`
  (`ServiceSettingDefinitions.cs:19`, `SyncWorker.cs:43`). Fehlt die Zeile in `ServiceSettings`, läuft der flache Sync an IDEAL also.
- **Z-2 AgentJob 01:** In M ist `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` noch aktiv. In W liegt er unter `_archiv`. Laut
  `_archiv/README.md:20-22` entfernt das Archivieren keinen auf AKESQL20 eingerichteten Job.
- **Z-3 Retry-Obergrenze wirkungslos:** `AttemptCount >= maxRetries` wird geprüft (`SageBookingWorker.cs:150`). Der Worker liest aber nur
  `Offen` (`SageBookingQueueRepository.cs:48`), ein Fehler setzt sofort `Fehler`, und ein Requeue setzt `AttemptCount` auf 0
  (`Repo:131`). Einen automatischen Retry gibt es nicht. Für einen neuen Rückmelde-Pfad ist das wichtig, denn ein „FA gesperrt“ braucht Retry.
- **Z-4 Vermisster Unter-FA bleibt offen:** `SageMissingSince` wird in keiner Offen-Query gefiltert. Der Tooltip „Rückmeldung weiterhin möglich“
  (`_ProductionOrderRow.cshtml:57-59`) zeigt, dass das so gewollt ist. Ist der Unter-FA aber in Material umgewandelt, gibt es in Sage nichts
  mehr, das man zurückmelden könnte.

**Präzisierungen nach Spec-Lauf (2026-11-02):**
- Die SData-Ansicht v1.71.0 liest den FA-Kopf aus `dtsFaBelegsuche.Sage.PPS` (`SageSdataFaModels.cs:8,65`), **nicht** aus `dtsFaUebersicht`. Letztere
  ist nur in Postman getestet. Der Client liegt im Web-Projekt.
- `dtsRueckKorrektur` und `dtsFertigmeldungen` lieferten am AKE-Test **leere** Ergebnisse. Belegt ist nur, dass der Abruf funktioniert, nicht der Inhalt.
- DEV IDEAL zeigt mit SData auf den **AKE**-Test (`sagetest01.ake.at`, Dataset `ake_TEST2026;1`) und kann deshalb keine IDEAL-Sage-Daten liefern.

## E) Was Phase 2 nachliefern muss (Gerald)

Die Datei [[2026-11-02-sage-pps-pruefabfragen]] je Installation ausführen, und zwar zuerst Q0, Q3, Q4/Q4b/Q5, Q7, Q11 und Q12. Diese
entscheiden über R2, R5, R7, R8, R16, W1–W3. Ergebnisse unter `tests/anhaenge/2026-11-02-sage-pps-abgleich/` ablegen und hier die Spalte
„Befund Sage“ nachtragen.

## Anhang: Suchmuster der Negativaussagen

- Schreibwege nach Sage (W und M, `*.cs`): `PostAsync|PutAsync|SendAsync|PatchAsync|HttpMethod\.(Post|Put|Patch|Delete)`; DML
  `\b(INSERT|UPDATE|DELETE|MERGE|TRUNCATE|EXEC|EXECUTE)\b`; Drei-Teil-Namen `OPENQUERY|OPENROWSET|\[ake\]\.|\bake\.dbo\b`; `.sql` (inkl.
  `SQL/AgentJobs`, `_archiv`, `SQL/sage-views`, `SQL/Einmalig`): `(INSERT INTO|UPDATE|DELETE FROM|MERGE).*(KHK|\[ake\]|IDEAL_)`; BO/COM
  `Sagede|OfficeLine|BusinessObjects|RueckmeldungEngine|Interop|ComImport|GetTypeFromProgID`; alle `*.csproj`; `scripts/*.ps1`.
- „Umgewandelt“-Unterscheidung (W inkl. docs/Tests/SQL): `Bedarfspr|umgewandelt|Einlast|eingelastet|in Material|Materialposition|aufgel[oö]st`.
- Mandant/Einheit/Charge: `mandant` in `Models`, `Data`, `SQL`, Snapshot; `Mengeneinheit|\bUnit\b|Einheit`; `charge|seriennummer|serialnumber|\blot\b|batchnumber`.
- `SageRueckmeldungAktiv` in `*.cs`/`*.cshtml` (W): nur `Program.cs:291`, `Views/Settings/Index.cshtml:39`; M: nur `Program.cs:235`.

## F) Koordination 2026-11-02: Korrekturen und Entscheidungen

**Korrekturen am Bericht**
- **K-A (R11):** Von „widerlegt“ auf **ungeprüft** gesetzt. `strFaBeleg` ist der FA-Beleg; PUT/POST belegt nur, dass ein FA angelegt oder geändert
  werden kann, nicht, dass Zeit-, Material- oder Fertigmeldungen gebucht werden können. `Execute` ist undokumentiert und lieferte 403.
- **K-B (Zahlen):** Tabelle A hat 33 Zeilen (R1–R33). Stand nach K-A: 8 bestätigt, 0 widerlegt, 25 ungeprüft.
- **K-C (Fehler in der Recherche §7):** „Unter-FA gelöscht *nach* dem Einlasten = in Material umgewandelt“ widerspricht Recherche §3: Die
  Bedarfsprüfung löscht *beim* Einlasten, eingelastete FAs können nicht entlastet werden (DPS S. 127). Folge: Mit Statusfilter „eingelastet“
  (R7) sieht das WMS umgewandelte Unter-FAs im Normalfall nicht. Ein Unter-FA, der *nach* dem Einlasten verschwindet, ist ein Konfliktfall
  (z. B. manuell gelöscht), keine Umwandlung. Offen: Wird beim Einlasten des Haupt-FA gelöscht, bevor die Unter-FAs als eingelastet gelten? (Q3b, Q12)
- **K-D (R19):** Die Doppel-Entnahme kann je nach Rückmelde-Grundlagen schon **heute** auftreten (Sage entnimmt selbst bei „Automatisch bei
  Fertigmeldung Arbeitsgang“ oder Gesamtfertigmeldung mit MA). Stehen die Grundlagen laut DPS auf „entfällt, da BDE“, fehlt das Material
  stattdessen in der Nachkalkulation des FA (MEK) und der Materialbedarf bleibt in der Dispo offen. Entscheidet Q7.

**Lücken**
- **L-1 Fertigungsart „Einkauf“:** Die Baugruppe wird inaktiv, nicht gelöscht (DPS S. 88–90). Ein inaktiver Unter-FA darf im WMS nicht offen
  erscheinen. Q3 sucht `Aktiv`/`Fertigungsart`, aber keine R-Zeile und kein Akzeptanzkriterium deckt den Fall.
- **L-2 R27 ist Voraussetzung für die Zeit-RM:** Eine Zeit-RM braucht eine AG-Position (BelPosID). WMS-Arbeitsgänge stammen aus Freitext.
  Ohne AG-Positionen in den IDEAL-Stücklisten keine Zeit-RM.

**Entscheidungen Gerald (2026-11-02)**
- **E-1 Umfang:** vorerst nur **IDEAL** und der **hierarchische** Pfad (FAListe/Materialisierung). Der flache AKE-Pfad (Migration 80) ist vorerst
  nicht im Umfang.
- **E-2 Schreibweg:** Rückmeldungen wo möglich über den **SData-Endpunkt** (dieselbe Schnittstelle wie der `LagerbuchungService`), nicht über
  eine eigene SageBridge. Das entspricht Weg A aus [[2026-10-30-fa-rueckmeldung-an-sage]]; die SageBridge (Weg C) nur, wenn SData nicht reicht.
- **E-3 Lesen:** wo möglich ebenfalls über SData; die SQL-Views nur ergänzend.
- **E-4 IDEAL-SData:** Ein SData-Endpunkt für IDEAL existiert noch nicht. Er muss erst gekauft werden; beauftragt wird, wenn die Grundsatzfragen an DPS geklärt sind (Ergänzung 2026-11-03).
- **E-5 DPS-Anfrage (2026-11-03):** nur Grundstruktur, grundsätzliche Fragen und Beispiele (Postman/Request-Response); IDEAL/AKE-spezifische Unterschiede noch nicht. Text: [[2026-11-03-dps-anfrage-produktion-sdata]].
- **Schnittstellenprüfung 2026-11-03** ([[sdata-befund-ake-test-2026-10-30]] §7–§9): Status über SData lesbar; Hierarchie-Kanten nicht (bleibt `FAListe`);
  kein Sync/Linking/UUID/ETag in RealTimeData/CommonWawiServices; Delta nur über `GeaendertAm`; `trackingID` laut Schema zulässig (ungetestet);
  MasterData 403 auf jede Ressource. Folge für E-3: Lesen über SData für Kopf/Status, Kanten weiter über die View.

**Folgen der Entscheidungen**
- Bis der IDEAL-Endpunkt (E-4) steht, ist nichts davon an IDEAL prüfbar; die Views bleiben bis dahin der aktive Lesepfad. Der vorhandene
  SData-Baumaufbau (`SageSdataFaTreeBuilder`, Kanten über `BelIDNachfolger`) ist nur gegen den AKE-Test gelesen worden.
- Für E-2 muss DPS liefern: die Methoden für Zeit-, Material- und Fertig-RM (bzw. die MethodNames von `strFaBeleg … Execute`), die Rechte
  für den SData-Benutzer (heute 403) und ein Feld für die Idempotenz-ID.

**Noch offen an Gerald:** Rückmelde-Grundlagen IDEAL (Q7 bzw. Screenshot), AG-Positionen in IDEAL-Stücklisten, Umfang Etappe 1 (nur Lesen
korrigieren oder gleich zurückschreiben), `Sync:ProductionOrdersEnabled = false` an IDEAL gesetzt und Code-Schutz vorziehen (Z-1).

**DPS-Antwort 2026-11-04** ([[2026-11-03-dps-anfrage-produktion-sdata]]): Partner-Service für Rückmeldungen existiert (Preis offen) → E-2 bestätigt.
RealTimeData/MasterData für Endanwender **nicht freigegeben** → **E-3 neu zu entscheiden** (Vorschlag: Lesen über SQL-Views, SData nur schreiben).
Keine externe ID (nur per Anpassung), keine FA-Status-Prüfung, keine Reihenfolge-Prüfung im Service; Rückmelde-Grundlagen je Aufruf übergebbar.
Frage Bedarfsprüfung (K-C/Q12) → DPS-Consulting.
