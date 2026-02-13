// Client-side table filtering and sorting for BOM table
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var table = document.querySelector('.filterable-table');
        if (!table) return;

        var thead = table.querySelector('thead');
        var tbody = table.querySelector('tbody');
        var headers = thead.querySelectorAll('th[data-filterable]');

        if (headers.length === 0) return;

        // Create filter row
        var filterRow = document.createElement('tr');
        filterRow.className = 'filter-row';
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

            filterRow.appendChild(filterTd);
        });
        thead.appendChild(filterRow);

        // Sorting
        headers.forEach(function (th) {
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
                headers.forEach(function (h) {
                    h.removeAttribute('data-sort-dir');
                    h.querySelector('.sort-indicator').textContent = '';
                });

                th.setAttribute('data-sort-dir', newDir);
                th.querySelector('.sort-indicator').textContent = newDir === 'asc' ? '\u25B2' : '\u25BC';

                sortTable(col, newDir);
            });
        });

        function applyFilters() {
            var filters = {};
            filterRow.querySelectorAll('input').forEach(function (input) {
                var col = parseInt(input.getAttribute('data-col'));
                var val = input.value.toLowerCase().trim();
                if (val) filters[col] = val;
            });

            var rows = tbody.querySelectorAll('tr[data-picking-id], tr:not(.no-data-row)');
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
            var rows = Array.from(tbody.querySelectorAll('tr[data-picking-id], tr:not(.no-data-row)'));
            var dataRows = rows.filter(function (r) { return !r.querySelector('td[colspan]'); });

            dataRows.sort(function (a, b) {
                var cellA = a.querySelectorAll('td')[col];
                var cellB = b.querySelectorAll('td')[col];
                if (!cellA || !cellB) return 0;

                var valA = cellA.textContent.trim();
                var valB = cellB.textContent.trim();

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
                tbody.appendChild(row);
            });
        }
    });
})();
