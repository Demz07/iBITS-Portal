$(document).ready(function () {
    const studentId = $('#hiddenStudentId').val();

    // 1. SMALL QR CODE (In the Profile Card)
    const qrContainer = document.getElementById("qrcode");
    if (qrContainer && studentId) {
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
        // We create a temporary modal specifically for the expanded QR
        let modalHtml = `
            <div class="modal fade" id="qrExpandModal" tabindex="-1">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content" style="background: rgba(15, 23, 42, 0.95); backdrop-filter: blur(10px); border: 1px solid rgba(255,255,255,0.1);">
                        <div class="modal-body text-center p-5">
                            <h4 class="text-white mb-4">Scan My Digital ID</h4>
                            <div id="qrcode-large" class="d-inline-block p-3 bg-white rounded"></div>
                            <h5 class="mt-4 text-warning" style="font-family: monospace; letter-spacing: 2px;">${studentId}</h5>
                            <button type="button" class="btn btn-sm btn-outline-light mt-4" data-bs-dismiss="modal">Close</button>
                        </div>
                    </div>
                </div>
            </div>`;

        // Remove old one if exists
        $('#qrExpandModal').remove();
        $('body').append(modalHtml);

        const largeContainer = document.getElementById("qrcode-large");
        new QRCode(largeContainer, { text: `iBITS:${studentId}`, width: 280, height: 280 });

        new bootstrap.Modal(document.getElementById('qrExpandModal')).show();
    });

    // 4. DOWNLOAD QR
    $('#btnDownloadQr').on('click', function () {
        const img = qrContainer.querySelector('img');
        if (img) {
            const link = document.createElement('a');
            link.download = `iBITS-ID-${studentId}.png`;
            link.href = img.src;
            link.click();
        }
    });

    // 5. PROFILE PICTURE PREVIEW
    $('#profileImageInput').on('change', function (event) {
        if (event.target.files && event.target.files[0]) {
            const reader = new FileReader();
            reader.onload = function (e) {
                $('#imagePreview').attr('src', e.target.result);
            }
            reader.readAsDataURL(event.target.files[0]);
        }
    });
});

function updateTimerDisplay(d, h, m, s) {
    const format = (t) => t < 10 ? `0${t}` : t;
    $('#days').text(format(d));
    $('#hours').text(format(h));
    $('#minutes').text(format(m));
    $('#seconds').text(format(s));
}
