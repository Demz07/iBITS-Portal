// FILE PATH: wwwroot/js/batch-management.js

document.addEventListener('DOMContentLoaded', function () {

    const feeBatchList = document.getElementById('fee-batch-list');
    const fineBatchList = document.getElementById('fine-batch-list');
    const feeDetailView = document.getElementById('fee-detail-view');
    const fineDetailView = document.getElementById('fine-detail-view');
    const feeSearch = document.getElementById('fee-search');
    const fineSearch = document.getElementById('fine-search');

    const placeholderHtml = `
        <div class="detail-placeholder">
            <i class="bi bi-list-ul"></i>
            <h5 class="fw-bold">Select a Batch</h5>
            <p>Choose a batch from the left to view details and manage it.</p>
        </div>`;

    // Function to load details via AJAX
    const loadBatchDetails = async (batchId, batchName, batchType) => {
        const detailView = batchType === 'fee' ? feeDetailView : fineDetailView;
        if (!detailView) return;

        detailView.innerHTML = `<div class="d-flex justify-content-center align-items-center h-100"><div class="spinner-border text-gold" role="status"><span class="visually-hidden">Loading...</span></div></div>`;

        try {
            const response = await fetch(`/Officer/GetBatchDetails?batchId=${batchId}&type=${batchType}`);
            const result = await response.json();

            if (result.success) {
                renderDetails(detailView, batchName, batchId, batchType, result.data);
            } else {
                detailView.innerHTML = `<div class="detail-placeholder"><i class="bi bi-x-circle-fill text-danger"></i><h5>Error</h5><p>${result.message}</p></div>`;
            }
        } catch (error) {
            console.error('Error fetching details:', error);
            detailView.innerHTML = `<div class="detail-placeholder"><i class="bi bi-wifi-off text-danger"></i><h5>Network Error</h5><p>Could not load details.</p></div>`;
        }
    };

    // Function to render the details panel
    const renderDetails = (container, batchName, batchId, batchType, students) => {
        const totalStudents = students.length;

        // Count statuses
        const validatedStudents = students.filter(s => s.status === 'Validated').length;
        const collectedStudents = students.filter(s => s.status === 'Collected').length;
        const unpaidStudents = students.filter(s => s.status === 'Unpaid').length;

        // Calculate Total Paid (Validated + Collected)
        const totalPaid = validatedStudents + collectedStudents;

        let studentRows = '';
        students.forEach(s => {
            let statusBadge = '';
            let rowClass = '';

            if (s.status === 'Validated') {
                // Official Validated Status
                statusBadge = `<span class="badge bg-success"><i class="bi bi-shield-check me-1"></i> Validated</span>`;
            } else if (s.status === 'Collected') {
                // Paid to Class Treasurer (Not Validated)
                statusBadge = `<span class="badge bg-info text-dark"><i class="bi bi-cash-stack me-1"></i> Collected</span>`;
            } else {
                // Unpaid
                statusBadge = `<span class="badge bg-danger"><i class="bi bi-x-circle me-1"></i> Unpaid</span>`;
            }

            studentRows += `
                <tr>
                    <td>${s.studentName}</td>
                    <td>${s.studentNum}</td>
                    <td>${statusBadge}</td>
                    <td class="text-end">₱${(s.amount || 0).toFixed(2)}</td>
                </tr>
            `;
        });

        const html = `
            <div class="detail-header d-flex justify-content-between align-items-center">
                <h5 class="text-white fw-bold mb-0">${batchName}</h5>
                <div>
                    <button class="btn btn-gold-outline btn-sm me-2" onclick="openEditModal('${batchId}', '${batchName}', '${batchType}')">
                        <i class="bi bi-pencil me-1"></i> Edit Batch
                    </button>
                    
                    <button class="btn btn-action-delete btn-sm" onclick="openDeleteModal('${batchId}', '${batchName}', '${batchType}')">
                        <i class="bi bi-trash3-fill"></i> Delete Batch
                    </button>
                </div>
            </div>
            <div class="detail-panel-content">
                <div class="row g-3 mb-4">
                    <div class="col-md-3">
                        <div class="summary-card text-center p-3">
                            <h3 class="text-white fw-bold mb-0">${totalStudents}</h3>
                            <small class="text-muted text-uppercase" style="font-size: 0.7rem;">Total</small>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="summary-card text-center p-3" style="border-bottom: 2px solid #10b981;">
                            <h3 class="text-white fw-bold mb-0">${validatedStudents}</h3>
                            <small class="text-success text-uppercase" style="font-size: 0.7rem;">Validated</small>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="summary-card text-center p-3" style="border-bottom: 2px solid #0dcaf0;">
                            <h3 class="text-white fw-bold mb-0">${collectedStudents}</h3>
                            <small class="text-info text-uppercase" style="font-size: 0.7rem;">Collected</small>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="summary-card text-center p-3" style="border-bottom: 2px solid #ef4444;">
                            <h3 class="text-white fw-bold mb-0">${unpaidStudents}</h3>
                            <small class="text-danger text-uppercase" style="font-size: 0.7rem;">Unpaid</small>
                        </div>
                    </div>
                </div>
                
                <div class="alert alert-info small mb-3">
                    <i class="bi bi-info-circle me-2"></i>
                    <strong>Note:</strong> "Collected" means paid to Class Treasurer but not yet remitted/validated. "Validated" means officially received by Org Treasurer.
                </div>

                <h6 class="text-gold mb-3"><i class="bi bi-people-fill me-2"></i>Students Assigned</h6>
                 <div class="student-table-container">
                    <table class="table table-glass table-sm align-middle">
                        <thead><tr><th>Name</th><th>Student ID</th><th>Status</th><th class="text-end">Amount</th></tr></thead>
                        <tbody>${studentRows}</tbody>
                    </table>
                </div>
            </div>
        `;
        container.innerHTML = html;
    };

    // Attach click listeners
    [feeBatchList, fineBatchList].forEach(list => {
        if (list) {
            list.addEventListener('click', function (e) {
                const card = e.target.closest('.batch-card');
                if (card) {
                    document.querySelectorAll('.batch-card.active').forEach(c => c.classList.remove('active'));
                    card.classList.add('active');

                    const { batchId, batchName, batchType } = card.dataset;
                    loadBatchDetails(batchId, batchName, batchType);
                }
            });
        }
    });

    // Search functionality
    const filterList = (input, list) => {
        const query = input.value.toLowerCase();
        list.querySelectorAll('.batch-card').forEach(card => {
            const name = (card.dataset.batchName || '').toLowerCase();
            card.style.display = name.includes(query) ? '' : 'none';
        });
    };

    if (feeSearch) feeSearch.addEventListener('input', () => filterList(feeSearch, feeBatchList));
    if (fineSearch) fineSearch.addEventListener('input', () => filterList(fineSearch, fineBatchList));

});

// Global functions for modals
function openEditModal(batchId, batchName, batchType) {
    document.getElementById('editBatchId').value = batchId;
    document.getElementById('editBatchType').value = batchType;
    document.getElementById('editBatchName').textContent = batchName;
    new bootstrap.Modal(document.getElementById('editBatchModal')).show();
}

function openDeleteModal(batchId, batchName, batchType) {
    document.getElementById('deleteBatchId').value = batchId;
    document.getElementById('deleteBatchType').value = batchType;
    document.getElementById('deleteBatchName').textContent = batchName;

    // Reset the password field
    const passInput = document.getElementById('deletePassword');
    passInput.value = '';

    // Disable button initially
    document.getElementById('confirmDeleteBtn').disabled = true;

    new bootstrap.Modal(document.getElementById('deleteBatchModal')).show();

    // Focus on password field for UX
    setTimeout(() => passInput.focus(), 500);
}

function toggleDeleteButton() {
    const input = document.getElementById('deletePassword');
    const btn = document.getElementById('confirmDeleteBtn');

    // Enable button only if password field is not empty
    btn.disabled = input.value.trim().length === 0;
}