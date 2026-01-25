// wwwroot/js/manual-fines.js

// Reusable modal helper functions
function openMarkPaidModal(id) {
    // Implement if needed, or reuse from a global script
}

function openWaiveModal(id, name, amount) {
    // Implement if needed, or reuse from a global script
}

function openAdjustModal(id, name, amount) {
    // Implement if needed, or reuse from a global script
}

// Specific delete modal handler for this page
function openDeleteModal(id, name, amount) {
    const modal = new bootstrap.Modal(document.getElementById('deleteFineModal'));
    document.getElementById('deleteFineId').value = id;
    document.getElementById('deleteStudentName').innerText = name || 'N/A';
    document.getElementById('deleteFineAmount').innerText = amount ? parseFloat(amount).toFixed(2) : '0.00';
    modal.show();
}