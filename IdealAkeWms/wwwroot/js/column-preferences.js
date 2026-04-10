// Column Preferences — customizable column visibility, width, order, sort defaults
// Handles: load/apply/auto-save, gear dialog (offcanvas), context menu, resize handles
(function () {
    'use strict';

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------
    var _table = null;
    var _viewConfig = null;      // { viewKey, supportsReorder, supportsSortDefault }
    var _columnConfig = [];      // [{ key, label, locked, defaultWidth }, ...]
    var _settings = null;        // { columns: [...], defaultSortColumn, defaultSortDirection }
    var _saveTimer = null;
    var _offcanvas = null;
    var _offcanvasEl = null;
    var _dragState = null;       // for list drag-and-drop in offcanvas
    var _resizeState = null;     // for column resize
    var _contextMenu = null;     // active context menu DOM node

    var SAVE_DELAY = 1500;

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------
    function getApiUrl(extra) {
        var url = '/api/user-view-preferences/' + encodeURIComponent(_viewConfig.viewKey);
        return extra ? url + extra : url;
    }

    var _colIndexCache = null;

    function buildColIndexCache() {
        _colIndexCache = {};
        var ths = _table.querySelectorAll('thead tr:first-child th');
        for (var i = 0; i < ths.length; i++) {
            var key = ths[i].getAttribute('data-col-key');
            if (key) _colIndexCache[key] = i;
        }
    }

    function invalidateColIndexCache() {
        _colIndexCache = null;
    }

    function colIndexByKey(key) {
        if (!_colIndexCache) buildColIndexCache();
        var idx = _colIndexCache[key];
        return idx !== undefined ? idx : -1;
    }

    function getFilterRow() {
        return _table.querySelector('thead tr.filter-row');
    }

    function buildDefaultSettings() {
        return {
            columns: _columnConfig.map(function (c, i) {
                return { key: c.key, visible: true, width: c.defaultWidth || null, order: i };
            }),
            defaultSortColumn: null,
            defaultSortDirection: 'asc'
        };
    }

    function mergeWithDefaults(saved) {
        var defaults = buildDefaultSettings();
        if (!saved) return defaults;

        // Build a map of saved column settings keyed by column key
        var savedMap = {};
        (saved.columns || []).forEach(function (c) { savedMap[c.key] = c; });

        // Only include columns that are actually in _columnConfig (ignore unknown keys)
        defaults.columns = _columnConfig.map(function (c, i) {
            var s = savedMap[c.key];
            if (!s) return { key: c.key, visible: true, width: c.defaultWidth || null, order: i };
            return {
                key: c.key,
                visible: s.visible !== undefined ? s.visible : true,
                width: s.width !== undefined ? s.width : (c.defaultWidth || null),
                order: s.order !== undefined ? s.order : i
            };
        });

        defaults.defaultSortColumn = saved.defaultSortColumn || null;
        defaults.defaultSortDirection = saved.defaultSortDirection || 'asc';
        return defaults;
    }

    // -------------------------------------------------------------------------
    // Apply settings to DOM
    // -------------------------------------------------------------------------
    function applySettings() {
        if (!_table || !_settings) return;

        if (_viewConfig.supportsReorder) {
            applyColumnOrder();
        }

        var allThs = _table.querySelectorAll('thead tr:first-child th');
        _settings.columns.forEach(function (cs) {
            var idx = colIndexByKey(cs.key);
            if (idx < 0) return;

            var th = allThs[idx];
            setColVisibility(idx, cs.visible, th);
            if (cs.width) {
                th.style.width = cs.width + 'px';
                th.style.minWidth = cs.width + 'px';
            }
        });
        if (_settings.defaultSortColumn) {
            // Store on table element so table-filter.js can read after init
            _table.dataset.defaultSortColumn = _settings.defaultSortColumn;
            _table.dataset.defaultSortDirection = _settings.defaultSortDirection || 'asc';
        } else {
            delete _table.dataset.defaultSortColumn;
            delete _table.dataset.defaultSortDirection;
        }
    }

    function setColVisibility(physicalIdx, visible, thEl) {
        var display = visible ? '' : 'none';
        var th = thEl || _table.querySelectorAll('thead tr:first-child th')[physicalIdx];
        if (th) th.style.display = display;

        // Filter row cell
        var filterRow = getFilterRow();
        if (filterRow) {
            var filterCells = filterRow.querySelectorAll('th');
            if (filterCells[physicalIdx]) filterCells[physicalIdx].style.display = display;
        }

        // tbody cells
        var tbodyRows = _table.querySelectorAll('tbody tr');
        tbodyRows.forEach(function (row) {
            var cells = row.querySelectorAll('td, th');
            if (cells[physicalIdx]) cells[physicalIdx].style.display = display;
        });
    }

    function applyColumnOrder() {
        if (!_settings.columns.length) return;

        // Build desired order from settings (by order value), only for keys present in DOM
        var ordered = _settings.columns.slice()
            .filter(function (cs) { return colIndexByKey(cs.key) >= 0; })
            .sort(function (a, b) { return a.order - b.order; });

        var desiredKeys = ordered.map(function (cs) { return cs.key; });

        // Get current order of keys in DOM
        var ths = _table.querySelectorAll('thead tr:first-child th');
        var currentKeys = Array.from(ths).map(function (th) { return th.getAttribute('data-col-key'); });

        // Check if reorder is needed
        var needsReorder = false;
        for (var i = 0; i < desiredKeys.length; i++) {
            if (currentKeys[i] !== desiredKeys[i]) { needsReorder = true; break; }
        }
        if (!needsReorder) return;

        // Build old-index → new-index mapping for physical columns
        // currentKeys may include columns not in our config (no data-col-key)
        var noCfgCols = []; // indices of columns without a key
        var keyToCurrentIdx = {};
        currentKeys.forEach(function (k, i) {
            if (k) keyToCurrentIdx[k] = i;
            else noCfgCols.push(i);
        });

        // New physical order: desiredKeys first, then any un-keyed columns at end
        var newOrder = desiredKeys.map(function (k) { return keyToCurrentIdx[k]; });
        noCfgCols.forEach(function (i) { newOrder.push(i); });

        reorderDomColumns(newOrder);
        invalidateColIndexCache();
    }

    function reorderDomColumns(newPhysicalOrder) {
        // newPhysicalOrder[newIndex] = oldIndex
        // Reorder th elements in header row, filter row cells, and each tbody tr cells

        function reorderChildren(parentEl, selector, newOrder) {
            var children = Array.from(parentEl.querySelectorAll(':scope > ' + selector));
            if (children.length === 0) return;
            // Build array of elements in new order
            newOrder.forEach(function (oldIdx) {
                if (oldIdx < children.length) {
                    parentEl.appendChild(children[oldIdx]);
                }
            });
        }

        // Header row
        var headerRow = _table.querySelector('thead tr:first-child');
        if (headerRow) reorderChildren(headerRow, 'th', newPhysicalOrder);

        // Filter row
        var filterRow = getFilterRow();
        if (filterRow) reorderChildren(filterRow, 'th', newPhysicalOrder);

        // tbody rows
        _table.querySelectorAll('tbody tr').forEach(function (row) {
            reorderChildren(row, 'td, th', newPhysicalOrder);
        });
    }

    // -------------------------------------------------------------------------
    // Column hide / show (public API + internal use)
    // -------------------------------------------------------------------------
    function hideColumn(key) {
        var cs = _settings.columns.find(function (c) { return c.key === key; });
        if (!cs) return;
        cs.visible = false;
        var idx = colIndexByKey(key);
        if (idx >= 0) setColVisibility(idx, false);
        scheduleSave();
        refreshOffcanvas();
    }

    function showColumn(key) {
        var cs = _settings.columns.find(function (c) { return c.key === key; });
        if (!cs) return;
        cs.visible = true;
        var idx = colIndexByKey(key);
        if (idx >= 0) setColVisibility(idx, true);
        scheduleSave();
        refreshOffcanvas();
    }

    function showAllColumns() {
        _settings.columns.forEach(function (cs) {
            cs.visible = true;
            var idx = colIndexByKey(cs.key);
            if (idx >= 0) setColVisibility(idx, true);
        });
        scheduleSave();
        refreshOffcanvas();
    }

    function setColumnWidth(key, width) {
        var cs = _settings.columns.find(function (c) { return c.key === key; });
        if (!cs) return;
        cs.width = width;
        var idx = colIndexByKey(key);
        if (idx >= 0) {
            var th = _table.querySelectorAll('thead tr:first-child th')[idx];
            if (th) {
                th.style.width = width ? width + 'px' : '';
                th.style.minWidth = width ? width + 'px' : '';
            }
        }
        scheduleSave();
    }

    // -------------------------------------------------------------------------
    // Persistence
    // -------------------------------------------------------------------------
    function scheduleSave() {
        if (_saveTimer) clearTimeout(_saveTimer);
        _saveTimer = setTimeout(function () {
            _saveTimer = null;
            saveSettings();
        }, SAVE_DELAY);
    }

    function saveSettings() {
        if (!_viewConfig) return;
        fetch(getApiUrl(), {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(JSON.stringify(_settings))
        }).catch(function (err) {
            console.warn('[column-preferences] save failed:', err);
        });
    }

    function loadSettings(callback) {
        fetch(getApiUrl())
            .then(function (resp) {
                if (resp.status === 204) return null;
                if (!resp.ok) return null;
                return resp.json();
            })
            .then(function (data) {
                var saved = null;
                if (typeof data === 'string') {
                    try { saved = JSON.parse(data); } catch (e) { }
                } else if (data && typeof data === 'object') {
                    saved = data;
                }
                _settings = mergeWithDefaults(saved);
                callback();
            })
            .catch(function () {
                _settings = buildDefaultSettings();
                callback();
            });
    }

    function resetToDefault() {
        fetch(getApiUrl(), { method: 'DELETE' })
            .then(function () {
                window.location.reload();
            })
            .catch(function (err) {
                console.warn('[column-preferences] reset failed:', err);
                window.location.reload();
            });
    }

    // -------------------------------------------------------------------------
    // Gear button + Offcanvas
    // -------------------------------------------------------------------------
    function createGearButton() {
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-sm btn-outline-secondary column-prefs-gear ms-2';
        btn.title = 'Spalten konfigurieren';
        btn.setAttribute('aria-label', 'Spalten konfigurieren');
        btn.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">' +
            '<path d="M8 4.754a3.246 3.246 0 1 0 0 6.492 3.246 3.246 0 0 0 0-6.492zM5.754 8a2.246 2.246 0 1 1 4.492 0 2.246 2.246 0 0 1-4.492 0z"/>' +
            '<path d="M9.796 1.343c-.527-1.79-3.065-1.79-3.592 0l-.094.319a.873.873 0 0 1-1.255.52l-.292-.16c-1.64-.892-3.433.902-2.54 2.541l.159.292a.873.873 0 0 1-.52 1.255l-.319.094c-1.79.527-1.79 3.065 0 3.592l.319.094a.873.873 0 0 1 .52 1.255l-.16.292c-.892 1.64.901 3.434 2.541 2.54l.292-.159a.873.873 0 0 1 1.255.52l.094.319c.527 1.79 3.065 1.79 3.592 0l.094-.319a.873.873 0 0 1 1.255-.52l.292.16c1.64.893 3.434-.902 2.54-2.541l-.159-.292a.873.873 0 0 1 .52-1.255l.319-.094c1.79-.527 1.79-3.065 0-3.592l-.319-.094a.873.873 0 0 1-.52-1.255l.16-.292c.893-1.64-.902-3.433-2.541-2.54l-.292.159a.873.873 0 0 1-1.255-.52l-.094-.319zm-2.633.283c.246-.835 1.428-.835 1.674 0l.094.319a1.873 1.873 0 0 0 2.693 1.115l.291-.16c.764-.415 1.6.42 1.184 1.185l-.159.292a1.873 1.873 0 0 0 1.116 2.692l.318.094c.835.246.835 1.428 0 1.674l-.319.094a1.873 1.873 0 0 0-1.115 2.693l.16.291c.415.764-.42 1.6-1.185 1.184l-.291-.159a1.873 1.873 0 0 0-2.693 1.116l-.094.318c-.246.835-1.428.835-1.674 0l-.094-.319a1.873 1.873 0 0 0-2.692-1.115l-.292.16c-.764.415-1.6-.42-1.184-1.185l.159-.291A1.873 1.873 0 0 0 1.945 8.93l-.319-.094c-.835-.246-.835-1.428 0-1.674l.319-.094A1.873 1.873 0 0 0 3.06 4.474l-.16-.292c-.415-.764.42-1.6 1.185-1.184l.292.159a1.873 1.873 0 0 0 2.692-1.115l.094-.319z"/>' +
            '</svg>';
        btn.addEventListener('click', openOffcanvas);
        return btn;
    }

    function insertGearButton() {
        // Find or create a container div above the table
        var tableWrapper = _table.closest('.table-responsive') || _table.parentElement;

        // Look for an existing toolbar row above the table wrapper
        var gearContainer = tableWrapper.previousElementSibling;
        var insertedInToolbar = false;

        if (gearContainer && (gearContainer.classList.contains('d-flex') || gearContainer.tagName === 'DIV')) {
            // Try to append to existing toolbar
            var btn = createGearButton();
            gearContainer.appendChild(btn);
            insertedInToolbar = true;
        }

        if (!insertedInToolbar) {
            // Create a small right-aligned container above the table
            var wrapper = document.createElement('div');
            wrapper.className = 'd-flex justify-content-end mb-1';
            var btn = createGearButton();
            wrapper.appendChild(btn);
            tableWrapper.parentElement.insertBefore(wrapper, tableWrapper);
        }
    }

    function createOffcanvasEl() {
        var el = document.createElement('div');
        el.className = 'offcanvas offcanvas-end';
        el.tabIndex = -1;
        el.id = 'columnPrefsOffcanvas';
        el.setAttribute('aria-labelledby', 'columnPrefsOffcanvasLabel');
        el.innerHTML =
            '<div class="offcanvas-header">' +
            '  <h5 class="offcanvas-title" id="columnPrefsOffcanvasLabel">Spalten konfigurieren</h5>' +
            '  <button type="button" class="btn-close" data-bs-dismiss="offcanvas" aria-label="Schließen"></button>' +
            '</div>' +
            '<div class="offcanvas-body">' +
            '  <div id="columnPrefsContent"></div>' +
            '</div>';
        document.body.appendChild(el);
        return el;
    }

    function openOffcanvas() {
        if (!_offcanvasEl) {
            _offcanvasEl = createOffcanvasEl();
        }
        renderOffcanvasContent();
        if (!_offcanvas) {
            _offcanvas = new bootstrap.Offcanvas(_offcanvasEl);
        }
        _offcanvas.show();
    }

    function renderOffcanvasContent() {
        var content = document.getElementById('columnPrefsContent');
        if (!content) return;
        content.innerHTML = '';

        // ----- Column list -----
        var listLabel = document.createElement('h6');
        listLabel.className = 'mb-2';
        listLabel.textContent = 'Sichtbare Spalten';
        content.appendChild(listLabel);

        var list = document.createElement('div');
        list.className = 'column-prefs-list mb-3';
        content.appendChild(list);

        // Get columns in current display order (by their order setting)
        var orderedCols = _settings.columns.slice()
            .filter(function (cs) { return colIndexByKey(cs.key) >= 0; }) // only rendered cols
            .sort(function (a, b) { return a.order - b.order; });

        orderedCols.forEach(function (cs) {
            var cfgEntry = _columnConfig.find(function (c) { return c.key === cs.key; });
            if (!cfgEntry) return;

            var item = document.createElement('div');
            item.className = 'column-prefs-item d-flex align-items-center py-2 px-1';
            item.dataset.colKey = cs.key;

            // Drag handle (only if reorder supported)
            if (_viewConfig.supportsReorder) {
                var handle = document.createElement('span');
                handle.className = 'column-prefs-drag-handle me-2';
                handle.style.cursor = 'grab';
                handle.innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">' +
                    '<path d="M7 2a1 1 0 1 1-2 0 1 1 0 0 1 2 0zm3 0a1 1 0 1 1-2 0 1 1 0 0 1 2 0zM7 5a1 1 0 1 1-2 0 1 1 0 0 1 2 0zm3 0a1 1 0 1 1-2 0 1 1 0 0 1 2 0zM7 8a1 1 0 1 1-2 0 1 1 0 0 1 2 0zm3 0a1 1 0 1 1-2 0 1 1 0 0 1 2 0zm-3 3a1 1 0 1 1-2 0 1 1 0 0 1 2 0zm3 0a1 1 0 1 1-2 0 1 1 0 0 1 2 0zm-3 3a1 1 0 1 1-2 0 1 1 0 0 1 2 0zm3 0a1 1 0 1 1-2 0 1 1 0 0 1 2 0z"/>' +
                    '</svg>';
                setupDragHandle(handle, item, list);
                item.appendChild(handle);
            }

            // Checkbox
            var cb = document.createElement('input');
            cb.type = 'checkbox';
            cb.className = 'form-check-input me-2';
            cb.checked = cs.visible;
            cb.disabled = !!cfgEntry.locked;
            cb.id = 'col-pref-cb-' + cs.key;
            cb.addEventListener('change', function () {
                if (cb.checked) showColumn(cs.key);
                else hideColumn(cs.key);
            });
            item.appendChild(cb);

            // Label
            var lbl = document.createElement('label');
            lbl.htmlFor = cb.id;
            lbl.className = 'form-check-label flex-grow-1';
            lbl.textContent = cfgEntry.label;
            item.appendChild(lbl);

            // Locked badge
            if (cfgEntry.locked) {
                var badge = document.createElement('small');
                badge.className = 'text-muted ms-1';
                badge.textContent = '(Pflichtspalte)';
                item.appendChild(badge);
            }

            list.appendChild(item);
        });

        // ----- Default sort section -----
        if (_viewConfig.supportsSortDefault) {
            var sep = document.createElement('hr');
            content.appendChild(sep);

            var sortLabel = document.createElement('h6');
            sortLabel.className = 'mb-2';
            sortLabel.textContent = 'Standard-Sortierung';
            content.appendChild(sortLabel);

            var sortRow = document.createElement('div');
            sortRow.className = 'd-flex gap-2 mb-3';

            var colSelect = document.createElement('select');
            colSelect.className = 'form-select form-select-sm flex-grow-1';
            var noneOpt = document.createElement('option');
            noneOpt.value = '';
            noneOpt.textContent = '— keine —';
            colSelect.appendChild(noneOpt);

            _columnConfig.forEach(function (c) {
                if (!c.label) return; // skip structural columns (empty label = icon-only)
                var opt = document.createElement('option');
                opt.value = c.key;
                opt.textContent = c.label;
                colSelect.appendChild(opt);
            });
            colSelect.value = _settings.defaultSortColumn || '';

            var dirSelect = document.createElement('select');
            dirSelect.className = 'form-select form-select-sm';
            dirSelect.style.width = '110px';
            [['asc', 'Aufsteigend'], ['desc', 'Absteigend']].forEach(function (pair) {
                var opt = document.createElement('option');
                opt.value = pair[0];
                opt.textContent = pair[1];
                dirSelect.appendChild(opt);
            });
            dirSelect.value = _settings.defaultSortDirection || 'asc';

            colSelect.addEventListener('change', function () {
                _settings.defaultSortColumn = colSelect.value || null;
                scheduleSave();
            });
            dirSelect.addEventListener('change', function () {
                _settings.defaultSortDirection = dirSelect.value;
                scheduleSave();
            });

            sortRow.appendChild(colSelect);
            sortRow.appendChild(dirSelect);
            content.appendChild(sortRow);
        }

        // ----- Reset button -----
        var sep2 = document.createElement('hr');
        content.appendChild(sep2);

        var resetBtn = document.createElement('button');
        resetBtn.type = 'button';
        resetBtn.className = 'btn btn-outline-danger btn-sm';
        resetBtn.textContent = 'Auf Standard zurücksetzen';
        resetBtn.addEventListener('click', function () {
            if (_offcanvas) _offcanvas.hide();
            resetToDefault();
        });
        content.appendChild(resetBtn);
    }

    function refreshOffcanvas() {
        if (_offcanvasEl && _offcanvas) {
            var isOpen = _offcanvasEl.classList.contains('show');
            if (isOpen) renderOffcanvasContent();
        }
    }

    // -------------------------------------------------------------------------
    // Offcanvas drag & drop (reorder columns)
    // -------------------------------------------------------------------------
    function setupDragHandle(handle, item, list) {
        handle.addEventListener('mousedown', function (e) {
            e.preventDefault();
            _dragState = {
                item: item,
                startY: e.clientY,
                placeholder: null
            };

            var placeholder = document.createElement('div');
            placeholder.className = 'column-prefs-item';
            placeholder.style.height = item.offsetHeight + 'px';
            placeholder.style.background = '#f0f8ff';
            placeholder.style.border = '2px dashed var(--ake-secondary)';
            placeholder.style.borderRadius = '4px';
            _dragState.placeholder = placeholder;

            item.classList.add('dragging');
            item.style.position = 'fixed';
            item.style.width = item.offsetWidth + 'px';
            item.style.zIndex = '2000';
            item.style.pointerEvents = 'none';
            item.style.top = (e.clientY - 20) + 'px';

            list.insertBefore(placeholder, item);

            document.addEventListener('mousemove', onDragMove);
            document.addEventListener('mouseup', onDragEnd);
        });
    }

    function onDragMove(e) {
        if (!_dragState) return;
        var item = _dragState.item;
        var placeholder = _dragState.placeholder;
        item.style.top = (e.clientY - 20) + 'px';

        // Find the element under the cursor (excluding dragged item)
        var list = placeholder.parentElement;
        var children = Array.from(list.children).filter(function (c) { return c !== item && c !== placeholder; });
        var targetItem = null;
        for (var i = 0; i < children.length; i++) {
            var rect = children[i].getBoundingClientRect();
            if (e.clientY < rect.top + rect.height / 2) {
                targetItem = children[i];
                break;
            }
        }
        if (targetItem) {
            list.insertBefore(placeholder, targetItem);
        } else {
            list.appendChild(placeholder);
        }
    }

    function onDragEnd(e) {
        if (!_dragState) return;
        document.removeEventListener('mousemove', onDragMove);
        document.removeEventListener('mouseup', onDragEnd);

        var item = _dragState.item;
        var placeholder = _dragState.placeholder;
        var list = placeholder.parentElement;

        // Restore item styles
        item.style.position = '';
        item.style.width = '';
        item.style.zIndex = '';
        item.style.pointerEvents = '';
        item.style.top = '';
        item.style.left = '';
        item.classList.remove('dragging');

        // Insert item where placeholder is
        list.insertBefore(item, placeholder);
        placeholder.remove();

        _dragState = null;

        // Read new order from DOM list
        var items = list.querySelectorAll('.column-prefs-item[data-col-key]');
        var newOrder = Array.from(items).map(function (el) { return el.dataset.colKey; });

        // Update _settings.columns order values
        newOrder.forEach(function (key, idx) {
            var cs = _settings.columns.find(function (c) { return c.key === key; });
            if (cs) cs.order = idx;
        });

        // Apply reorder to table DOM
        applyColumnOrder();
        scheduleSave();
    }

    // -------------------------------------------------------------------------
    // Context menu
    // -------------------------------------------------------------------------
    function closeContextMenu() {
        if (_contextMenu) {
            _contextMenu.remove();
            _contextMenu = null;
        }
    }

    function showContextMenu(e, th) {
        e.preventDefault();
        closeContextMenu();

        var colKey = th.getAttribute('data-col-key');
        var cfgEntry = _columnConfig.find(function (c) { return c.key === colKey; });
        var isLocked = cfgEntry && cfgEntry.locked;

        var menu = document.createElement('div');
        menu.className = 'column-context-menu';
        _contextMenu = menu;

        // "Spalte ausblenden" (only if not locked)
        if (!isLocked) {
            var hideItem = document.createElement('div');
            hideItem.className = 'column-context-item';
            hideItem.textContent = 'Spalte ausblenden';
            hideItem.addEventListener('click', function () {
                closeContextMenu();
                hideColumn(colKey);
            });
            menu.appendChild(hideItem);
        }

        // "Alle Spalten anzeigen"
        var showAllItem = document.createElement('div');
        showAllItem.className = 'column-context-item';
        showAllItem.textContent = 'Alle Spalten anzeigen';
        showAllItem.addEventListener('click', function () {
            closeContextMenu();
            showAllColumns();
        });
        menu.appendChild(showAllItem);

        // Separator
        var sep = document.createElement('div');
        sep.className = 'column-context-separator';
        menu.appendChild(sep);

        // "Spalten-Einstellungen..."
        var settingsItem = document.createElement('div');
        settingsItem.className = 'column-context-item';
        settingsItem.textContent = 'Spalten-Einstellungen...';
        settingsItem.addEventListener('click', function () {
            closeContextMenu();
            openOffcanvas();
        });
        menu.appendChild(settingsItem);

        document.body.appendChild(menu);

        // Position menu
        var x = e.pageX;
        var y = e.pageY;
        var menuW = 210;
        var menuH = menu.offsetHeight || 120;
        if (x + menuW > window.innerWidth) x = window.innerWidth - menuW - 8;
        if (y + menuH > window.innerHeight + window.scrollY) y = y - menuH;
        menu.style.left = x + 'px';
        menu.style.top = y + 'px';

        // Close on outside click or Escape
        setTimeout(function () {
            document.addEventListener('click', closeContextMenu, { once: true });
        }, 0);
    }

    function attachContextMenus() {
        if ('ontouchstart' in window) return; // disabled on touch devices

        var ths = _table.querySelectorAll('thead tr:first-child th[data-col-key]');
        ths.forEach(function (th) {
            th.addEventListener('contextmenu', function (e) {
                showContextMenu(e, th);
            });
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') closeContextMenu();
        });
    }

    // -------------------------------------------------------------------------
    // Column resize handles
    // -------------------------------------------------------------------------
    function attachResizeHandles() {
        if ('ontouchstart' in window) return; // disabled on touch

        var ths = _table.querySelectorAll('thead tr:first-child th[data-col-key]');
        ths.forEach(function (th) {
            // Make th position:relative so handle can position against it
            var existingPos = window.getComputedStyle(th).position;
            if (existingPos === 'static') th.style.position = 'relative';

            var handle = document.createElement('div');
            handle.className = 'col-resize-handle';
            th.appendChild(handle);

            handle.addEventListener('mousedown', function (e) {
                e.preventDefault();
                e.stopPropagation();

                var startX = e.clientX;
                var startWidth = th.offsetWidth;
                var colKey = th.getAttribute('data-col-key');

                handle.classList.add('active');
                document.body.style.cursor = 'col-resize';
                document.body.style.userSelect = 'none';

                _resizeState = { colKey: colKey, th: th };

                function onMove(ev) {
                    var newWidth = Math.max(40, startWidth + ev.clientX - startX);
                    th.style.width = newWidth + 'px';
                    th.style.minWidth = newWidth + 'px';
                }

                function onUp(ev) {
                    document.removeEventListener('mousemove', onMove);
                    document.removeEventListener('mouseup', onUp);
                    handle.classList.remove('active');
                    document.body.style.cursor = '';
                    document.body.style.userSelect = '';

                    var finalWidth = th.offsetWidth;
                    setColumnWidth(colKey, finalWidth);
                    _resizeState = null;
                }

                document.addEventListener('mousemove', onMove);
                document.addEventListener('mouseup', onUp);
            });

            // Double-click: reset to default width
            handle.addEventListener('dblclick', function (e) {
                e.preventDefault();
                e.stopPropagation();
                var colKey = th.getAttribute('data-col-key');
                var cfgEntry = _columnConfig.find(function (c) { return c.key === colKey; });
                var defaultW = cfgEntry ? cfgEntry.defaultWidth : null;
                th.style.width = defaultW ? defaultW + 'px' : '';
                th.style.minWidth = defaultW ? defaultW + 'px' : '';
                setColumnWidth(colKey, defaultW || null);
            });
        });
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------
    window.columnPreferences = {
        hideColumn: hideColumn,
        showColumn: showColumn,
        showAllColumns: showAllColumns,
        setColumnWidth: setColumnWidth,
        getViewConfig: function () { return _viewConfig; },
        getColumnConfig: function () { return _columnConfig; },
        getCurrentSettings: function () { return _settings; },
        scheduleSave: scheduleSave,
        resetToDefault: resetToDefault
    };

    // -------------------------------------------------------------------------
    // Init
    // -------------------------------------------------------------------------
    function readConfig() {
        var viewConfigEl = document.getElementById('view-config');
        var colConfigEl = document.getElementById('column-config');
        if (!viewConfigEl || !colConfigEl) return false;
        try {
            _viewConfig = JSON.parse(viewConfigEl.textContent);
            _columnConfig = JSON.parse(colConfigEl.textContent);
        } catch (e) {
            console.error('[column-preferences] failed to parse config:', e);
            return false;
        }
        return true;
    }

    document.addEventListener('DOMContentLoaded', function () {
        _table = document.querySelector('table[data-view-key]');
        if (!_table) return;

        if (!readConfig()) {
            // No config blocks — dispatch ready immediately so table-filter doesn't hang
            document.dispatchEvent(new CustomEvent('column-preferences-ready'));
            return;
        }

        loadSettings(function () {
            applySettings();
            insertGearButton();
            attachContextMenus();
            attachResizeHandles();

            // Signal to table-filter.js that we are done
            document.dispatchEvent(new CustomEvent('column-preferences-ready'));

            // After table-filter.js creates the filter row, re-apply visibility
            // so hidden columns also have their filter cells hidden
            var thead = _table.querySelector('thead');
            if (thead) {
                var observer = new MutationObserver(function () {
                    var filterRow = getFilterRow();
                    if (filterRow) {
                        observer.disconnect();
                        // Re-apply visibility to filter row cells
                        _settings.columns.forEach(function (cs) {
                            if (cs.visible === false) {
                                var idx = colIndexByKey(cs.key);
                                if (idx >= 0) {
                                    var filterCells = filterRow.querySelectorAll('th');
                                    if (filterCells[idx]) filterCells[idx].style.display = 'none';
                                }
                            }
                        });
                    }
                });
                observer.observe(thead, { childList: true });
            }
        });
    });

})();
