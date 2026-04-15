(function () {
    'use strict';

    const terminalId = parseInt(document.getElementById('terminalId').value);
    let workplaceId = parseInt(document.getElementById('workplaceId').value);
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    let currentOperator = null;   // {id, displayName}
    let currentWorkOp = null;     // {id, orderNumber, operationNumber, name}

    const scanInput = document.getElementById('scanInput');
    const scanFeedback = document.getElementById('scanFeedback');
    const currentBooking = document.getElementById('currentBooking');
    const actionPanel = document.getElementById('actionPanel');

    // Scan-Handler: Enter-Taste = Scan abgeschlossen
    scanInput.addEventListener('keydown', async (e) => {
        if (e.key !== 'Enter') return;
        e.preventDefault();
        const raw = scanInput.value.trim();
        scanInput.value = '';
        if (!raw) return;
        await handleScan(raw);
    });

    async function handleScan(raw) {
        // Heuristik: enthält Komma oder Bindestrich + nur Ziffern-Prefix → FA-AG
        // Alles andere → Personalnummer
        // (Für Produktion ggf. mit Prefix-Konvention aus QR-Code arbeiten)
        if (/^[A-Z0-9\-]+,[0-9]+/i.test(raw) || raw.includes('/')) {
            await scanFaAg(raw);
        } else {
            await scanOperator(raw);
        }
        renderState();
    }

    async function scanOperator(personnelNumber) {
        const r = await fetch(`/api/bde/operator/${encodeURIComponent(personnelNumber)}`);
        if (!r.ok) { scanFeedback.textContent = 'Unbekannte Personalnummer'; return; }
        currentOperator = await r.json();
        scanFeedback.textContent = `Operator: ${currentOperator.displayName}`;
    }

    async function scanFaAg(raw) {
        // Format erwartet: "FA-123456,10" oder "FA-123456/10"
        const [fa, op] = raw.split(/[,\/]/);
        const r = await fetch(`/api/bde/workoperation?faNumber=${encodeURIComponent(fa)}&opNumber=${encodeURIComponent(op)}`);
        if (!r.ok) { scanFeedback.textContent = 'Arbeitsgang nicht gefunden'; return; }
        currentWorkOp = await r.json();
        scanFeedback.textContent = `AG: ${currentWorkOp.orderNumber} / ${currentWorkOp.operationNumber} — ${currentWorkOp.name}`;
    }

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

    function renderActive(b) { /* HTML für aktive Buchung */ return `<strong>${b.bookingType}</strong> seit ${new Date(b.startedAt).toLocaleTimeString()}`; }
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
            scanFeedback.textContent = 'Mengen-Eingabe erforderlich — bitte aktuelle Buchung beenden.';
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

    // Aktivitäts-Flow
    document.getElementById('btnActivity').addEventListener('click', async () => {
        if (!currentOperator) { scanFeedback.textContent = 'Zuerst Operator scannen'; return; }
        const r = await fetch('/api/bde/activities');
        const list = await r.json();
        const sel = document.getElementById('activitySelect');
        sel.innerHTML = list.map(a => `<option value="${a.id}">${a.code} — ${a.name}</option>`).join('');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('activityModal')).show();
    });
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
