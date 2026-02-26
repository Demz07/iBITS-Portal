// wwwroot/js/portal-layout.js

$(function () { // Modern document ready syntax - START

    // ============================================================
    // THEME MANAGEMENT (Dark/Light Mode)
    // ============================================================
    const themeBtn = $('#themeToggle');
    const themeIcon = $('#themeIcon'); // The icon inside the button (sun/moon)
    const htmlEl = $('html');
    const currentTheme = localStorage.getItem('theme') || 'dark'; // Default to dark if null

    // 1. Initialize Theme on Load
    applyTheme(currentTheme);

    // 2. Handle Toggle Click
    themeBtn.on('click', function () {
        let newTheme = htmlEl.attr('data-theme') === 'light' ? 'dark' : 'light';
        applyTheme(newTheme);
    });

    // Helper: Apply theme to DOM, LocalStorage, Icons, and Charts
    function applyTheme(theme) {
        htmlEl.attr('data-theme', theme);
        localStorage.setItem('theme', theme);

        // Update Icon
        if (theme === 'light') {
            themeIcon.removeClass('bi-sun-fill').addClass('bi-moon-stars-fill');
        } else {
            themeIcon.removeClass('bi-moon-stars-fill').addClass('bi-sun-fill');
        }

        // Update Charts (Canvas elements don't respond to CSS classes automatically)
        updateChartTheme(theme);
    }

    // Helper: Update Chart.js colors dynamically
    function updateChartTheme(theme) {
        // specific colors for light vs dark mode
        const isLight = theme === 'light';
        const textColor = isLight ? '#334155' : '#ffffff';        // Dark Slate vs White
        const gridColor = isLight ? 'rgba(0, 0, 0, 0.1)' : 'rgba(255, 255, 255, 0.1)';
        const tooltipBg = isLight ? 'rgba(255, 255, 255, 0.9)' : 'rgba(0, 0, 0, 0.8)';
        const tooltipText = isLight ? '#0f172a' : '#ffffff';

        // Check if Chart.js is loaded on this page
        if (typeof Chart !== 'undefined') {
            // 1. Update Global Defaults for new charts
            Chart.defaults.color = textColor;
            Chart.defaults.borderColor = gridColor;

            // 2. Update all currently rendered chart instances
            Object.values(Chart.instances).forEach((chart) => {
                // Update Scales (X/Y Axis)
                if (chart.options.scales) {
                    ['x', 'y'].forEach(axis => {
                        if (chart.options.scales[axis]) {
                            chart.options.scales[axis].ticks.color = textColor;
                            chart.options.scales[axis].grid.color = gridColor;
                        }
                    });
                }

                // Update Legend Labels
                if (chart.options.plugins && chart.options.plugins.legend) {
                    chart.options.plugins.legend.labels.color = textColor;
                }

                // Update Tooltips
                if (chart.options.plugins && chart.options.plugins.tooltip) {
                    chart.options.plugins.tooltip.backgroundColor = tooltipBg;
                    chart.options.plugins.tooltip.titleColor = tooltipText;
                    chart.options.plugins.tooltip.bodyColor = tooltipText;
                }

                chart.update(); // Re-render the chart
            });
        }
    }

    // ============================================================
    // LOGOUT CONFIRMATION
    // ============================================================
    $('#confirmLogoutModal-confirmBtn').on('click', function () {
        $('#logoutForm').submit();
    });

    // ============================================================
    // INACTIVITY AUTO-LOGOUT (SOFT LOCK)
    // ============================================================
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