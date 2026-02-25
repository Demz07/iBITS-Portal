// wwwroot/js/portal-layout.js

$(function () { // Modern document ready syntax - START

    // Theme Management
    const themeBtn = $('#themeToggle');
    const themeIcon = $('#themeIcon');
    const htmlEl = $('html');
    const currentTheme = localStorage.getItem('theme');

    if (currentTheme) {
        htmlEl.attr('data-theme', currentTheme);
        updateIcon(currentTheme);
    }

    themeBtn.on('click', function () {
        let newTheme = htmlEl.attr('data-theme') === 'light' ? 'dark' : 'light';
        htmlEl.attr('data-theme', newTheme);
        localStorage.setItem('theme', newTheme);
        updateIcon(newTheme);
    });

    function updateIcon(theme) {
        if (theme === 'light') {
            themeIcon.removeClass('bi-sun-fill').addClass('bi-moon-stars-fill');
        } else {
            themeIcon.removeClass('bi-moon-stars-fill').addClass('bi-sun-fill');
        }
    }

    // --- LOGOUT CONFIRMATION ---
    $('#confirmLogoutModal-confirmBtn').on('click', function () {
        $('#logoutForm').submit();
    });

    // --- INACTIVITY AUTO-LOGOUT (SOFT LOCK) ---
    let inactivityTimer;
    // 4 minutes = 4 * 60 * 1000
    const warningTime = 4 * 60 * 1000;
    // 5 minutes = 5 * 60 * 1000
    const logoutTime = 5 * 60 * 1000;
    let warningShown = false;

    // Exposed global function
    window.resetTimer = function () {
        clearTimeout(inactivityTimer);
        warningShown = false;

        // Safely hide warning modal if open
        var warningEl = document.getElementById('inactivityWarning');
        if (warningEl) {
            var warningModal = bootstrap.Modal.getInstance(warningEl);
            if (warningModal) {
                warningModal.hide();
            } else {
                // If modal instance doesn't exist but modal is visible, create and hide
                var bsModal = new bootstrap.Modal(warningEl);
                bsModal.hide();
            }
        }

        startTimer();
    };

    function startTimer() {
        inactivityTimer = setTimeout(showWarning, warningTime);
    }

    function showWarning() {
        warningShown = true;
        var warningModalElement = document.getElementById('inactivityWarning');
        if (warningModalElement) {
            new bootstrap.Modal(warningModalElement).show();
        }

        // Wait remaining 1 minute before locking
        setTimeout(function () {
            if (warningShown) {
                lockSession();
            }
        }, logoutTime - warningTime);
    }

    function lockSession() {
        // 1. Kill the server session in the background
        const form = $('#logoutForm');
        if (form.length > 0) {
            $.ajax({
                url: form.attr('action'),
                type: 'POST',
                data: form.serialize()
            });
        }

        // 2. Hide Warning Modal
        var warningEl = document.getElementById('inactivityWarning');
        if (warningEl) {
            var warningModal = bootstrap.Modal.getInstance(warningEl);
            if (warningModal) warningModal.hide();
        }

        // 3. Show "Session Expired" Blocking Modal
        var expiredModalElement = document.getElementById('sessionExpiredModal');
        if (expiredModalElement) {
            var expiredModal = new bootstrap.Modal(expiredModalElement, {
                backdrop: 'static', // Clicking outside won't close it
                keyboard: false     // ESC key won't close it
            });
            expiredModal.show();
        }
    }

    // Events that reset the timer (only if not locked)
    $(document).on('mousedown keydown mousemove scroll touchstart', function () {
        // Check if the session is already locked
        var expiredEl = document.getElementById('sessionExpiredModal');
        var isLocked = expiredEl && expiredEl.classList.contains('show');

        if (!isLocked) {
            resetTimer();
        }
    });

    // Start timer on load
    startTimer();

}); // Modern document ready syntax - END