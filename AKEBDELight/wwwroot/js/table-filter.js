// Client-side table filtering and sorting for tables with .filterable-table class
(function () {
    'use strict';

    var _filterRow = null;
    var _headers = null;
    var _tbody = null;

    document.addEventListener('DOMContentLoaded', function () {
        var table = document.querySelector('.filterable-table');
        if (!table) return;

        var thead = table.querySelector('thead');
        _tbody = table.querySelector('tbody');
        _headers = thead.querySelectorAll('th[data-filterable]');

        if (_headers.length === 0) return;

        // Create filter row
        _filterRow = document.createElement('tr');
        _filterRow.className = 'filter-row';
        var allThs = thead.querySelectorAll('tr:first-child th');
        allThs.forEach(function (th) {
            var filterTd = document.createElement('th');
            filterTd.style.padding = '4px';
            filterTd.style.backgroundColor = '#f8f9fa';

            if (th.hasAttribute('data-filterable')) {
                var input = document.createElement('input');
                input.type = 'text';
                input.className = 'form-control form-control-sm';
                input.style.fontSize = '0.75rem';
                input.placeholder = 'Filter...';
                input.setAttribute('data-col', th.getAttribute('data-col'));
                input.addEventListener('input', applyFilters);
                filterTd.appendChild(input);
            }

            _filterRow.appendChild(filterTd);
        });
        thead.appendChild(_filterRow);

        // Sorting
        _headers.forEach(function (th) {
            th.style.cursor = 'pointer';
            th.style.userSelect = 'none';
            var span = document.createElement('span');
            span.className = 'sort-indicator ms-1';
            span.style.fontSize = '0.7rem';
            th.appendChild(span);

            th.addEventListener('click', function () {
                var col = parseInt(th.getAttribute('data-col'));
                var currentDir = th.getAttribute('data-sort-dir');
                var newDir = currentDir === 'asc' ? 'desc' : 'asc';

                // Reset all
                _headers.forEach(function (h) {
                    h.removeAttribute('data-sort-dir');
                    h.querySelector('.sort-indicator').textContent = '';
                });

                th.setAttribute('data-sort-dir', newDir);
                th.querySelector('.sort-indicator').textContent = newDir === 'asc' ? '\u25B2' : '\u25BC';

                sortTable(col, newDir);
            });
        });
    });

    function applyFilters() {
        if (!_filterRow || !_tbody) return;
        var filters = {};
        _filterRow.querySelectorAll('input').forEach(function (input) {
            var col = parseInt(input.getAttribute('data-col'));
            var val = input.value.toLowerCase().trim();
            if (val) filters[col] = val;
        });

        var rows = _tbody.querySelectorAll('tr');
        rows.forEach(function (row) {
            if (row.querySelector('td[colspan]')) return; // skip "no data" row
            var visible = true;

            for (var col in filters) {
                var cell = row.querySelectorAll('td')[parseInt(col)];
                if (cell) {
                    var text = cell.textContent.toLowerCase();
                    if (text.indexOf(filters[col]) === -1) {
                        visible = false;
                        break;
                    }
                }
            }

            row.style.display = visible ? '' : 'none';
        });
    }

    function sortTable(col, dir) {
        if (!_tbody) return;
        var rows = Array.from(_tbody.querySelectorAll('tr'));
        var dataRows = rows.filter(function (r) { return !r.querySelector('td[colspan]'); });

        dataRows.sort(function (a, b) {
            var cellA = a.querySelectorAll('td')[col];
            var cellB = b.querySelectorAll('td')[col];
            if (!cellA || !cellB) return 0;

            var valA = cellA.textContent.trim();
            var valB = cellB.textContent.trim();

            // Try date comparison (dd.MM.yyyy)
            var dateRegex = /^(\d{2})\.(\d{2})\.(\d{4})$/;
            var matchA = valA.match(dateRegex);
            var matchB = valB.match(dateRegex);
            if (matchA && matchB) {
                var dateA = new Date(matchA[3], matchA[2] - 1, matchA[1]);
                var dateB = new Date(matchB[3], matchB[2] - 1, matchB[1]);
                return dir === 'asc' ? dateA - dateB : dateB - dateA;
            }
            // Empty dates sort to end
            if (matchA && !matchB) return dir === 'asc' ? -1 : 1;
            if (!matchA && matchB) return dir === 'asc' ? 1 : -1;

            // Try numeric comparison
            var numA = parseFloat(valA.replace(/\./g, '').replace(',', '.'));
            var numB = parseFloat(valB.replace(/\./g, '').replace(',', '.'));
            if (!isNaN(numA) && !isNaN(numB)) {
                return dir === 'asc' ? numA - numB : numB - numA;
            }

            // String comparison
            var cmp = valA.localeCompare(valB, 'de');
            return dir === 'asc' ? cmp : -cmp;
        });

        dataRows.forEach(function (row) {
            _tbody.appendChild(row);
        });
    }

    // Global function: Set a column filter value programmatically
    window.setColumnFilter = function (colIndex, value) {
        if (!_filterRow) return;
        var input = _filterRow.querySelector('input[data-col="' + colIndex + '"]');
        if (input) {
            input.value = value;
            applyFilters();
        }
    };

    // Global function: Trigger sorting on a column programmatically
    window.triggerSort = function (colIndex, direction) {
        if (!_headers) return;
        var th = null;
        _headers.forEach(function (h) {
            if (parseInt(h.getAttribute('data-col')) === colIndex) th = h;
        });
        if (!th) return;

        // Reset all
        _headers.forEach(function (h) {
            h.removeAttribute('data-sort-dir');
            h.querySelector('.sort-indicator').textContent = '';
        });

        th.setAttribute('data-sort-dir', direction);
        th.querySelector('.sort-indicator').textContent = direction === 'asc' ? '\u25B2' : '\u25BC';
        sortTable(colIndex, direction);
    };
})();
