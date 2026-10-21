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

console.log(`hardware-scanner core ok (${n} Pruefungen)`);
