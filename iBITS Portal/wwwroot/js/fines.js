// fines.js - Fines Management Page JavaScript

document.addEventListener('DOMContentLoaded', function () {
    initializeCharts();
    initializeFilters();
    initializeModals();
});

// ============================================================
// CHARTS INITIALIZATION
// ============================================================
function initializeCharts() {
    const paidData = window.finesChartsData?.paid || {};
    const unpaidData = window.finesChartsData?.unpaid || {};

    // Check if data exists
    const hasPaidData = Object.keys(paidData).length > 0 && Object.values(paidData).some(v => v > 0);
    const hasUnpaidData = Object.keys(unpaidData).length > 0 && Object.values(unpaidData).some(v => v > 0);

    // Paid Fines Chart
    const paidCanvas = document.getElementById('paidChart');
    const paidNoData = document.getElementById('paidNoData');
    
    if (hasPaidData) {
        paidCanvas.style.display = 'block';
        paidNoData.style.display = 'none';
        createChart('paidChart', paidData, 'Paid Fines', '#10b981');
    } else {
        paidCanvas.style.display = 'none';
        paidNoData.style.display = 'flex';
    }

    // Unpaid Fines Chart
    const unpaidCanvas = document.getElementById('unpaidChart');
    const unpaidNoData = document.getElementById('unpaidNoData');
    
    if (hasUnpaidData) {
        unpaidCanvas.style.display = 'block';
        unpaidNoData.style.display = 'none';
        createChart('unpaidChart', unpaidData, 'Unpaid Fines', '#fbbf24');
    } else {
        unpaidCanvas.style.display = 'none';
        unpaidNoData.style.display = 'flex';
    }
}

function createChart(canvasId, data, label, color) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: Object.keys(data),
            datasets: [{
                label: label,
                data: Object.values(data),
                backgroundColor: [
                    color,
                    adjustColorOpacity(color, 0.7),
                    adjustColorOpacity(color, 0.4)
                ],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        color: 'rgba(255, 255, 255, 0.8)',
                        padding: 15,
                        font: { size: 12 }
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12,
                    titleColor: '#fff',
                    bodyColor: '#fff',
                    borderColor: 'rgba(255, 255, 255, 0.1)',
                    borderWidth: 1
                }
            }
        }
    });
}

function adjustColorOpacity(hex, opacity) {
    // Simple opacity adjustment for hex colors
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    return `rgba(${r}, ${g}, ${b}, ${opacity})`;
}

// ============================================================
// FILTERS
// ============================================================
function initializeFilters() {
    const filterSearch = document.getElementById('filterSearch');
    const filterEvent = document.getElementById('filterEvent');
    const filterStatus = document.getElementById('filterStatus');
    const filterProgram = document.getElementById('filterProgram');
    const filterOverdue = document.getElementById('filterOverdue');

    [filterSearch, filterEvent, filterStatus, filterProgram, filterOverdue].forEach(filter => {
        if (filter) {
            filter.addEventListener('input', applyFilters);
            filter.addEventListener('change', applyFilters);
        }
    });
}

function applyFilters() {
    const searchTerm = document.getElementById('filterSearch').value.toLowerCase();
    const eventFilter = document.getElementById('filterEvent').value;
    const statusFilter = document.getElementById('filterStatus').value.toLowerCase();
    const programFilter = document.getElementById('filterProgram').value;
    const overdueFilter = document.getElementById('filterOverdue').value;

    const table = document.getElementById('finesTable');
    const rows = table.querySelectorAll('tbody tr');

    rows.forEach(row => {
        const studentName = row.getAttribute('data-student')?.toLowerCase() || '';
        const studentNum = row.getAttribute('data-studentnum')?.toLowerCase() || '';
        const eventId = row.getAttribute('data-event') || '';
        const status = row.getAttribute('data-status')?.toLowerCase() || '';
        const program = row.getAttribute('data-program') || '';
        const isOverdue = row.getAttribute('data-overdue') === 'true';
        const daysOverdue = parseInt(row.getAttribute('data-daysoverdue') || '0');

        let show = true;

        // Search filter
        if (searchTerm && !studentName.includes(searchTerm) && !studentNum.includes(searchTerm)) {
            show = false;
        }

        // Event filter
        if (eventFilter && eventId !== eventFilter) {
            show = false;
        }

        // Status filter
        if (statusFilter && status !== statusFilter) {
            show = false;
        }

        // Program filter
        if (programFilter && program !== programFilter) {
            show = false;
        }

        // Overdue filter
        if (overdueFilter === 'overdue' && !isOverdue) {
            show = false;
        } else if (overdueFilter === 'upcoming' && (isOverdue || daysOverdue < 0)) {
            // Due soon: not overdue but due within 7 days
            const dueSoon = daysOverdue >= -7 && daysOverdue < 0;
            if (!dueSoon) show = false;
        }

        row.style.display = show ? '' : 'none';
    });

    updateStats();
}

function clearFilters() {
    document.getElementById('filterSearch').value = '';
    document.getElementById('filterEvent').value = '';
    document.getElementById('filterStatus').value = '';
    document.getElementById('filterProgram').value = '';
    document.getElementById('filterOverdue').value = '';
    applyFilters();
}

function updateStats() {
    const table = document.getElementById('finesTable');
    const visibleRows = Array.from(table.querySelectorAll('tbody tr')).filter(row => row.style.display !== 'none');

    let totalExpected = 0;
    let totalCollected = 0;
    let paidCount = 0;
    let unpaidCount = 0;

    visibleRows.forEach(row => {
        const amountText = row.cells[4].textContent.replace('₱', '').replace(',', '');
        const amount = parseFloat(amountText) || 0;
        const status = row.getAttribute('data-status')?.toLowerCase() || '';

        totalExpected += amount;

        if (status === 'paid') {
            totalCollected += amount;
            paidCount++;
        } else if (status === 'unpaid') {
            unpaidCount++;
        }
    });

    const totalPending = totalExpected - totalCollected;
    const collectionRate = totalExpected > 0 ? ((totalCollected / totalExpected) * 100).toFixed(1) : 0;

    document.getElementById('totalExpected').textContent = totalExpected.toFixed(2);
    document.getElementById('totalCollected').textContent = totalCollected.toFixed(2);
    document.getElementById('totalPending').textContent = totalPending.toFixed(2);
    document.getElementById('collectionRate').textContent = collectionRate;
}

// ============================================================
// MODALS
// ============================================================
function initializeModals() {
    // Modal initialization is handled by functions called from HTML
}

function markAsPaid(fineId) {
    document.getElementById('markPaidFineId').value = fineId;
    document.getElementById('markPaidFineIdDisplay').textContent = fineId;
    const modal = new bootstrap.Modal(document.getElementById('markPaidModal'));
    modal.show();
}

function waiveFine(fineId, studentName, amount) {
    document.getElementById('waiveFineId').value = fineId;
    document.getElementById('waiveStudentName').textContent = studentName;
    document.getElementById('waiveFineAmount').textContent = amount.toFixed(2);
    document.getElementById('waiveReason').value = '';
    const modal = new bootstrap.Modal(document.getElementById('waiveFineModal'));
    modal.show();
}

function adjustFine(fineId, studentName, currentAmount) {
    document.getElementById('adjustFineId').value = fineId;
    document.getElementById('adjustStudentName').textContent = studentName;
    document.getElementById('adjustCurrentAmount').textContent = currentAmount.toFixed(2);
    document.getElementById('newAmount').value = currentAmount.toFixed(2);
    document.getElementById('adjustReason').value = '';
    const modal = new bootstrap.Modal(document.getElementById('adjustFineModal'));
    modal.show();
}

function deleteFine(fineId, studentName, amount) {
    document.getElementById('deleteFineId').value = fineId;
    document.getElementById('deleteStudentName').textContent = studentName;
    document.getElementById('deleteFineAmount').textContent = amount.toFixed(2);
    const modal = new bootstrap.Modal(document.getElementById('deleteFineModal'));
    modal.show();
}

// ============================================================
// EXPORT
// ============================================================
function exportFines() {
    window.location.href = '/Admin/ExportFinesToExcel';
}

// ============================================================
// UTILITY FUNCTIONS
// ============================================================

// Auto-dismiss alerts after 5 seconds
document.addEventListener('DOMContentLoaded', function () {
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = bootstrap.Alert.getInstance(alert) || new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
});

// Confirm before form submission for destructive actions
document.querySelectorAll('form[action*="Delete"]').forEach(form => {
    form.addEventListener('submit', function (e) {
        if (!confirm('Are you sure? This action cannot be undone.')) {
            e.preventDefault();
        }
    });
});
