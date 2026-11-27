---
typ: notiz
status: offen
created: 2026-11-17
bezug: []
---
# Jira als Meldeweg und Ort für Rückfragen an den Fachbereich

## Stand 2026-11-17
- Jira-Projekt **WMS „IDEAL-AKE-WMS“** (bis 19.11. Schlüssel KAN) (team-managed), Lead Gerald Weichbold. Status: Zu erledigen → In Arbeit →
  In Überprüfung → Erledigt.
- Bereichs-Epics angelegt: WMS-1 Kommissionierung, WMS-2 Lagerbestellung und Bedarfsmeldung, WMS-3 Wareneingang
  und offene Bestellungen, WMS-4 Einkauf und Bestellvorschläge, WMS-5 Lagerbuchungen und Bestände, WMS-6 Produktion
  und BDE, WMS-7 Anmeldung, Benutzer und Rechte, WMS-8 Sage-Schnittstelle und Betrieb.
- Anleitung für Kollegen: Confluence AKE-Dokumentation → WMS Anwender-Anleitungen → „WMS – Fehler melden und
  Wünsche einreichen“ (Seite 683933697, direkt in Confluence gepflegt, nicht aus docs/).
- Spec-Vorlage `_templates/spec.md`: Frontmatter `jira`, Kennzeichnung „(Fachbereich: Name)“ bei offenen
  Rückfragen, neuer Abschnitt „ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)“ vor dem Freigabe-Block.

## Ablauf (Gerald 2026-11-17)
- **Benjamin Andl-Braun trägt die Meldungen in Jira ein** und füllt alles aus, auch für andere Kollegen
  (z. B. Walter Kronberger). Je Meldung: Gemeldet von (Person) und Abteilung.
- Koordination prüft regelmäßig auf neue Einträge in WMS, spezifiziert und holt Rückfragen bei Benjamin
  Andl-Braun oder Gerald Berger.

## Ablauf Rückfragen
1. spec-agent schreibt die Spec; Fragen an den Fachbereich tragen „(Fachbereich: Name)“.
2. **Schritt 2 macht der geplante Spec-Lauf** (Windows-Aufgabe „WMS Spec-Lauf Backlog“, Prompt
   [[spec-lauf-backlog]], seit 2026-11-19): Hat die Backlog-Notiz `jira: WMS-<n>`, setzt er `jira:` in der Spec,
   stellt die gekennzeichneten Fragen als **einen** Kommentar mit @Erwähnung (nummeriert wie in der Spec) und setzt
   den Vorgang auf **RUECKFRAGE** (ohne Fachbereichs-Fragen auf IN ARBEIT). Die Erwähnung löst die E-Mail aus.
   Ohne Jira-Referenz bleiben die Fragen nur in der Spec; einen Vorgang legt die Koordination nur auf Zuruf an.
3. Der Fachbereich antwortet als Kommentar im Vorgang.
4. Koordination prüft die Kommentare, trägt die Antworten wortgetreu in den Antwort-Abschnitt der Spec ein
   (Name, Datum, Link) und entfernt das Label. Widerspricht eine Antwort dem Code oder einer Entscheidung: als
   neue offene Rückfrage an Gerald.
5. Gerald entscheidet im Freigabe-Block („wie Fachbereich“ oder abweichend). Danach Nacharbeit über den
   spec-agent (REWORK MODE liest „ANTWORTEN auf …“-Abschnitte).

## Beteiligte (Jira-Konten)
| Person | Account-ID | Zugang 2026-11-17 |
|---|---|---|
| Benjamin Andl-Braun | 712020:a177451e-9fa1-46b8-b9b2-bfe7602cd32f | nur Confluence (Gruppe confluence-users-ideal-ake), **kein Jira** |
| Gerald Berger | 712020:8ffb4521-8201-423d-aca7-0d991ef9b74c | nur Confluence, **kein Jira** |

## Offen (Gerald)
- [ ] Benjamin Andl-Braun und Gerald Berger Jira-Zugang geben (admin.atlassian.com → Produktzugriff Jira) und
      im Projekt WMS berechtigen. Ohne Jira-Zugang kommen @Erwähnungen nicht an und die Kollegen können keine
      Meldung anlegen.
- [x] (19.11.) Status **RUECKFRAGE** angelegt: ZU ERLEDIGEN → RUECKFRAGE, RUECKFRAGE → IN ARBEIT („geklaert“), alle Status „Beliebig“ erreichbar; ersetzt das Label `rueckfrage-offen`. Ursprünglich: Optional: Status „Neu“ vor „Zu erledigen“ und Status „Rückfrage“ (wartet auf Fachbereich) im
      Projekt-Workflow ergänzen; dann ersetzt der Status das Label `rueckfrage-offen`.
- [x] Mit „gerald“ ist Gerald Berger gemeint (bestätigt 2026-11-17).
- [ ] Felder im Projekt WMS anlegen (team-managed, nur über die Oberfläche): **Gemeldet von** (Kurztext –
      kein Personenfeld, die Melder haben meist kein Jira-Konto) und **Abteilung** (Auswahlliste). Bis dahin
      stehen beide als erste Zeilen in der Beschreibung (Vorlagen auf der Confluence-Seite).
- [ ] Danach (Gerald 2026-11-17): wiederkehrende Aufgabe für die Jira-Sichtung einrichten – neue Einträge in WMS
      sichten, beantwortete Rückfragen in die Specs übernehmen, kurze Liste an Gerald. Erst wenn Zugang und
      Felder stehen.
- [ ] spec-agent-Anweisung (`.claude/agents/spec-agent.md`) um die Kennzeichnung ergänzen – nur auf
      ausdrücklichen Auftrag; die Vorlage trägt die Regel bereits als Kommentar.

## Stand 2026-11-19 12:05 (Gerald, Screenshots)
- Projektschlüssel **KAN → WMS** (per Jira geprüft: WMS-1 „Bereich: Kommissionierung“, Projekt IDEAL-AKE-WMS). Confluence-Seite 683933697 nennt noch KAN-1…8 → anpassen.
- Workflow mit Status RUECKFRAGE eingerichtet (siehe oben).
- Benachrichtigungen: alle Ereignisse an zugewiesene Person, Beobachter, Autor (Erwähnungen schickt Jira unabhängig davon).
- Erledigt 12:12–13:40: Zugang Benjamin + Gerald Berger (beide im Projekt zuweisbar), Felder `Gemeldet_von` (customfield_10108, Text) und `Abteilung` (customfield_10109, Auswahl Produktion/Einkauf/Lager/Kommissionierung/Versand/Blechfertigung) bei Bug/Feature/Story/Task, Confluence-Seite Version 4 angepasst, Testvorgang WMS-9 von Gerald Berger angelegt, @-Erwähnung kam per Mail an.
- Offen: Jira-Sichtung als wiederkehrende Aufgabe (Takt mit Gerald klären); Pflichtfelder optional.
