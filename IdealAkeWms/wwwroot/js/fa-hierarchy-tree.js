// fa-hierarchy-tree.js — FA-Struktur-Baumtabelle (IDEAL Teil 2, Darstellungs-Nachtrag Etappe 7).
//
// Kompletter Ersatz des frueheren Inline-Skriptblocks aus Index.cshtml: dieser beruhte auf
// DOM-Verschachtelung (.fa-node-children-Container). Das Tree-Table rendert alle Ebenen als
// flache Geschwister-<tr> in EINEM <tbody> je Struktur; Eltern-Kind-Beziehung laeuft ausschliesslich
// ueber data-parent-id (VaterFA) -> data-node-id (SubFA des Vaters). Sichtbarkeit ueber display
// je Zeile, analog OseonIndex.cshtml.
//
// Enthaelt: Expand/Collapse je Knoten UND je Struktur (ganzer <tbody>), "Alle auf/zu",
// expandAncestors (fuer das Zeilen-/data-parent-id-Modell neu), #faClientFilter-Hervorheben
// (blendet NICHTS aus), PLUS den lokalen Baum-Spaltenfilter (blendet nicht passende Zweige aus,
// Vorfahrenpfad bleibt gedimmt als Kontext, Zaehler "X von Y Knoten", Auswahlfilter je Spalte).
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var table = document.getElementById('faTree');
        if (!table) return;

        var scriptEl = document.querySelector('script[src*="fa-hierarchy-tree.js"]');
        var recursiveFilter = scriptEl && scriptEl.getAttribute('data-recursive-filter') === 'true';

        var tbodies = Array.prototype.slice.call(table.querySelectorAll('tbody.fa-structure-group'));

        // Pro Struktur-<tbody> die Zeilenbeziehungen einmalig aufbauen (Zeilen sind stabil;
        // column-preferences ordnet nur Zellen INNERHALB einer Zeile um, nie die Zeilen selbst).
        var groups = tbodies.map(function (tbody) {
            var rows = Array.prototype.slice.call(tbody.children).filter(function (el) {
                return el.tagName === 'TR' && el.classList.contains('fa-node-row');
            });
            var childrenByParent = {}; // parentId -> [rows]
            var rowById = {};          // nodeId -> row (Nicht-Blaetter haben eindeutige SubFA)
            rows.forEach(function (r) {
                var pid = r.getAttribute('data-parent-id') || '';
                (childrenByParent[pid] = childrenByParent[pid] || []).push(r);
                var nid = r.getAttribute('data-node-id') || '';
                if (nid) rowById[nid] = r;
            });
            return {
                tbody: tbody,
                headRow: tbody.querySelector(':scope > tr.fa-structure-group-head'),
                subheadRow: tbody.querySelector(':scope > tr.fa-structure-subhead'),
                rows: rows,
                childrenByParent: childrenByParent,
                rowById: rowById
            };
        });

        var totalNodes = groups.reduce(function (s, g) { return s + g.rows.length; }, 0);

        // ---- Helpers -------------------------------------------------------
        function directChildren(g, row) {
            return g.childrenByParent[row.getAttribute('data-node-id') || ''] || [];
        }
        function hasChildren(g, row) {
            return directChildren(g, row).length > 0;
        }
        function setChevron(row, open) {
            var c = row.querySelector('.fa-chevron');
            if (c) c.style.transform = open ? 'rotate(90deg)' : '';
        }
        function setStructChevron(g, open) {
            var c = g.headRow ? g.headRow.querySelector('.fa-struct-chevron') : null;
            if (c) c.style.transform = open ? 'rotate(90deg)' : '';
        }
        function showRow(row) { row.style.display = ''; }
        function hideRow(row) { row.style.display = 'none'; }

        // Alle Nachfahren (rekursiv) eines Knotens.
        function descendants(g, row) {
            var out = [];
            (function walk(r) {
                directChildren(g, r).forEach(function (c) { out.push(c); walk(c); });
            })(row);
            return out;
        }

        // Expand: nur direkte Kinder zeigen (Enkel bleiben eingeklappt).
        function expandNode(g, row) {
            directChildren(g, row).forEach(showRow);
            setChevron(row, true);
            row.classList.add('fa-open');
        }
        // Collapse: gesamten Unterbaum ausblenden und Chevrons zuruecksetzen.
        function collapseNode(g, row) {
            descendants(g, row).forEach(function (d) {
                hideRow(d);
                setChevron(d, false);
                d.classList.remove('fa-open');
            });
            setChevron(row, false);
            row.classList.remove('fa-open');
        }
        function toggleNode(g, row) {
            if (!hasChildren(g, row)) return;
            if (row.classList.contains('fa-open')) collapseNode(g, row);
            else expandNode(g, row);
        }

        // Vorfahrenpfad aufklappen, damit ein tief liegender Treffer sichtbar wird.
        function expandAncestors(g, row) {
            var pid = row.getAttribute('data-parent-id') || '';
            while (pid) {
                var parent = g.rowById[pid];
                if (!parent) break;
                expandNode(g, parent);
                pid = parent.getAttribute('data-parent-id') || '';
            }
        }

        function groupOf(row) {
            var tb = row.closest('tbody');
            for (var i = 0; i < groups.length; i++) if (groups[i].tbody === tb) return groups[i];
            return null;
        }

        // ---- Struktur-Ebene (ganzer <tbody>) -------------------------------
        function structIsCollapsed(g) { return g.tbody.classList.contains('fa-struct-collapsed'); }
        function collapseStruct(g) {
            g.tbody.classList.add('fa-struct-collapsed');
            if (g.subheadRow) hideRow(g.subheadRow);
            g.rows.forEach(hideRow);
            setStructChevron(g, false);
        }
        function expandStruct(g) {
            g.tbody.classList.remove('fa-struct-collapsed');
            if (g.subheadRow) showRow(g.subheadRow);
            // Zurueck auf Default: Wurzeln sichtbar, tiefere Zeilen eingeklappt.
            g.rows.forEach(function (r) {
                var depth = parseInt(r.getAttribute('data-depth') || '0', 10);
                if (depth === 0) showRow(r); else hideRow(r);
                setChevron(r, false);
                r.classList.remove('fa-open');
            });
            setStructChevron(g, true);
        }
        function toggleStruct(g) {
            if (structIsCollapsed(g)) expandStruct(g); else collapseStruct(g);
        }

        // ---- Default-Zustand (nach Filter-Reset) ---------------------------
        function resetTreeToDefault() {
            groups.forEach(function (g) {
                g.tbody.style.display = '';
                g.tbody.classList.remove('fa-struct-collapsed');
                if (g.headRow) showRow(g.headRow);
                if (g.subheadRow) showRow(g.subheadRow);
                g.rows.forEach(function (r) {
                    r.classList.remove('fa-filter-hit', 'fa-node-context', 'fa-open');
                    var depth = parseInt(r.getAttribute('data-depth') || '0', 10);
                    if (depth === 0) showRow(r); else hideRow(r);
                    setChevron(r, false);
                });
                setStructChevron(g, true);
            });
        }

        // ---- Zaehler -------------------------------------------------------
        var counterEl = document.getElementById('faNodeCounter');
        function setCounter(hits, filtered) {
            if (!counterEl) return;
            counterEl.textContent = filtered
                ? (hits + ' von ' + totalNodes + ' Knoten (Spaltenfilter aktiv)')
                : (totalNodes + ' Knoten');
        }
        setCounter(0, false);

        // ---- "Alle auf / Alle zu" -----------------------------------------
        var btnExpand = document.getElementById('btnFaExpandAll');
        var btnCollapse = document.getElementById('btnFaCollapseAll');
        if (btnExpand) {
            btnExpand.addEventListener('click', function () {
                groups.forEach(function (g) {
                    g.tbody.classList.remove('fa-struct-collapsed');
                    if (g.subheadRow) showRow(g.subheadRow);
                    setStructChevron(g, true);
                    g.rows.forEach(function (r) {
                        showRow(r);
                        if (hasChildren(g, r)) { setChevron(r, true); r.classList.add('fa-open'); }
                    });
                });
            });
        }
        if (btnCollapse) {
            btnCollapse.addEventListener('click', function () {
                groups.forEach(function (g) {
                    g.rows.forEach(function (r) {
                        var depth = parseInt(r.getAttribute('data-depth') || '0', 10);
                        if (depth === 0) showRow(r); else hideRow(r);
                        setChevron(r, false);
                        r.classList.remove('fa-open');
                    });
                });
            });
        }

        // ---- Klick-Delegation: Knoten- und Struktur-Toggle -----------------
        // Knoten: Chevron. Struktur: gesamte Kopfzeile (Parität zu OseonIndex oseon-tree-group).
        table.addEventListener('click', function (e) {
            // AG-Knopf sitzt IN der Struktur-Kopfzeile -> vor dem Struktur-Toggle abfangen.
            var woBtn = e.target.closest('.fa-wo-btn');
            if (woBtn) {
                openWorkOperations(woBtn.getAttribute('data-hauptfa'));
                return;
            }
            var nodeToggle = e.target.closest('.fa-toggle');
            if (nodeToggle) {
                var row = nodeToggle.closest('tr.fa-node-row');
                var g2 = row ? groupOf(row) : null;
                if (g2 && row) toggleNode(g2, row);
                return;
            }
            var head = e.target.closest('tr.fa-structure-group-head');
            if (head) {
                var gh = head.closest('tbody.fa-structure-group');
                var g1 = groups.filter(function (g) { return g.tbody === gh; })[0];
                if (g1) toggleStruct(g1);
            }
        });
        // ---- Modal "Arbeitsgaenge": Inhalt erst beim Oeffnen laden (eine Struktur je Klick) ----
        // Nie ein leeres Modal: erst Ladezustand, dann Inhalt ODER Fehlermeldung.
        var woModal = document.getElementById('faWorkOperationsModal');
        var woRequest = 0;
        function openWorkOperations(hauptFa) {
            if (!woModal || !hauptFa) return;
            var body = woModal.querySelector('.modal-body');
            var requestId = ++woRequest; // schnelles Umschalten: nur die letzte Antwort gewinnt
            woModal.querySelector('.modal-title').textContent = 'FA ' + hauptFa;
            body.innerHTML = '<div class="d-flex align-items-center gap-2 text-body-secondary">'
                + '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>'
                + '<span>Arbeitsgänge werden geladen …</span></div>';
            bootstrap.Modal.getOrCreateInstance(woModal).show();

            fetch(woModal.getAttribute('data-url') + '?hauptFa=' + encodeURIComponent(hauptFa),
                { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function (r) {
                    // redirected: z. B. AccessDenied-Seite mit 200 — nie eine ganze Seite ins Modal setzen.
                    if (!r.ok || r.redirected) throw new Error('HTTP ' + r.status);
                    return r.text();
                })
                .then(function (html) {
                    if (requestId === woRequest) body.innerHTML = html;
                })
                .catch(function () {
                    if (requestId !== woRequest) return;
                    body.innerHTML = '<div class="alert alert-danger mb-0" role="alert">'
                        + 'Arbeitsgänge konnten nicht geladen werden. Seite neu laden und erneut versuchen.</div>';
                });
        }

        // Tastatur (Enter/Space) fuer die role=button-Toggles.
        table.addEventListener('keydown', function (e) {
            if (e.key !== 'Enter' && e.key !== ' ') return;
            var t = e.target.closest('.fa-struct-toggle, .fa-toggle');
            if (!t) return;
            e.preventDefault();
            t.click();
        });

        // ---- #faClientFilter: hervorheben (blendet NICHTS aus) -------------
        var clientFilter = document.getElementById('faClientFilter');
        if (clientFilter) {
            clientFilter.addEventListener('input', function () {
                var term = this.value.trim().toLowerCase();
                groups.forEach(function (g) {
                    g.rows.forEach(function (r) { r.classList.remove('fa-hl'); });
                });
                if (!term) return;
                groups.forEach(function (g) {
                    g.rows.forEach(function (r) {
                        var hay = r.getAttribute('data-search') || '';
                        if (hay.indexOf(term) !== -1) {
                            r.classList.add('fa-hl');
                            if (recursiveFilter) { expandAncestors(g, r); showRow(r); }
                        }
                    });
                });
            });
        }

        // ---- Baum-Spaltenfilter (blendet aus) ------------------------------
        function activeFilters() {
            var out = [];
            document.querySelectorAll('.fa-col-filter').forEach(function (el) {
                var v = (el.value || '').trim().toLowerCase();
                if (v) out.push({ key: el.getAttribute('data-filter-key'), val: v, exact: el.tagName === 'SELECT' });
            });
            return out;
        }
        function rowMatchesAll(row, filters) {
            for (var i = 0; i < filters.length; i++) {
                var f = filters[i];
                var hay = row.getAttribute('data-f-' + f.key) || '';
                if (f.exact ? (hay !== f.val) : (hay.indexOf(f.val) === -1)) return false;
            }
            return true;
        }
        function applyColumnFilters() {
            var filters = activeFilters();
            if (filters.length === 0) { resetTreeToDefault(); setCounter(0, false); return; }

            var totalHits = 0;
            groups.forEach(function (g) {
                var selfMatch = new Set();
                g.rows.forEach(function (r) { if (rowMatchesAll(r, filters)) selfMatch.add(r); });

                // keep = matcht selbst ODER hat einen behaltenen Nachfahren (bottom-up).
                var byDepth = g.rows.slice().sort(function (a, b) {
                    return parseInt(b.getAttribute('data-depth') || '0', 10) - parseInt(a.getAttribute('data-depth') || '0', 10);
                });
                var keep = new Set();
                byDepth.forEach(function (r) {
                    if (selfMatch.has(r)) { keep.add(r); }
                    else if (directChildren(g, r).some(function (c) { return keep.has(c); })) { keep.add(r); }
                });

                var structHasHits = selfMatch.size > 0;
                totalHits += selfMatch.size;

                if (!structHasHits) {
                    // Struktur faellt auf null Treffer -> ganzer <tbody> verschwindet.
                    g.tbody.style.display = 'none';
                    return;
                }
                g.tbody.style.display = '';
                if (g.headRow) showRow(g.headRow);
                if (g.subheadRow) showRow(g.subheadRow);

                g.rows.forEach(function (r) {
                    r.classList.remove('fa-filter-hit', 'fa-node-context');
                    if (keep.has(r)) {
                        showRow(r);
                        if (selfMatch.has(r)) r.classList.add('fa-filter-hit');
                        else r.classList.add('fa-node-context'); // Vorfahrenpfad, gedimmt
                        // Chevron offen zeigen, wenn ein behaltenes Kind sichtbar ist.
                        var openChild = directChildren(g, r).some(function (c) { return keep.has(c); });
                        setChevron(r, openChild);
                        if (openChild) r.classList.add('fa-open'); else r.classList.remove('fa-open');
                    } else {
                        hideRow(r);
                        setChevron(r, false);
                        r.classList.remove('fa-open');
                    }
                });
            });
            setCounter(totalHits, true);
        }

        var filterDebounce = null;
        document.querySelectorAll('.fa-col-filter').forEach(function (el) {
            var ev = el.tagName === 'SELECT' ? 'change' : 'input';
            el.addEventListener(ev, function () {
                if (filterDebounce) clearTimeout(filterDebounce);
                filterDebounce = setTimeout(applyColumnFilters, el.tagName === 'SELECT' ? 0 : 150);
            });
        });

        // ---- Auswahlfilter (<select>) mit den auf der Seite vorkommenden Werten befuellen.
        document.querySelectorAll('select.fa-col-filter').forEach(function (sel) {
            var key = sel.getAttribute('data-filter-key');
            var seen = {}; // lower -> display
            groups.forEach(function (g) {
                g.rows.forEach(function (r) {
                    var lower = r.getAttribute('data-f-' + key) || '';
                    if (!lower) return;
                    if (seen[lower] === undefined) {
                        var cell = r.querySelector('td[data-col-key="' + key + '"]');
                        seen[lower] = cell ? cell.textContent.trim() : lower;
                    }
                });
            });
            Object.keys(seen).sort(function (a, b) { return seen[a].localeCompare(seen[b]); })
                .forEach(function (lower) {
                    var opt = document.createElement('option');
                    opt.value = lower;
                    opt.textContent = seen[lower];
                    sel.appendChild(opt);
                });
        });
    });
})();
