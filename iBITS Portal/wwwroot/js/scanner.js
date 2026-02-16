import QrScanner from "https://cdn.jsdelivr.net/npm/qr-scanner@1.4.2/qr-scanner.min.js";

window.exportToExcel = exportToExcel;
window.exportToPdf = exportToPdf;

document.addEventListener("DOMContentLoaded", function () {
    // --- Elements ---
    const modeLinks = document.querySelectorAll('.nav-link-scanner');
    const livePanel = document.getElementById('live-panel');
    const verifierPanel = document.getElementById('verifier-panel');
    const eventSel = document.getElementById('eventSelector');
    const startBtn = document.getElementById('startSessionBtn');
    const liveContainer = document.getElementById('live-scanner-container');
    const stopBtn = document.getElementById('endSessionBtn-live');
    const liveVideoEl = document.getElementById('live-video');
    const liveOverlayText = document.getElementById('live-overlay-text');
    const liveFileInput = document.getElementById('live-file-input');
    const tableBody = document.getElementById('attendance-table-body');
    const emptyState = document.getElementById('empty-state-msg');

    const verifierCard = document.getElementById('verifier-result-card');
    const verifierVideoEl = document.getElementById('verifier-video');
    const verifierOverlayText = document.getElementById('verifier-overlay-text');
    const verifierFileInput = document.getElementById('verifier-file-input');

    const audioOk = document.getElementById('success-sound');
    const audioErr = document.getElementById('error-sound');

    // --- State ---
    let scanner;
    let currentMode = 'live';
    let isProcessing = false;
    let lastCode = null;
    let lastTime = 0;
    const COOLDOWN = 2500; // Increased cooldown to prevent flicker

    // --- TEMPLATES (To avoid innerHTML rewrite flicker) ---
    const verifierPromptHTML = `
        <div class="text-center" style="color: var(--text-main);">
            <i class="bi bi-qr-code-scan" style="font-size: 4rem; opacity: 0.5;"></i>
            <p class="mt-3 fw-500" style="font-size: 1.1rem;">Scan a QR code to verify</p>
        </div>`;

    // --- HELPERS ---
    function normalizeImagePath(path) {
        const defaultAvatar = '/images/default-avatar.png'; // Fallback image
        if (!path || path.trim() === '') {
            return defaultAvatar;
        }
        if (path.startsWith('/')) {
            return path;
        }
        return `/${path}`;
    }

    // --- Event Listeners ---
    modeLinks.forEach(link => {
        link.addEventListener('click', () => {
            stopCamera();
            currentMode = link.dataset.mode;
            modeLinks.forEach(l => l.classList.remove('active'));
            link.classList.add('active');
            if (currentMode === 'live') {
                livePanel.classList.add('active');
                verifierPanel.classList.remove('active');
            } else {
                livePanel.classList.remove('active');
                verifierPanel.classList.add('active');
                resetUI(); // Reset verifier UI when switching
                startCamera();
            }
        });
    });

    if (startBtn) startBtn.addEventListener('click', () => startCamera(eventSel.value));
    if (eventSel) eventSel.addEventListener('change', () => {
        const selectedValue = eventSel.value;
        startBtn.disabled = !selectedValue || selectedValue === "0" || selectedValue === 0;
    });
    if (stopBtn) stopBtn.addEventListener('click', stopCamera);

    // Verifier "Scan Next" button is now handled within the dynamic template

    if (liveFileInput) liveFileInput.addEventListener('change', (e) => handleFileSelect(e));
    if (verifierFileInput) verifierFileInput.addEventListener('change', (e) => handleFileSelect(e));

    // --- CAMERA FUNCTIONS ---
    function startCamera(eventId) {
        if (currentMode === 'live' && (!eventId || eventId === "0" || eventId === 0)) {
            alert("Please select an event to start.");
            return;
        }

        const videoEl = currentMode === 'live' ? liveVideoEl : verifierVideoEl;
        if (currentMode === 'live') {
            liveContainer.style.display = 'block';
        }

        isProcessing = false;
        lastCode = null;

        scanner = new QrScanner(
            videoEl,
            (result) => handleScanResult(result, eventId),
            { highlightScanRegion: true, highlightCodeOutline: true, maxScansPerSecond: 5 }
        );

        scanner.start().then(() => {
            toggleHeartbeat(true);
            updateOverlayText("Point QR Code at Camera");
        }).catch(err => {
            console.error('Camera start error:', err);
            updateOverlayText("Camera Error!");
            alert("Could not start camera: " + (err.message || err));
        });
    }

    function stopCamera() {
        if (scanner) {
            try {
                scanner.stop();
                scanner.destroy();
                scanner = null;
            } catch (e) {
                console.warn("Scanner could not be stopped gracefully:", e);
            }
        }
        toggleHeartbeat(false);
        if (currentMode === 'live') liveContainer.style.display = 'none';
    }

    function handleScanResult(result, eventId) {
        const decodedText = result.data;
        const now = Date.now();
        if (isProcessing || (decodedText === lastCode && (now - lastTime < COOLDOWN))) {
            return; // Exit if already processing or in cooldown
        }

        isProcessing = true; // Set flag immediately
        lastCode = decodedText;
        lastTime = now;

        updateOverlayText("Processing...");
        updateScannerOverlay("processing");

        if (currentMode === 'live') {
            sendLiveScan(decodedText, eventId);
        } else {
            sendVerificationScan(decodedText);
        }
    }

    // --- API CALLS ---
    function sendLiveScan(code, eventId) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        fetch('/Officer/ProcessScan', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ scannedData: code, eventId: parseInt(eventId) })
        })
            .then(r => r.json())
            .then(data => {
                if (data.success) {
                    playSound(audioOk);
                    addTableRow(data);
                    showLiveFlashCard(data);
                    updateOverlayText("Success!");
                    updateScannerOverlay('success');
                } else {
                    playSound(audioErr);
                    showLiveFlashCardError(data.message);
                    updateOverlayText("Failed: " + (data.message || 'Scan failed'));
                    updateScannerOverlay('error');
                }
                setTimeout(() => {
                    isProcessing = false;
                    resetUI();
                    hideLiveFlashCard();
                }, COOLDOWN); // Use cooldown period
            })
            .catch(err => {
                console.error('Scan error:', err);
                playSound(audioErr);
                showLiveFlashCardError('Network or server error.');
                isProcessing = false;
            });
    }

    function sendVerificationScan(code) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        fetch('/Officer/VerifyStudentQr', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ scannedData: code })
        })
            .then(r => r.json())
            .then(data => {
                if (data.success) {
                    playSound(audioOk);
                    updateOverlayText("Verified!");
                    updateScannerOverlay('success');
                    verifierCard.className = "glass-panel success"; // Add success class for border/shadow

                    // FIXED: Populate success template instead of replacing all HTML
                    verifierCard.innerHTML = `
                    <div class="profile-container">
                        <img src="${normalizeImagePath(data.profileImage)}" 
                             class="profile-image" 
                             alt="Student Photo" 
                             onerror="this.src='/images/default-avatar.png'">
                        <div class="verification-badge">
                            <i class="bi bi-patch-check-fill"></i>
                        </div>
                    </div>
                    <div class="profile-divider"></div>
                    <h3 class="profile-name">${data.fullName}</h3>
                    <div class="info-badges">
                        <div class="info-badge">
                            <i class="bi bi-person-badge badge-icon"></i>
                            <span class="badge-text">${data.studentNum}</span>
                        </div>
                        <div class="info-badge section-badge">
                            <i class="bi bi-mortarboard-fill badge-icon"></i>
                            <span class="badge-text">${data.yearLevelSection}</span>
                        </div>
                    </div>
                    <button id="scan-again-btn" class="btn btn-scan-next">
                        <i class="bi bi-arrow-repeat"></i>
                        <span>SCAN NEXT</span>
                    </button>
                `;
                    // Re-bind the event listener to the newly created button
                    document.getElementById('scan-again-btn').addEventListener('click', () => {
                        resetUI();
                        isProcessing = false;
                    });

                } else {
                    playSound(audioErr);
                    updateOverlayText("Verification Failed");
                    updateScannerOverlay('error');
                    verifierCard.className = "glass-panel error"; // Add error class

                    // FIXED: Populate error template
                    verifierCard.innerHTML = `
                    <div class="error-message-card">
                        <i class="bi bi-x-octagon-fill error-icon"></i>
                        <h3>Verification Failed</h3>
                        <p>${data.message || "Invalid or unrecognized QR Code."}</p>
                    </div>
                    <button id="scan-again-btn" class="btn btn-scan-next">
                        <i class="bi bi-arrow-repeat"></i>
                        <span>TRY AGAIN</span>
                    </button>
                `;
                    // Re-bind the event listener for the error state too
                    document.getElementById('scan-again-btn').addEventListener('click', () => {
                        resetUI();
                        isProcessing = false;
                    });
                }
            })
            .catch(err => {
                console.error('Verification error:', err);
                playSound(audioErr);
                alert('Verification failed due to a network error.');
                resetUI(); // Reset on error
                isProcessing = false;
            });
    }


    // --- LIVE FLASH CARD UI ---
    function showLiveFlashCard(data) {
        const flashCard = document.getElementById('live-flash-card');
        const flashContent = document.getElementById('live-flash-content');
        if (!flashCard || !flashContent) return;

        // FIXED: Use normalized path
        flashContent.innerHTML = `
            <div class="flash-card-container success">
                <div class="profile-container">
                    <img src="${normalizeImagePath(data.profileImage)}" 
                         class="profile-image" 
                         alt="Profile" 
                         onerror="this.src='/images/default-avatar.png'">
                    <div class="verification-badge">
                        <i class="bi bi-patch-check-fill"></i>
                    </div>
                </div>
                <div class="profile-divider"></div>
                <h3 class="profile-name">${data.studentName}</h3>
                <div class="info-badges">
                    <div class="info-badge">
                        <i class="bi bi-person-badge badge-icon"></i>
                        <span class="badge-text">${data.studentId}</span>
                    </div>
                    <div class="info-badge section-badge">
                        <i class="bi bi-mortarboard-fill badge-icon"></i>
                        <span class="badge-text">${data.section}</span>
                    </div>
                </div>
            </div>`;

        flashCard.style.display = 'block';
        flashCard.style.animation = 'slideInUp 0.4s ease-out';
    }

    function showLiveFlashCardError(message) {
        const flashCard = document.getElementById('live-flash-card');
        const flashContent = document.getElementById('live-flash-content');
        if (!flashCard || !flashContent) return;

        flashContent.innerHTML = `
            <div class="flash-card-container error">
                <div class="flash-card-error">
                    <i class="bi bi-x-circle-fill text-danger"></i>
                    <h4 class="fw-bold mt-3 text-danger">Scan Failed</h4>
                    <p class="text-muted">${message}</p>
                </div>
            </div>`;

        flashCard.style.display = 'block';
        flashCard.style.animation = 'slideInUp 0.4s ease-out';
    }

    function hideLiveFlashCard() {
        const flashCard = document.getElementById('live-flash-card');
        if (flashCard) {
            flashCard.style.animation = 'slideOutDown 0.3s ease-in';
            setTimeout(() => {
                flashCard.style.display = 'none';
            }, 300);
        }
    }

    // --- TABLE UI ---
    //function addTableRow(data) {
    //    if (emptyState) emptyState.style.display = 'none';
    //    const row = document.createElement('tr');
    //    row.style.animation = "fadeIn 0.5s";

    //    // FIXED: Use normalized path
    //    row.innerHTML = `
    //        <td><span class="text-muted small">${data.scanTime}</span></td>
    //        <td class="fw-bold">${data.studentId}</td>
    //        <td>
    //            <div class="d-flex align-items-center gap-2">
    //                <img src="${normalizeImagePath(data.profileImage)}"
    //                     alt="Profile"
    //                     class="table-profile-img"
    //                     onerror="this.src='/images/default-avatar.png'">
    //                <div>
    //                    <div>${data.studentName}</div>
    //                    <span class="small text-muted">${data.section}</span>
    //                </div>
    //            </div>
    //        </td>
    //        <td><span class="status-badge status-present">${data.status}</span></td>
    //    `;
    //    tableBody.prepend(row);
    //}

    function addTableRow(data) {
        if (emptyState) emptyState.style.display = 'none';
        const row = document.createElement('tr');

        // Uses the ID we just added to the C# Controller
        const id = data.attendanceId;

        row.innerHTML = `
        <td><span class="text-muted small">${data.scanTime}</span></td>
        <td class="fw-bold">${data.studentId}</td>
        <td>
            <div class="d-flex align-items-center gap-2">
                <img src="${normalizeImagePath(data.profileImage)}" 
                     alt="Profile" 
                     class="table-profile-img" 
                     style="width: 30px; height: 30px; border-radius: 50%;"
                     onerror="this.src='/images/default-avatar.png'">
                <div>
                    <div>${data.studentName}</div>
                    <span class="small text-muted">${data.section}</span>
                </div>
            </div>
        </td>
        <td><span class="status-badge status-present">${data.status}</span></td>
        <td class="text-end">
            <button type="button" 
                    onclick="window.removeAttendee('${id}', this)" 
                    class="btn btn-sm btn-outline-danger border-0">
                <i class="bi bi-trash"></i>
            </button>
        </td>
    `;
        tableBody.prepend(row);
    }

    

    // --- UI HELPERS ---
    function playSound(audio) {
        if (audio && audio.readyState >= 3) {
            audio.currentTime = 0;
            audio.play().catch(e => console.warn("Audio play failed:", e));
        }
    }

    function toggleHeartbeat(on) {
        const hb = document.getElementById(currentMode === 'live' ? 'live-heartbeat' : 'verifier-heartbeat');
        if (hb) hb.className = on ? "heartbeat active" : "heartbeat";
    }

    function updateOverlayText(text) {
        const el = currentMode === 'live' ? liveOverlayText : verifierOverlayText;
        if (el) el.textContent = text;
    }

    function updateScannerOverlay(status) {
        const selector = currentMode === 'live' ? '#live-panel .scanner-overlay' : '#verifier-panel .scanner-overlay';
        const overlay = document.querySelector(selector);
        if (!overlay) return;

        overlay.className = 'scanner-overlay'; // Reset
        if (status === 'success') overlay.classList.add('success-overlay');
        else if (status === 'error') overlay.classList.add('error-overlay');
        else if (status === 'processing') overlay.classList.add('processing-overlay');
    }

    function resetUI() {
        // For verifier mode, reset the card to its initial prompt state
        if (currentMode === 'verifier') {
            verifierCard.className = "glass-panel";
            verifierCard.innerHTML = verifierPromptHTML;
        }
        // For both modes, reset the camera overlay
        updateOverlayText("Scanning...");
        updateScannerOverlay('default');
    }

    function handleFileSelect(event) {
        const file = event.target.files?.[0];
        if (!file) return;

        const eventId = (currentMode === 'live') ? eventSel.value : null;
        if (currentMode === 'live' && (!eventId || eventId === "0" || eventId === 0)) {
            alert("Please select an event before uploading an image.");
            event.target.value = ''; // Clear input
            return;
        }

        updateOverlayText("Processing Image...");
        updateScannerOverlay("processing");
        isProcessing = true; // Set flag

        QrScanner.scanImage(file, { returnDetailedScanResult: true })
            .then(result => {
                // Manually wrap the result to match the camera's output
                handleScanResult({ data: result.data }, eventId);
            })
            .catch((err) => {
                console.error("File Scan Error:", err);
                alert("No QR code was found in the selected image.");
                resetUI();
                isProcessing = false;
            })
            .finally(() => {
                event.target.value = ''; // Clear the file input
            });
    }
});

// ========================================
// EXPORT FUNCTIONS
// ========================================
function exportToExcel() {
    const table = document.getElementById("attendance-table");
    if (!table || table.rows.length <= 1) {
        alert("No data to export!");
        return;
    }
    // Use a library like SheetJS (XLSX) if it's included in your project
    if (typeof XLSX === 'undefined') {
        console.error("XLSX library is not loaded. Please include it to use Excel export.");
        alert("Excel export library is not available.");
        return;
    }
    const wb = XLSX.utils.table_to_book(table, { sheet: "Attendance" });
    XLSX.writeFile(wb, `Attendance_Log_${new Date().toISOString().slice(0, 10)}.xlsx`);
}

function exportToPdf() {
    // Use a library like jsPDF if it's included
    if (typeof window.jspdf === 'undefined') {
        console.error("jsPDF library is not loaded. Please include it to use PDF export.");
        alert("PDF export library is not available.");
        return;
    }
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();
    const table = document.getElementById("attendance-table");
    if (!table || table.rows.length <= 1) {
        alert("No data to export!");
        return;
    }
    doc.text("Event Attendance Report", 14, 15);
    doc.autoTable({ html: '#attendance-table', startY: 25 });
    doc.save(`Attendance_Log_${new Date().toISOString().slice(0, 10)}.pdf`);
}

// AT THE VERY BOTTOM OF YOUR JS FILE (Outside the DOMContentLoaded)
window.removeAttendee = function (recordId, button) {
    // If ID is 0 or undefined, the backend didn't send it correctly
    if (!recordId || recordId === "0" || recordId === "undefined") {
        alert("Cannot delete: Missing Record ID. Please refresh the page.");
        return;
    }

    if (!confirm("Are you sure you want to remove this record?")) return;

    const row = button.closest('tr');
    row.style.opacity = '0.5';

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    fetch('/Officer/DeleteAttendance', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ id: parseInt(recordId) })
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                row.remove();
                if (tableBody.children.length === 0) { // tableBody is available from the parent scope
                    emptyState.style.display = 'block';
                }
            } else {
                alert("Error: " + data.message);
                row.style.opacity = '1';
            }
        })
        .catch(err => {
            console.error("Delete Error:", err);
            row.style.opacity = '1';
        });
};