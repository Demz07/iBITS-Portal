/* C:\Users\Dave\OneDrive\Desktop\needs to be update\Heres the code you will need to update\events.js */
/* wwwroot/js/events.js */

$(document).ready(function () {
    console.log("📅 Events Manager Initialized");

    // =========================================================
    // 1. INITIALIZE SELECT2 (Custom Dropdown Theme)
    // =========================================================

    // Initialize Select2 for filter dropdowns
    $('.select2-enable').select2({
        minimumResultsForSearch: Infinity,
        width: '100%'
    });

    // Auto-submit filter on change
    $('.auto-submit-filter').on('change', function () {
        $(this).closest('form').submit();
    });

    // Initialize for Create Modal
    $('#createEventModal').on('shown.bs.modal', function () {
        $(this).find('select').select2({
            dropdownParent: $('#createEventModal'),
            minimumResultsForSearch: Infinity, // Hides search box for cleaner look
            width: '100%'
        });
    });

    // Initialize for Edit Modal
    $('#editEventModal').on('shown.bs.modal', function () {
        $(this).find('select').select2({
            dropdownParent: $('#editEventModal'),
            minimumResultsForSearch: Infinity,
            width: '100%'
        });
    });

    // =========================================================
    // 2. EDIT MODAL POPULATION
    // =========================================================
    const editEventModal = document.getElementById('editEventModal');

    if (editEventModal) {
        editEventModal.addEventListener('show.bs.modal', function (event) {
            // Button that triggered the modal
            const button = event.relatedTarget;
            if (!button) return; // Prevention if triggered manually

            // Helper to safely get attributes
            const getAttr = (attr, def = '') => button.getAttribute(attr) || def;

            // --- 1. Basic Info ---
            document.getElementById('editEventId').value = getAttr('data-event-id');
            document.getElementById('deleteEventId').value = getAttr('data-event-id'); // Sync for delete modal
            document.getElementById('editEventName').value = getAttr('data-event-name');
            document.getElementById('editEventLocation').value = getAttr('data-event-location');
            document.getElementById('editEventDate').value = getAttr('data-event-date');
            document.getElementById('editStartTime').value = getAttr('data-start-time');
            document.getElementById('editEndDate').value = getAttr('data-end-date');
            document.getElementById('editEndTime').value = getAttr('data-end-time');
            document.getElementById('editEventDuration').value = getAttr('data-event-duration');
            document.getElementById('editAcadYear').value = getAttr('data-event-acad-year');
            document.getElementById('editEventDesc').value = getAttr('data-event-desc');

            // --- 2. Event Type (Trigger Select2 Update) ---
            const eventType = getAttr('data-event-type');
            $('#editEventType').val(eventType).trigger('change');

            // --- 3. Fines (iBITS) ---
            document.getElementById('editFineForMember').value = getAttr('data-fine-member', '0.00');
            document.getElementById('editFineForClassOfficer').value = getAttr('data-fine-class-officer', '0.00');
            document.getElementById('editFineForOrgOfficer').value = getAttr('data-fine-org-officer', '0.00');

            // --- 4. Fines (Non-iBITS) ---
            document.getElementById('editNonIbitsFineForMember').value = getAttr('data-non-ibits-fine-member', '50.00');
            document.getElementById('editNonIbitsFineForClassOfficer').value = getAttr('data-non-ibits-fine-class-officer', '100.00');
            document.getElementById('editNonIbitsFineForOrgOfficer').value = getAttr('data-non-ibits-fine-org-officer', '150.00');
        });
    }

    // =========================================================
    // 3. FORM RESET LOGIC (Templates Removed)
    // =========================================================
    const createEventModal = document.getElementById('createEventModal');
    if (createEventModal) {
        createEventModal.addEventListener('hidden.bs.modal', function () {
            const form = createEventModal.querySelector('form');
            if (form) {
                form.reset();
                // Reset Select2 to default
                $('#createEventModal select').val('iBITS Event').trigger('change');
            }
        });
    }
});

// =========================================================
// 4. CLOSE EVENT HELPER
// =========================================================
window.setCloseEventModal = function (id, name) {
    document.getElementById('closeEventId').value = id;
    document.getElementById('closeEventName').textContent = name;
};
