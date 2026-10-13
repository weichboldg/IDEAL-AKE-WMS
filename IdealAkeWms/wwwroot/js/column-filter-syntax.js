// Spaltenfilter-Suchsyntax (Spec 2026-09-23-suchsyntax-spaltenfilter-erweitern, Etappe A, v1.58.0) — JS-Spiegel des C#-Kerns
// IdealAkeWms/Services/ColumnFilterSyntax.cs. parse/matches sind reiner Kern ohne DOM; darunter die gemeinsamen Browser-Hilfen
// (Hilfe-Knopf, Kurztext, Rot-Markierung) fuer alle Filterzeilen. Beide Kerne pruefen gegen
// IdealAkeWms.Tests/Js/column-filter-cases.json (C#-Theory + column-filter-syntax.check.js) — Aenderungen IMMER an beiden.
//   7% beginnt · %7 endet · =x genau · ; oder (Text auch ,) · ! nicht (alle Teile) · (leer)
//   Zahl/Datum: >x >=x <x <=x, a...b (inklusive, vertauscht egal); = mit lesbarem Wert vergleicht den Wert
(function (root) {
    'use strict';

    var CELL_NUMBER = /^-?[\d.]*\d(,\d+)?/;
    var FULL_NUMBER = /^-?[\d.]*\d(,\d+)?$/;
    var CELL_DATE = /^(\d{1,2})\.(\d{1,2})\.(\d{2}|\d{4})(?!\d)/;
    var OPS = [['>=', 'ge'], ['<=', 'le'], ['>', 'gt'], ['<', 'lt']];

    function dateKey(m) {
        var d = parseInt(m[1], 10), mo = parseInt(m[2], 10), y = parseInt(m[3], 10);
        if (y < 100) y += 2000;
        if (mo < 1 || mo > 12 || d < 1 || d > new Date(y, mo, 0).getDate()) return null;
        return y * 10000 + mo * 100 + d;
    }

    function toNumber(s) { return parseFloat(s.replace(/\./g, '').replace(',', '.')); }

    function parseValue(s, type) {
        if (type === 'date') {
            var m = s.match(CELL_DATE);
            return m && m[0].length === s.length ? dateKey(m) : null;
        }
        return FULL_NUMBER.test(s) ? toNumber(s) : null;
    }

    function cellValue(cell, type) {
        var s = cell.trim();
        if (type === 'date') { var m = s.match(CELL_DATE); return m ? dateKey(m) : null; }
        var n = s.match(CELL_NUMBER);
        return n ? toNumber(n[0]) : null;
    }

    function unlesbar(type) { return type === 'date' ? 'Datum nicht lesbar (TT.MM.JJJJ)' : 'Zahl nicht lesbar'; }

    // Liefert { part } | { error } | {} (wirkungslos).
    function parsePart(t, type) {
        if (t === '(leer)') return { part: { op: 'empty', value: t } };
        for (var i = 0; i < OPS.length; i++) {
            if (t.indexOf(OPS[i][0]) !== 0) continue;
            if (type === 'text') return { error: 'Vergleich nur in Zahl- und Datumsspalten' };
            var v = parseValue(t.substring(OPS[i][0].length).trim(), type);
            return v === null ? { error: unlesbar(type) } : { part: { op: OPS[i][1], value: t, low: v, high: v } };
        }
        if (t.charAt(0) === '=') {
            var rest = t.substring(1).trim();
            if (!rest) return {};
            var ev = type === 'text' ? null : parseValue(rest, type);
            return { part: { op: 'eq', value: rest, low: ev, high: ev } };
        }
        var r = t.indexOf('...');
        if (r >= 0) {
            if (type === 'text') return { error: 'Bereich nur in Zahl- und Datumsspalten' };
            var a = parseValue(t.substring(0, r).trim(), type), b = parseValue(t.substring(r + 3).trim(), type);
            if (a === null || b === null) return { error: unlesbar(type) };
            return { part: { op: 'range', value: t, low: Math.min(a, b), high: Math.max(a, b) } };
        }
        var starts = t.charAt(t.length - 1) === '%', ends = t.charAt(0) === '%';
        var value = t.replace(/^%+|%+$/g, '').trim();
        if (!value) return {};
        if (ends && !starts) return { part: { op: 'ends', value: value } };
        if (starts && !ends) return { part: { op: 'starts', value: value } };
        return { part: { op: 'contains', value: ends ? value : t } };
    }

    function parse(raw, type) {
        type = type || 'text';
        var trimmed = (raw == null ? '' : String(raw)).trim();
        var result = { negate: false, parts: [], invalid: [] };
        if (!trimmed) return result;
        result.negate = trimmed.charAt(0) === '!';
        var payload = result.negate ? trimmed.substring(1) : trimmed;
        var sep = type === 'text' ? /[;,]/ : /;/;
        payload.split(sep).forEach(function (token) {
            var tok = token.trim();
            if (!tok) return;
            var res = parsePart(tok.toLowerCase(), type);
            if (res.error) result.invalid.push(tok + ': ' + res.error);
            else if (res.part) result.parts.push(res.part);
        });
        return result;
    }

    function matches(cell, parsed, type) {
        type = type || 'text';
        if (!parsed.parts.length) return true;
        var text = (cell == null ? '' : String(cell)).toLowerCase();
        var trimmed = text.trim();
        var hit = parsed.parts.some(function (p) {
            switch (p.op) {
                case 'empty': return trimmed.length === 0;
                case 'contains': return text.indexOf(p.value) !== -1;
                case 'starts': return trimmed.indexOf(p.value) === 0;
                case 'ends': return trimmed.length >= p.value.length && trimmed.lastIndexOf(p.value) === trimmed.length - p.value.length;
                case 'eq': return p.low != null ? cellValue(text, type) === p.low : trimmed === p.value;
            }
            var c = cellValue(text, type);
            if (c === null) return false;
            switch (p.op) {
                case 'gt': return c > p.low;
                case 'ge': return c >= p.low;
                case 'lt': return c < p.low;
                case 'le': return c <= p.low;
                default: return c >= p.low && c <= p.high; // range
            }
        });
        return parsed.negate ? !hit : hit;
    }


    // ----- Browser-Hilfen (DOM) — gemeinsam fuer table-filter.js, Stueckliste und FA-Baum: EINE Hilfe, EINE Markierung -----
    var HINT_ID = 'colFilterSyntaxHint';

    function ensureHint() {
        if (!document.getElementById(HINT_ID)) {
            var hint = document.createElement('span');
            hint.id = HINT_ID;
            hint.className = 'visually-hidden';
            hint.textContent = 'Filter: 7% beginnt mit, %7 endet mit, =Wert genau, Semikolon oder, Ausrufezeichen am Anfang nicht, '
                + '(leer) leer. Zahl und Datum: >5, <=01.10.2026, 40000...70000. Text: Komma oder.';
            document.body.appendChild(hint);
        }
        return HINT_ID;
    }

    // Filterfeld vorbereiten: Typ merken, Kurztext per aria-describedby, beim Tippen ungueltige Teile markieren.
    function prepareInput(input, type) {
        input.setAttribute('data-filter-type', type || 'text');
        input.setAttribute('aria-describedby', ensureHint());
        input.addEventListener('input', function () { validateInput(input); });
    }

    // Ungueltige Teile (Bereich/Vergleich in Textspalte, unlesbare Zahl/Datum): Feld rot + Text, der Teil ist wirkungslos —
    // der Server ignoriert ihn genauso (Antwort 6). Farbe nie allein: der Hinweis steht als Text darunter.
    function validateInput(input) {
        var parsed = parse(input.value, input.getAttribute('data-filter-type') || 'text');
        var cell = input.closest('th, td');
        var fb = cell && cell.querySelector('.col-filter-feedback');
        if (!parsed.invalid.length) {
            input.classList.remove('is-invalid');
            input.removeAttribute('aria-invalid');
            input.setAttribute('aria-describedby', ensureHint());
            if (fb) fb.remove();
            return;
        }
        input.classList.add('is-invalid');
        input.setAttribute('aria-invalid', 'true');
        if (!fb && cell) {
            fb = document.createElement('div');
            fb.className = 'invalid-feedback d-block col-filter-feedback';
            fb.id = 'colFilterFb-' + (input.getAttribute('data-col-key') || input.getAttribute('data-filter-key') || Math.random().toString(36).slice(2));
            cell.appendChild(fb);
        }
        if (fb) {
            fb.textContent = parsed.invalid.join(' · ') + ' — ignoriert';
            input.setAttribute('aria-describedby', ensureHint() + ' ' + fb.id);
        }
    }

    function helpContent() {
        var rows = [['7%', 'beginnt mit 7'], ['%7', 'endet mit 7'], ['=KA', 'genau KA (Zahl/Datum: gleicher Wert)'],
            ['KA;SW', 'KA oder SW'], ['!KA;SW', 'weder KA noch SW'], ['(leer)', 'leeres Feld (!(leer) = nicht leer)'],
            ['>5  <=5', 'Vergleich (Zahl, Datum)'], ['1...5', 'von bis (Zahl, Datum)'], ['KA,SW', 'oder – nur in Textspalten']];
        var table = document.createElement('table');
        table.className = 'table table-sm mb-1 col-filter-help-table';
        rows.forEach(function (r) {
            var tr = table.insertRow();
            var c1 = document.createElement('th'); c1.scope = 'row'; c1.textContent = r[0];
            var c2 = document.createElement('td'); c2.textContent = r[1];
            tr.appendChild(c1); tr.appendChild(c2);
        });
        var note = document.createElement('div');
        note.className = 'small';
        note.textContent = '„Oder“ immer mit ; – in Zahl- und Datumsspalten ist das Komma Dezimalzeichen (kw41;kw42). Groß/Klein egal.';
        var content = document.createElement('div');
        content.appendChild(table);
        content.appendChild(note);
        return content;
    }

    // „?“ in der ERSTEN SICHTBAREN Zelle der Filterzeile: wird eine Spalte ausgeblendet oder verschoben (column-preferences.js setzt
    // style.display bzw. ordnet Zellen um), wandert der Knopf mit — er darf nie verschwinden.
    function placeHelp(row, btn) {
        var target = null;
        for (var c = row.firstElementChild; c; c = c.nextElementSibling) {
            if (c.style.display !== 'none') { target = c; break; }
        }
        var wrap = btn.parentNode;
        if (!target || (wrap && wrap.parentNode === target)) return;
        if (wrap && wrap.classList && wrap.classList.contains('col-filter-help-wrap')) {
            var old = wrap.parentNode;
            while (wrap.firstChild) { if (wrap.firstChild === btn) wrap.removeChild(btn); else old.appendChild(wrap.firstChild); }
            old.removeChild(wrap);
        }
        var w = document.createElement('div');
        w.className = 'col-filter-help-wrap';
        while (target.firstChild) w.appendChild(target.firstChild);
        w.insertBefore(btn, w.firstChild);
        target.appendChild(w);
    }

    function attachHelp(row) {
        if (!row) return null;
        ensureHint();
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-sm btn-outline-secondary col-filter-help';
        btn.textContent = '?';
        btn.setAttribute('aria-label', 'Hilfe zur Filtersyntax');
        placeHelp(row, btn);
        if (window.MutationObserver) {
            new MutationObserver(function () { placeHelp(row, btn); })
                .observe(row, { attributes: true, attributeFilter: ['style'], subtree: true, childList: true });
        }
        if (window.bootstrap && window.bootstrap.Popover) {
            var pop = new window.bootstrap.Popover(btn, { title: 'Filter-Syntax', content: helpContent(), html: true,
                trigger: 'click', placement: 'bottom', sanitize: false });
            document.addEventListener('keydown', function (e) { if (e.key === 'Escape') pop.hide(); });
        }
        return btn;
    }

    var api = { parse: parse, matches: matches, prepareInput: prepareInput, validateInput: validateInput, attachHelp: attachHelp };
    if (typeof module !== 'undefined' && module.exports) module.exports = api; else root.ColumnFilterSyntax = api;
})(this);
