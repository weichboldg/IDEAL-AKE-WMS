// Node-Lauf der gemeinsamen Testtabelle column-filter-cases.json gegen den JS-Kern (column-filter-syntax.js), Spec
// 2026-09-23-suchsyntax-spaltenfilter-erweitern. Aufruf durch ColumnFilterSyntaxTests.Kern_NodeSelbstpruefung:
// node column-filter-syntax.check.js <pfad zu column-filter-syntax.js> <pfad zu column-filter-cases.json>. Exit 0 = gruen.
'use strict';
const assert = require('assert');
const core = require(process.argv[2]);
const cases = JSON.parse(require('fs').readFileSync(process.argv[3], 'utf8'));

let n = 0;
for (const c of cases) {
    const parsed = core.parse(c.filter.toLowerCase(), c.type);
    assert.strictEqual(core.matches(c.cell, parsed, c.type), c.match, `${c.id}: matches`);
    assert.strictEqual(parsed.invalid.length > 0, !!c.invalid, `${c.id}: invalid`);
    n++;
}
assert.ok(n >= 50, 'Testtabelle zu klein');
console.log(`column-filter-syntax core ok (${n} Faelle)`);
