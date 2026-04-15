(function () {
    'use strict';
    const grid = document.getElementById('cockpitGrid');
    const serverTimeEl = document.getElementById('serverTime');

    function colorFor(tile) {
        if (tile.status === 'Idle') return 'bg-secondary';
        if (tile.status === 'Paused') return 'bg-warning';
        switch (tile.bookingType) {
            case 'Setup': return 'bg-orange';
            case 'Production': return 'bg-success';
            case 'Activity': return 'bg-info';
            default: return 'bg-secondary';
        }
    }

    function fmt(secs) {
        const h = Math.floor(secs/3600), m = Math.floor((secs%3600)/60);
        return h > 0 ? `${h}h ${m}m` : `${m}m`;
    }

    function render(data) {
        grid.innerHTML = data.workplaces.map(t => {
            if (t.status === 'Idle') {
                return `<div class="col-md-4 col-lg-3"><div class="card text-white ${colorFor(t)}">
                    <div class="card-body"><h5>${t.workplaceName}</h5><em>Frei</em></div></div></div>`;
            }
            const target = t.orderNumber
                ? `${t.orderNumber} / ${t.operationNumber} — ${t.operationName}`
                : t.activityName;
            return `<div class="col-md-4 col-lg-3"><div class="card text-white ${colorFor(t)}">
                <div class="card-body">
                    <h5>${t.workplaceName}</h5>
                    <div><strong>${t.bookingType}</strong> · ${t.status}</div>
                    <div class="small">${target ?? ''}</div>
                    <div class="small">${t.operatorName}</div>
                    <div class="small">Läuft seit: ${fmt(t.runtimeSeconds)}</div>
                </div></div></div>`;
        }).join('');
        serverTimeEl.textContent = new Date(data.serverTime).toLocaleTimeString();
    }

    async function tick() {
        try {
            const r = await fetch('/api/bde/cockpit');
            if (r.ok) render(await r.json());
        } catch (e) { /* ignore transient */ }
    }

    tick();
    setInterval(tick, 5000);
})();
