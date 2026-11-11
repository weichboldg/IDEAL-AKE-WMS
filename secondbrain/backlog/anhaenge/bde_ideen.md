1.	EINLEITUNG
Das vorliegende Lastenheft beschreibt die Anforderungen und Rahmenbedingungen für das IT-Projekt Einführung BDE-System. Es bildet die Grundlage für die Entwicklung des Pflichtenhefts sowie für die spätere Realisierung und Qualitätssicherung des Systems. Das Lastenheft richtet sich an alle Projektbeteiligten wie Auftraggeber*innen, IT-Dienstleister*innen sowie Nutzer*innen.
2.	ZIELSETZUNG UND PROJEKTBESCHREIBUNG
Ziel ist die Erfassung (und spätere Auswertung) von Produktionsschritten und –Zeiten, sowie die PPS (Terminierung/Leitstand). Als Buchungs-Basis dienen in beiden Häusern Stücklisten.
System soll Meldebestände berücksichtigen.

3.	BEGRIFFLICHKEITEN
BDE-System:
Eine Software zur Erfassung von Start-Stop-Zeiten

MDE-System:
Eine Software zum Ein- und Auslagern von Teilen

MDE-Geräte:
Mobile Daten Erfassungsgeräte. Smartphoneähnlich mit Barcode u/od QR-Scannern

Terminal:
Ein Rechner mit vollwertigem (Touch-)Monitor und Handgeräten für Barcode u/od QR-Scannen

Stückliste:
Eine Auflistung von Material (Blechteile/Baugruppen, Zukaufteile), sowie Arbeitsgänge und SOLL-Zeiten

Rückmelden: 
in sehr kurzen Arbeitsschritten möchten wir nur “rückmelden”. Also bspw nur einen Barcode zu scannen, der auf einem Fertigungsteil angebracht ist. Für den Scanner ist der Arbeitsplatz hinterlegt.
Dadurch benötigt man nur einen Scan. Daraus sieht man wann und wo ein Teil ist/war und im System soll die SOLL-Zeit als IST-Zeit eingetragen werden. (Dann sind zb spätere Kalkulationsbetrachtungen korrekt)
4.	IST-ANALYSE
4.1	Allgemeines
Standort IDEAL und AKE haben jeweils eine eigens installierte SAGE100-ERP System.


4.2	 Ideal
Aktuell wird nur rückgemeldet:
Barcodes werden gescannt und die Erfassung wird dann als Status abgespeichert (also keine Historie; dh wenn ein Teil eines fast fertigen Auftrags nochmals in der Blechfertigung gescannt wird, weil er neu produziert wird, scheint der Auftrag wieder als Status ‘Blechfertigung’ auf) (‘Herzog’- Lösung)
Außerdem wird gerade eine manuelle Zeitenerfassung getestet. (Eintrag in ein Blatt, das wird dann gesammelt und in eine Excel eingetragen)

Hardware: 
simple Barcode-Scanner und ältere Zebras (ca. 30) 

PPS: 
Excel-Lösung Rainer Kopp

Stückliste:
Kommt aus Excel von Konstruktion, inhaltlich gut; Arbeitsgänge fehlen noch.

4.3	AKE
Blechfertigung wird übers Oseon gesteuert.
Lasern und Stanzen wird automatisch rückgemeldet.
Arbeitspläne sind vorhanden. Kommen aus dem Vault (bidirektional)

Hardware: 
aktuell nur große WinCE-Geräte in Logistik

PPS: 
Vertrieb plant Aufträge ein. Zieht Infos aus Auslastungs/Kapazitäten Excels (werden von AV befüllt)

Stückliste:
Kommt aus Vault von Konstruktion direkt ins SAGE, inhaltlich muss noch nachgebessert werden (auf Montage-Prozess optimiert); Arbeitsgänge sind vorhanden, müssen aber noch mittels Anpassung Schnittstelle ins SAGE gebracht werden; Arbeitszeiten fehlen noch.


5.	SOLL-KONZEPT
ZIEL: Ideal und AKE arbeiten nach demselben Prozess, gegebenenfalls mit geringfügigen Abweichungen.

Produktionsplanung und Betriebsdatenerfassung an den Standorten der IDEAL und AKE.

Um manuelle Scan-Aufwand zu minimieren, sollen wo möglich, Maschinen angebunden werden. Eventuell auch Schnittstellen zu anderen Systemen wie zb. Oseon, Wicam, etc..


Blechfertigung: 
Am Standort AKE folgende Systeme eingesetzt:
	 Oseon
Am Standort IDEAL werden folgende System eingesetzt:
WiCAM


Die Datenerfassung soll in allen Produktionsabteilungen stattfinden.:

1. Stanzen soll nicht mittel BDE-System Start-Stop erfassen 
Ideal: macht das WiCAM bereits, muss dann per Schnittstelle ins BDE bzw SAGE übertragen werden
AKE: Oseon leistet das bereits, muss dann per Schnittstelle ins BDE bzw SAGE übertragen werden
2. Kanten zuviel Scan-Aufwand? Lösung evt über Sammelauftrag
Idee: Teile scannen und auf “Sammelpalette” buchen, dann die Sammelpalette Start-Stop
3. Punkten 
Ideal: soll Start-Stop erfasst werden
AKE: soll Start-Stop erfasst werden
4. Schweißen 
Ideal: soll Start-Stop erfasst werden (Reinigen wird nicht erfasst, geht über Leistungsgrad)
AKE: manuelles Schweißen wird manuell gescannt, Laserschweißen könnte evt angebunden werden
5. Montage
Ideal: soll erfasst werden (Isolieren, Löten, Endmontage, Endprüfung(?)) (
AKE: alle Arbeitsbereiche (40-60 Arbeitsplätze)


Hardware: 

MDE-Geräte (vergl Zebra) bevorzugt (Soll Display und Touch bieten, Android OS)
Zusätzlich sind Mischbetrieb mit Terminals erwünscht (je nach Arbeitsplatz Anforderung)
Die Erfassung soll so wenig Aufwand wie möglich verursachen

Stückliste:
Sollen belastbare Baugruppen bieten, Arbeitsgänge und –Zeiten, Lesbarkeit (Baumstruktur)

PPS:
Auftragslisten/Arbeitsvorrat für jeden Arbeitsplatz, Reihung nach Fertigstellungstermin
Reihenfolge optimiert auf möglichst wenig Rüstzeiten
Grob- und Feinplanung
Grafische Aufbereitung inkl Fortschrittsanzeige


Reporting:
-Rückgemeldete Zeit /Anwesende Zeit
-SOLL-IST Vergleich
-Kapazitätsplanung
-Termintreue (wo entsteht Rückstand)
-Ausschuss-Teile (Gutmenge passt nicht zu SOLL)
-Verschnitt-Quoten (aus Oseon/Wicam)
-Maschinenstillstands Zeiten (evt in Pkt1)
-jeweils historische Vergleichswerte

Weitere Anforderungen:
 -Transportwege automatisch/manuell berücksichtigen (Transportlisten als Resultat)
 -Simples Rückmelden soll möglich sein: SOLL- wird zur IST-Zeit
 -Mehrzeiten sollen begründbar sein (im BDE auswählbar)
 -Dokumente für Produktion an Terminals verfügbar machen
 -Mehrere Mitarbeiter am gleichen Arbeitsgang
 -Chargenerfassung/-Rückverfolgbarkeit
 -First-in-First-Out Hinweis bei mehreren Lagerplätzen
 -Anzeige für Mitarbeiter - Priorisierte Aufträge
 -Sammelauftrag
 -Anbindung Biege-Programm (für Rüstzeitenoptimierung)
 -Etikettendruck per BDE beim Einlagern von Baugruppen, und sonst Fertigungspapiere am Beginn aus SAGE oder OSeon/Wicam
 -Teileverfolgung

-nice to Have: 
Revisionierung von Artikelnr 
Qualifikationsmatrix
6.	ANFORDERUNGEN
5.1 Funktionale Anforderungen
Diese Anforderungen beschreiben, welche Funktionen das System erfüllen muss.

Nr.	Anforderung	Beschreibung	Priorität	Akzeptanzkriterien
1	Erfassung SOLL-Zeiten	Das BDE muss Start und Stop-Zeit erfassen 	Hoch	
2	Teileverfolgung	Wir wollen jederzeit sehen können, wo Teile für den Fertigungsauftrag liegen. Dafür wird eine Auswertung auf Lagerplätze und Arbeitsschritte benötigt.		
3	Erfasste Zeiten von Maschinen übernehmen	Zeiten, die von Maschinen erfasst wurden, werden aus Oseon bzw WiCam exportiert		
4	Erfassung manuelle Arbeitsbereiche	Manuelle Arbeitsbereiche müssen mit Hardware zur Erfassung von Start-Stop ausgerüstet werden		
5	Hardware	Je nach Arbeitsplatzanforderung sollen Terminals u/o MDE-Geräten eingesetzt werden		
6	PPS für Grob- und Feinplanung	Die gewonnen Daten sollen in einer PPS zusammengeführt werden		
7	BDE - Mehrzeiten	Mehrzeiten und Gründe sollen vom Arbeiter eingetragen werden könne		
8	BDE – mehrerer Arbeiter	Das BDE muss mehrere Arbeiter auf den Arbeitsgang buchen lassen können		
9	BDE - Sammelauftrag	Das BDE muss Sammelaufträge verwerten können. Dh entweder werden vorher mehrere Teile zu einem Sammelauftrag zusammengefasst und dann nur der Sammelauftrag gescannt, oder es werden mehrere Teile gleichzeitig bearbeitet (und dann später die Zeit auf die Anzahl der Teile aufgeteilt).		
10	BDE - Etikettendruck	Für Fertigungsteile, die auf Lager gelegt werden, soll nach Scannen des letzten Arbeitsschritts (=Fertigmeldung der Baugruppe) ein Etikett gedruckt werden. Damit wird es (per MDE-Software) eingelagert		
11	BDE – priorisierte Aufträge	Zusätzlich zur Reihung nach Fertigungstermin soll dem MA auch eine Priorisierung angezeigt werden		
12	BDE - Rückmelden	Es soll möglich sein in bestimmten Arbeitsbereichen einen Barcode nur einmal zu scannen, damit das System weiß, dass der Teil hier bearbeitet wurde. Der Teil soll dann ‘fertig gemeldet’ sein. Dh. Er ist vom zb von dem Werkbanklager abgebucht. 
Die SOLL-Zeit wurde als IST-Zeit verbucht (für Nachkalkulationen) und auch die Teileverfolgung soll wissen, dass dieser Teil hier ist/war.		
13	Stücklisten als Basis	Stücklisten werden von der Konstruktion gepflegt und per Schnittstelle eingespielt.
Die Stücklisten beinhalten Fertigungsteile/Baugruppen, Zukaufteile, sowie Arbeitsgänge und SOLL-Arbeitszeiten		
14	Fertigungsgrad	Mit den Informationen aus Pkt 14 soll eine Aufsummierug möglich sein, zu wieviel Prozent ein FA fertig gestellt ist		
15	Chargenerfassung/Rückverfolgbarkeit	Bei manchen Zukauf-Teilen möchten wir Chargennummern führen. Wenn ein Problem mit einer Charge auftaucht, möchten wir wissen welche Geräte dieses Teil verbaut hatten und an welche Kunden sie gingen.		
16	First-In-First-Out bei mehreren Lagerplätzen	Die Kommisionierung soll immer auf den ältesten Lagerstand angewiesen werden.		
17	Transportlisten	Für die Kommisionierung und den Transport der Teile zwischen den Arbeitsbereichen soll es eine Liste geben, welche Teile von wo, wann, wo hin geliefert werden müssen.		
18	Rüstzeiten Optimierung	Um Rüstzeiten gering zu halten, soll in der PPS eine Gliederung nach Werkzeug möglich sein. Dafür wird es Informationen aus dem Biege-Programm benötigen, damit es weiß, welche Teile welche Werkzeuge benötigen.		
19	Dokumente auf Terminals 	Auf den Terminals sollen zusätzliche Informationen abrufbar sein: Arbeitsanweisungen, Installationsanleitungen, 3D-Zeichnungen,...		
20	Revisionierung von Artikelnummern	Nachträgliche Änderungen an bestehenden Fertigungsaufträgen sollen möglich und nachvollziehbar sein. (Beispielsweise Änderung normales Glas auf Spiegelglas, wer hat was wann geändert?)		
21	Qualifikationsmatrix	Es soll eine Übersicht geben, welcher Mitarbeiter, für welche Arbeitsbereiche qualifiziert ist		
22	Verbräuche nicht in Stückliste	Es muss möglich sein, Material zu verbrauchen, auch wenn sie nicht in der Stückliste enthalten sind.		




5.2 Nicht-funktionale Anforderungen
Hier werden Anforderungen an Qualität, Sicherheit und Leistung beschrieben.

Nr.	Anforderung	Beschreibung	Priorität	Akzeptanzkriterien
1	Grafische Aufbereitung PPS	Die PPS soll Dinge wie Fertigungsfortschritt, Arbeitsplatz-Auslastung, und ggfs eine Umplanung (Drag-and-Drop) grafisch anzeigen können.	Hoch	
2				

5.3 Schnittstellen
Hier sind Schnittstellen zu anderen Systemen und Anwendungen zu beschreiben.
•	ERP-System: Das neue System muss Daten mit dem bestehenden ERP-System austauschen können.
•	Mail-System: Versand von Benachrichtigungen an Kund*innen und interne Nutzer*innen via E-Mail.
Nr.	Anforderung	Beschreibung	Priorität	Akzeptanzkriterien
1	BDE <-> Sage	SAGE muss Informationen vom BDE erhalten und das BDE muss Buchungen zurückspielen	Hoch	
2.1	IDEAL:
Excel-Stücklisten Importer ->SAGE	Bei Ideal werden Stücklisten in Excel-Format ins SAGE eingespielt	Mittel	
2.2	AKE:
Vault <-> SAGE	Bei AKE werden Stücklisten im SAGE angelegt, sobald ein Gerät in Vault erstellt wurde.
Fehlende Felder: Arbeitsgänge und SOLL-Zeiten		
3.1	IDEAL:
WICAM -> SAGE	Teile, die im WICAM angelegt werden, müssen ins SAGE gelangen		
3.2	IDEAL:
WICAM -> BDE	Die Start-Stop Buchungen müssen ins BDE kommen		
3.3.	AKE:
Oseon <-> SAGE	Schnittstelle existiert bereits.		
3.4	AKE:
Oseon -> BDE	Die Start-Stop Buchungen müssen ins BDE kommen		
4.	Biegeprogramme sollen Werkzeug-Informationen für PPS liefern	In beiden Häusern werden die Informationen benötigt, welches Werkzeug für welches Teil benötigt wird		
5.	Schnittstelle zum DMS-System	TBD
Mögliche Bereiche:
-Fertigungsaufträge
-Workflow
-Etikettendruck		
6.	Schnittstelle Lagerlift	TBD
Notwendig für MDE AKE.
Anforderung BDE Ideal und AKE muss noch geklärt werden		
				
