/* activity-logs.js */

$(document).ready(function () {
    console.log("🛡️ Audit Trail Initialized");

    // Export Button Logic
    $('#btnExportLogs').on('click', function (e) {
        e.preventDefault();
        /* activity-logs.js */

        $(document).ready(function () {
            console.log("🛡️ Audit Trail Initialized");

            // Export Button Logic
            $('#btnExportLogs').on('click', function (e) {
                e.preventDefault();

                // Show a small UI feedback (optional)
                const $btn = $(this);
                const originalHtml = $btn.html();
                $btn.html('<i class="bi bi-hourglass-split me-1"></i> Generating...');
                $btn.addClass('disabled');

                // Redirect to the controller action to trigger download
                window.location.href = '/Admin/ExportActivityLogs';

                // Reset button after a short delay
                setTimeout(() => {
                    $btn.html(originalHtml);
                    $btn.removeClass('disabled');
                }, 2000);
            });
        });


    });
});

