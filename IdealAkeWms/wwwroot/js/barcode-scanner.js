// AKE BDE Light - Barcode/QR Scanner
// Verwendet html5-qrcode Library
// Unterstützt Kamera-Scan (HTTPS) und Bild-Upload als Fallback

var _activeScanner = null;
var _scannerClosing = false;

function initScanner(buttonId, targetSelectId, scanType) {
    const button = document.getElementById(buttonId);
    if (!button) return;

    button.addEventListener('click', function () {
        openScannerModal(targetSelectId, scanType);
    });
}

function isCameraSupported() {
    return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
}

function isSecureContext() {
    return window.isSecureContext || location.protocol === 'https:' || location.hostname === 'localhost';
}

function openScannerModal(targetSelectId, scanType) {
    _scannerClosing = false;

    // Altes Modal entfernen
    let modal = document.getElementById('scannerModal');
    if (modal) modal.remove();

    const cameraAvailable = isCameraSupported() && isSecureContext();
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
                    ${!isSecureContext()
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
                scanFromFile(e.target.files[0], targetSelectId, scanType);
            }
        });
    }

    // Kamera starten wenn verfügbar
    if (cameraAvailable) {
        startCameraScanner(targetSelectId, scanType);
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

function startCameraScanner(targetSelectId, scanType) {
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
            processScannedValue(decodedText, targetSelectId, scanType);
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

function scanFromFile(file, targetSelectId, scanType) {
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
            processScannedValue(decodedText, targetSelectId, scanType);
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

function processScannedValue(value, targetSelectId, scanType) {
    var searchValue = value;

    if (scanType === 'article') {
        // QR-Code: Erster Teil vor ; ist die Artikelnummer
        // Format: 87040362;1472230-04;45
        var parts = value.split(';');
        searchValue = parts[0].trim();
    }

    var select = document.getElementById(targetSelectId);
    if (!select) return;

    // Plain text input: Wert direkt setzen und Change-Event auslösen
    if (select.tagName === 'INPUT' && select.type !== 'hidden') {
        select.value = searchValue;
        select.dispatchEvent(new Event('change'));
        showScanFeedback(select, true, searchValue);
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

function escapeHtml(text) {
    var div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Initialisierung wenn DOM bereit
document.addEventListener('DOMContentLoaded', function () {
    initScanner('btnScanArticle', 'ArticleId', 'article');
    initScanner('btnScanStorageLocation', 'StorageLocationId', 'storageLocation');
    initScanner('btnScanSourceLocation', 'SourceStorageLocationId', 'storageLocation');
    initScanner('btnScanArticleInfo', 'articleNumber', 'article');
});
