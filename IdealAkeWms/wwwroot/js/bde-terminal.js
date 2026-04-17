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

    const currentBooking = document.getElementById('currentBooking');
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

    // --- Running Timer ---
    function startRunningTimer(startedAt) {
        if (timerInterval) clearInterval(timerInterval);
        var start = new Date(startedAt);
        function update() {
            var diff = Math.floor((Date.now() - start.getTime()) / 1000);
            var h = Math.floor(diff / 3600);
            var m = Math.floor((diff % 3600) / 60);
            var s = diff % 60;
            var el = document.getElementById('runningTimer');
            if (el) el.textContent = h > 0 ? h + 'h ' + m + 'm ' + s + 's' : m + 'm ' + s + 's';
        }
        update();
        timerInterval = setInterval(update, 1000);
    }

    function stopRunningTimer() {
        if (timerInterval) { clearInterval(timerInterval); timerInterval = null; }
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
        stopRunningTimer();
        document.getElementById('operatorInfo').classList.add('d-none');
        document.getElementById('operatorScan').classList.remove('d-none');
        document.getElementById('operationsCard').style.display = 'none';
        document.getElementById('historyCard').style.display = 'none';
        document.getElementById('operationButtons').innerHTML = '';
        document.getElementById('scanOperator').focus();
        actionPanel.innerHTML = '';
        currentBooking.innerHTML = '<em class="text-muted">Keine aktive Buchung</em>';
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
        // Laufende Buchung holen
        const r = await fetch(`/api/bde/operator/${currentOperator.id}/active-booking`);
        const data = await r.json();
        const active = data.booking;
        if (active) {
            currentBooking.innerHTML = renderActiveDetailed(active);
            startRunningTimer(active.startedAt);
            actionPanel.innerHTML = renderActionsForActive(active);
        } else {
            stopRunningTimer();
            currentBooking.innerHTML = '<em class="text-muted">Keine aktive Buchung</em>';
            actionPanel.innerHTML = currentWorkOp ? renderStartButtons() : '';
        }
        bindActionHandlers();
    }

    function renderActiveDetailed(b) {
        var typeColors = { 'Setup': 'bg-orange', 'Production': 'bg-success', 'Activity': 'bg-info' };
        var typeLabels = { 'Setup': 'Rüsten', 'Production': 'Produktion', 'Activity': 'Aktivität' };
        var color = typeColors[b.bookingType] || 'bg-secondary';
        var label = typeLabels[b.bookingType] || b.bookingType;
        var startTime = new Date(b.startedAt).toLocaleTimeString('de-AT', { hour: '2-digit', minute: '2-digit' });
        var statusBadge = b.status === 'Paused' ? ' <span class="badge bg-warning text-dark ms-1">Pausiert</span>' : '';

        var html = '<div class="d-flex align-items-center mb-2">' +
            '<span class="badge ' + color + ' text-white fs-6 me-2">' + label + '</span>' + statusBadge +
            '<span class="text-muted">seit ' + startTime + '</span>' +
            '<span class="ms-auto fw-bold fs-5" id="runningTimer">—</span>' +
            '</div>';
        if (b.target) {
            html += '<div class="fs-5 mb-1">' + b.target + '</div>';
        }
        if (b.workplaceName) {
            html += '<div class="text-muted small">Werkbank: ' + b.workplaceName + '</div>';
        }
        return html;
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
            case 'finish': await promptQty(async function (good, scrap) { await post('/BdeTerminal/Finish', { bookingId: await activeId(), goodQty: good, scrapQty: scrap }, action); }); break;
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
    }

    function promptQty(callback) {
        return new Promise(function (resolve) {
            const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('quantityModal'));
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

    renderState();
})();
