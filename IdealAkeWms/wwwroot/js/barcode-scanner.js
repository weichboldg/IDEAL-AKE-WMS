// AKE BDE Light - Barcode/QR Scanner
// Verwendet html5-qrcode Library
// Unterstützt Kamera-Scan (HTTPS) und Bild-Upload als Fallback

var _activeScanner = null;
var _scannerClosing = false;

function initScanner(buttonId, targetSelectId, scanType) {
    const button = document.getElementById(buttonId);
    if (!button) return;

    var qrFaEnabled = button.getAttribute('data-qr-fa-enabled') === 'true';
    var faTargetId = button.getAttribute('data-fa-target') || null;

    button.addEventListener('click', function () {
        openScannerModal(targetSelectId, scanType, qrFaEnabled, faTargetId);
    });
}

function isCameraSupported() {
    return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
}

function isSecureContext() {
    return window.isSecureContext || location.protocol === 'https:' || location.hostname === 'localhost';
}

/**
 * Pre-Warm der Camera-Permission im synchronen User-Gesture-Stack.
 * iOS Safari verweigert sonst die Permission, wenn getUserMedia erst nach Modal-Show
 * (asynchron) aufgerufen wird. Bei Erfolg: Stream sofort wieder stoppen — wir wollen
 * nur die Permission etablieren, der eigentliche Scanner startet spaeter via html5-qrcode.
 *
 * Returns true bei Erfolg, false bei Fehler (Permission denied, kein Camera-Support, etc.).
 */
async function requestCameraPermission() {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        return false;
    }
    try {
        const stream = await navigator.mediaDevices.getUserMedia({
            video: { facingMode: 'environment' }
        });
        // Stream sofort wieder stoppen
        stream.getTracks().forEach(track => track.stop());
        return true;
    } catch (err) {
        console.warn('Camera permission pre-warm failed:', err);
        return false;
    }
}

async function openScannerModal(targetSelectId, scanType, qrFaEnabled, faTargetId) {
    // iOS-Fix: Permission im synchronen User-Gesture-Stack anfragen, BEVOR das Modal
    // geoeffnet wird. html5-qrcode wuerde getUserMedia sonst erst nach Modal-Show rufen,
    // und iOS Safari verweigert dann die Permission.
    let cameraAvailable = false;
    const secureCtx = isSecureContext();
    if (isCameraSupported() && secureCtx) {
        cameraAvailable = await requestCameraPermission();
    }

    _scannerClosing = false;

    // Altes Modal entfernen
    let modal = document.getElementById('scannerModal');
    if (modal) modal.remove();
    const title = scanType === 'article' ? 'Artikel QR-Code scannen' : 'Lagerplatz Barcode scannen';

    modal = document.createElement('div');
    modal.id = 'scannerModal';
    modal.className = 'scanner-modal';
    modal.innerHTML = `
        <div class="scanner-modal-content">
            <div class="scanner-modal-header">
                <h5>${title}</h5>
                <button type="button" class="btn-close btn-close-white" id="scannerCloseBtn"></button>
            </div>
            ${cameraAvailable ? `
                <div id="scannerTabs" class="scanner-tabs">
                    <button type="button" class="scanner-tab active" data-tab="camera">Kamera</button>
                    <button type="button" class="scanner-tab" data-tab="file">Bild hochladen</button>
                </div>
                <div id="scannerCameraPane">
                    <div id="scannerReader" style="width: 100%;"></div>
                </div>
            ` : `
                <div class="scanner-info-box">
                    ${!secureCtx
                        ? 'Kamera-Scan erfordert HTTPS. Bitte ein Bild des Codes hochladen.'
                        : 'Kamera nicht verfügbar. Bitte ein Bild des Codes hochladen.'}
                </div>
            `}
            <div id="scannerFilePane" style="${cameraAvailable ? 'display:none;' : ''}">
                <div class="scanner-file-upload">
                    <label for="scannerFileInput" class="scanner-file-label">
                        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" fill="currentColor" viewBox="0 0 16 16">
                            <path d="M.5 9.9a.5.5 0 0 1 .5.5v2.5a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-2.5a.5.5 0 0 1 1 0v2.5a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2v-2.5a.5.5 0 0 1 .5-.5"/>
                            <path d="M7.646 1.146a.5.5 0 0 1 .708 0l3 3a.5.5 0 0 1-.708.708L8.5 2.707V11.5a.5.5 0 0 1-1 0V2.707L5.354 4.854a.5.5 0 1 1-.708-.708z"/>
                        </svg>
                        <span>Bild auswählen oder Foto aufnehmen</span>
                    </label>
                    <input type="file" id="scannerFileInput" accept="image/*" capture="environment" style="display:none;" />
                </div>
            </div>
            <div id="scannerResult" class="scanner-result" style="display:none;">
                <span id="scannerResultText"></span>
            </div>
            <div class="scanner-modal-footer">
                <button type="button" class="btn btn-outline-light" id="scannerCancelBtn">Abbrechen</button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);

    // Event-Handler mit addEventListener (robuster als onclick)
    document.getElementById('scannerCloseBtn').addEventListener('click', closeScannerModal);
    document.getElementById('scannerCancelBtn').addEventListener('click', closeScannerModal);

    // Tab-Handler
    document.querySelectorAll('.scanner-tab').forEach(function (tab) {
        tab.addEventListener('click', function () {
            switchScannerTab(this.getAttribute('data-tab'));
        });
    });

    // File-Input Handler
    var fileInput = document.getElementById('scannerFileInput');
    if (fileInput) {
        fileInput.addEventListener('change', function (e) {
            if (e.target.files && e.target.files.length > 0) {
                scanFromFile(e.target.files[0], targetSelectId, scanType, qrFaEnabled, faTargetId);
            }
        });
    }

    // Kamera starten wenn verfügbar
    if (cameraAvailable) {
        startCameraScanner(targetSelectId, scanType, qrFaEnabled, faTargetId);
    }
}

function switchScannerTab(tab) {
    var cameraPane = document.getElementById('scannerCameraPane');
    var filePane = document.getElementById('scannerFilePane');
    var tabs = document.querySelectorAll('.scanner-tab');

    tabs.forEach(function (t) { t.classList.remove('active'); });

    if (tab === 'camera') {
        if (cameraPane) cameraPane.style.display = '';
        if (filePane) filePane.style.display = 'none';
        if (tabs[0]) tabs[0].classList.add('active');
    } else {
        if (cameraPane) cameraPane.style.display = 'none';
        if (filePane) filePane.style.display = '';
        if (tabs[1]) tabs[1].classList.add('active');
        stopScanner();
    }
}

function getSupportedFormats() {
    var formats = [];
    var F = Html5QrcodeSupportedFormats;
    if (F) {
        formats = [F.QR_CODE, F.CODE_128, F.CODE_39, F.EAN_13, F.EAN_8, F.CODE_93];
    }
    return formats;
}

function startCameraScanner(targetSelectId, scanType, qrFaEnabled, faTargetId) {
    var formats = getSupportedFormats();
    var html5QrCode = formats.length > 0
        ? new Html5Qrcode("scannerReader", { formatsToSupport: formats, verbose: false })
        : new Html5Qrcode("scannerReader");
    _activeScanner = html5QrCode;

    var config = {
        fps: 10,
        qrbox: { width: 300, height: scanType === 'article' ? 300 : 150 },
        rememberLastUsedCamera: true
    };

    html5QrCode.start(
        { facingMode: "environment" },
        config,
        function (decodedText) {
            if (_scannerClosing) return;
            _scannerClosing = true;

            // Zuerst Wert verarbeiten, dann Modal schließen
            processScannedValue(decodedText, targetSelectId, scanType, qrFaEnabled, faTargetId);
            closeScannerModal();
        },
        function (errorMessage) {
            // Ignoriere kontinuierliche Scan-Fehler
        }
    ).catch(function (err) {
        _activeScanner = null;
        // Kamera fehlgeschlagen - auf File-Upload wechseln
        var resultEl = document.getElementById('scannerResult');
        var resultText = document.getElementById('scannerResultText');
        if (resultEl && resultText) {
            resultEl.style.display = 'block';
            resultText.textContent = 'Kamera nicht verfügbar. Bitte Bild hochladen.';
        }
        var cameraPane = document.getElementById('scannerCameraPane');
        var filePane = document.getElementById('scannerFilePane');
        var tabsEl = document.getElementById('scannerTabs');
        if (cameraPane) cameraPane.style.display = 'none';
        if (filePane) filePane.style.display = '';
        if (tabsEl) tabsEl.style.display = 'none';
    });
}

function stopScanner() {
    var scanner = _activeScanner;
    _activeScanner = null;
    if (scanner) {
        try {
            scanner.stop().catch(function () {});
        } catch (e) {
            // Ignorieren
        }
    }
}

function scanFromFile(file, targetSelectId, scanType, qrFaEnabled, faTargetId) {
    var resultEl = document.getElementById('scannerResult');
    var resultText = document.getElementById('scannerResultText');

    if (resultEl && resultText) {
        resultEl.style.display = 'block';
        resultText.textContent = 'Bild wird verarbeitet...';
    }

    var formats = getSupportedFormats();
    var html5QrCode = formats.length > 0
        ? new Html5Qrcode("scannerFileReader_temp", { formatsToSupport: formats, verbose: false })
        : new Html5Qrcode("scannerFileReader_temp", false);
    html5QrCode.scanFile(file, true)
        .then(function (decodedText) {
            processScannedValue(decodedText, targetSelectId, scanType, qrFaEnabled, faTargetId);
            closeScannerModal();
        })
        .catch(function (err) {
            if (resultEl && resultText) {
                resultEl.style.display = 'block';
                resultText.textContent = 'Kein Code im Bild erkannt. Bitte erneut versuchen.';
            }
            var fileInput = document.getElementById('scannerFileInput');
            if (fileInput) fileInput.value = '';
        });
}

function closeScannerModal() {
    // Scanner stoppen (fire-and-forget)
    stopScanner();

    // Modal sofort entfernen - killt auch den Video-Stream
    var modal = document.getElementById('scannerModal');
    if (modal) {
        modal.remove();
    }

    _scannerClosing = false;
}

function processScannedValue(value, targetSelectId, scanType, qrFaEnabled, faTargetId) {
    var searchValue = value;
    var faNumber = null;

    if (scanType === 'article' || scanType === 'productionOrder') {
        // QR-Code-Format: Artikelnummer;Feld2;FA-Nummer[,Suffix];...
        var parts = value.split(';');

        if (scanType === 'productionOrder') {
            // FA-Nummer extrahieren (3. Position), Komma-Suffix abschneiden
            if (parts.length >= 3 && parts[2].trim()) {
                searchValue = parts[2].trim().split(',')[0];
            } else {
                // Fallback: gesamten gescannten Wert verwenden (evtl. manuell eingetippt)
                searchValue = value.trim().split(',')[0];
            }
        } else {
            // Artikel: Erster Teil vor ; ist die Artikelnummer
            searchValue = parts[0].trim();

            // FA-Nummer aus QR extrahieren (Index 2) wenn Setting aktiv und >= 3 Teile
            if (qrFaEnabled && parts.length >= 3 && parts[2].trim()) {
                faNumber = parts[2].trim().split(',')[0];
            }
        }
    }

    var select = document.getElementById(targetSelectId);
    if (!select) return;

    // Input-Elemente (text, hidden etc.): Wert direkt setzen und Change-Event auslösen
    if (select.tagName === 'INPUT') {
        select.value = searchValue;
        select.dispatchEvent(new Event('change'));
        if (select.type !== 'hidden') {
            showScanFeedback(select, true, searchValue);
        }
        clearAndFillFaNumber(faNumber, faTargetId);
        return;
    }

    // Select2 AJAX-Artikel: API-Lookup statt Option-Iteration
    if (scanType === 'article' && $(select).hasClass('select2-article')) {
        var feedbackTarget = $(select).closest('.input-group').length
            ? $(select).closest('.input-group')[0]
            : select;
        $.ajax({
            url: '/api/articles/by-number/' + encodeURIComponent(searchValue),
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                var option = new Option(data.text, data.id, true, true);
                $(select).append(option).trigger('change');
                showScanFeedback(feedbackTarget, true, searchValue);
                clearAndFillFaNumber(faNumber, faTargetId);
            },
            error: function () {
                showScanFeedback(feedbackTarget, false, searchValue);
            }
        });
        return;
    }

    // Native <select>: Option-Iteration (Lagerplätze etc.)
    var found = false;
    for (var i = 0; i < select.options.length; i++) {
        var optionText = select.options[i].text.trim();
        if (optionText === searchValue || optionText.startsWith(searchValue)) {
            select.selectedIndex = i;
            select.dispatchEvent(new Event('change'));
            found = true;
            showScanFeedback(select, true, searchValue);
            break;
        }
    }

    if (!found) {
        showScanFeedback(select, false, searchValue);
    }
}

function showScanFeedback(element, success, value) {
    var parent = element.parentNode;
    if (!parent) return;
    var existing = parent.querySelector('.scan-feedback');
    if (existing) existing.remove();

    var feedback = document.createElement('div');
    feedback.className = 'scan-feedback mt-1';

    if (success) {
        feedback.innerHTML = '<small class="text-success"><strong>&#10003;</strong> Gescannt: ' + escapeHtml(value) + '</small>';
    } else {
        feedback.innerHTML = '<small class="text-danger"><strong>&#10007;</strong> Nicht gefunden: ' + escapeHtml(value) + '</small>';
    }

    parent.appendChild(feedback);

    setTimeout(function () {
        feedback.remove();
    }, 5000);
}

function clearAndFillFaNumber(faNumber, faTargetId) {
    if (!faTargetId) return;
    var faInput = document.getElementById(faTargetId);
    if (!faInput) return;
    // Immer zuerst leeren, dann nur befüllen wenn FA vorhanden
    faInput.value = faNumber || '';
    faInput.dispatchEvent(new Event('change'));
}

function escapeHtml(text) {
    var div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// --- Text-Input Scanner ---
// Lightweight scanner for plain text inputs (no Select2).
// valueExtractor: 'article' | 'fa' | 'raw'
// onScanned: optional callback(extractedValue) after value is set
function initTextInputScanner(buttonId, targetInputId, valueExtractor, onScanned) {
    var btn = document.getElementById(buttonId);
    var target = document.getElementById(targetInputId);
    if (!btn || !target) return;

    btn.addEventListener('click', function () {
        // Store callback info in a global so openScannerModal's pipeline can use it
        _textInputScanCallback = {
            targetInputId: targetInputId,
            valueExtractor: valueExtractor,
            onScanned: onScanned || null
        };
        openScannerModal(targetInputId, valueExtractor === 'fa' ? 'productionOrder' : 'article', false, null);
    });
}

var _textInputScanCallback = null;

// Hook into processScannedValue: after filling an INPUT, call the stored callback
var _origProcessScannedValue = processScannedValue;
processScannedValue = function (value, targetSelectId, scanType, qrFaEnabled, faTargetId) {
    _origProcessScannedValue(value, targetSelectId, scanType, qrFaEnabled, faTargetId);

    // If there's a pending text-input callback and the target matches, fire it
    if (_textInputScanCallback && _textInputScanCallback.targetInputId === targetSelectId) {
        var target = document.getElementById(targetSelectId);
        if (target && target.tagName === 'INPUT') {
            // Also dispatch input event (original only dispatches change)
            target.dispatchEvent(new Event('input', { bubbles: true }));
            if (typeof _textInputScanCallback.onScanned === 'function') {
                _textInputScanCallback.onScanned(target.value);
            }
        }
        _textInputScanCallback = null;
    }
};

// Initialisierung wenn DOM bereit
document.addEventListener('DOMContentLoaded', function () {
    initScanner('btnScanArticle', 'ArticleId', 'article');
    initScanner('btnScanStorageLocation', 'StorageLocationId', 'storageLocation');
    initScanner('btnScanSourceLocation', 'SourceStorageLocationId', 'storageLocation');
    initScanner('btnScanArticleInfo', 'articleNumber', 'article');
});
