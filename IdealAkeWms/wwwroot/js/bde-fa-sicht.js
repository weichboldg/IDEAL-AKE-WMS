// v1.99.77 (Spec 2026-11-25-bde-masken-vereinheitlichung, Etappe 4b, E4-3..E4-6): Buchen auf der FA-Sicht der BDE-Teileliste.
// Ruft die BESTEHENDEN JSON-Aktionen von BdeTerminal (Pruefungen inkl. Kollision im BdeBookingService), danach Neuladen; die
// Erfolgsmeldung ueberlebt das Neuladen in sessionStorage. Dialoge: _BdeDialoge (gemeinsam mit dem Terminal), Bestaetigung
// window.bdeBestaetigen (bde-teileliste.js), Auswahl #faAuswahlModal. Keine Browser-Dialoge. Doppeltipp: window.wmsSperren (site.js).
(function () {
    'use strict';
    var cfg = document.getElementById('bdeFaSicht');
    if (!cfg) return;
    var operatorId = cfg.dataset.operatorId, workplaceId = cfg.dataset.workplaceId, terminalId = cfg.dataset.terminalId;
    var token = (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value;
    var MELDUNG = 'wms.bdeFaSicht.meldung';
    var box = document.getElementById('faMeldung');
    var modal = function (id) { return bootstrap.Modal.getOrCreateInstance(document.getElementById(id)); };
    var zeit = function (d) { return d ? new Date(d).toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' }) : '–'; };
    var zahl = function (s) { return s != null && s !== '' && !isNaN(parseFloat(s)) ? parseFloat(s) : null; };
    var fmt = function (n) { return n.toLocaleString('de-DE', { maximumFractionDigits: 4 }); };

    function melden(text, art) {
        var d = document.createElement('div');
        d.className = 'alert alert-' + art + ' alert-dismissible fade show';
        d.setAttribute('role', art === 'success' ? 'status' : 'alert');
        d.textContent = text;
        var x = document.createElement('button');
        x.type = 'button'; x.className = 'btn-close'; x.setAttribute('data-bs-dismiss', 'alert'); x.setAttribute('aria-label', 'Schließen');
        d.appendChild(x);
        box.replaceChildren(d);
        box.scrollIntoView({ block: 'nearest' });
    }

    // Neu laden ohne „markiert“ (sonst wuerde der alte Scan-Treffer erneut markiert)
    function neuLaden(text) {
        try { sessionStorage.setItem(MELDUNG, text); } catch (e) { /* ponytail: ohne Speicher keine Meldung nach dem Neuladen, die Liste zeigt den Stand */ }
        var u = new URL(location.href);
        u.searchParams.delete('markiert');
        location.assign(u.toString());
    }
    // v1.99.79 F-26: Laufzeit in „Meine Buchungen“ live (Muster Elapsed-Ticker des alten Terminals); Basis = Serverstand, nur die
    // seit dem Seitenaufbau vergangene Zeit kommt vom Browser – eine falsch gestellte Terminal-Uhr verfaelscht nichts.
    var laufzeiten = document.querySelectorAll('.bde-laufzeit[data-sekunden]');
    if (laufzeiten.length) {
        var t0 = Date.now();
        setInterval(function () {
            var plus = Math.floor((Date.now() - t0) / 1000);
            laufzeiten.forEach(function (el) {
                var s = parseInt(el.dataset.sekunden, 10) + plus;
                var m = Math.floor(s / 60);
                el.textContent = Math.floor(m / 60) + ':' + String(m % 60).padStart(2, '0') + ' h';
            });
        }, 15000);
    }

    try {
        var alt = sessionStorage.getItem(MELDUNG);
        if (alt) { sessionStorage.removeItem(MELDUNG); melden(alt, 'success'); }
    } catch (e) { }

    async function post(url, daten) {
        var f = new FormData();
        Object.keys(daten).forEach(function (k) { if (daten[k] != null) f.append(k, daten[k]); });
        f.append('__RequestVerificationToken', token);
        try {
            var r = await fetch(url, { method: 'POST', body: f });
            if (!r.ok) return { outcome: 'Http', message: 'Serverfehler (HTTP ' + r.status + '). Seite neu laden und Buchung prüfen.' };
            return await r.json();
        } catch (e) {
            return { outcome: 'Http', message: 'Keine Verbindung zum Server. Seite neu laden und Buchung prüfen.' };
        }
    }

    // Ergebnis einer Buchung: Erfolg → ggf. weitere Buchungen beenden, sonst neu laden; Kollision/Gruppe → Dialog; Rest → sichtbare Meldung.
    function ergebnis(json, okText, frei) {
        if (json.outcome !== 'Success' && frei) frei();
        switch (json.outcome) {
            case 'Success':
                if (json.otherActiveBookings && json.otherActiveBookings.length) { andereBeenden(json, okText); return; }
                neuLaden(json.message ? okText + ' ' + json.message : okText); return;  // v1.99.81 U-3: z. B. „Sollmenge erreicht“
            case 'CollisionOtherOperator':
                document.getElementById('collisionText').textContent = 'AG ist bereits in Arbeit durch ' + (json.collidingOperator || 'einen anderen Werker')
                    + ' an ' + (json.collidingWorkplace || 'einer anderen Werkbank') + ' seit ' + zeit(json.collidingSince) + '.';
                // v1.99.79 F-37: Fokus auf „OK“ (Enter/Scanner-Enter schliesst), nicht auf den Dialog-Rahmen
                document.getElementById('collisionModal').addEventListener('shown.bs.modal', function () {
                    this.querySelector('.modal-footer .btn').focus();
                }, { once: true });
                modal('collisionModal').show(); return;
            case 'GroupFinishRequired': gruppe(json); return;
            case 'QuantityRequired': laufendZeigen(json.bookingId); return;
            case 'Http': melden(json.message, 'warning'); return;
            default:
                // v1.99.81: Texte, die schon „nichts … gebucht“ sagen (Serverfehler), nicht doppelt ergaenzen
                var t = json.message || 'Aktion nicht möglich';
                // v1.99.81 (U-5): eigener Satz statt angehaengtem Strich (sonst „… möglich. – nichts gebucht.“)
                melden(/nichts/.test(t) ? t : t.replace(/[.\s]*$/, '.') + ' Nichts gebucht.', 'warning');
        }
    }

    // v1.99.81 (U-1, 8A): Start abgewiesen, weil die eigene Produktion noch laeuft (QuantityRequired mit deren bookingId). Klare Meldung,
    // die Kopfzeile bekommt Rahmen + Text und wird angesprungen (Kopf aufgeklappt, K-1). Ohne passende Kopfzeile der allgemeine Text.
    function laufendZeigen(bookingId) {
        var z = bookingId != null && document.querySelector('.bde-kopf-zeile[data-booking-id="' + bookingId + '"]');
        if (!z) { melden('Mengen-Eingabe erforderlich – nichts gebucht.', 'warning'); return; }
        melden('Es läuft noch Ihre Produktion ' + z.dataset.bez + (z.dataset.start ? ' (seit ' + z.dataset.start + ')' : '')
            + '. Solange sie läuft, kann kein zweiter Arbeitsgang starten – es wurde nichts gebucht. Oben unter „Meine Buchungen“ Pause oder Fertig'
            + ' melden, dann hier neu starten.', 'warning');
        document.querySelectorAll('.bde-kopf-markiert').forEach(function (x) { x.classList.remove('bde-kopf-markiert'); });
        z.classList.add('bde-kopf-markiert');
        if (!z.querySelector('.bde-kopf-hinweis')) {
            var h = document.createElement('span');
            h.className = 'bde-kopf-hinweis';
            h.textContent = 'Zuerst hier Pause oder Fertig melden';
            z.insertBefore(h, z.querySelector('.bde-kopf-knoepfe'));
        }
        var bereich = document.getElementById('bdeKopfBereich');
        if (bereich && !bereich.classList.contains('show')) bootstrap.Collapse.getOrCreateInstance(bereich).show();
        z.scrollIntoView({ block: 'center' });
        var k = z.querySelector('[data-kopf-aktion]');
        if (k) k.focus();
    }

    // Mengendialog (#quantityModal): Promise mit {gut, aus} oder null (Abbrechen/Schliessen). Fokus auf Gutmenge.
    // v1.99.81 (Entscheid 4, U-4): Gut ist Pflicht; mit bisher gemeldeter Menge zeigt der Hinweis Soll, bisher und Rest.
    function mengen(soll, vorgabe, bisher) {
        return new Promise(function (fertig) {
            var el = document.getElementById('quantityModal'), m = modal('quantityModal');
            var hint = document.getElementById('qty-sollmenge-hint');
            hint.textContent = soll == null ? '' : 'Sollmenge: ' + fmt(soll)
                + (bisher > 0 ? ', bisher gut gemeldet: ' + fmt(bisher) + ', Rest: ' + fmt(Math.max(0, soll - bisher)) : '');
            hint.classList.toggle('d-none', soll == null);
            var gut = document.getElementById('inputGood'), aus = document.getElementById('inputScrap');
            gut.value = vorgabe != null ? vorgabe : '';
            aus.value = '0';
            var wert = null;
            document.getElementById('btnQtyOk').onclick = function () {
                if (!gut.reportValidity() || !aus.reportValidity()) return;
                wert = { gut: parseFloat(gut.value), aus: parseFloat(aus.value || '0') };
                m.hide();
            };
            document.getElementById('btnQtyCancel').onclick = function () { m.hide(); };
            // v1.99.81 (U-10): Enter bestaetigt wie „Bestätigen“ (auch Hardware-Scanner mit Enter-Suffix, wie in der Teileliste)
            var enter = function (ev) { if (ev.key === 'Enter') { ev.preventDefault(); document.getElementById('btnQtyOk').click(); } };
            el.addEventListener('keydown', enter);
            el.addEventListener('shown.bs.modal', function () { gut.select(); }, { once: true });
            el.addEventListener('hidden.bs.modal', function () { el.removeEventListener('keydown', enter); fertig(wert); }, { once: true });
            m.show();
        });
    }

    // E4-6 / #13: Gut < Soll → Bestaetigung im Modal (statt des Browser-Dialogs im alten Terminal).
    // v1.99.81 (Entscheid 4): gerechnet mit der bisher gemeldeten Menge; Gut + Ausschuss ueber dem Rest → Rueckfrage „über Soll“.
    function gutOk(q, soll, bisher) {
        bisher = bisher || 0;
        return new Promise(function (ja) {
            var text = null;
            if (soll != null && bisher + q.gut + q.aus > soll)
                text = 'Gut ' + fmt(q.gut) + ' + Ausschuss ' + fmt(q.aus) + (bisher > 0 ? ' + bisher ' + fmt(bisher) : '')
                    + ' liegt über der Sollmenge ' + fmt(soll) + '. Trotzdem als fertig melden?';
            else if (soll != null && bisher + q.gut < soll)
                text = 'Gutmenge ' + fmt(bisher + q.gut) + (bisher > 0 ? ' (mit bisher ' + fmt(bisher) + ')' : '') + ' liegt unter der Sollmenge '
                    + fmt(soll) + '. Wirklich als fertig melden?';
            if (!text) { ja(true); return; }
            window.bdeBestaetigen(text, 'Fertig melden', function () { ja(true); }, function () { ja(false); });
        });
    }

    function andereBeenden(json, okText) {
        var liste = document.getElementById('close-others-list');
        liste.replaceChildren.apply(liste, json.otherActiveBookings.map(function (o) {
            var li = document.createElement('li');
            li.textContent = o.operatorName + ' – seit ' + zeit(o.startedAt);
            return li;
        }));
        var el = document.getElementById('close-others-modal'), m = modal('close-others-modal');
        var text = okText;
        var ja = document.getElementById('close-others-confirm');
        ja.onclick = async function () {
            var frei = window.wmsSperren(ja);
            if (!frei) return;
            var r = await post('/BdeTerminal/CloseOthers', { workOperationId: json.workOperationId, operatorId: operatorId, terminalId: terminalId });
            frei();
            text = r.closedCount != null ? okText + ' ' + r.closedCount + (r.closedCount === 1 ? ' weitere Buchung beendet.' : ' weitere Buchungen beendet.') : okText + ' Weitere Buchungen beenden fehlgeschlagen: ' + (r.message || '');
            m.hide();
        };
        el.addEventListener('hidden.bs.modal', function () { neuLaden(text); }, { once: true });  // „Nein, nur meine“: die anderen laufen weiter
        m.show();
    }

    // v1.99.81 (3A, U-2): Gruppen-Abschluss. Zeile = Haupt-FA · Sub-FA / AG Name, Werkbank, Soll. Die angeklickte Buchung uebernimmt die
    // schon eingegebenen Mengen (letzteFertig), die anderen Felder sind leer und Pflicht; Gut unter Soll -> eine Sammel-Rueckfrage.
    var letzteFertig = null;
    function gruppe(json) {
        var tbody = document.getElementById('group-finish-rows');
        tbody.replaceChildren.apply(tbody, json.bookings.map(function (b) {
            var tr = document.createElement('tr');
            tr.dataset.bookingId = b.bookingId;
            tr.dataset.soll = b.targetQuantity != null ? b.targetQuantity : '';
            var vorbelegt = letzteFertig && String(letzteFertig.id) === String(b.bookingId) ? letzteFertig : null;
            var fa = document.createElement('td');
            fa.textContent = b.orderNumber + (b.subOrderNumber && b.subOrderNumber !== b.orderNumber ? ' · Sub-FA ' + b.subOrderNumber : '')
                + ' / ' + (b.operationNumber || '').trim() + (b.operationName ? ' ' + b.operationName : '');
            var wb = document.createElement('td');
            wb.textContent = b.workplaceName || '–';
            var soll = document.createElement('td');
            soll.className = 'text-end';
            soll.textContent = b.targetQuantity != null ? fmt(b.targetQuantity) : '–';
            tr.append(fa, wb, soll);
            [['group-qty-good', 'Gut', vorbelegt && vorbelegt.gut], ['group-qty-scrap', 'Ausschuss', vorbelegt && vorbelegt.aus]].forEach(function (f) {
                var td = document.createElement('td'), i = document.createElement('input');
                i.type = 'number'; i.min = '0'; i.step = '1'; i.required = true; i.inputMode = 'numeric';
                i.value = f[2] != null ? f[2] : '';
                i.className = 'form-control bde-gruppe-menge ' + f[0];
                i.setAttribute('aria-label', f[1] + ' ' + fa.textContent);
                td.appendChild(i); tr.appendChild(td);
            });
            var abs = document.createElement('td'), c = document.createElement('input');
            abs.className = 'text-center';
            c.type = 'checkbox'; c.checked = true; c.className = 'form-check-input group-is-final';
            c.setAttribute('aria-label', 'Abschließen ' + fa.textContent);
            abs.appendChild(c); tr.appendChild(abs);
            return tr;
        }));
        var el = document.getElementById('group-finish-modal'), m = modal('group-finish-modal'), ok = document.getElementById('group-finish-confirm');
        var zeilen = function () { return Array.prototype.slice.call(tbody.querySelectorAll('tr[data-booking-id]')); };
        async function senden() {
            var frei = window.wmsSperren(ok);
            if (!frei) return;
            var eintraege = zeilen().map(function (r) {
                return {
                    bookingId: parseInt(r.dataset.bookingId, 10),
                    goodQty: parseFloat(r.querySelector('.group-qty-good').value),
                    scrapQty: parseFloat(r.querySelector('.group-qty-scrap').value),
                    isFinal: r.querySelector('.group-is-final').checked
                };
            });
            var r = await post('/BdeTerminal/FinishGroup', { operatorId: operatorId, terminalId: terminalId, entriesJson: JSON.stringify(eintraege) });
            if (r.outcome === 'Success') { neuLaden(r.closedCount + (r.closedCount === 1 ? ' Buchung' : ' Buchungen') + ' fertig gemeldet.'); return; }
            frei();
            m.hide();
            ergebnis(r, '');
        }
        ok.onclick = function () {
            // Pflichtfelder: das erste leere/ungueltige Feld meldet der Browser selbst
            if (!Array.prototype.every.call(tbody.querySelectorAll('input[type="number"]'), function (i) { return i.reportValidity(); })) return;
            var unter = zeilen().filter(function (r) {
                var s = zahl(r.dataset.soll), g = parseFloat(r.querySelector('.group-qty-good').value);
                return r.querySelector('.group-is-final').checked && s != null && g < s;
            });
            if (!unter.length) { senden(); return; }
            var text = unter.map(function (r) { return r.cells[0].textContent + ': Gut ' + fmt(parseFloat(r.querySelector('.group-qty-good').value)) + ' von ' + fmt(zahl(r.dataset.soll)); });
            // erst den Gruppen-Dialog schliessen (keine gestapelten Modals); „Abbrechen“ oeffnet ihn mit den Eingaben wieder
            el.addEventListener('hidden.bs.modal', function () {
                window.bdeBestaetigen('Gutmenge unter Soll: ' + text.join('; ') + '. Trotzdem alle fertig melden?', 'Alle fertig melden',
                    senden, function () { m.show(); });
            }, { once: true });
            m.hide();
        };
        m.show();  // „Abbrechen“ bucht nichts
    }

    var start = function (woId, ruesten) {
        return post(ruesten ? '/BdeTerminal/StartSetup' : '/BdeTerminal/StartProduction',
            { operatorId: operatorId, workOperationId: woId, workplaceId: workplaceId, terminalId: terminalId });
    };

    // Kopf-Buchungen (E4-4): Knoepfe je Zustand wie das Terminal (renderActionsForActive / Pausiert-Liste).
    document.addEventListener('click', async function (e) {
        var btn = e.target.closest('[data-kopf-aktion]');
        if (!btn) return;
        var z = btn.closest('.bde-kopf-zeile');
        var id = z.dataset.bookingId, soll = zahl(z.dataset.soll), bisher = zahl(z.dataset.bisher) || 0;
        box.replaceChildren();  // v1.99.81 (U-9): alte Meldung weg
        // v1.99.81 (U-6): die drei Mengen-Aktionen sperren erst NACH dem Dialog (der offene Dialog verhindert den Doppelklick),
        // sonst stuende „wird gesendet …“ schon waehrend der Eingabe; alle anderen sperren sofort.
        var mitMenge = /^(pause|teilfertig|fertig|abschliessen)$/.test(btn.dataset.kopfAktion);
        var frei = mitMenge ? function () { } : window.wmsSperren(btn);
        if (!frei) return;
        var sperren = function () { var f = window.wmsSperren(btn); if (f) frei = f; return !!f; };
        var q, r, ok;
        switch (btn.dataset.kopfAktion) {
            case 'pause':
                if (!(q = await mengen(soll, null, bisher)) || !sperren()) return frei();
                r = await post('/BdeTerminal/Pause', { bookingId: id, operatorId: operatorId, terminalId: terminalId, goodQty: q.gut, scrapQty: q.aus }); ok = 'Pausiert.'; break;
            case 'teilfertig':
                if (!(q = await mengen(soll, null, bisher)) || !sperren()) return frei();
                r = await post('/BdeTerminal/ReportPartial', { bookingId: id, operatorId: operatorId, terminalId: terminalId, goodQty: q.gut, scrapQty: q.aus }); ok = 'Teilmenge gemeldet.'; break;
            case 'fertig':
                // v1.99.81 (Entscheid 4): nach Teilmengen Gut mit dem Rest vorbelegen (0, wenn Soll schon erreicht)
                if (!(q = await mengen(soll, bisher > 0 && soll != null ? Math.max(0, soll - bisher) : null, bisher))
                    || !(await gutOk(q, soll, bisher)) || !sperren()) return frei();
                letzteFertig = { id: id, gut: q.gut, aus: q.aus };
                r = await post('/BdeTerminal/Finish', { bookingId: id, operatorId: operatorId, terminalId: terminalId, goodQty: q.gut, scrapQty: q.aus }); ok = 'Fertig gemeldet.'; break;
            case 'ruesten-fertig': r = await post('/BdeTerminal/Finish', { bookingId: id, operatorId: operatorId, terminalId: terminalId }); ok = 'Rüsten beendet.'; break;
            case 'produktion': r = await start(z.dataset.woId, false); ok = 'Produktion gestartet.'; break;
            case 'beenden': r = await post('/BdeTerminal/Finish', { bookingId: id, operatorId: operatorId, terminalId: terminalId }); ok = 'Tätigkeit beendet.'; break;
            case 'fortsetzen':
                r = await post('/BdeTerminal/Resume', { pausedBookingId: id, operatorId: operatorId, resumeAs: 2, workplaceId: workplaceId, terminalId: terminalId });
                ok = 'Fortgesetzt.'; break;
            case 'abschliessen':
                // v1.99.81 (Entscheid 9): nicht fortsetzbare Pause beenden, ohne Mengen – Rueckfrage im Modal, dann sperren
                if (!(await new Promise(function (ja) {
                    window.bdeBestaetigen('Pause von ' + z.dataset.bez + ' abschließen? Die Zeit bis zur Pause bleibt gebucht, es wird keine Menge gemeldet.',
                        'Abschließen', function () { ja(true); }, function () { ja(false); });
                })) || !sperren()) return frei();
                r = await post('/BdeTerminal/ClosePaused', { bookingId: id, operatorId: operatorId, workplaceId: workplaceId, terminalId: terminalId }); ok = 'Pause abgeschlossen.'; break;
            default: return frei();
        }
        ergebnis(r, ok, frei);
    });

    // Zeilen: Anstempeln per JSON (Kollision als Dialog), Formular bleibt der Weg ohne JS. NurAg: „Fertig melden“ mit Mengendialog.
    document.addEventListener('submit', async function (e) {
        var f = e.target;
        if (!f.matches || !f.matches('form[data-fa-anstempeln]')) return;
        e.preventDefault();
        var knoepfe = f.querySelectorAll('button[type="submit"]');
        var ruesten = e.submitter && e.submitter.value === 'ruesten';
        var frei = window.wmsSperren(knoepfe);
        if (!frei) return;
        ergebnis(await start(f.dataset.woId, ruesten), ruesten ? 'Rüsten gestartet.' : 'Produktion gestartet.', frei);
    }, true);

    document.addEventListener('click', async function (e) {
        var btn = e.target.closest('[data-fa-fertig]');
        if (!btn) return;
        box.replaceChildren();  // v1.99.81 (U-9)
        var soll = zahl(btn.dataset.soll);
        var q = await mengen(soll, soll);
        if (!q || !(await gutOk(q, soll, 0))) return;
        var frei = window.wmsSperren(btn);  // v1.99.81 (U-6): erst nach dem Dialog sperren
        if (!frei) return;
        ergebnis(await post('/BdeTerminal/ReportCompletion', {
            operatorId: operatorId, workOperationId: btn.dataset.woId, workplaceId: workplaceId, terminalId: terminalId, goodQty: q.gut, scrapQty: q.aus
        }), 'Fertig gemeldet: ' + btn.dataset.bez + '.', frei);
    });

    // Auswahl-Dialog: je Eintrag ein Knopf (Text escaped per textContent); Fokus auf den ersten Eintrag.
    function auswahl(titel, text, eintraege, gewaehlt) {
        var el = document.getElementById('faAuswahlModal'), m = modal('faAuswahlModal');
        document.getElementById('faAuswahlTitel').textContent = titel;
        document.getElementById('faAuswahlText').textContent = text;
        var liste = document.getElementById('faAuswahlListe');
        liste.replaceChildren.apply(liste, eintraege.map(function (x) {
            var b = document.createElement('button');
            b.type = 'button'; b.className = 'btn btn-outline-primary btn-lg text-start bde-touch';
            b.textContent = x.text;
            b.addEventListener('click', function () { m.hide(); gewaehlt(x); });
            return b;
        }));
        el.addEventListener('shown.bs.modal', function () { var b = liste.querySelector('button'); if (b) b.focus(); }, { once: true });
        m.show();
    }

    // #14 Taetigkeit: Auswahl aus /api/bde/activities, StartActivity.
    document.getElementById('btnTaetigkeit').addEventListener('click', async function () {
        var r;
        try { r = await fetch('/api/bde/activities'); } catch (err) { r = null; }
        if (!r || !r.ok) { melden('Tätigkeiten konnten nicht geladen werden.', 'warning'); return; }
        var liste = await r.json();
        if (!liste.length) { melden('Keine Tätigkeiten angelegt. Bitte an den BDE-Admin wenden.', 'warning'); return; }
        auswahl('Tätigkeit starten', 'Ungeplante Tätigkeit wählen:', liste.map(function (a) { return { id: a.id, text: a.code + ' – ' + a.name }; }),
            async function (a) {
                ergebnis(await post('/BdeTerminal/StartActivity',
                    { operatorId: operatorId, activityId: a.id, workplaceId: workplaceId, terminalId: terminalId }), 'Tätigkeit gestartet: ' + a.text + '.');
            });
    });

    // Schalter „Zeiten und Details“ (nur Anzeige, je Werker gemerkt)
    var kopf = document.querySelector('.bde-kopf'), schalter = document.getElementById('bdeKopfDetails');
    var DETAILS = 'wms.bdeKopfDetails.' + operatorId;
    try { schalter.checked = localStorage.getItem(DETAILS) === 'an'; } catch (e) { }
    var zeigen = function () { kopf.classList.toggle('bde-kopf-mit-details', schalter.checked); };
    zeigen();
    schalter.addEventListener('change', function () {
        zeigen();
        try { localStorage.setItem(DETAILS, schalter.checked ? 'an' : 'aus'); } catch (e) { }
    });

    // E4-5 Scan: Treffer markieren (Zeile auf dieser Seite) bzw. auf die Seite mit der Zeile wechseln (Server: markiert=).
    function markieren(woId) {
        var tr = document.querySelector('tr[data-wo-id="' + woId + '"]');
        if (!tr) {
            var u = new URL(location.href);
            u.searchParams.set('markiert', woId);
            u.searchParams.delete('page');
            location.assign(u.toString());
            return;
        }
        document.querySelectorAll('tr.bde-fa-markiert').forEach(function (x) { x.classList.remove('bde-fa-markiert'); x.removeAttribute('aria-current'); });
        tr.classList.add('bde-fa-markiert');
        tr.setAttribute('aria-current', 'true');
        tr.scrollIntoView({ block: 'center' });
        var b = tr.querySelector('[data-fa-anstempeln] button, [data-fa-fertig]');
        if (b) b.focus();
        else melden('Dieser Arbeitsgang läuft bereits bei Ihnen – Buchung oben unter „Meine Buchungen“.', 'warning');
    }
    var vorab = cfg.dataset.markiert;
    if (vorab) {
        var tr0 = document.querySelector('tr[data-wo-id="' + vorab + '"]');
        if (tr0) markieren(vorab);
    }

    var scanForm = document.getElementById('faScanForm'), scan = document.getElementById('faScan');
    scanForm.addEventListener('submit', async function (e) {
        e.preventDefault();
        box.replaceChildren();  // v1.99.81 (U-9)
        var roh = scan.value.trim();
        scan.value = '';
        if (!roh) return;
        var teile = roh.split(/[,\/]/), fa = teile[0].trim(), ag = teile.length > 1 ? teile[1].trim() : '';
        if (!fa) return;
        var r;
        try {
            r = await fetch('/api/bde/resolve-scan?faSicht=true&faNumber=' + encodeURIComponent(fa) + (ag ? '&opNumber=' + encodeURIComponent(ag) : '')
                + '&workplaceId=' + encodeURIComponent(workplaceId) + '&operatorId=' + encodeURIComponent(operatorId));
        } catch (err) { r = null; }
        if (!r || !r.ok) { melden('Scan „' + roh + '“ konnte nicht aufgelöst werden.', 'warning'); return; }
        var res = await r.json();
        var kand = res.candidates.filter(function (c) { return c.target === 'WorkOperation'; });
        if (res.kind === 'Exact' && kand.length === 1) { markieren(kand[0].id); return; }
        if (res.kind === 'Ambiguous' && kand.length > 1) {
            auswahl('Arbeitsgang wählen', 'Zu „' + roh + '“ gibt es mehrere offene Arbeitsgänge an dieser Werkbank:',
                kand.map(function (c) {
                    return { id: c.id, text: (c.subOrderNumber ? c.subOrderNumber : c.orderNumber) + ' / ' + (c.operationNumber || '').trim()
                        + (c.operationName ? ' ' + c.operationName : '') + (c.description ? ' – ' + c.description : '') };
                }), function (c) { markieren(c.id); });
            return;
        }
        // v1.99.79 F-27: „läuft bereits“ eindeutig (Server-Ergebnis Running), „nicht gefunden“ nur noch für unbekannte Nummern
        melden(res.kind === 'Running' ? 'FA ' + fa + (ag ? ' / ' + ag : '') + ': Der Arbeitsgang läuft bereits (offene Buchung) – eigene Buchungen stehen oben unter „Meine Buchungen“.'
            : res.kind === 'NotInScope' ? 'FA ' + fa + ' hat an dieser Werkbank keinen offenen Arbeitsgang.'
            : 'FA „' + roh + '“ nicht gefunden.', 'warning');
    });
})();
