---
type: aufgabe
status: erledigt
created: 2026-11-25
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-25: Beschichtungsauftrag-Bugfix, Sage-Buchungen Gesamtsicht, Sage-Referenz

Auftrag Gerald 2026-11-25 („passt“): Reihenfolge Bugfix → Gesamtsicht → Referenz. Alle drei Freigegeben
(Gerald 2026-11-24 / 2026-11-25). Start-Stand Worktree: v1.99.64, höchstes SQL 166, höchstes TS 188.

| Spec | Typ | SQL-Block | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]] | Spec (Bugfix) | – | 1.99.65 | **Testbereit** | 8ffa4c4a, QA 186e63dd | |
| [[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]] Nachtrag (VaterFA, kein Übergeben mit 0 Pos.) | Nachtrag | – | 1.99.67 | **Testbereit** | fcc894c6, QA 2b4dceaf | |
| [[2026-11-23-sage-buchungen-gesamtsicht-spec]] | Spec | – | 1.99.66 | **Testbereit** | 0725fc3e, Foto 2c20cfea, QA 56851a66 | |
| v1.99.69 Nachtrag 2 Beschichtung (N-1 leerer Entwurf) + Gesamtsicht B3 | Nachtrag | – | 1.99.69 | **Testbereit** | eff16911, QA 6b672073 | |
| [[2026-11-24-sage-buchung-referenz-benutzer-abteilung-spec]] | Spec | 167 | 1.99.68 | **Testbereit** | 19244bfa, QA 6b672073 | P-2 Sage-Testsystem (Mensch) |

## Vorprüfung
- Alle drei: status Freigegeben, worktree/branch = Bündel, Freigabe-Antworten vollständig, kein Epic.
- Worktree sauber (keine uncommitteten Änderungen) bei Start.

## Protokoll
- Bugfix v1.99.65: Umsetzung 8ffa4c4a, Build 0 Fehler, Tests Web 3504/1 skip/0 fail, Service 790/0. Abweichungen vom Rumpf wegen Antworten R-1/R-2 (keine harte Übergeben-Sperre, Entfernen in Übergeben für offene). Befund F1: „entfernen“ bei nur offenen Positionen → Übergeben mit 0 Positionen (erlaubt, Versendet sperrt) – Gerald fragen.
- Bugfix v1.99.65 QA bestanden (186e63dd): DEV AKE+IDEAL v1.99.65, Befunde B-1 (Positionsbezeichnung an IDEAL nicht eindeutig, VaterFA?) und B-2 (Fenster-Knopf bleibt bei Browser-Validierung gesperrt) niedrig, offen für Gerald.
- Gesamtsicht v1.99.66: Umsetzung 0725fc3e, Build 0 Fehler, Tests Web 3519/1 skip/0, Service 790/0. TS-189.1–189.14. Abweichungen: Einkauf sieht WE-Zeilen nur mit EinkaufAktiv (wie alte Liste); Herkunft-Filter ohne WE-Recht ignoriert; Foto 05-05 noch alt.
- Gerald 2026-11-25: B-1 ja (VaterFA), F1 ablehnen/stornieren statt 0 Positionen → FREIGABE-NACHTRAG, Bau als v1.99.67 nach der Gesamtsicht-QA; Referenz rückt auf v1.99.68.
- Gesamtsicht v1.99.66 QA bestanden (56851a66): DEV beide v1.99.66; B2 fehlendes Leerzeichen Intro (wird in v1.99.67 mitbehoben), B3 Einkauf/Admin sieht WE-Zeilen ohne Modul (Spec F7/12a, Gerald entscheidet). Nicht möglich: Erneut senden (kein Sage-POST), 189.9/189.12 (zweiter User), 390 px.
- Nachtrag v1.99.67: fcc894c6, Build 0 Fehler, Tests Web 3530/1 skip/0, Service 790/0, TS-162.26–162.29; B2-Leerzeichen Gesamtsicht mitbehoben.
- Nachtrag v1.99.67 QA bestanden (2b4dceaf). Befund N-1 (mittel, Altbestand): leerer Entwurf lässt sich nicht absenden („ungültiger Wert in .“), vermutlich Dictionary-Binding `mengen` – Gerald entscheidet. DEV IDEAL BA-8 Start Beschichtung versehentlich auf 12.07.2026.
- Gerald 2026-11-25: N-1 beheben → FREIGABE-NACHTRAG 2, Bau als v1.99.69 nach der Referenz.
- Gerald 2026-11-25: Gesamtsicht B3 → WE-Zeilen auch für Einkauf nur mit Modul, in v1.99.69 mit N-1.
- Referenz v1.99.68: 19244bfa, SQL/167 + Migration 20261125134408_AddStockMovementAbteilung, TS-190, Build 0 Fehler, Tests Web 3536/1 skip/0, Service 798/0. Offen P-2: übernimmt Sage die gesendete Zeilen-Referenz / zeigt es Zeilen-Memo? (nur Mensch, Testsystem).
- v1.99.69 (N-1 leerer Entwurf + Gesamtsicht B3) wird vor der gemeinsamen QA 68+69 gebaut.
- v1.99.69: eff16911, Root Cause N-1 belegt (Dictionary<int,…>-Binder fällt ohne `mengen[..]` auf leeres Präfix → ModelState-Fehler unter ""), Fix [Bind(Prefix="mengen")]; B3 zeigtWe = Modul && (stockRead || Einkauf). Tests Web 3539/1/0, Service 798/0. Gleiches Muster latent in StockMovementsController.OutboundAllConfirm (stueckGezeigt, ModelState ungeprüft) – nur gemeldet. QA 68+69 läuft.
- QA 68+69 (6b672073): alle Teile Testbereit, DEV AKE+IDEAL v1.99.69, SQL/167 auf beiden DEV-DBs. Nichts an Sage gesendet (DEV: SageLagerbuchungAktiv/SageWareneingangBelegAktiv false).

## Paket 19 (Definition of Done)
- `publish\paket19-v1.99.69.zip` (Commit eff16911): **237 120 043 Bytes**, SHA-256 `237FC701015B0A4ED6D61833F30BE8AF80EAFA54C9ED63300B6CC15B1EEF5A05`.
- `\\AKENET01.ake.at\Installsourcen`: 237 120 043, SHA-256 gleich – OK.
- `\\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen`: 237 120 043, SHA-256 gleich – OK.
- Deploy: Web + Service + SQL/167 (Update-Skript, `nachlauf.psd1`: SQL/167 Schema, Infos v1.99.66-SageBuchungenInfo, v1.99.68-SageReferenzInfo). Dienst erst nach dem Schema starten.

## Doku / Confluence (nur Kapitelliste für spätere Übernahme)
- `docs/anwender`: README (Stand 1.99.69), 05 Wareneingang zu Bestellung (Foto 05-05 neu), 09 Einbuchen/Ausbuchen/Umbuchen, 15 Beschichtungsauftrag, 18.
- `docs/betrieb`: 00-index (Stand), 03 Installation/Update/Rollback (§5.2a SQL/167), 04, 05 Schnittstellen, 06, 07, 08, 10.
- Fotos: 05-05 gegen DEV neu; **fehlen noch**: Übergeben-Fenster Beschichtungsauftrag, Abteilungs-Auswahl Ausbuchung (Screenshots liefen im Lauf ins Zeitlimit) → Folgearbeit.

## Offen für Gerald (Schranke 2)
- P-2 auf dem Sage-Testsystem (Referenz/Zeilen-Memo/WE-Memo sichtbar?) – Testanleitung im Chat 2026-11-25, TS-190.
- Manual-UAT-Checklisten in den drei Specs; Einkauf-Menü „Sage-Wareneingänge (offen)“ bei Modul aus (B3-H1) entscheiden.
- DEV-Testdaten: AKE Bestand −2 auf QA173-L (00193009, 00193050); IDEAL BA-8 Start Beschichtung 12.07.2026.
- Kein Merge, kein Push.
