(function () {
    'use strict';

    console.log('BDE Terminal JS initializing...');

    const terminalIdEl = document.getElementById('terminalId');
    const workplaceIdEl = document.getElementById('workplaceId');
    const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');

    if (!terminalIdEl || !workplaceIdEl || !tokenEl) {
        console.error('BDE Terminal: required hidden inputs missing', { terminalIdEl, workplaceIdEl, tokenEl });
        return;
    }

    const terminalId = parseInt(terminalIdEl.value);
    let workplaceId = parseInt(workplaceIdEl.value);
    const token = tokenEl.value;
    const nurFaMode = document.getElementById('nurFaMode')?.value === 'true';

    console.log('BDE Terminal initialized:', { terminalId, workplaceId, nurFaMode });

    let currentOperator = null;   // {id, displayName}
    let currentWorkOp = null;     // {id, orderNumber, operationNumber, name}
    let timerInterval = null;

    const actionPanel = document.getElementById('actionPanel');

    // --- Toast Notifications ---
    function showToast(message, type) {
        type = type || 'success';
        var container = document.getElementById('toastContainer');
        var id = 'toast-' + Date.now();
        var bgClass = type === 'success' ? 'bg-success' : type === 'warning' ? 'bg-warning' : 'bg-danger';
        var html = '<div id="' + id + '" class="toast align-items-center text-white ' + bgClass + ' border-0 show" role="alert">' +
            '<div class="d-flex">' +
            '<div class="toast-body fs-5">' + message + '</div>' +
            '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>' +
            '</div></div>';
        container.insertAdjacentHTML('beforeend', html);
        setTimeout(function () {
            var el = document.getElementById(id);
            if (el) el.remove();
        }, 3000);
    }

    function getSuccessMessage(action) {
        var messages = {
            'startSetup': 'Rüsten gestartet',
            'startProduction': 'Produktion gestartet',
            'startActivity': 'Aktivität gestartet',
            'pause': 'Pausiert',
            'resume': 'Fortgesetzt',
            'finish': 'Beendet',
            'finishSetup': 'Rüsten beendet',
            'finishActivity': 'Aktivität beendet',
            'reportPartial': 'Teilmenge gemeldet'
        };
        return messages[action] || 'Aktion durchgeführt';
    }

    // --- Elapsed Ticker (for active-bookings-list cards) ---
    let elapsedTickerHandle = null;

    function updateElapsedTickers() {
        var now = Date.now();
        document.querySelectorAll('.elapsed-ticker[data-started-at]').forEach(function (el) {
            var started = new Date(el.dataset.startedAt).getTime();
            var secs = Math.max(0, Math.floor((now - started) / 1000));
            var h = Math.floor(secs / 3600);
            var m = Math.floor((secs % 3600) / 60);
            var s = secs % 60;
            el.textContent = h > 0 ? h + 'h ' + m + 'm ' + s + 's' : m + 'm ' + s + 's';
        });
    }

    function startElapsedTicker() {
        stopElapsedTicker();
        updateElapsedTickers();
        elapsedTickerHandle = setInterval(updateElapsedTickers, 1000);
    }

    function stopElapsedTicker() {
        if (elapsedTickerHandle) { clearInterval(elapsedTickerHandle); elapsedTickerHandle = null; }
    }

    // --- Operator Scan ---
    document.getElementById('btnScanOperator').addEventListener('click', scanOperatorInput);
    document.getElementById('scanOperator').addEventListener('keydown', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); scanOperatorInput(); }
    });

    async function scanOperatorInput() {
        const input = document.getElementById('scanOperator');
        const raw = input.value.trim();
        input.value = '';
        if (!raw) return;
        const feedback = document.getElementById('operatorFeedback');
        try {
            const r = await fetch(`/api/bde/operator/${encodeURIComponent(raw)}`);
            if (r.status === 404) {
                feedback.textContent = 'Unbekannte Personalnummer: ' + raw;
                return;
            }
            if (!r.ok) {
                feedback.textContent = 'Fehler: HTTP ' + r.status + ' — ' + r.statusText;
                return;
            }
            currentOperator = await r.json();
            feedback.textContent = '';
            showOperatorBadge();
            await renderState();
            await loadAvailableOperations();
            await loadTodayHistory();
            await loadPausedBookings(currentOperator.id);
            await loadActiveBookings(currentOperator.id);
        } catch (err) {
            feedback.textContent = 'Netzwerkfehler: ' + err.message;
            console.error('BDE Operator Scan Error:', err);
        }
    }

    function showOperatorBadge() {
        document.getElementById('operatorName').textContent = currentOperator.displayName;
        document.getElementById('operatorInfo').classList.remove('d-none');
        document.getElementById('operatorScan').classList.add('d-none');
        document.getElementById('operationsCard').style.display = '';
        document.getElementById('historyCard').style.display = '';
    }

    document.getElementById('btnChangeOperator').addEventListener('click', function () {
        currentOperator = null;
        currentWorkOp = null;
        stopElapsedTicker();
        document.getElementById('operatorInfo').classList.add('d-none');
        document.getElementById('operatorScan').classList.remove('d-none');
        document.getElementById('operationsCard').style.display = 'none';
        document.getElementById('historyCard').style.display = 'none';
        document.getElementById('operationButtons').innerHTML = '';
        document.getElementById('scanOperator').focus();
        actionPanel.innerHTML = '';
        // Reset active-bookings panel
        var emptyMsg = document.getElementById('active-bookings-empty');
        var list = document.getElementById('active-bookings-list');
        if (emptyMsg) emptyMsg.classList.remove('d-none');
        if (list) list.innerHTML = '';
        var pausedHint = document.getElementById('paused-bookings-hint');
        if (pausedHint) { pausedHint.classList.add('d-none'); document.getElementById('paused-bookings-list').innerHTML = ''; }
    });

    // --- FA/AG Scan ---
    document.getElementById('btnScanFaAg').addEventListener('click', scanFaAgInput);
    document.getElementById('scanFaAg').addEventListener('keydown', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); scanFaAgInput(); }
    });

    async function scanFaAgInput() {
        const input = document.getElementById('scanFaAg');
        const raw = input.value.trim();
        input.value = '';
        if (!raw) return;
        const feedback = document.getElementById('faAgFeedback');

        if (nurFaMode) {
            // NurFA: nur FA-Nummer, AG-Teil ignorieren
            var faNumber = raw.split(/[,\/]/)[0].trim();
            // Suche den passenden FA-Button und klicke ihn
            var faBtn = null;
            document.querySelectorAll('.bde-op-btn[data-type="fa"]').forEach(function (btn) {
                if (btn.textContent.indexOf(faNumber) !== -1) faBtn = btn;
            });
            if (faBtn) {
                feedback.textContent = '';
                faBtn.click();
            } else {
                // Fallback: Versuche ueber API den Arbeitsgang zu finden
                var r = await fetch('/api/bde/workoperation?faNumber=' + encodeURIComponent(faNumber) + '&opNumber=01');
                if (!r.ok) {
                    feedback.textContent = 'FA nicht gefunden';
                    return;
                }
                var wo = await r.json();
                await post('/BdeTerminal/StartProduction', {
                    operatorId: currentOperator.id,
                    workOperationId: wo.id,
                    workplaceId: workplaceId, terminalId: terminalId
                }, 'startProduction');
                await renderState();
                await loadAvailableOperations();
                await loadTodayHistory();
            }
            return;
        }

        // Normaler Modus
        const parts = raw.split(/[,\/]/);
        if (parts.length < 2) {
            feedback.textContent = 'Format: FA-Nummer,AG-Nummer oder FA-Nummer/AG-Nummer';
            return;
        }
        const [fa, op] = parts;
        var resp = await fetch(`/api/bde/workoperation?faNumber=${encodeURIComponent(fa.trim())}&opNumber=${encodeURIComponent(op.trim())}`);
        if (!resp.ok) { feedback.textContent = 'Arbeitsgang nicht gefunden'; return; }
        currentWorkOp = await resp.json();
        feedback.textContent = '';
        await renderState();
    }

    // --- State Rendering ---
    async function renderState() {
        if (!currentOperator) { actionPanel.innerHTML = '<em class="text-muted">Zuerst Operator scannen</em>'; return; }
        // Refresh active-bookings panel and derive action panel state from the same data
        await loadActiveBookings(currentOperator.id);
        // Laufende Buchung holen (fuer Action-Panel)
        const r = await fetch(`/api/bde/operator/${currentOperator.id}/active-booking`);
        const data = await r.json();
        const active = data.booking;
        if (active) {
            actionPanel.innerHTML = renderActionsForActive(active);
        } else {
            actionPanel.innerHTML = currentWorkOp ? renderStartButtons() : '';
        }
        bindActionHandlers();
    }

    function renderActionsForActive(b) {
        if (b.status === 'Running' && b.bookingType === 'Setup') return buttons(['pause','finishSetup','startProduction']);
        if (b.status === 'Running' && b.bookingType === 'Production') return buttons(['pause','reportPartial','finish']);
        if (b.status === 'Running' && b.bookingType === 'Activity') return buttons(['finishActivity']);
        return '';
    }
    function renderStartButtons() {
        if (nurFaMode) return buttons(['startProduction']);
        return buttons(['startSetup','startProduction']);
    }
    function buttons(ids) {
        var labels = { startSetup:'Rüsten starten', startProduction:'Produktion starten', pause:'Pause', finish:'Beenden (mit Mengen)', finishSetup:'Rüsten beenden', finishActivity:'Beenden', reportPartial:'Teilfertigmeldung' };
        return ids.map(function (id) { return '<button data-action="' + id + '" class="btn btn-primary btn-lg m-1">' + labels[id] + '</button>'; }).join('');
    }

    function bindActionHandlers() {
        actionPanel.querySelectorAll('button[data-action]').forEach(function (btn) {
            btn.addEventListener('click', function () { handleAction(btn.dataset.action); });
        });
    }

    async function handleAction(action) {
        switch (action) {
            case 'startSetup': await post('/BdeTerminal/StartSetup', { operatorId: currentOperator.id, workOperationId: currentWorkOp.id, workplaceId: workplaceId, terminalId: terminalId }, action); break;
            case 'startProduction': await post('/BdeTerminal/StartProduction', { operatorId: currentOperator.id, workOperationId: currentWorkOp.id, workplaceId: workplaceId, terminalId: terminalId }, action); break;
            case 'pause': await promptQty(async function (good, scrap) { await post('/BdeTerminal/Pause', { bookingId: await activeId(), goodQty: good, scrapQty: scrap }, action); }); break;
            case 'finish': await promptQty(async function (good, scrap) {
                const response = await post('/BdeTerminal/Finish', { bookingId: await activeId(), goodQty: good, scrapQty: scrap }, action);
                if (response && response.outcome === 'Success' && response.otherActiveBookings && response.otherActiveBookings.length > 0) {
                    showCloseOthersModal(response);
                }
            }); break;
            case 'finishSetup': await post('/BdeTerminal/Finish', { bookingId: await activeId() }, action); break;
            case 'finishActivity': await post('/BdeTerminal/Finish', { bookingId: await activeId() }, action); break;
            case 'reportPartial': await promptQty(async function (good, scrap) { await post('/BdeTerminal/ReportPartial', { bookingId: await activeId(), goodQty: good, scrapQty: scrap }, action); }); break;
        }
        await renderState();
        await loadAvailableOperations();
        await loadTodayHistory();
    }

    async function activeId() {
        const r = await fetch(`/api/bde/operator/${currentOperator.id}/active-booking`);
        return (await r.json()).booking?.id;
    }

    async function post(url, data, actionName) {
        const form = new FormData();
        Object.keys(data).forEach(function (k) { if (data[k] !== undefined && data[k] !== null) form.append(k, data[k]); });
        form.append('__RequestVerificationToken', token);
        const r = await fetch(url, { method: 'POST', body: form });
        const json = await r.json();
        if (json.outcome === 'Success') {
            showToast(getSuccessMessage(actionName));
        } else if (json.outcome === 'CollisionOtherOperator') {
            document.getElementById('collisionText').textContent =
                'AG ist bereits in Arbeit durch ' + json.collidingOperator + ' an ' + json.collidingWorkplace + ' seit ' + new Date(json.collidingSince).toLocaleTimeString() + '.';
            bootstrap.Modal.getOrCreateInstance(document.getElementById('collisionModal')).show();
        } else if (json.outcome === 'QuantityRequired') {
            showToast('Mengen-Eingabe erforderlich', 'warning');
        }
        return json;
    }

    function promptQty(callback, targetQty) {
        return new Promise(function (resolve) {
            const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('quantityModal'));
            const hint = document.getElementById('qty-sollmenge-hint');
            if (hint) {
                if (targetQty != null) {
                    hint.textContent = 'Sollmenge: ' + targetQty;
                    hint.classList.remove('d-none');
                } else {
                    hint.textContent = '';
                    hint.classList.add('d-none');
                }
            }
            modal.show();
            document.getElementById('inputGood').value = '';
            document.getElementById('inputScrap').value = '0';
            document.getElementById('btnQtyOk').onclick = async function () {
                const g = parseFloat(document.getElementById('inputGood').value || '0');
                const s = parseFloat(document.getElementById('inputScrap').value || '0');
                modal.hide();
                await callback(g, s);
                resolve();
            };
            document.getElementById('btnQtyCancel').onclick = function () { modal.hide(); resolve(); };
        });
    }

    // --- Today's History ---
    async function loadTodayHistory() {
        if (!currentOperator) return;
        var r = await fetch('/api/bde/operator/' + currentOperator.id + '/today-history');
        if (!r.ok) return;
        var items = await r.json();
        var container = document.getElementById('todayHistory');
        if (items.length === 0) {
            container.innerHTML = '<em class="text-muted">Noch keine Buchungen heute.</em>';
            return;
        }
        var typeLabels = { 'Setup': 'Rüsten', 'Production': 'Produktion', 'Activity': 'Aktivität' };
        var typeColors = { 'Setup': 'bg-orange', 'Production': 'bg-success', 'Activity': 'bg-info' };
        var rows = items.map(function (i) {
            var start = new Date(i.startedAt).toLocaleTimeString('de-AT', { hour: '2-digit', minute: '2-digit' });
            var end = i.endedAt ? new Date(i.endedAt).toLocaleTimeString('de-AT', { hour: '2-digit', minute: '2-digit' }) : '—';
            var label = typeLabels[i.bookingType] || i.bookingType;
            var color = typeColors[i.bookingType] || 'bg-secondary';
            return '<tr>' +
                '<td>' + start + '–' + end + '</td>' +
                '<td><span class="badge ' + color + '">' + label + '</span></td>' +
                '<td>' + (i.target || '—') + '</td>' +
                '<td>' + i.durationMinutes + 'm</td>' +
                '<td>' + (i.totalGood > 0 ? i.totalGood : '') + '</td>' +
                '<td>' + (i.totalScrap > 0 ? i.totalScrap : '') + '</td>' +
                '</tr>';
        }).join('');
        container.innerHTML = '<div class="table-responsive"><table class="table table-sm table-striped mb-0">' +
            '<thead><tr><th>Zeit</th><th>Typ</th><th>Arbeitsgang</th><th>Dauer</th><th>Gut</th><th>Aus.</th></tr></thead>' +
            '<tbody>' + rows + '</tbody></table></div>';
    }

    // --- Available Operations (AG Buttons) ---
    async function loadAvailableOperations() {
        const r = await fetch(`/api/bde/available-operations/${workplaceId}`);
        if (!r.ok) return;
        const data = await r.json();
        renderOperationButtons(data.productive, data.unplanned);
    }

    function renderOperationButtons(productive, unplanned) {
        const container = document.getElementById('operationButtons');
        let html = '';

        if (productive.length > 0) {
            html += nurFaMode
                ? '<h6 class="mt-2 text-muted">Produktionsaufträge</h6>'
                : '<h6 class="mt-2 text-muted">Produktive Arbeitsgänge</h6>';
            html += '<div class="d-flex flex-wrap gap-2 mb-3">';
            productive.forEach(function (op) {
                if (op.type === 'fa') {
                    html += '<button class="btn btn-outline-success btn-lg bde-op-btn" data-fa-id="' + op.id + '" data-type="fa">' + op.label + '</button>';
                } else {
                    html += '<button class="btn btn-outline-success btn-lg bde-op-btn" data-wo-id="' + op.id + '" data-type="productive">' + op.label + '</button>';
                }
            });
            html += '</div>';
        }

        if (!nurFaMode && unplanned.length > 0) {
            html += '<h6 class="text-muted">Ungeplante Tätigkeiten</h6>';
            html += '<div class="d-flex flex-wrap gap-2 mb-3">';
            unplanned.forEach(function (a) {
                html += '<button class="btn btn-outline-secondary btn-lg bde-op-btn" data-activity-id="' + a.id + '" data-type="unplanned">' + a.label + '</button>';
            });
            html += '</div>';
        }

        if (!productive.length && (nurFaMode || !unplanned.length)) {
            html = nurFaMode
                ? '<p class="text-muted">Keine offenen Produktionsaufträge an dieser Werkbank.</p>'
                : '<p class="text-muted">Keine offenen Arbeitsgänge an dieser Werkbank.</p>';
        }

        container.innerHTML = html;
        bindOperationButtonHandlers();
    }

    function bindOperationButtonHandlers() {
        document.querySelectorAll('.bde-op-btn').forEach(function (btn) {
            btn.addEventListener('click', async function () {
                if (btn.dataset.type === 'fa') {
                    // NurFA: direkt Produktion auf FA starten
                    await post('/BdeTerminal/StartProductionForOrder', {
                        operatorId: currentOperator.id,
                        productionOrderId: parseInt(btn.dataset.faId),
                        workplaceId: workplaceId, terminalId: terminalId
                    }, 'startProduction');
                    await renderState();
                    await loadAvailableOperations();
                    await loadTodayHistory();
                } else if (btn.dataset.type === 'productive') {
                    currentWorkOp = { id: parseInt(btn.dataset.woId) };
                    // Show action buttons (Rüsten/Produktion)
                    await renderState();
                } else {
                    // Start unplanned activity directly
                    await post('/BdeTerminal/StartActivity', {
                        operatorId: currentOperator.id,
                        activityId: parseInt(btn.dataset.activityId),
                        workplaceId: workplaceId, terminalId: terminalId
                    }, 'startActivity');
                    await renderState();
                    await loadAvailableOperations();
                    await loadTodayHistory();
                }
            });
        });
    }

    function clearOperationButtons() {
        var container = document.getElementById('operationButtons');
        if (container) container.innerHTML = '';
    }

    // Aktivitäts-Flow (via modal — still reachable from activity modal)
    document.getElementById('btnActivityOk').addEventListener('click', async function () {
        var activityId = parseInt(document.getElementById('activitySelect').value);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('activityModal')).hide();
        await post('/BdeTerminal/StartActivity', { operatorId: currentOperator.id, activityId: activityId, workplaceId: workplaceId, terminalId: terminalId }, 'startActivity');
        await renderState();
        await loadAvailableOperations();
        await loadTodayHistory();
    });

    // Werkbank-Umschaltung
    document.getElementById('workplaceSwitch').addEventListener('change', function (e) {
        window.location.href = '/BdeTerminal/Index?workplaceId=' + e.target.value;
    });

    // NurFA-Modus: UI-Anpassungen
    if (nurFaMode) {
        var scanFaAgEl = document.getElementById('scanFaAg');
        if (scanFaAgEl) scanFaAgEl.placeholder = 'Oder FA-Nr scannen\u2026';
        // Aktivitaets-Modal ausblenden (nicht erreichbar im NurFA-Modus)
        var activityModal = document.getElementById('activityModal');
        if (activityModal) activityModal.style.display = 'none';
    }

    // --- Paused Bookings Hint ---
    async function loadPausedBookings(operatorId) {
        const hint = document.getElementById('paused-bookings-hint');
        const list = document.getElementById('paused-bookings-list');
        if (!hint || !list) return;

        try {
            const res = await fetch(`/BdeTerminal/PausedBookings?operatorId=${operatorId}`);
            if (!res.ok) { hint.classList.add('d-none'); return; }
            const items = await res.json();

            if (!items || items.length === 0) {
                hint.classList.add('d-none');
                list.innerHTML = '';
                return;
            }

            list.innerHTML = items.map(function (i) {
                return '<li class="mb-2">' +
                    '<strong>' + i.orderNumber + ' / ' + i.operationNumber + ' ' + (i.operationName || '') + '</strong>' +
                    '<small class="text-muted d-block">pausiert seit ' + (i.pausedAt ? new Date(i.pausedAt).toLocaleString('de-DE') : '') + '</small>' +
                    '<button type="button" class="btn btn-sm btn-warning mt-1" data-booking-id="' + i.bookingId + '" data-resume>Fortsetzen</button>' +
                    '</li>';
            }).join('');
            hint.classList.remove('d-none');
        } catch (e) {
            console.error('Error loading paused bookings', e);
            hint.classList.add('d-none');
        }
    }

    // Fortsetzen-Button-Handler (event delegation, einmalig registriert)
    document.getElementById('paused-bookings-list').addEventListener('click', async function (e) {
        var btn = e.target.closest('[data-resume]');
        if (!btn) return;
        var bookingId = parseInt(btn.dataset.bookingId, 10);
        if (!currentOperator) return;

        var form = new FormData();
        form.append('pausedBookingId', bookingId);
        form.append('operatorId', currentOperator.id);
        form.append('resumeAs', '2'); // 2 = Production
        form.append('workplaceId', workplaceId);
        form.append('terminalId', terminalId);
        form.append('__RequestVerificationToken', token);

        try {
            var res = await fetch('/BdeTerminal/Resume', { method: 'POST', body: form });
            if (!res.ok) {
                showToast('Fortsetzen fehlgeschlagen (HTTP ' + res.status + ')', 'danger');
                return;
            }
            var data = await res.json();
            if (data.outcome !== 'Success') {
                var msg = 'Fortsetzen nicht möglich';
                if (data.outcome === 'CollisionOtherOperator' && data.collidingOperator) {
                    msg = 'Arbeitsgang wird bereits von ' + data.collidingOperator + ' bearbeitet';
                    if (data.collidingSince) {
                        var since = new Date(data.collidingSince).toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' });
                        msg += ' (seit ' + since + ')';
                    }
                } else if (data.message) {
                    msg = data.message;
                } else if (data.outcome) {
                    msg = 'Fortsetzen fehlgeschlagen: ' + data.outcome;
                }
                showToast(msg, 'danger');
                return;
            }
            btn.closest('li')?.remove();
            if (!document.querySelectorAll('#paused-bookings-list li').length) {
                document.getElementById('paused-bookings-hint').classList.add('d-none');
            }
            await renderState();
            await loadTodayHistory();
        } catch (err) {
            console.error('Error resuming booking', err);
            showToast('Fortsetzen fehlgeschlagen', 'danger');
        }
    });

    // --- Active Bookings Panel (merged into "Aktuelle Buchungen" card) ---
    async function loadActiveBookings(operatorId) {
        var emptyMsg = document.getElementById('active-bookings-empty');
        var list = document.getElementById('active-bookings-list');
        if (!emptyMsg || !list) return;

        try {
            var res = await fetch('/BdeTerminal/ActiveBookings?operatorId=' + operatorId);
            if (!res.ok) { emptyMsg.classList.remove('d-none'); list.innerHTML = ''; stopElapsedTicker(); return; }
            var items = await res.json();

            if (!items || items.length === 0) {
                emptyMsg.classList.remove('d-none');
                list.innerHTML = '';
                stopElapsedTicker();
                return;
            }

            emptyMsg.classList.add('d-none');
            list.innerHTML = items.map(function (i) { return renderActiveBookingItem(i); }).join('');
            startElapsedTicker();
        } catch (e) {
            console.error('Error loading active bookings', e);
        }
    }

    function renderActiveBookingItem(i) {
        var typeLabel = i.bookingType === 'Production' ? 'Produktion'
            : i.bookingType === 'Setup' ? 'Rüsten'
            : (i.activityName || 'Tätigkeit');
        var badgeClass = i.bookingType === 'Production' ? 'bg-success'
            : i.bookingType === 'Setup' ? 'bg-info text-dark'
            : 'bg-secondary';
        var startedLocal = new Date(i.startedAt).toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' });
        var orderInfo = i.bookingType === 'Activity'
            ? (i.activityName || '')
            : (i.orderNumber + ' / ' + i.operationNumber + (i.operationName ? ' \u2014 ' + i.operationName : ''));
        var workplaceRow = i.workplaceName ? '<div class="text-muted small">Werkbank: ' + i.workplaceName + '</div>' : '';
        var sollmengeRow = (i.targetQuantity != null) ? '<div class="text-muted small">Sollmenge: ' + i.targetQuantity + '</div>' : '';

        return '<div class="active-booking-item px-3 py-3 border-bottom" data-booking-id="' + i.bookingId + '" data-target-quantity="' + (i.targetQuantity != null ? i.targetQuantity : '') + '">' +
            '<div class="d-flex justify-content-between align-items-start flex-wrap gap-2">' +
            '<div>' +
            '<span class="badge ' + badgeClass + '">' + typeLabel + '</span>' +
            '<span class="ms-2 text-muted">seit ' + startedLocal + '</span>' +
            '<div class="fs-5 mt-1">' + orderInfo + '</div>' +
            workplaceRow +
            sollmengeRow +
            '</div>' +
            '<div class="fs-4 fw-bold elapsed-ticker" data-started-at="' + i.startedAt + '">—</div>' +
            '</div>' +
            '<div class="d-flex gap-2 mt-2 flex-wrap">' +
            '<button type="button" class="btn btn-sm btn-outline-primary" data-action="partial" data-booking-id="' + i.bookingId + '">Teilfertig</button>' +
            '<button type="button" class="btn btn-sm btn-outline-warning" data-action="pause" data-booking-id="' + i.bookingId + '">Pause</button>' +
            '<button type="button" class="btn btn-sm btn-success" data-action="finish" data-booking-id="' + i.bookingId + '">Fertig</button>' +
            '</div>' +
            '</div>';
    }

    async function triggerFinishFlow(bookingId) {
        const card = document.querySelector('.active-booking-item[data-booking-id="' + bookingId + '"]');
        const targetQty = (card && card.dataset.targetQuantity !== '') ? parseFloat(card.dataset.targetQuantity) : null;

        await promptQty(async function (good, scrap) {
            if (targetQty != null && good < targetQty) {
                const ok = confirm('Gutmenge ' + good + ' liegt unter Sollmenge ' + targetQty + '.\nWirklich als fertig melden?');
                if (!ok) return;
            }
            var response = await post('/BdeTerminal/Finish', { bookingId: bookingId, goodQty: good, scrapQty: scrap }, 'finish');
            if (response && response.outcome === 'Success' && response.otherActiveBookings && response.otherActiveBookings.length > 0) {
                showCloseOthersModal(response);
            }
        }, targetQty);
    }

    async function triggerPauseFlow(bookingId) {
        const card = document.querySelector('.active-booking-item[data-booking-id="' + bookingId + '"]');
        const targetQty = (card && card.dataset.targetQuantity !== '') ? parseFloat(card.dataset.targetQuantity) : null;

        await promptQty(async function (good, scrap) {
            await post('/BdeTerminal/Pause', { bookingId: bookingId, goodQty: good, scrapQty: scrap }, 'pause');
        }, targetQty);
    }

    async function triggerPartialFlow(bookingId) {
        const card = document.querySelector('.active-booking-item[data-booking-id="' + bookingId + '"]');
        const targetQty = (card && card.dataset.targetQuantity !== '') ? parseFloat(card.dataset.targetQuantity) : null;

        await promptQty(async function (good, scrap) {
            await post('/BdeTerminal/ReportPartial', { bookingId: bookingId, goodQty: good, scrapQty: scrap }, 'reportPartial');
        }, targetQty);
    }

    document.getElementById('active-bookings-list').addEventListener('click', async function (e) {
        var btn = e.target.closest('button[data-action]');
        if (!btn) return;
        var bookingId = parseInt(btn.dataset.bookingId, 10);
        var action = btn.dataset.action;

        if (action === 'finish') {
            await triggerFinishFlow(bookingId);
        } else if (action === 'pause') {
            await triggerPauseFlow(bookingId);
        } else if (action === 'partial') {
            await triggerPartialFlow(bookingId);
        }

        // Refresh both lists and main state after any action
        if (currentOperator && currentOperator.id) {
            await loadActiveBookings(currentOperator.id);
            await loadPausedBookings(currentOperator.id);
        }
        await renderState();
        await loadTodayHistory();
    });

    // --- Close-Others-Modal ---
    function showCloseOthersModal(response) {
        var list = document.getElementById('close-others-list');
        list.innerHTML = response.otherActiveBookings.map(function (o) {
            return '<li>' + o.operatorName + ' \u2014 seit ' + new Date(o.startedAt).toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' }) + '</li>';
        }).join('');

        var modalEl = document.getElementById('close-others-modal');
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();

        // Alten Event-Listener via cloneNode entfernen, um Mehrfach-Registrierung zu vermeiden
        var confirmBtn = document.getElementById('close-others-confirm');
        var newBtn = confirmBtn.cloneNode(true);
        confirmBtn.parentNode.replaceChild(newBtn, confirmBtn);
        newBtn.addEventListener('click', async function () {
            var workOperationId = response.workOperationId || (currentWorkOp && currentWorkOp.id);
            var form = new FormData();
            form.append('workOperationId', workOperationId);
            form.append('operatorId', currentOperator.id);
            form.append('__RequestVerificationToken', token);

            try {
                var res = await fetch('/BdeTerminal/CloseOthers', { method: 'POST', body: form });
                if (res.ok) {
                    var data = await res.json();
                    modal.hide();
                    showToast(data.closedCount + ' weitere Buchungen beendet.');
                } else {
                    showToast('Schliessen fehlgeschlagen', 'danger');
                }
            } catch (err) {
                console.error('Error closing others', err);
                showToast('Schliessen fehlgeschlagen', 'danger');
            }
        });
    }

    renderState();
})();
