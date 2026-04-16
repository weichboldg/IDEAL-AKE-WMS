(function () {
    'use strict';

    const terminalId = parseInt(document.getElementById('terminalId').value);
    let workplaceId = parseInt(document.getElementById('workplaceId').value);
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    let currentOperator = null;   // {id, displayName}
    let currentWorkOp = null;     // {id, orderNumber, operationNumber, name}

    const currentBooking = document.getElementById('currentBooking');
    const actionPanel = document.getElementById('actionPanel');

    // --- Operator Scan ---
    document.getElementById('btnScanOperator').addEventListener('click', scanOperatorInput);
    document.getElementById('scanOperator').addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); scanOperatorInput(); }
    });

    async function scanOperatorInput() {
        const input = document.getElementById('scanOperator');
        const raw = input.value.trim();
        input.value = '';
        if (!raw) return;
        const feedback = document.getElementById('operatorFeedback');
        const r = await fetch(`/api/bde/operator/${encodeURIComponent(raw)}`);
        if (!r.ok) { feedback.textContent = 'Unbekannte Personalnummer'; return; }
        currentOperator = await r.json();
        feedback.textContent = '';
        showOperatorBadge();
        await renderState();
    }

    function showOperatorBadge() {
        document.getElementById('operatorName').textContent = currentOperator.displayName;
        document.getElementById('operatorInfo').classList.remove('d-none');
        document.getElementById('operatorScan').classList.add('d-none');
        document.getElementById('operationsCard').style.display = '';
    }

    document.getElementById('btnChangeOperator').addEventListener('click', () => {
        currentOperator = null;
        currentWorkOp = null;
        document.getElementById('operatorInfo').classList.add('d-none');
        document.getElementById('operatorScan').classList.remove('d-none');
        document.getElementById('operationsCard').style.display = 'none';
        document.getElementById('operationButtons').innerHTML = '';
        document.getElementById('scanOperator').focus();
        actionPanel.innerHTML = '';
    });

    // --- FA/AG Scan ---
    document.getElementById('btnScanFaAg').addEventListener('click', scanFaAgInput);
    document.getElementById('scanFaAg').addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); scanFaAgInput(); }
    });

    async function scanFaAgInput() {
        const input = document.getElementById('scanFaAg');
        const raw = input.value.trim();
        input.value = '';
        if (!raw) return;
        const feedback = document.getElementById('faAgFeedback');
        const parts = raw.split(/[,\/]/);
        if (parts.length < 2) {
            feedback.textContent = 'Format: FA-Nummer,AG-Nummer oder FA-Nummer/AG-Nummer';
            return;
        }
        const [fa, op] = parts;
        const r = await fetch(`/api/bde/workoperation?faNumber=${encodeURIComponent(fa.trim())}&opNumber=${encodeURIComponent(op.trim())}`);
        if (!r.ok) { feedback.textContent = 'Arbeitsgang nicht gefunden'; return; }
        currentWorkOp = await r.json();
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
            currentBooking.innerHTML = renderActive(active);
            actionPanel.innerHTML = renderActionsForActive(active);
        } else {
            currentBooking.innerHTML = '<em class="text-muted">Keine aktive Buchung</em>';
            actionPanel.innerHTML = currentWorkOp ? renderStartButtons() : '';
        }
        bindActionHandlers();
    }

    function renderActive(b) { return `<strong>${b.bookingType}</strong> seit ${new Date(b.startedAt).toLocaleTimeString()}`; }
    function renderActionsForActive(b) {
        if (b.status === 'Running' && b.bookingType === 'Setup') return buttons(['pause','finishSetup','startProduction']);
        if (b.status === 'Running' && b.bookingType === 'Production') return buttons(['pause','reportPartial','finish']);
        if (b.status === 'Running' && b.bookingType === 'Activity') return buttons(['finishActivity']);
        return '';
    }
    function renderStartButtons() { return buttons(['startSetup','startProduction']); }
    function buttons(ids) {
        const labels = { startSetup:'Rüsten starten', startProduction:'Produktion starten', pause:'Pause', finish:'Beenden (mit Mengen)', finishSetup:'Rüsten beenden', finishActivity:'Beenden', reportPartial:'Teilfertigmeldung' };
        return ids.map(id => `<button data-action="${id}" class="btn btn-primary btn-lg m-1">${labels[id]}</button>`).join('');
    }

    function bindActionHandlers() {
        actionPanel.querySelectorAll('button[data-action]').forEach(btn => {
            btn.addEventListener('click', () => handleAction(btn.dataset.action));
        });
    }

    async function handleAction(action) {
        switch (action) {
            case 'startSetup': await post('/BdeTerminal/StartSetup', { operatorId: currentOperator.id, workOperationId: currentWorkOp.id, workplaceId, terminalId }); break;
            case 'startProduction': await post('/BdeTerminal/StartProduction', { operatorId: currentOperator.id, workOperationId: currentWorkOp.id, workplaceId, terminalId }); break;
            case 'pause': await promptQty(async (good, scrap) => await post('/BdeTerminal/Pause', { bookingId: await activeId(), goodQty: good, scrapQty: scrap })); break;
            case 'finish': await promptQty(async (good, scrap) => await post('/BdeTerminal/Finish', { bookingId: await activeId(), goodQty: good, scrapQty: scrap })); break;
            case 'finishSetup': await post('/BdeTerminal/Finish', { bookingId: await activeId() }); break;
            case 'finishActivity': await post('/BdeTerminal/Finish', { bookingId: await activeId() }); break;
            case 'reportPartial': await promptQty(async (good, scrap) => await post('/BdeTerminal/ReportPartial', { bookingId: await activeId(), goodQty: good, scrapQty: scrap })); break;
        }
        await renderState();
    }

    async function activeId() {
        const r = await fetch(`/api/bde/operator/${currentOperator.id}/active-booking`);
        return (await r.json()).booking?.id;
    }

    async function post(url, data) {
        const form = new FormData();
        Object.keys(data).forEach(k => { if (data[k] !== undefined && data[k] !== null) form.append(k, data[k]); });
        form.append('__RequestVerificationToken', token);
        const r = await fetch(url, { method: 'POST', body: form });
        const json = await r.json();
        if (json.outcome === 'CollisionOtherOperator') {
            document.getElementById('collisionText').textContent =
                `AG ist bereits in Arbeit durch ${json.collidingOperator} an ${json.collidingWorkplace} seit ${new Date(json.collidingSince).toLocaleTimeString()}.`;
            bootstrap.Modal.getOrCreateInstance(document.getElementById('collisionModal')).show();
        } else if (json.outcome === 'QuantityRequired') {
            document.getElementById('faAgFeedback').textContent = 'Mengen-Eingabe erforderlich — bitte aktuelle Buchung beenden.';
        }
    }

    function promptQty(callback) {
        return new Promise(resolve => {
            const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('quantityModal'));
            modal.show();
            document.getElementById('inputGood').value = '';
            document.getElementById('inputScrap').value = '0';
            document.getElementById('btnQtyOk').onclick = async () => {
                const g = parseFloat(document.getElementById('inputGood').value || '0');
                const s = parseFloat(document.getElementById('inputScrap').value || '0');
                modal.hide();
                await callback(g, s);
                resolve();
            };
            document.getElementById('btnQtyCancel').onclick = () => { modal.hide(); resolve(); };
        });
    }

    // Aktivitäts-Flow (via modal — still reachable from activity modal)
    // The activityModal is kept for backward compatibility; Task 3 will add AG buttons
    // to #operationButtons including unplanned activities
    document.getElementById('btnActivityOk').addEventListener('click', async () => {
        const activityId = parseInt(document.getElementById('activitySelect').value);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('activityModal')).hide();
        await post('/BdeTerminal/StartActivity', { operatorId: currentOperator.id, activityId, workplaceId, terminalId });
        await renderState();
    });

    // Werkbank-Umschaltung
    document.getElementById('workplaceSwitch').addEventListener('change', (e) => {
        window.location.href = '/BdeTerminal/Index?workplaceId=' + e.target.value;
    });

    renderState();
})();
