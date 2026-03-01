$(document).ready(function () {
    const studentId = $('#hiddenStudentId').val();

    // 1. SMALL QR CODE (In the Profile Card)
    const qrContainer = document.getElementById("qrcode");
    if (qrContainer && studentId) {
        qrContainer.innerHTML = ""; // Clear any existing QR before rendering (Prevents doubling)
        new QRCode(qrContainer, {
            text: `iBITS:${studentId}`,
            width: 140, height: 140, // Fits nicely in the side card
            colorDark: "#000000", colorLight: "#ffffff",
            correctLevel: QRCode.CorrectLevel.M
        });
    }

    // 2. COUNTDOWN TIMER
    const eventDateInput = document.getElementById('eventDate');
    if (eventDateInput && eventDateInput.value) {
        // Parse date. If only YYYY-MM-DD is provided, we treat it as midnight local time
        // Parse as Philippine midnight (UTC+8) to avoid 8-hour countdown error
        const rawVal = eventDateInput.value;
        // Force Philippine Time (UTC+8) for all date parsing
        // If date-only (YYYY-MM-DD), treat as midnight PHT
        // If full datetime without offset, append +08:00 to prevent UTC interpretation
        const targetDate = rawVal.length === 10
            ? new Date(rawVal + 'T00:00:00+08:00').getTime()
            : (rawVal.includes('+') || rawVal.includes('Z')
                ? new Date(rawVal).getTime()
                : new Date(rawVal + '+08:00').getTime());

        const updateTimer = () => {
            const now = new Date().getTime();
            const distance = targetDate - now;

            if (distance < 0) {
                clearInterval(timerInterval);
                updateTimerDisplay(0, 0, 0, 0);
                $('#countdown-active').html('<div class="text-center py-3"><h4 class="text-gold">Event is Happening Today!</h4></div>');
                return;
            }

            const d = Math.floor(distance / (1000 * 60 * 60 * 24));
            const h = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
            const m = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
            const s = Math.floor((distance % (1000 * 60)) / 1000);

            updateTimerDisplay(d, h, m, s);
        };

        const timerInterval = setInterval(updateTimer, 1000);
        updateTimer(); // Run immediately on load
    }

    // 3. EXPAND QR (Create Modal dynamically or use existing)
    $('#btnExpandQr').on('click', function () {
        // NUCLEAR OPTION: Completely remove any existing modals first
        $('#qrExpandModal').remove();
        $('.modal-backdrop').remove(); // Remove any leftover backdrops
        $('body').removeClass('modal-open'); // Reset body state
        
        // We create a temporary modal specifically for the expanded QR
        let modalHtml = `
            <div class="modal fade" id="qrExpandModal" tabindex="-1">
                <div class="modal-dialog modal-dialog-centered" style="max-width: 400px; margin: 1.75rem auto;">
                    <div class="modal-content" style="background: rgba(15, 23, 42, 0.98); backdrop-filter: blur(20px); border: 1px solid var(--gold-primary); border-radius: 24px;">
                        <div class="modal-body text-center p-4 p-md-5">
                            <h4 class="text-white fw-bold mb-4" style="font-size: 1.25rem;">Scan My Digital ID</h4>
                            <div id="qrcode-large" class="d-inline-block p-2 bg-white rounded shadow-sm" style="max-width: 100%;"></div>
                            <h5 class="mt-4 text-gold" style="font-family: monospace; letter-spacing: 2px; font-weight: 700;">${studentId}</h5>
                            <p class="text-muted small mb-0 mt-2">iBITS Unified Portal</p>
                            <button type="button" class="btn btn-sm btn-outline-light mt-4 px-4" data-bs-dismiss="modal" style="border-radius: 10px; opacity: 0.7;">Close</button>
                        </div>
                    </div>
                </div>
                <style>
                    #qrcode-large img, #qrcode-large canvas {
                        max-width: 100% !important;
                        height: auto !important;
                        display: block !important;
                        margin: 0 auto;
                    }
                    /* Force only ONE QR code to display */
                    #qrcode-large > *:not(:first-child) {
                        display: none !important;
                    }
                </style>
            </div>`;

        // Append fresh modal
        $('body').append(modalHtml);

        // Use setTimeout to ensure DOM is ready
        setTimeout(() => {
            const largeContainer = document.getElementById("qrcode-large");
            if (largeContainer) {
                // AGGRESSIVE CLEANUP
                while (largeContainer.firstChild) {
                    largeContainer.removeChild(largeContainer.firstChild);
                }
                
                // Create QR code - library may create multiple elements
                new QRCode(largeContainer, { 
                    text: `iBITS:${studentId}`, 
                    width: 320, 
                    height: 320,
                    correctLevel: QRCode.CorrectLevel.H
                });
                
                // FORCE: Hide all but the first child (canvas or img)
                const children = largeContainer.children;
                for (let i = 1; i < children.length; i++) {
                    children[i].style.display = 'none';
                }
                
                // Show modal
                new bootstrap.Modal(document.getElementById('qrExpandModal')).show();
            }
        }, 100);
    });


    // 4. DOWNLOAD QR
    $('#btnDownloadQr').on('click', function () {
        // Try to get image or canvas
        const img = qrContainer.querySelector('img');
        const canvas = qrContainer.querySelector('canvas');
        let dataUrl = "";

        if (img && img.src && img.src.startsWith('data:image')) {
            dataUrl = img.src;
        } else if (canvas) {
            dataUrl = canvas.toDataURL("image/png");
        }

        if (dataUrl) {
            const link = document.createElement('a');
            link.download = `iBITS-ID-${studentId}.png`;
            link.href = dataUrl;
            document.body.appendChild(link); // Necessary for some mobile browsers
            link.click();
            document.body.removeChild(link);
        } else {
            // Fallback: If it's not ready, show a message
            console.error("QR Code source not found or invalid.");
            alert("Unable to download QR ID yet. Please wait a second and try again.");
        }
    });

    // 5. PROFILE PICTURE PREVIEW
    $('#profilePictureInput').on('change', function (event) {
        if (event.target.files && event.target.files[0]) {
            const reader = new FileReader();
            reader.onload = function (e) {
                $('#profilePicturePreview').attr('src', e.target.result);
            }
            reader.readAsDataURL(event.target.files[0]);
        }
    });

    // 6. AJAX PROFILE UPDATE
    $('#ajaxProfileForm').on('submit', function (e) {
        e.preventDefault();
        const btn = $('#btnUpdateProfile');
        const alert = $('#profileAlert');
        const formData = new FormData(this);

        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');
        alert.addClass('d-none').removeClass('alert-success alert-danger');

        $.ajax({
            url: '/Student/UpdateProfilePicture',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (res) {
                if (res.success) {
                    $('#settingsModal').modal('hide');
                    $('#successModalMsg').text(res.message);
                    new bootstrap.Modal(document.getElementById('successModal')).show();
                    
                    // Update header and offcanvas images
                    if (res.newImageUrl) {
                        $('#headerMyProfileImg').attr('src', res.newImageUrl);
                        $('#offcanvasProfileImg').attr('src', res.newImageUrl);
                        $('#profilePicturePreview').attr('src', res.newImageUrl);
                    }
                } else {
                    alert.addClass('alert-danger').removeClass('d-none').text(res.message);
                }
            },
            error: function () {
                alert.addClass('alert-danger').removeClass('d-none').text("An error occurred during upload.");
            },
            complete: function () {
                btn.prop('disabled', false).text('Save');
            }
        });
    });

    // 7. AJAX EMAIL UPDATE
    $('#ajaxEmailForm').on('submit', function (e) {
        e.preventDefault();
        const btn = $('#btnUpdateEmail');
        const alert = $('#emailAlert');

        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');
        alert.addClass('d-none').removeClass('alert-success alert-danger');

        $.ajax({
            url: '/Student/UpdateEmail',
            type: 'POST',
            data: $(this).serialize(),
            success: function (res) {
                if (res.success) {
                    var newEmail = $('input[name="NewEmail"]').val();
                    $('#currentEmailDisplay').text(newEmail);
                    $('#ajaxEmailForm input[name="NewEmail"]').val('');

                    $('#settingsModal').modal('hide');
                    $('#successModalMsg').text(res.message);
                    new bootstrap.Modal(document.getElementById('successModal')).show();
                } else {
                    alert.addClass('alert-danger').removeClass('d-none').text(res.message);
                }
            },
            error: function () {
                alert.addClass('alert-danger').removeClass('d-none').text("An error occurred.");
            },
            complete: function () {
                btn.prop('disabled', false).text('Save');
            }
        });
    });

    // 8. AJAX PASSWORD UPDATE
    $('#ajaxPasswordForm').on('submit', function (e) {
        e.preventDefault();
        const btn = $('#btnUpdatePass');
        const alert = $('#passwordAlert');

        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Updating...');
        alert.addClass('d-none').removeClass('alert-success alert-danger');

        $.ajax({
            url: '/Student/UpdatePassword',
            type: 'POST',
            data: $(this).serialize(),
            success: function (res) {
                if (res.success) {
                    $('#settingsModal').modal('hide');
                    $('#successModalMsg').text(res.message);
                    new bootstrap.Modal(document.getElementById('successModal')).show();
                    $('#ajaxPasswordForm')[0].reset();
                } else {
                    alert.addClass('alert-danger').removeClass('d-none').text(res.message);
                }
            },
            error: function () {
                alert.addClass('alert-danger').removeClass('d-none').text("An error occurred.");
            },
            complete: function () {
                btn.prop('disabled', false).text('Update');
            }
        });
    });

    // 9. INITIALIZE ACTIVITY COUNTDOWNS
    initActivityCountdowns();
});

function initActivityCountdowns() {
    const activityCards = document.querySelectorAll('.current-event-card');
    
    activityCards.forEach(card => {
        const startTimeStr = card.dataset.startTime;
        const endTimeStr = card.dataset.endTime;
        const eventDateStr = card.dataset.eventDate;
        
        const timerDisplay = card.querySelector('.event-countdown-timer');
        const countdownWrap = card.querySelector('.live-countdown-wrap');
        const countdownLabel = card.querySelector('.countdown-label');
        const badge = card.querySelector('.state-badge');
        const timeDetails = card.querySelector('.event-time-details');
        const dismissBtn = card.querySelector('.dismiss-event-btn');
        
        if (!endTimeStr || !timerDisplay || !badge) return;

        const startDate = new Date(`${eventDateStr}T${startTimeStr}+08:00`).getTime();
        const endDate = new Date(endTimeStr + '+08:00').getTime();

        const updateInterval = setInterval(() => {
            const now = new Date().getTime();
            const diffToStart = startDate - now;
            const diffToEnd = endDate - now;

            // 1. STATE: ENDED
            if (diffToEnd <= 0) {
                clearInterval(updateInterval);
                timerDisplay.innerText = "EVENT ENDED";
                timerDisplay.classList.add('text-danger');
                countdownWrap.style.display = 'flex';
                countdownLabel.innerText = "Status: ";
                if (timeDetails) timeDetails.style.display = 'none';
                
                badge.innerText = "EVENT ENDED";
                badge.className = "badge bg-danger state-badge";
                
                // Show manual dismiss button
                if (dismissBtn) dismissBtn.style.display = 'block';

                // AUTO-CLEANUP PRIORITY LOGIC:
                // If there's another active or upcoming event card, remove this one immediately
                const otherCards = document.querySelectorAll('.current-event-card');
                let anotherActive = false;
                otherCards.forEach(oc => {
                    if (oc !== card) {
                        const ocBadge = oc.querySelector('.state-badge');
                        if (ocBadge && (ocBadge.innerText.includes('HAPPENING') || ocBadge.innerText.includes('START LATER'))) {
                            anotherActive = true;
                        }
                    }
                });

                if (anotherActive) {
                    removeWithAnimation(card);
                } else {
                    // Standard 3-minute grace period if it's the only event
                    setTimeout(() => removeWithAnimation(card), 180000);
                }
                return;
            }

            // 2. STATE: HAPPENING RIGHT NOW (Ongoing)
            if (now >= startDate) {
                badge.innerText = "HAPPENING RIGHT NOW";
                badge.className = "badge bg-success state-badge pulse-happening";
                
                countdownWrap.style.display = 'flex';
                countdownLabel.innerText = "Ends in: ";
                timerDisplay.innerText = formatTime(diffToEnd);
                
                // Hide static times once ongoing to focus on the countdown
                if (timeDetails) timeDetails.style.display = 'none';
            } 
            // 3. STATE: EVENT WILL START LATER (Within 1 hour)
            else if (diffToStart <= 3600000) {
                badge.innerText = "EVENT WILL START LATER";
                badge.className = "badge bg-warning text-dark state-badge";
                
                countdownWrap.style.display = 'flex';
                countdownLabel.innerText = "Starts in: ";
                timerDisplay.innerText = formatTime(diffToStart);
                if (timeDetails) timeDetails.style.display = 'flex';
            }
            // 4. STATE: SCHEDULED TODAY (More than 1 hour away)
            else {
                badge.innerText = "SCHEDULED TODAY";
                badge.className = "badge bg-info state-badge";
                
                // Show countdown even when far away if requested
                countdownWrap.style.display = 'flex';
                countdownLabel.innerText = "Starts in: ";
                timerDisplay.innerText = formatTime(diffToStart);
                
                if (timeDetails) timeDetails.style.display = 'flex';
            }

        }, 1000);
    });

    function formatTime(ms) {
        const total_seconds = Math.floor(ms / 1000);
        const hours = Math.floor(total_seconds / 3600);
        const minutes = Math.floor((total_seconds % 3600) / 60);
        const seconds = total_seconds % 60;
        
        const f = (t) => t < 10 ? `0${t}` : t;
        return `${f(hours)}:${f(minutes)}:${f(seconds)}`;
    }
}

// Global helper for format since it was used inside initActivityCountdowns
function format(t) { return t < 10 ? `0${t}` : t; }

window.dismissEventCard = function(btn) {
    const card = btn.closest('.current-event-card');
    if (card) removeWithAnimation(card);
};

function removeWithAnimation(card) {
    if (!card) return;
    card.style.transition = 'opacity 0.8s ease, transform 0.8s ease, margin 0.8s ease';
    card.style.opacity = '0';
    card.style.transform = 'translateX(30px)';
    card.style.marginBottom = '0';
    setTimeout(() => {
        card.remove();
        // Check if all cards are gone, if so you could show an 'All Clear' message here
    }, 800);
}

function updateTimerDisplay(d, h, m, s) {
    const format = (t) => t < 10 ? `0${t}` : t;
    $('#days').text(format(d));
    $('#hours').text(format(h));
    $('#minutes').text(format(m));
    $('#seconds').text(format(s));
}
