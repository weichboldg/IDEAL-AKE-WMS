// Client-side table filtering and sorting for tables with .filterable-table class
(function () {
    'use strict';

    var _filterRow = null;
    var _headers = null;
    var _tbody = null;
    var _table = null;

    function getPhysicalIndex(colKey) {
        if (!_table) return -1;
        var allThs = _table.querySelectorAll('thead tr:first-child th');
        for (var i = 0; i < allThs.length; i++) {
            if (allThs[i].getAttribute('data-col-key') === colKey) return i;
        }
        return -1;
    }

    function init() {
        _table = document.querySelector('.filterable-table');
        if (!_table) return;

        var thead = _table.querySelector('thead');
        _tbody = _table.querySelector('tbody');
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
                var colKey = th.getAttribute('data-col-key');
                var isDateCol = th.hasAttribute('data-date-filter');

                if (isDateCol) {
                    var wrapper = document.createElement('div');
                    wrapper.style.display = 'flex';
                    wrapper.style.gap = '2px';

                    let input = document.createElement('input');
                    input.type = 'text';
                    input.className = 'form-control form-control-sm';
                    input.style.fontSize = '0.75rem';
                    input.style.flex = '1';
                    input.style.minWidth = '0';
                    input.placeholder = 'Filter...';
                    input.setAttribute('data-col-key', colKey);
                    input.addEventListener('input', applyFilters);

                    var calBtn = document.createElement('button');
                    calBtn.type = 'button';
                    calBtn.className = 'btn btn-sm btn-outline-secondary date-filter-btn';
                    calBtn.title = 'Kalender / KW-Filter';
                    calBtn.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16"><path d="M3.5 0a.5.5 0 0 1 .5.5V1h8V.5a.5.5 0 0 1 1 0V1h1a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V3a2 2 0 0 1 2-2h1V.5a.5.5 0 0 1 .5-.5M1 4v10a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V4z"/></svg>';
                    calBtn.addEventListener('click', function (e) {
                        e.stopPropagation();
                        openDatePicker(input, calBtn);
                    });

                    wrapper.appendChild(input);
                    wrapper.appendChild(calBtn);
                    filterTd.appendChild(wrapper);
                } else {
                    let input = document.createElement('input');
                    input.type = 'text';
                    input.className = 'form-control form-control-sm';
                    input.style.fontSize = '0.75rem';
                    input.placeholder = 'Filter...';
                    input.setAttribute('data-col-key', colKey);
                    input.addEventListener('input', applyFilters);
                    filterTd.appendChild(input);
                }
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
                var colKey = th.getAttribute('data-col-key');
                var currentDir = th.getAttribute('data-sort-dir');
                var newDir = currentDir === 'asc' ? 'desc' : 'asc';

                // Reset all
                _headers.forEach(function (h) {
                    h.removeAttribute('data-sort-dir');
                    h.querySelector('.sort-indicator').textContent = '';
                });

                th.setAttribute('data-sort-dir', newDir);
                th.querySelector('.sort-indicator').textContent = newDir === 'asc' ? '\u25B2' : '\u25BC';

                sortTable(colKey, newDir);
            });
        });
    }

    // Prüft ob ein Zelltext einem Filterwert entspricht.
    // Unterstützt: "960,886" (OR-Logik), "!960" (Ausschluss), "!960,886" (beide ausschließen)
    function matchesFilter(text, val) {
        if (val.startsWith('!')) {
            var excludes = val.substring(1).split(',').map(function (s) { return s.trim(); }).filter(Boolean);
            return excludes.every(function (ex) { return text.indexOf(ex) === -1; });
        }
        var parts = val.split(',').map(function (s) { return s.trim(); }).filter(Boolean);
        return parts.some(function (p) { return text.indexOf(p) !== -1; });
    }

    // Globale Funktion: Gibt die aktiven Filter zurück (für externe Verwendung z.B. BOM)
    window.getActiveFilters = function () {
        if (!_filterRow) return {};
        var filters = {};
        _filterRow.querySelectorAll('input').forEach(function (input) {
            var colKey = input.getAttribute('data-col-key');
            var val = input.value.toLowerCase().trim();
            if (val && colKey) filters[colKey] = val;
        });
        return filters;
    };

    function applyFilters() {
        if (!_filterRow || !_tbody) return;
        var filters = window.getActiveFilters();

        // Resolve column keys to physical indices once before the row loop
        var resolvedFilters = [];
        for (var colKey in filters) {
            var colIndex = getPhysicalIndex(colKey);
            if (colIndex >= 0) resolvedFilters.push({ index: colIndex, value: filters[colKey] });
        }

        var rows = _tbody.querySelectorAll('tr');
        rows.forEach(function (row) {
            if (row.querySelector('td[colspan]')) return;
            var visible = true;
            var cells = row.cells || row.querySelectorAll('td');

            for (var i = 0; i < resolvedFilters.length; i++) {
                var cell = cells[resolvedFilters[i].index];
                if (cell) {
                    var text = cell.textContent.toLowerCase();
                    if (!matchesFilter(text, resolvedFilters[i].value)) {
                        visible = false;
                        break;
                    }
                }
            }

            row.style.display = visible ? '' : 'none';
        });
    }

    function sortTable(colKey, dir) {
        if (!_tbody) return;
        var colIndex = getPhysicalIndex(colKey);
        if (colIndex < 0) return;

        var rows = Array.from(_tbody.querySelectorAll('tr'));
        var dataRows = rows.filter(function (r) { return !r.querySelector('td[colspan]'); });

        dataRows.sort(function (a, b) {
            var cellA = a.querySelectorAll('td')[colIndex];
            var cellB = b.querySelectorAll('td')[colIndex];
            if (!cellA || !cellB) return 0;

            var valA = cellA.textContent.trim();
            var valB = cellB.textContent.trim();

            // Try date comparison (dd.MM.yyyy or dd.MM.yyyy KWxx)
            var dateRegex = /^(\d{2})\.(\d{2})\.(\d{4})/;
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

    // ========================================================================
    // Date Picker / KW-Filter Popup
    // ========================================================================

    var _activePopup = null;

    function openDatePicker(input, anchorBtn) {
        // Bestehendes Popup schliessen
        closeDatePicker();

        var now = new Date();
        var displayMonth = now.getMonth();
        var displayYear = now.getFullYear();

        // Wenn der Input schon einen Wert hat, versuche den Monat davon zu nehmen
        var existingMatch = input.value.match(/(\d{2})\.(\d{2})\.(\d{4})/);
        if (existingMatch) {
            displayMonth = parseInt(existingMatch[2]) - 1;
            displayYear = parseInt(existingMatch[3]);
        }

        var popup = document.createElement('div');
        popup.className = 'date-filter-popup';
        document.body.appendChild(popup);
        _activePopup = popup;

        // Position relativ zum Button
        var rect = anchorBtn.getBoundingClientRect();
        popup.style.top = (rect.bottom + window.scrollY + 4) + 'px';
        popup.style.left = Math.max(4, rect.left + window.scrollX - 200) + 'px';

        function render() {
            popup.innerHTML = '';

            // Header: Monat-Navigation
            var header = document.createElement('div');
            header.className = 'date-filter-header';

            var prevBtn = document.createElement('button');
            prevBtn.type = 'button';
            prevBtn.textContent = '\u25C0';
            prevBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                displayMonth--;
                if (displayMonth < 0) { displayMonth = 11; displayYear--; }
                render();
            });

            var title = document.createElement('span');
            var monthNames = ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni',
                'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'];
            title.textContent = monthNames[displayMonth] + ' ' + displayYear;

            var nextBtn = document.createElement('button');
            nextBtn.type = 'button';
            nextBtn.textContent = '\u25B6';
            nextBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                displayMonth++;
                if (displayMonth > 11) { displayMonth = 0; displayYear++; }
                render();
            });

            header.appendChild(prevBtn);
            header.appendChild(title);
            header.appendChild(nextBtn);
            popup.appendChild(header);

            // Grid: KW | Mo Di Mi Do Fr Sa So
            var grid = document.createElement('table');
            grid.className = 'date-filter-grid';

            var headRow = document.createElement('tr');
            ['KW', 'Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'].forEach(function (label) {
                var th = document.createElement('th');
                th.textContent = label;
                headRow.appendChild(th);
            });
            grid.appendChild(headRow);

            // Ersten Tag und letzten Tag des Monats bestimmen
            var firstDay = new Date(displayYear, displayMonth, 1);
            var lastDay = new Date(displayYear, displayMonth + 1, 0);

            // Wochentag des 1. (Mo=0 ... So=6)
            var startDow = (firstDay.getDay() + 6) % 7; // Montag = 0

            // Kalender-Grid aufbauen
            var day = 1 - startDow;
            while (day <= lastDay.getDate()) {
                var row = document.createElement('tr');

                // KW-Zelle: Berechne ISO-KW vom Donnerstag dieser Woche
                var thursdayOfWeek = new Date(displayYear, displayMonth, day + 3);
                if (day + 3 < 1) thursdayOfWeek = new Date(displayYear, displayMonth, 1);
                var kw = getIsoWeek(thursdayOfWeek);
                var kwCell = document.createElement('td');
                kwCell.className = 'date-filter-kw';
                kwCell.textContent = 'KW' + kw;
                kwCell.title = 'Nach KW' + kw + ' filtern';
                kwCell.addEventListener('click', (function (kwVal) {
                    return function (e) {
                        e.stopPropagation();
                        input.value = 'KW' + kwVal;
                        applyFilters();
                        closeDatePicker();
                    };
                })(kw));
                row.appendChild(kwCell);

                // 7 Tageszellen
                for (var d = 0; d < 7; d++) {
                    var cell = document.createElement('td');
                    if (day >= 1 && day <= lastDay.getDate()) {
                        cell.textContent = day;
                        var cellDate = new Date(displayYear, displayMonth, day);
                        var isToday = cellDate.toDateString() === now.toDateString();
                        if (isToday) cell.className = 'date-filter-today';

                        cell.addEventListener('click', (function (dd) {
                            return function (e) {
                                e.stopPropagation();
                                var formatted = pad2(dd.getDate()) + '.' + pad2(dd.getMonth() + 1) + '.' + dd.getFullYear();
                                input.value = formatted;
                                applyFilters();
                                closeDatePicker();
                            };
                        })(new Date(cellDate)));
                        cell.title = pad2(cellDate.getDate()) + '.' + pad2(cellDate.getMonth() + 1) + '.' + cellDate.getFullYear();
                    }
                    row.appendChild(cell);
                    day++;
                }

                grid.appendChild(row);
            }

            popup.appendChild(grid);

            // "Filter loeschen" Button
            var clearBtn = document.createElement('button');
            clearBtn.type = 'button';
            clearBtn.className = 'date-filter-clear';
            clearBtn.textContent = 'Filter entfernen';
            clearBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                input.value = '';
                applyFilters();
                closeDatePicker();
            });
            popup.appendChild(clearBtn);
        }

        render();

        // Klick ausserhalb schliesst das Popup
        setTimeout(function () {
            document.addEventListener('click', onOutsideClick);
        }, 10);

        function onOutsideClick(e) {
            if (popup && !popup.contains(e.target) && e.target !== anchorBtn) {
                closeDatePicker();
                document.removeEventListener('click', onOutsideClick);
            }
        }
    }

    function closeDatePicker() {
        if (_activePopup && _activePopup.parentNode) {
            _activePopup.parentNode.removeChild(_activePopup);
        }
        _activePopup = null;
    }

    // ISO 8601 Kalenderwoche berechnen
    function getIsoWeek(date) {
        var d = new Date(date.getTime());
        d.setHours(0, 0, 0, 0);
        // Donnerstag dieser Woche bestimmen
        d.setDate(d.getDate() + 3 - (d.getDay() + 6) % 7);
        var jan4 = new Date(d.getFullYear(), 0, 4);
        return 1 + Math.round(((d.getTime() - jan4.getTime()) / 86400000 - 3 + (jan4.getDay() + 6) % 7) / 7);
    }

    function pad2(n) {
        return n < 10 ? '0' + n : '' + n;
    }

    // Global function: Set a column filter value programmatically
    window.setColumnFilter = function (colKey, value) {
        if (!_filterRow) return;
        var input = _filterRow.querySelector('input[data-col-key="' + colKey + '"]');
        if (input) {
            input.value = value;
            applyFilters();
        }
    };

    // Global function: Trigger sorting on a column programmatically
    window.triggerSort = function (colKey, direction) {
        if (!_headers) return;
        var th = null;
        _headers.forEach(function (h) {
            if (h.getAttribute('data-col-key') === colKey) th = h;
        });
        if (!th) return;

        // Reset all
        _headers.forEach(function (h) {
            h.removeAttribute('data-sort-dir');
            h.querySelector('.sort-indicator').textContent = '';
        });

        th.setAttribute('data-sort-dir', direction);
        th.querySelector('.sort-indicator').textContent = direction === 'asc' ? '\u25B2' : '\u25BC';
        sortTable(colKey, direction);
    };

    // Deferred init support for column-preferences integration
    var _initialized = false;
    document.addEventListener('column-preferences-ready', function () {
        if (!_initialized) {
            _initialized = true;
            init();
        }
    });
    document.addEventListener('DOMContentLoaded', function () {
        var table = document.querySelector('.filterable-table');
        if (table && !table.hasAttribute('data-view-key')) {
            // No column-preferences involved — init immediately
            if (!_initialized) {
                _initialized = true;
                init();
            }
        }
        if (table && table.hasAttribute('data-view-key')) {
            // Wait for column-preferences-ready, but fallback after 2s
            setTimeout(function () {
                if (!_initialized) {
                    _initialized = true;
                    init();
                }
            }, 2000);
        }
    });
})();
