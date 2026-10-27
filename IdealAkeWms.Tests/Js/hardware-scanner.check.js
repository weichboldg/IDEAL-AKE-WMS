// Node-Selbstpruefung des reinen Kerns HwScanCore (hardware-scanner.js), Spec 2026-10-15-scan-handler-hardware-scanner AK 15.
// Aufruf durch HardwareScannerTests.Kern_NodeSelbstpruefung: node hardware-scanner.check.js <pfad zu hardware-scanner.js>. Exit 0 = gruen.
'use strict';
const assert = require('assert');
const core = require(process.argv[2]);

// Tasten im Abstand dt ab t0 einspeisen; liefert alle Ergebnisse plus einen tick weit hinter der letzten Taste.
function feed(keys, dt, opts) {
    const s = core.create(Object.assign({ prefix: '~09', maxGapMs: 50 }, opts));
    const res = [];
    let t = 1000;
    for (const k of keys) { res.push(s.key(k, t)); t += dt; }
    res.push(s.tick(t + 1000));
    return { res, scans: res.filter(r => r.scan).map(r => r.scan), replay: res.map(r => r.replay || '').join(''), s };
}
let n = 0;
const ok = (cond, msg) => { assert.ok(cond, msg); n++; };

// Praefix erkannt, Code abgetrennt, Enter beendet und wird verschluckt (AK 2, 3)
let r = feed(['~', '0', '9', 'A', '-', '1', 'Enter'], 5);
ok(r.scans.length === 1 && r.scans[0].code === 'A-1' && r.scans[0].endedWithEnter, 'Scan mit Enter');
ok(r.res.slice(0, 7).every(x => x.action !== 'pass'), 'alle Scanner-Tasten abgefangen');
ok(r.replay === '', 'kein Replay bei echtem Scan');

// Tab beendet ebenso, ohne Enter-Kennzeichen
r = feed(['~', '0', '9', '4', '2', 'Tab'], 5);
ok(r.scans[0].code === '42' && !r.scans[0].endedWithEnter && r.res[5].action === 'swallow', 'Tab-Ende');

// Ende per Zeitgrenze ohne Suffix
r = feed(['~', '0', '9', '7', '7'], 5);
ok(r.scans.length === 1 && r.scans[0].code === '77' && !r.scans[0].endedWithEnter, 'Ende per Timeout');

// Shift zwischen den Zeichen (Grossbuchstaben) bricht nicht ab
r = feed(['~', 'Shift', '0', '9', 'Shift', 'X', 'y', 'Enter'], 5);
ok(r.scans[0] && r.scans[0].code === 'Xy', 'Shift unschaedlich');

// Praefix abgelehnt: falsches Folgezeichen -> Replay der gepufferten Zeichen, aktuelle Taste geht durch
r = feed(['~', 'a'], 5);
ok(r.replay === '~' && r.res[1].action === 'pass' && r.scans.length === 0, 'falsches Folgezeichen');

// Langsames Tippen (Abstand > Zeichenabstand): nichts geht verloren, Reihenfolge bleibt (AK 5)
r = feed(['~', '0', '9', 'x'], 200);
ok(r.scans.length === 0, 'langsam = kein Scan');
ok(r.replay === '~', 'nur das erste Praefix-Zeichen war gehalten und kommt zurueck, der Rest geht sofort durch');
const passed = r.res.slice(0, 4).filter(x => x.action === 'pass').length;
ok(passed + r.replay.length === 4, 'jede Taste genau einmal (gehalten+replay oder durchgelassen)');

// Praefix-Zeichen mitten im Text: "a~b" normal getippt
r = feed(['a', '~', 'b'], 120);
ok(r.res[0].action === 'pass' && r.res[1].action === 'hold' && r.res[2].replay === '~' && r.res[2].action === 'pass', 'Praefix-Zeichen im Text');

// Praefix ohne Code + Enter: Praefix kommt zurueck, Enter geht durch
r = feed(['~', '0', '9', 'Enter'], 5);
ok(r.scans.length === 0 && r.replay === '~09' && r.res[3].action === 'pass', 'leerer Scan');

// Zweiter Scan direkt nach dem ersten (Zeitgrenze dazwischen) wird sauber getrennt
const s = core.create({ prefix: '~09', maxGapMs: 50 });
['~', '0', '9', '1'].forEach((k, i) => s.key(k, 10 + i * 5));
const zweiter = s.key('~', 500);
ok(zweiter.scan && zweiter.scan.code === '1' && zweiter.action === 'hold', 'Timeout-Scan beim naechsten Tastendruck geliefert');

// Neuer Praefix-Anfang nach Fehlstart: "~~09" -> erstes ~ zurueck, Rest ist Scan
r = feed(['~', '~', '0', '9', 'Z', 'Enter'], 5);
ok(r.replay === '~' && r.scans[0] && r.scans[0].code === 'Z', 'Fehlstart dann Scan');

// Tastenkuerzel brechen den Kandidaten ab
r = feed(['~', 'Kuerzel'], 5);
ok(r.replay === '~' && r.res[1].action === 'pass', 'Kuerzel');

// Konfigurierbarer Praefix (TS-99 Tottaste: anderer Praefix)
r = feed(['#', '#', '5', 'Enter'], 5, { prefix: '##' });
ok(r.scans[0] && r.scans[0].code === '5', 'anderer Praefix');

// v1.66.1 N-2: Zeichenabstand nur 10..500, sonst Standard 50 - negativ/0/unsinnig schaltet die Erkennung nie still ab
for (const [raw, soll] of [['50', 50], ['80', 80], ['10', 10], ['500', 500], ['9', 50], ['501', 50], ['0', 50], ['-5', 50], ['abc', 50], ['', 50], [null, 50]]) {
    ok(core.abstand(raw) === soll, `abstand(${raw}) = ${soll}`);
}

// v1.67.1 Fokus-Eingabe (Freigabe-Nachtrag 4, Netum): Erkennung am eingefuegten Text
const tx = () => core.createText({ prefix: '~09', maxGapMs: 50 });
let e = core.eingefuegt('ab', 'a~09Xb');
ok(e.pos === 1 && e.text === '~09X' && e.entfernt === 0, 'eingefuegt in der Mitte');
e = core.eingefuegt('abc', 'ac');
ok(e.entfernt === 1 && e.text === '', 'eingefuegt: Loeschung');

// Ganzer Code auf einmal, Enter als eigenes keydown: Feld zurueck auf den Stand davor, Scan erst mit Enter
let c = tx(), x = c.eingabe('', '~09A-1', 1000);
ok(x.action === 'zuruecksetzen' && x.wert === '' && x.pos === 0 && !x.scan && c.aktiv(), 'Text mit Praefix: zuruecksetzen, Scan offen');
x = c.enter();
ok(x.scan && x.scan.code === 'A-1' && x.scan.endedWithEnter && !c.aktiv(), 'Enter beendet Text-Scan');

// Zeilenumbruch im Text beendet sofort
c = tx(); x = c.eingabe('', '~09ABC\n', 1000);
ok(x.action === 'zuruecksetzen' && x.scan && x.scan.code === 'ABC' && x.scan.endedWithEnter, 'Zeilenumbruch = Enter-Suffix');

// Ohne Enter: Zeitgrenze; Text mitten im vorhandenen Wert, Cursor-Position bleibt
c = tx(); x = c.eingabe('x', 'x~0977', 1000);
ok(x.wert === 'x' && x.pos === 1, 'vorhandener Text bleibt, Praefix nie im Feld');
ok(!c.tick(1040).scan, 'vor der Zeitgrenze kein Ende');
x = c.tick(1100);
ok(x.scan && x.scan.code === '77' && !x.scan.endedWithEnter, 'Ende per Zeitgrenze');

// Zeichenweise (keyCode 229, jede Taste ein input): Praefix-Anfang geht durch, ab vollem Praefix wird zurueckgesetzt
c = tx();
ok(c.eingabe('', '~', 1000).action === 'pass' && c.eingabe('~', '~0', 1005).action === 'pass', 'Praefix-Anfang durchgelassen');
x = c.eingabe('~0', '~09', 1010);
ok(x.action === 'zuruecksetzen' && x.wert === '', 'voller Praefix: Feld zurueck auf den Stand vor der Folge');
x = c.eingabe('', 'Q', 1015);
ok(x.action === 'zuruecksetzen' && c.enter().scan.code === 'Q', 'Zeichen nach dem Praefix landen im Code');

// Zwei input-Ereignisse kurz hintereinander (Praefix, dann Code)
c = tx(); c.eingabe('', '~09', 1000); c.eingabe('', 'Z1', 1020);
ok(c.enter().scan.code === 'Z1', 'Code in zweitem Ereignis');

// Langsames Tippen von Hand: nie ein Scan, nie zurueckgesetzt
c = tx();
const langsam = [c.eingabe('', '~', 1000), c.eingabe('~', '~0', 1200), c.eingabe('~0', '~09', 1400), c.eingabe('~09', '~09a', 1600), c.tick(3000)];
ok(langsam.every(y => !y.scan && y.action !== 'zuruecksetzen'), 'Hand-Eingabe ~09 bleibt Text');

// Text ohne Praefix und Praefix ohne Code
c = tx(); ok(c.eingabe('', 'abc', 1000).action === 'pass' && !c.aktiv(), 'Text ohne Praefix');
c = tx(); c.eingabe('ab', 'a~09b', 1000); x = c.enter();
ok(!x.scan && x.zurueck === '~09' && x.pos === 1, 'Praefix ohne Code kommt als Text zurueck');

// Feld waehrend des Scans anders geaendert (Loeschen): Scan endet mit dem bisherigen Code, Eingabe geht durch
c = tx(); c.eingabe('ab', 'ab~09K', 1000); x = c.eingabe('ab', 'a', 1010);
ok(x.action === 'pass' && x.scan && x.scan.code === 'K', 'Loeschen beendet Text-Scan');

// Zeitgrenze verpasst (Timer gedrosselt): naechste Eingabe liefert den alten Scan und geht selbst durch
c = tx(); c.eingabe('', '~095', 1000); x = c.eingabe('', 'n', 2000);
ok(x.action === 'pass' && x.scan && x.scan.code === '5', 'verpasste Zeitgrenze');

// Anderer Praefix
c = core.createText({ prefix: '##', maxGapMs: 50 }); c.eingabe('', '##7', 1000);
ok(c.enter().scan.code === '7', 'Text: anderer Praefix');

// v1.67.2 Scannen ohne Feld: Zuordnung nach Inhalt (Freigabe-Nachtrag 5 Punkt 4)
const lager = c2 => ['A-01', 'HL-3'].indexOf(c2) >= 0;
let z = core.zuordnung('G-KT-FRR-510-2010--1760B-250-V01;;1065141;09', lager);
ok(z.feld === 'artikel' && z.art === 'Artikel-QR', 'mit ; -> Artikel-QR');
ok(core.zuordnung('A-01;x', lager).feld === 'artikel', 'mit ; immer Artikel, auch wenn der erste Teil ein Lagerplatz waere');
z = core.zuordnung('A-01', lager);
ok(z.feld === 'lagerplatz', 'ohne ; und genau ein Lagerplatz-Code -> Lagerplatz');
ok(core.zuordnung(' HL-3 ', lager).feld === 'lagerplatz', 'Rand-Leerzeichen getrimmt');
z = core.zuordnung('A-0', lager);
ok(z.feld === 'artikel' && z.art === 'Artikelnummer', 'nur Anfang eines Lagerplatz-Codes -> Artikelnummer (genau, kein startsWith)');
ok(core.zuordnung('102044', lager).art === 'Artikelnummer', 'unbekannter Code -> Artikelnummer');
ok(core.zuordnung('', lager).feld === null && core.zuordnung('   ', lager).feld === null, 'leer -> nicht zuordenbar');

console.log(`hardware-scanner core ok (${n} Pruefungen)`);
