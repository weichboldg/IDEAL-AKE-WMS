// Node-Selbstpruefung des reinen CSV-Kerns (csv-export.js, Spec 2026-10-06-csv-export-alle-tabellen). Aufruf durch
// CsvExportCoverageTests.Kern_NodeSelbstpruefung: node csv-export.check.js <pfad zu csv-export.js>. Exit 0 = alles gruen, sonst Meldung + Exit 1.
'use strict';
const assert = require('assert');
const core = require(process.argv[2]);
const { cleanValue, quoteField, toCsv, fileName } = core;

// Injection (Antwort 5 a): =, +, -, @ am Anfang → ' davor; reine Zahlen im Anzeigeformat bleiben Zahlen.
for (const [input, expected] of [
    ['=1+1', "'=1+1"], ['+43 664 1234567', "'+43 664 1234567"], ['@SUMME(A1:A2)', "'@SUMME(A1:A2)"], ['-x', "'-x"],
    ['=HYPERLINK("http://x";"klick")', '\'=HYPERLINK("http://x";"klick")'],
    ['-5,000', '-5,000'], ['-1.234,500', '-1.234,500'], ['+12', '+12'], ['1.234', '1.234'], ['-', "'-"],
    ['-1.23,4', "'-1.23,4"],
]) assert.strictEqual(cleanValue(input), expected, `cleanValue(${JSON.stringify(input)})`);

// Tab/CR am Anfang: Whitespace wird normalisiert und getrimmt — es bleibt kein gefaehrliches Zeichen vorne stehen.
assert.strictEqual(cleanValue('\t=1'), "'=1");
assert.strictEqual(cleanValue('\r\nText'), 'Text');

// Whitespace + KW-Suffix (Antwort 3): Datum ohne KW, Badges/Zeilenumbrueche auf ein Leerzeichen.
assert.strictEqual(cleanValue('  12.10.2026  KW42 '), '12.10.2026');
assert.strictEqual(cleanValue('12.10.2026\u00a0KW 7'), '12.10.2026');
assert.strictEqual(cleanValue('4711\n   HauptFA'), '4711 HauptFA');
assert.strictEqual(cleanValue('12.10.2026 bis 13.10.2026'), '12.10.2026 bis 13.10.2026');
assert.strictEqual(cleanValue('12.10.2026 07:45 KW42'), '12.10.2026 07:45');
assert.strictEqual(cleanValue(null), '');

// RFC 4180
assert.strictEqual(quoteField('a;b'), '"a;b"');
assert.strictEqual(quoteField('sag "hallo"'), '"sag ""hallo"""');
assert.strictEqual(quoteField('ohne'), 'ohne');

// Format: BOM, ; , CRLF, keine sep=-Zeile
const csv = toCsv(['Artnr.', 'Menge'], [['A;1', '1.234,500'], ['ä ö ü ß –', '-5,000']]);
assert.ok(csv.startsWith('\uFEFFArtnr.;Menge\r\n'), 'BOM + Kopf');
assert.ok(!/^\uFEFF?sep=/.test(csv), 'keine sep=-Zeile');
assert.strictEqual(csv, '\uFEFFArtnr.;Menge\r\n"A;1";1.234,500\r\nä ö ü ß –;-5,000\r\n');
assert.strictEqual(Buffer.from(csv, 'utf8').subarray(0, 3).toString('hex'), 'efbbbf', 'UTF-8-BOM');

// Dateiname <Ansicht>_yyyy-MM-dd_HHmm.csv ohne unzulaessige Zeichen
assert.strictEqual(fileName('ProductionOrders', new Date(2026, 9, 2, 7, 5)), 'ProductionOrders_2026-10-02_0705.csv');
assert.strictEqual(fileName('a/b:c*?"<>|\\', new Date(2026, 0, 1, 0, 0)), 'a_b_c________2026-01-01_0000.csv');
assert.strictEqual(fileName('', new Date(2026, 0, 1, 0, 0)), 'Export_2026-01-01_0000.csv');

console.log('csv-export core ok');
