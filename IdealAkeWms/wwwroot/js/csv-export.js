// CSV-Export fuer jede table.filterable-table (Spec 2026-10-06-csv-export-alle-tabellen, Variante A, v1.53.0).
// Ein Skript im Layout haengt je Tabelle einen Knopf „CSV" an (Opt-out: data-csv-export="false").
//   Server-Mode (data-server-column-filter="true"): dieselbe URL mit pageSize=0&page=1 per fetch — gleiche Action,
//     gleiche Rechte, Gates und Filter; Kappung PageSize.AllCap (5000) mit sichtbarer Warnung.
//   Client-Mode: Zeilen der Live-Tabelle, die der Spaltenfilter nicht ausblendet; Stueckliste ueber
//     data-csv-filter-match (Filtertreffer unabhaengig vom Klappzustand).
// Spalten = sichtbare <th data-col-key> der Live-Tabelle mit Ueberschrift, Bildschirm-Reihenfolge; Sortierung = aktiver
// Sortierindikator (Vergleich aus table-filter.js: window.tableFilterCompareValues). Format: Semikolon, UTF-8-BOM, CRLF.
// Jede DOM-Iteration mit ':scope >' (verschachtelte Kombigeraete-Tabellen, fallstricke „Tree-Table").
(function (root) {
    'use strict';

    // ===================== Reiner Kern (ohne DOM; per Node geprueft: IdealAkeWms.Tests/Js) =====================

    var KW_DATE = /^(\d{2}\.\d{2}\.\d{4})\s*KW\s*\d{1,2}$/;
    var PLAIN_NUMBER = /^[+-]?(\d{1,3}(\.\d{3})+|\d+)(,\d+)?$/;
    var DANGEROUS_START = /^[=+\-@\t\r]/;

    /** Whitespace normalisieren, „dd.MM.yyyy KWnn" → „dd.MM.yyyy", CSV-Injection entschaerfen (ausser reinen Zahlen). */
    function cleanValue(raw) {
        var v = String(raw == null ? '' : raw).replace(/\s+/g, ' ').trim();
        var kw = v.match(KW_DATE);
        if (kw) v = kw[1];
        if (DANGEROUS_START.test(v) && !PLAIN_NUMBER.test(v)) v = "'" + v;
        return v;
    }

    /** RFC 4180: Feld in Anfuehrungszeichen, wenn ; " CR oder LF vorkommt; innere " verdoppelt. */
    function quoteField(v) {
        return /[";\r\n]/.test(v) ? '"' + v.replace(/"/g, '""') + '"' : v;
    }

    /** Kopf + Zeilen (bereits bereinigte Werte) → CSV-Text mit BOM, Trenner ;, Zeilenende CRLF, ohne sep=-Zeile. */
    function toCsv(header, rows) {
        var lines = [header].concat(rows).map(function (r) { return r.map(quoteField).join(';'); });
        return '\uFEFF' + lines.join('\r\n') + '\r\n';
    }

    /** <Ansicht>_yyyy-MM-dd_HHmm.csv, unter Windows unzulaessige Zeichen durch _ ersetzt. */
    function fileName(view, now) {
        function p(n) { return (n < 10 ? '0' : '') + n; }
        var stamp = now.getFullYear() + '-' + p(now.getMonth() + 1) + '-' + p(now.getDate()) + '_' + p(now.getHours()) + p(now.getMinutes());
        return String(view || 'Export').replace(/[\\/:*?"<>|]/g, '_') + '_' + stamp + '.csv';
    }

    var core = { cleanValue: cleanValue, quoteField: quoteField, toCsv: toCsv, fileName: fileName };
    if (typeof module !== 'undefined' && module.exports) { module.exports = core; return; }
    root.csvExportCore = core;

    // ===================== Browser =====================

    var CAP_WARNING = 'Export auf 5.000 Treffer begrenzt, bitte Filter eingrenzen. Sortierung gilt nur innerhalb der exportierten 5.000.';

    function headerRow(table) { return table.querySelector(':scope > thead > tr'); }

    function isVisible(th) { return th.style.display !== 'none' && !th.classList.contains('d-none'); }

    function headerText(th) {
        var c = th.cloneNode(true);
        c.querySelectorAll('.sort-indicator').forEach(function (s) { s.remove(); });
        return c.textContent.replace(/\s+/g, ' ').trim();
    }

    /** col-key → physischer Zellindex in der ersten Kopfzeile (auch Spalten ohne Key zaehlen mit). */
    function indexMap(table) {
        var map = {};
        var tr = headerRow(table);
        if (!tr) return map;
        Array.prototype.forEach.call(tr.children, function (th, i) {
            var k = th.getAttribute('data-col-key');
            if (k) map[k] = i;
        });
        return map;
    }

    /** Zellwert (Anforderung 10/11): data-csv-value → Checkbox Ja/Nein → input-Wert / select-Text → Text inkl. Badges →
     *  sonst title/aria-label (Icon-Zelle). Zeilenklassen/Farben bleiben unberuecksichtigt. */
    function cellValue(td) {
        if (!td) return '';
        if (td.hasAttribute('data-csv-value')) return td.getAttribute('data-csv-value');
        var input = td.querySelector('input:not([type=hidden]), select, textarea');
        if (input) {
            if (input.type === 'checkbox') return input.checked ? 'Ja' : 'Nein';
            if (input.tagName === 'SELECT') return input.selectedIndex >= 0 ? input.options[input.selectedIndex].text : '';
            return input.value;
        }
        var text = td.textContent.replace(/\s+/g, ' ').trim();
        if (text) return text;
        var labelled = td.matches('[title],[aria-label]') ? td : td.querySelector('[title],[aria-label]');
        return labelled ? (labelled.getAttribute('title') || labelled.getAttribute('aria-label') || '') : '';
    }

    /** Datenzeilen je <tbody> (Gruppenkoepfe, Kombigeraete-Variantenzeilen und Leerzustand = td[colspan] fallen weg). */
    function dataRows(table, include) {
        var out = [];
        table.querySelectorAll(':scope > tbody').forEach(function (tbody) {
            tbody.querySelectorAll(':scope > tr').forEach(function (tr) {
                if (tr.querySelector(':scope > td[colspan]')) return;
                if (!include || include(tr)) out.push({ tr: tr, tbody: tbody });
            });
        });
        return out;
    }

    function clientInclude(table) {
        var attrMode = table.getAttribute('data-csv-filter-source') === 'attribute';
        return function (tr) {
            if (tr.hasAttribute('data-csv-filter-match')) return tr.getAttribute('data-csv-filter-match') === 'true';
            return attrMode || tr.style.display !== 'none';
        };
    }

    function activeSort(table) {
        var th = table.querySelector(':scope > thead > tr > th[data-sort-dir][data-col-key]');
        return th ? { key: th.getAttribute('data-col-key'), dir: th.getAttribute('data-sort-dir') } : null;
    }

    function exportRows(liveTable, sourceTable, rows) {
        var src = indexMap(sourceTable);
        var columns = [];
        Array.prototype.forEach.call(headerRow(liveTable).children, function (th) {
            var key = th.getAttribute('data-col-key');
            if (!key || !isVisible(th)) return;
            var label = headerText(th);
            if (label && src[key] !== undefined) columns.push({ key: key, label: label, idx: src[key] });
        });

        var sort = activeSort(liveTable);
        if (sort && src[sort.key] !== undefined) {
            if (typeof root.tableFilterCompareValues !== 'function')
                throw new Error('Sortierung nicht verfuegbar (table-filter.js fehlt) — Export abgebrochen.');
            var si = src[sort.key];
            var byGroup = [];
            rows.forEach(function (r) {
                var g = byGroup.length && byGroup[byGroup.length - 1].tbody === r.tbody ? byGroup[byGroup.length - 1] : null;
                if (!g) { g = { tbody: r.tbody, items: [] }; byGroup.push(g); }
                g.items.push(r);
            });
            rows = [];
            byGroup.forEach(function (g) {
                g.items.sort(function (a, b) {
                    var ca = a.tr.children[si], cb = b.tr.children[si];
                    return root.tableFilterCompareValues(ca ? ca.textContent.trim() : '', cb ? cb.textContent.trim() : '', sort.dir);
                });
                rows = rows.concat(g.items);
            });
        }

        // Gruppenschluessel nur aus tbody[data-group] und nur, wenn keine sichtbare hauptfa-Spalte da ist (Antwort 14 b).
        var groupCol = rows.some(function (r) { return r.tbody.hasAttribute('data-group'); })
            && !columns.some(function (c) { return c.key === 'hauptfa'; });

        var header = (groupCol ? ['HauptFA'] : []).concat(columns.map(function (c) { return c.label; }));
        var body = rows.map(function (r) {
            var vals = columns.map(function (c) { return cleanValue(cellValue(r.tr.children[c.idx])); });
            return groupCol ? [cleanValue(r.tbody.getAttribute('data-group') || '')].concat(vals) : vals;
        });
        return { header: header.map(cleanValue), body: body };
    }

    function download(text, name) {
        var blob = new Blob([text], { type: 'text/csv;charset=utf-8' });
        var a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = name;
        document.body.appendChild(a);
        a.click();
        setTimeout(function () { URL.revokeObjectURL(a.href); a.remove(); }, 0);
    }

    function viewName(table) {
        return table.getAttribute('data-view-key') || (location.pathname.split('/').filter(Boolean)[0]) || 'Export';
    }

    function showMessage(host, kind, text) {
        var old = host.querySelector(':scope > .csv-export-msg');
        if (old) old.remove();
        if (!text) return;
        var div = document.createElement('div');
        div.className = 'csv-export-msg alert alert-' + kind + ' py-1 px-2 mb-0 me-2 small';
        div.setAttribute('role', kind === 'danger' ? 'alert' : 'status');
        div.textContent = text;
        host.insertBefore(div, host.firstChild);
    }

    async function fetchServerTable(liveTable) {
        var all = Array.prototype.slice.call(document.querySelectorAll('table.filterable-table'));
        var position = all.indexOf(liveTable);
        var url = new URL(location.href);
        url.searchParams.set('pageSize', '0');
        url.searchParams.set('page', '1');
        var res = await fetch(url.toString(), { credentials: 'same-origin' });
        if (!res.ok) throw new Error('Export fehlgeschlagen (HTTP ' + res.status + ').');
        var doc = new DOMParser().parseFromString(await res.text(), 'text/html');
        var table = doc.querySelectorAll('table.filterable-table')[position];
        if (!table || (liveTable.id && table.id !== liveTable.id))
            throw new Error('Export fehlgeschlagen: Die Liste kam nicht zurueck (Sitzung abgelaufen oder kein Zugriff?). Seite neu laden.');
        var bar = doc.querySelector('.pagination-bar');
        return { table: table, capped: !!bar && bar.getAttribute('data-capped') === 'true' };
    }

    async function runExport(table, button, host) {
        showMessage(host, null, null);
        button.disabled = true;
        var label = button.innerHTML;
        button.innerHTML = '<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>CSV';
        try {
            var server = table.getAttribute('data-server-column-filter') === 'true';
            var fetched = server ? await fetchServerTable(table) : { table: table, capped: false };
            var rows = dataRows(fetched.table, server ? null : clientInclude(table));
            var result = exportRows(table, fetched.table, rows);
            download(toCsv(result.header, result.body), fileName(viewName(table), new Date()));
            if (fetched.capped) showMessage(host, 'warning', CAP_WARNING);
        } catch (e) {
            showMessage(host, 'danger', e && e.message ? e.message : 'Export fehlgeschlagen.');
        } finally {
            button.disabled = false;
            button.innerHTML = label;
        }
    }

    /** Knopf in den Toolbar-Host direkt vor .table-responsive (wie das Zahnrad, fallstricke §19); fehlt er, eigener Host. */
    function attach(table) {
        var wrapper = table.closest('.table-responsive') || table.parentElement;
        var host = wrapper.previousElementSibling;
        if (!host || host.tagName !== 'DIV' || !host.classList.contains('d-flex')) {
            host = document.createElement('div');
            host.className = 'd-flex justify-content-end mb-1';
            wrapper.parentElement.insertBefore(host, wrapper);
        }
        host.classList.add('flex-wrap', 'align-items-center', 'gap-1');
        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'btn btn-sm btn-outline-secondary csv-export-btn';
        button.title = 'Als CSV exportieren';
        button.setAttribute('aria-label', 'Als CSV exportieren');
        button.textContent = 'CSV';
        button.addEventListener('click', function () { runExport(table, button, host); });
        host.appendChild(button);
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('table.filterable-table').forEach(function (table) {
            if (table.getAttribute('data-csv-export') !== 'false') attach(table);
        });
    });
})(typeof window !== 'undefined' ? window : this);
