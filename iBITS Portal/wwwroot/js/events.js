$(document).ready(function () {
    console.log("ðŸ“… Events Manager Initialized");

    // =========================================================
    // 1. HELPER: DESTROY & INIT SELECT2
    // =========================================================
    function initSelect2(modalId) {
        // Destroy existing first to prevent duplication
        $(modalId).find('select.select2-modal').each(function () {
            if ($(this).data('select2')) {
                $(this).select2('destroy');
            }
        });

        // Initialize
        $(modalId).find('select.select2-modal').select2({
            dropdownParent: $(modalId),
            minimumResultsForSearch: Infinity,
            width: '100%'
        });
    }

    // =========================================================
    // 2. CREATE MODAL LOGIC
    // =========================================================
    $('#createEventModal').on('shown.bs.modal', function () {
        initSelect2('#createEventModal');
    });

    $('#createEventModal').on('hidden.bs.modal', function () {
        const form = this.querySelector('form');
        if (form) form.reset();

        // Reset Select2 value
        $('#createEventModal select').val('iBITS Event').trigger('change');

        // Remove manual date constraints
        $('#createEndDate').val('').removeAttr('min');
    });

    // --- Date Constraints (Philippine Time Safe) ---
    // Always use Asia/Manila timezone for correct PH date
    const today = new Date().toLocaleDateString('en-CA', { timeZone: 'Asia/Manila' }); // YYYY-MM-DD format





    // Set min for start date as today
    $('#createEventDate').attr('min', today);

    // Dynamic End Date Constraint
    $('#createEventDate').on('change', function () {
        const startDate = $(this).val();
        $('#createEndDate').attr('min', startDate);

        // If end date is set and is before start date, clear it
        if ($('#createEndDate').val() && $('#createEndDate').val() < startDate) {
            $('#createEndDate').val('');
        }
    });

    // =========================================================
    // 3. EDIT MODAL LOGIC (READ-ONLY SUPPORT)
    // =========================================================

    // Init Select2 when modal opens
    $('#editEventModal').on('shown.bs.modal', function () {
        initSelect2('#editEventModal');
    });

    const editEventModal = document.getElementById('editEventModal');
    if (editEventModal) {
        editEventModal.addEventListener('show.bs.modal', function (event) {
            // Button that triggered the modal
            const button = event.relatedTarget;
            if (!button) return;

            const getAttr = (attr, def = '') => button.getAttribute(attr) || def;

            // --- 1. CHECK LOCK STATUS ---
            const isLocked = getAttr('data-locked') === 'true';

            // --- 2. TOGGLE UI (EDIT VS READ-ONLY) ---
            const fieldset = document.getElementById('editEventFieldset');
            const btnSave = document.getElementById('btnEditSave');
            const btnDelete = document.getElementById('btnEditDelete');
            const title = document.getElementById('editEventTitle');
            const typeSelect = $('#editEventType');

            if (isLocked) {
                // COMPLETED EVENT: Read Only Mode
                fieldset.disabled = true; // Native HTML disable for inputs
                btnSave.style.display = 'none';
                btnDelete.style.display = 'none';

                title.innerHTML = '<i class="bi bi-eye-fill me-2"></i> Event Details <span class="badge bg-secondary ms-2" style="font-size: 0.6em; vertical-align: middle;">COMPLETED</span>';

                // Select2 requires explicit disable
                typeSelect.prop('disabled', true);
            } else {
                // ACTIVE EVENT: Edit Mode
                fieldset.disabled = false;
                btnSave.style.display = 'inline-block';
                btnDelete.style.display = 'inline-block';

                title.innerHTML = '<i class="bi bi-pencil-square me-2"></i> Edit Event';

                // Enable Select2
                typeSelect.prop('disabled', false);
            }

            // --- 3. POPULATE DATA ---
            document.getElementById('editEventId').value = getAttr('data-event-id');
            document.getElementById('deleteEventId').value = getAttr('data-event-id'); // For Delete Modal

            document.getElementById('editEventName').value = getAttr('data-event-name');
            document.getElementById('editEventLocation').value = getAttr('data-event-location');
            document.getElementById('editEventDate').value = getAttr('data-event-date');

            // Time inputs (Expects HH:mm format)
            document.getElementById('editStartTime').value = getAttr('data-start-time');
            document.getElementById('editEndTime').value = getAttr('data-end-time');

            document.getElementById('editEndDate').value = getAttr('data-end-date');
            document.getElementById('editEventDuration').value = getAttr('data-event-duration');
            document.getElementById('editAcadYear').value = getAttr('data-event-acad-year');
            document.getElementById('editEventDesc').value = getAttr('data-event-desc');

            // Set Select2 Value
            typeSelect.val(getAttr('data-event-type')).trigger('change');

            // Fines (iBITS)
            document.getElementById('editFineForMember').value = getAttr('data-fine-member', '0.00');
            document.getElementById('editFineForClassOfficer').value = getAttr('data-fine-class-officer', '0.00');
            document.getElementById('editFineForOrgOfficer').value = getAttr('data-fine-org-officer', '0.00');

            // Fines (Non-iBITS)
            document.getElementById('editNonIbitsFineForMember').value = getAttr('data-non-ibits-fine-member', '50.00');
            document.getElementById('editNonIbitsFineForClassOfficer').value = getAttr('data-non-ibits-fine-class-officer', '100.00');
            document.getElementById('editNonIbitsFineForOrgOfficer').value = getAttr('data-non-ibits-fine-org-officer', '150.00');
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



