/**
 * ============================================================
 * FILE PATH: wwwroot/js/admin-dashboard.js
 * ============================================================
 * iBITS Portal - Admin Dashboard Charts
 * Gold & Navy Blue Theme with Chart.js
 * Separate Modals for Enrollment and Financial Sections
 * ============================================================
 */

// ==========================================
// CHART CONFIGURATION & COLORS
// ==========================================

const chartColors = {
    gold: '#D4AF37',
    goldLight: 'rgba(212, 175, 55, 0.7)',
    goldDim: 'rgba(212, 175, 55, 0.2)',
    blue: '#3B82F6',
    blueLight: 'rgba(59, 130, 246, 0.7)',
    green: '#22C55E',
    greenLight: 'rgba(34, 197, 94, 0.2)',
    red: '#EF4444',
    redLight: 'rgba(239, 68, 68, 0.2)',
    cyan: '#06B6D4',
    cyanLight: 'rgba(6, 182, 212, 0.2)',
    purple: '#A855F7',
    orange: '#F97316',
    pink: '#EC4899',

    // Program specific
    bsit1: '#D4AF37',
    bsit2: '#F59E0B',
    bsit3: '#EAB308',
    bsit4: '#CA8A04',
    dit1: '#3B82F6',
    dit2: '#60A5FA',
    dit3: '#93C5FD',

    // Text colors
    textLight: 'rgba(255, 255, 255, 0.7)',
    textStrong: 'rgba(255, 255, 255, 0.95)',
    gridColor: 'rgba(255, 255, 255, 0.08)'
};

const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

// Default Chart.js options
Chart.defaults.color = chartColors.textLight;
Chart.defaults.borderColor = chartColors.gridColor;
Chart.defaults.font.family = "'Segoe UI', 'Roboto', sans-serif";

// ==========================================
// CHART INSTANCES
// ==========================================

let feesPaidChart = null;
let feesUnpaidChart = null;
let finesPaidChart = null;
let finesUnpaidChart = null;

// ==========================================
// INITIALIZATION
// ==========================================

document.addEventListener('DOMContentLoaded', function () {
    initializeDashboard();
});

function initializeDashboard() {
    const data = window.dashboardData || {};

    // Update counters
    updateCounters(data);

    // Initialize all charts
    initStudentsPerProgramChart(data);
    initPaymentsPieChart(data);
    initPendingPieChart(data);
    initFinesPieChart(data);

    // Initialize Fees Charts
    if (data.feesData) {
        initFinancialChart('feesPaidChart', data.feesData.paidByProgram, 'fees', 'paid');
        initFinancialChart('feesUnpaidChart', data.feesData.unpaidByProgram, 'fees', 'pending');
        updateFinancialSummary('fees', data.feesData);
    }

    // Initialize Fines Charts
    if (data.finesData) {
        initFinancialChart('finesPaidChart', data.finesData.paidByProgram, 'fines', 'paid');
        initFinancialChart('finesUnpaidChart', data.finesData.unpaidByProgram, 'fines', 'pending');
        updateFinancialSummary('fines', data.finesData);
    }
}

// Generic Financial Chart Initializer
function initFinancialChart(canvasId, dataMap, type, status) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy existing if re-initializing
    const existingChart = Chart.getChart(canvasId);
    if (existingChart) existingChart.destroy();

    const chartData = [
        dataMap.bsit1 || dataMap.BSIT1 || 0,
        dataMap.bsit2 || dataMap.BSIT2 || 0,
        dataMap.bsit3 || dataMap.BSIT3 || 0,
        dataMap.bsit4 || dataMap.BSIT4 || 0,
        dataMap.dit1 || dataMap.DIT1 || 0,
        dataMap.dit2 || dataMap.DIT2 || 0,
        dataMap.dit3 || dataMap.DIT3 || 0
    ];

    const hasData = chartData.some(val => val > 0);
    // Handle "No Data" visibility logic here... (hide canvas, show message)

    const colors = type === 'fees' && status === 'paid' ?
        [chartColors.bsit1, chartColors.bsit2, chartColors.bsit3, chartColors.bsit4, chartColors.dit1, chartColors.dit2, chartColors.dit3] : // Gold/Blue
        type === 'fees' && status === 'pending' ?
            // Make pending fees look slightly different (e.g., Orange tint) or keep uniform
            [chartColors.bsit1, chartColors.bsit2, chartColors.bsit3, chartColors.bsit4, chartColors.dit1, chartColors.dit2, chartColors.dit3] :
            // Fines (Red tint could be applied dynamically, but sticking to program colors is cleaner)
            [chartColors.bsit1, chartColors.bsit2, chartColors.bsit3, chartColors.bsit4, chartColors.dit1, chartColors.dit2, chartColors.dit3];

    new Chart(ctx, {
        type: 'doughnut', // Doughnut looks better for this
        data: {
            labels: ['BSIT1', 'BSIT2', 'BSIT3', 'BSIT4', 'DIT1', 'DIT2', 'DIT3'],
            datasets: [{
                data: chartData,
                backgroundColor: colors,
                borderWidth: 1,
                borderColor: 'rgba(11, 26, 51, 0.8)'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '60%',
            onClick: (e, elements, chart) => {
                if (!elements.length) return;
                const idx = elements[0].index;
                const segment = chart.data.labels[idx];
                const category = document.getElementById(type === 'fees' ? 'feeCategoryFilter' : 'fineCategoryFilter').value;

                // Open Modal
                if (type === 'fees') {
                    showFinancialDetailsModal('payments', segment, status, category); // Reuse existing 'payments' key
                } else {
                    showFinancialDetailsModal('fines', segment, status, category);
                }
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return `${context.label}: ₱${Number(context.raw).toLocaleString()}`;
                        }
                    }
                }
            }
        }
    });
}

// Refresh Data on Dropdown Change
function refreshFinancialData(type) {
    const feeCat = document.getElementById('feeCategoryFilter').value;
    const fineCat = document.getElementById('fineCategoryFilter').value;

    const btn = document.querySelector('.section-icon.' + (type === 'fees' ? 'financial-icon' : 'status-icon'));
    if (btn) btn.innerHTML = '<div class="spinner-border spinner-border-sm" role="status"></div>';

    fetch(`${window.dashboardRefreshUrl}?feeCategory=${feeCat}&fineCategory=${fineCat}`)
        .then(res => res.json())
        .then(data => {
            if (type === 'fees' && data.feesOverview) {
                initFinancialChart('feesPaidChart', data.feesOverview.paidByProgram, 'fees', 'paid');
                initFinancialChart('feesUnpaidChart', data.feesOverview.unpaidByProgram, 'fees', 'pending');
                updateFinancialSummary('fees', data.feesOverview);
            }
            if (type === 'fines' && data.finesOverview) {
                initFinancialChart('finesPaidChart', data.finesOverview.paidByProgram, 'fines', 'paid');
                initFinancialChart('finesUnpaidChart', data.finesOverview.unpaidByProgram, 'fines', 'pending');
                updateFinancialSummary('fines', data.finesOverview);
            }
        })
        .finally(() => {
            // Restore icon
            if (btn) btn.innerHTML = type === 'fees' ? '<i class="bi bi-wallet2"></i>' : '<i class="bi bi-exclamation-triangle"></i>';
        });
}

function updateFinancialSummary(type, data) {
    document.getElementById(type + 'Expected').textContent = formatCurrency(data.totalExpected);
    document.getElementById(type + 'Collected').textContent = formatCurrency(data.totalCollected);
    document.getElementById(type + 'Pending').textContent = formatCurrency(data.totalPending);

    // Rates
    const collRateEl = document.getElementById(type + 'CollectionRate');
    if (collRateEl) collRateEl.textContent = data.collectionRate.toFixed(1);

    const pendRateEl = document.getElementById(type + 'PendingRate');
    if (pendRateEl) {
        const rate = data.totalExpected > 0 ? ((data.totalPending / data.totalExpected) * 100).toFixed(1) : 0;
        pendRateEl.textContent = rate;
    }
}


// ==========================================
// UPDATE COUNTERS
// ==========================================

function updateCounters(data) {
    const yearLevels = data.yearLevelCounts || {};

    // Animate counter updates for year levels
    animateCounter('bsit1Count', yearLevels.bsit1 || yearLevels.BSIT1 || 0);
    animateCounter('bsit2Count', yearLevels.bsit2 || yearLevels.BSIT2 || 0);
    animateCounter('bsit3Count', yearLevels.bsit3 || yearLevels.BSIT3 || 0);
    animateCounter('bsit4Count', yearLevels.bsit4 || yearLevels.BSIT4 || 0);
    animateCounter('dit1Count', yearLevels.dit1 || yearLevels.DIT1 || 0);
    animateCounter('dit2Count', yearLevels.dit2 || yearLevels.DIT2 || 0);
    animateCounter('dit3Count', yearLevels.dit3 || yearLevels.DIT3 || 0);


    // Total students (use totalStudents from controller, fallback to calculated)
    const totalStudents = data.totalStudents || 0;
    animateCounter('totalStudents', totalStudents);

    // =====================================================
    // FINANCIAL SUMMARY CARDS
    // =====================================================

    // Total Expected
    const totalExpected = data.totalExpected || 0;
    updateCurrencyElement('totalExpected', totalExpected);

    // Total Collected (Paid)
    const totalCollected = data.totalCollected || 0;
    updateCurrencyElement('totalCollected', totalCollected);

    // Collection Rate
    const collectionRate = data.collectionRate || 0;
    const collectionRateEl = document.getElementById('collectionRate');
    if (collectionRateEl) collectionRateEl.textContent = collectionRate.toFixed(1);

    // Total Pending (Unpaid)
    const totalPending = data.totalPending || 0;
    updateCurrencyElement('totalPendingCard', totalPending);
    updateCurrencyElement('totalPending', totalPending);

    // Pending Rate
    const pendingRate = totalExpected > 0 ? ((totalPending / totalExpected) * 100).toFixed(1) : 0;
    const pendingRateEl = document.getElementById('pendingRate');
    if (pendingRateEl) pendingRateEl.textContent = pendingRate;

    // Total Fines (in summary card)
    const totalFines = data.totalFines || 0;
    updateCurrencyElement('totalFinesCard', totalFines);
    updateCurrencyElement('totalFines', totalFines);

    // Total Payments (Paid) - for chart badge
    const totalPayments = data.totalPayments || 0;
    updateCurrencyElement('totalPayments', totalPayments);
}

// ==========================================
// HELPER: Update Currency Element
// ==========================================

function updateCurrencyElement(elementId, amount) {
    const el = document.getElementById(elementId);
    if (el) {
        el.textContent = formatCurrency(amount);
    }
}

// ==========================================
// HELPER: Format Currency
// ==========================================

function formatCurrency(amount) {
    return Number(amount).toLocaleString('en-PH', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}

function animateCounter(elementId, targetValue) {
    const element = document.getElementById(elementId);
    if (!element) return;

    const duration = 1000;
    const startValue = parseInt(element.textContent) || 0;
    const increment = (targetValue - startValue) / (duration / 16);
    let currentValue = startValue;

    const timer = setInterval(() => {
        currentValue += increment;
        if ((increment > 0 && currentValue >= targetValue) || (increment < 0 && currentValue <= targetValue) || increment === 0) {
            element.textContent = targetValue;
            clearInterval(timer);
        } else {
            element.textContent = Math.round(currentValue);
        }
    }, 16);
}

// ==========================================
// 1. STUDENTS PER PROGRAM (Doughnut Chart)
// ==========================================

function initStudentsPerProgramChart(data) {
    const ctx = document.getElementById('studentsPerProgramChart');
    if (!ctx) return;

    const programs = data.studentsPerProgram || {};
    const bsitCount = programs.bsit || programs.BSIT || 0;
    const ditCount = programs.dit || programs.DIT || 0;

    studentsPerProgramChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['BSIT', 'DIT'],
            datasets: [{
                data: [bsitCount, ditCount],
                backgroundColor: [chartColors.gold, chartColors.blue],
                borderColor: ['rgba(212, 175, 55, 0.8)', 'rgba(59, 130, 246, 0.8)'],
                borderWidth: 2,
                hoverOffset: 10
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '65%',
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 20,
                        usePointStyle: true,
                        pointStyle: 'rectRounded',
                        font: { size: 12, weight: '500' }
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(11, 26, 51, 0.95)',
                    titleColor: chartColors.gold,
                    bodyColor: chartColors.textStrong,
                    borderColor: chartColors.gold,
                    borderWidth: 1,
                    padding: 12,
                    displayColors: true,
                    callbacks: {
                        label: function (context) {
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = total > 0 ? ((context.raw / total) * 100).toFixed(1) : 0;
                            return `${context.label}: ${context.raw} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });
}

// ==========================================
// FINANCIAL CHARTS
// ==========================================

function initPaymentsPieChart(data) {
    const ctx = document.getElementById('paymentsPieChart');
    if (!ctx) return;

    const payments = data.paymentsByProgram || {};

    const paymentData = [
        payments.bsit1 || payments.BSIT1 || 0,
        payments.bsit2 || payments.BSIT2 || 0,
        payments.bsit3 || payments.BSIT3 || 0,
        payments.bsit4 || payments.BSIT4 || 0,
        payments.dit1 || payments.DIT1 || 0,
        payments.dit2 || payments.DIT2 || 0,
        payments.dit3 || payments.DIT3 || 0
    ];

    const hasData = paymentData.some(val => val > 0);

    if (!hasData) {
        showNoDataMessage(ctx, 'No payment data available');
        return;
    }

    paymentsPieChart = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: ['BSIT1', 'BSIT2', 'BSIT3', 'BSIT4', 'DIT1', 'DIT2', 'DIT3'],
            datasets: [{
                data: paymentData,
                backgroundColor: [
                    chartColors.bsit1,
                    chartColors.bsit2,
                    chartColors.bsit3,
                    chartColors.bsit4,
                    chartColors.dit1,
                    chartColors.dit2,
                    chartColors.dit3
                ],
                borderWidth: 2,
                borderColor: 'rgba(11, 26, 51, 0.8)',
                hoverOffset: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            onClick: (event, elements) => handleFinancialChartClick(event, elements, 'payments', 'paid', paymentsPieChart),
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: 'rgba(11, 26, 51, 0.95)',
                    titleColor: chartColors.gold,
                    bodyColor: chartColors.textStrong,
                    borderColor: chartColors.gold,
                    borderWidth: 1,
                    padding: 12,
                    callbacks: {
                        label: function (context) {
                            return `${context.label}: ₱${context.raw.toLocaleString()}`;
                        }
                    }
                }
            }
        }
    });
}

function initPendingPieChart(data) {
    const ctx = document.getElementById('pendingPieChart');
    if (!ctx) return;

    const pending = data.pendingByProgram || {};

    const pendingData = [
        pending.bsit1 || pending.BSIT1 || 0,
        pending.bsit2 || pending.BSIT2 || 0,
        pending.bsit3 || pending.BSIT3 || 0,
        pending.bsit4 || pending.BSIT4 || 0,
        pending.dit1 || pending.DIT1 || 0,
        pending.dit2 || pending.DIT2 || 0,
        pending.dit3 || pending.DIT3 || 0
    ];

    const hasData = pendingData.some(val => val > 0);

    if (!hasData) {
        showNoDataMessage(ctx, 'No pending payments');
        return;
    }

    pendingPieChart = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: ['BSIT1', 'BSIT2', 'BSIT3', 'BSIT4', 'DIT1', 'DIT2', 'DIT3'],
            datasets: [{
                data: pendingData,
                backgroundColor: [
                    chartColors.bsit1,
                    chartColors.bsit2,
                    chartColors.bsit3,
                    chartColors.bsit4,
                    chartColors.dit1,
                    chartColors.dit2,
                    chartColors.dit3
                ],
                borderWidth: 2,
                borderColor: 'rgba(11, 26, 51, 0.8)',
                hoverOffset: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            onClick: (event, elements) => handleFinancialChartClick(event, elements, 'payments', 'pending', pendingPieChart),
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: 'rgba(11, 26, 51, 0.95)',
                    titleColor: chartColors.orange,
                    bodyColor: chartColors.textStrong,
                    borderColor: chartColors.orange,
                    borderWidth: 1,
                    padding: 12,
                    callbacks: {
                        label: function (context) {
                            return `${context.label}: ₱${context.raw.toLocaleString()}`;
                        }
                    }
                }
            }
        }
    });
}

function initFinesPieChart(data) {
    const ctx = document.getElementById('finesPieChart');
    if (!ctx) return;

    const fines = data.finesByProgram || {};

    const fineData = [
        fines.bsit1 || fines.BSIT1 || 0,
        fines.bsit2 || fines.BSIT2 || 0,
        fines.bsit3 || fines.BSIT3 || 0,
        fines.bsit4 || fines.BSIT4 || 0,
        fines.dit1 || fines.DIT1 || 0,
        fines.dit2 || fines.DIT2 || 0,
        fines.dit3 || fines.DIT3 || 0
    ];

    const hasData = fineData.some(val => val > 0);

    if (!hasData) {
        showNoDataMessage(ctx, 'No fines data available');
        return;
    }

    finesPieChart = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: ['BSIT1', 'BSIT2', 'BSIT3', 'BSIT4', 'DIT1', 'DIT2', 'DIT3'],
            datasets: [{
                data: fineData,
                backgroundColor: [
                    chartColors.bsit1,
                    chartColors.bsit2,
                    chartColors.bsit3,
                    chartColors.bsit4,
                    chartColors.dit1,
                    chartColors.dit2,
                    chartColors.dit3
                ],
                borderWidth: 2,
                borderColor: 'rgba(11, 26, 51, 0.8)',
                hoverOffset: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            onClick: (event, elements) => handleFinancialChartClick(event, elements, 'fines', null, finesPieChart),
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: 'rgba(11, 26, 51, 0.95)',
                    titleColor: chartColors.gold,
                    bodyColor: chartColors.textStrong,
                    borderColor: chartColors.red,
                    borderWidth: 1,
                    padding: 12,
                    callbacks: {
                        label: function (context) {
                            return `${context.label}: ₱${context.raw.toLocaleString()}`;
                        }
                    }
                }
            }
        }
    });
}

// ==========================================
// HELPER: Show "No Data" Message for Charts
// ==========================================

function showNoDataMessage(canvas, message) {
    const container = canvas.parentElement;

    // Hide the canvas
    canvas.style.display = 'none';

    // Check if message already exists
    if (container.querySelector('.no-data-message')) return;

    // Create and add no-data message
    const noDataDiv = document.createElement('div');
    noDataDiv.className = 'no-data-message';
    noDataDiv.innerHTML = `
        <div class="no-data-icon">
            <i class="bi bi-inbox"></i>
        </div>
        <p class="no-data-text">${message}</p>
    `;
    container.appendChild(noDataDiv);
}

// ==========================================
// REFRESH DASHBOARD
// ==========================================

function refreshDashboard() {
    const $btn = $('#btnRefresh');
    $btn.find('i').addClass('spin-animation');
    $btn.prop('disabled', true);

    if (window.dashboardRefreshUrl) {
        $.ajax({
            url: window.dashboardRefreshUrl,
            type: 'GET',
            success: function (data) {
                window.dashboardData = data;
                updateCounters(data);
                updateStudentsPerProgramChart(data);
                updatePaymentsPieChart(data);
                updateFinesPieChart(data);
            },
            error: function () {
                console.error('Failed to refresh dashboard data');
                alert('Failed to refresh data. Please try again.');
            },
            complete: function () {
                setTimeout(() => {
                    $btn.find('i').removeClass('spin-animation');
                    $btn.prop('disabled', false);
                }, 500);
            }
        });
    } else {
        setTimeout(() => {
            $btn.find('i').removeClass('spin-animation');
            $btn.prop('disabled', false);
        }, 1000);
    }
}

// ==========================================
// UPDATE CHART FUNCTIONS
// ==========================================

function updateStudentsPerProgramChart(data) {
    if (!studentsPerProgramChart) return;
    const programs = data.studentsPerProgram || {};
    studentsPerProgramChart.data.datasets[0].data = [
        programs.bsit || programs.BSIT || 0,
        programs.dit || programs.DIT || 0
    ];
    studentsPerProgramChart.update();
}

function updatePaymentsPieChart(data) {
    if (!paymentsPieChart) return;
    const payments = data.paymentsByProgram || {};
    paymentsPieChart.data.datasets[0].data = [
        payments.bsit1 || payments.BSIT1 || 0,
        payments.bsit2 || payments.BSIT2 || 0,
        payments.bsit3 || payments.BSIT3 || 0,
        payments.bsit4 || payments.BSIT4 || 0,
        payments.dit1 || payments.DIT1 || 0,
        payments.dit2 || payments.DIT2 || 0,
        payments.dit3 || payments.DIT3 || 0
    ];
    paymentsPieChart.update();
}

function updateFinesPieChart(data) {
    if (!finesPieChart) return;
    const fines = data.finesByProgram || {};
    finesPieChart.data.datasets[0].data = [
        fines.bsit1 || fines.BSIT1 || 0,
        fines.bsit2 || fines.BSIT2 || 0,
        fines.bsit3 || fines.BSIT3 || 0,
        fines.bsit4 || fines.BSIT4 || 0,
        fines.dit1 || fines.DIT1 || 0,
        fines.dit2 || fines.DIT2 || 0,
        fines.dit3 || fines.DIT3 || 0
    ];
    finesPieChart.update();
}

// ==========================================
// ENROLLMENT MODAL HANDLERS
// ==========================================

function showYearLevelModal(program, yearLevel) {
    const modal = new bootstrap.Modal(document.getElementById('enrollmentDetailsModal'));
    const modalTitle = document.getElementById('enrollmentModalTitle');
    const modalLoading = document.getElementById('enrollmentModalLoading');
    const modalContent = document.getElementById('enrollmentModalContent');
    const goToStudentsBtn = document.getElementById('enrollmentGoToStudentsBtn');

    // Set modal title
    modalTitle.textContent = `${program} ${getOrdinal(yearLevel)} Year Students`;

    // Update navigation button
    if (goToStudentsBtn) {
        const queryParams = `?program=${program}&year=${yearLevel}&autoApply=true`;
        goToStudentsBtn.href = window.studentRecordsUrl + queryParams;
    }

    // Show loading state
    modalLoading.style.display = 'block';
    modalContent.style.display = 'none';

    modal.show();

    fetch(`${window.studentsByYearLevelUrl}?program=${program}&yearLevel=${yearLevel}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                populateEnrollmentModal(data);
            } else {
                showEnrollmentModalError(data.message || 'Failed to load details');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showEnrollmentModalError('Failed to load details');
        })
        .finally(() => {
            modalLoading.style.display = 'none';
            modalContent.style.display = 'block';
        });
}

function populateEnrollmentModal(data) {
    const summary = data.summary || {};

    // Update summary cards
    const totalStudentsEl = document.getElementById('enrollmentTotalStudents');
    const activeStudentsEl = document.getElementById('enrollmentActiveStudents');
    const inactiveStudentsEl = document.getElementById('enrollmentInactiveStudents');

    if (totalStudentsEl) totalStudentsEl.textContent = summary.totalStudents || 0;
    if (activeStudentsEl) activeStudentsEl.textContent = summary.activeStudents || 0;
    if (inactiveStudentsEl) inactiveStudentsEl.textContent = (summary.totalStudents - summary.activeStudents) || 0;

    // Populate student list
    const studentListBody = document.getElementById('enrollmentStudentListBody');
    if (!studentListBody) {
        console.error('enrollmentStudentListBody element not found');
        return;
    }

    studentListBody.innerHTML = '';

    if (data.students && data.students.length > 0) {
        data.students.forEach(student => {
            const row = document.createElement('tr');
            const statusClass = student.status === 'Active' ? 'paid' : 'unpaid';
            row.innerHTML = `
                <td><code>${student.studentNum}</code></td>
                <td>${student.name}</td>
                <td>${student.section || 'N/A'}</td>
              
            `;
            studentListBody.appendChild(row);
        });
    } else {
        studentListBody.innerHTML = '<tr><td colspan="4" class="text-center text-muted">No students found</td></tr>';
    }
}

function showEnrollmentModalError(message) {
    const modalContent = document.getElementById('enrollmentModalContent');
    modalContent.innerHTML = `
        <div class="text-center py-4">
            <div class="text-danger mb-3">
                <i class="bi bi-exclamation-circle" style="font-size: 3rem;"></i>
            </div>
            <p class="text-muted">${message}</p>
        </div>
    `;
}

// ==========================================
// FINANCIAL MODAL HANDLERS
// ==========================================

function handleFinancialChartClick(event, elements, chartType, status, chart) {
    if (!elements || elements.length === 0) return;

    const index = elements[0].index;
    const label = chart.data.labels[index];
    const value = chart.data.datasets[0].data[index];

    if (value === 0) return;

    showFinancialDetailsModal(chartType, label, status);
}

function showFinancialDetailsModal(chartType, segment, status, category) {
    const modalEl = document.getElementById('financialDetailsModal');
    if (!modalEl) return;

    const modal = new bootstrap.Modal(modalEl);
    const modalTitle = document.getElementById('financialModalTitle');
    const modalLoading = document.getElementById('financialModalLoading');
    const modalContent = document.getElementById('financialModalContent');
    const breakdownTitle = document.getElementById('financialBreakdownTitle');
    const breakdownColName = document.getElementById('financialBreakdownColName');
    const studentColDetail = document.getElementById('financialStudentColDetail');

    const yearLevel = segment.replace('BSIT', '').replace('DIT', '');
    const program = segment.includes('BSIT') ? 'BSIT' : 'DIT';

    let titleText = '';
    let url = '';
    const categoryParam = category ? `&category=${encodeURIComponent(category)}` : '';

    if (chartType === 'payments') {
        titleText = status === 'paid' ?
            `${program} ${getOrdinal(yearLevel)} Year - Payments Collected` :
            `${program} ${getOrdinal(yearLevel)} Year - Payments Pending`;
        if (breakdownTitle) breakdownTitle.innerHTML = '<i class="bi bi-list-ul me-2"></i>Fee Breakdown';
        if (breakdownColName) breakdownColName.textContent = 'Fee Name';
        if (studentColDetail) studentColDetail.textContent = 'Fee';

        // Construct URL for GetChartDetails
        url = `${window.chartDetailsUrl}?chartType=${chartType}&segment=${segment}&status=${status}${categoryParam}`;

    } else if (chartType === 'fines') {
        titleText = status === 'paid' ?
            `${program} ${getOrdinal(yearLevel)} Year - Fines Collected` :
            `${program} ${getOrdinal(yearLevel)} Year - Fines Pending`;
        if (breakdownTitle) breakdownTitle.innerHTML = '<i class="bi bi-list-ul me-2"></i>Event/Reason Breakdown';
        if (breakdownColName) breakdownColName.textContent = 'Event/Reason';
        if (studentColDetail) studentColDetail.textContent = 'Event/Reason';

        // Construct URL for GetFinesDetails
        url = `${window.finesDetailsUrl}?segment=${segment}&status=${status}${categoryParam}`;
    }

    if (modalTitle) modalTitle.textContent = titleText;

    if (modalLoading) modalLoading.style.display = 'block';
    if (modalContent) modalContent.style.display = 'none';

    modal.show();

    fetch(url)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                populateFinancialModal(data, chartType);
            } else {
                showFinancialModalError(data.message || 'Failed to load details');
            }
        })
        .catch(error => {
            console.error('Error fetching chart details:', error);
            showFinancialModalError('Failed to load details. Please try again.');
        })
        .finally(() => {
            if (modalLoading) modalLoading.style.display = 'none';
            if (modalContent) modalContent.style.display = 'block';
        });
}

function populateFinancialModal(data, chartType) {
    const summary = data.summary || {};

    // Update summary cards
    document.getElementById('financialTotalAmount').textContent = `₱${formatCurrency(summary.totalAmount || 0)}`;
    document.getElementById('financialStudentCount').textContent = summary.studentCount || 0;

    // Populate breakdown table
    const breakdownBody = document.getElementById('financialBreakdownBody');
    breakdownBody.innerHTML = '';

    const breakdown = chartType === 'fines' ? data.eventBreakdown : data.feeBreakdown;

    if (breakdown && breakdown.length > 0) {
        breakdown.forEach(item => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${chartType === 'fines' ? item.eventName : item.feeName}</td>
                <td>₱${formatCurrency(chartType === 'fines' ? item.fineAmount : item.amount)}</td>
                <td>${item.studentCount}</td>
                <td class="text-gold fw-bold">₱${formatCurrency(item.totalAmount)}</td>
            `;
            breakdownBody.appendChild(row);
        });
    } else {
        breakdownBody.innerHTML = '<tr><td colspan="4" class="text-center text-muted">No data available</td></tr>';
    }

    // Populate student list
    const studentListBody = document.getElementById('financialStudentListBody');
    studentListBody.innerHTML = '';

    if (data.students && data.students.length > 0) {
        data.students.forEach(student => {
            const statusClass = getStatusClass(student.status);
            const row = document.createElement('tr');
            row.innerHTML = `
                <td><code>${student.studentNum}</code></td>
                <td>${student.name}</td>
                <td>${chartType === 'fines' ? student.eventName : student.feeName}</td>
                <td>₱${formatCurrency(student.amount)}</td>
                <td><span class="status-badge ${statusClass}">${student.status}</span></td>
            `;
            studentListBody.appendChild(row);
        });
    } else {
        studentListBody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">No students found</td></tr>';
    }
}

function showFinancialModalError(message) {
    const modalContent = document.getElementById('financialModalContent');
    modalContent.innerHTML = `
        <div class="text-center py-4">
            <div class="text-danger mb-3">
                <i class="bi bi-exclamation-circle" style="font-size: 3rem;"></i>
            </div>
            <p class="text-muted">${message}</p>
        </div>
    `;
}

// Show modal with all students
function showAllStudentsModal() {
    const modalTitle = document.getElementById('enrollmentModalTitle');
    const goToBtn = document.getElementById('enrollmentGoToStudentsBtn');

    if (modalTitle) {
        modalTitle.textContent = 'All Students';
    }

    // Show loading state
    const modalContent = document.getElementById('enrollmentModalContent');
    modalContent.innerHTML = '<div class="text-center py-4"><div class="spinner-border text-gold"></div></div>';

    // Open modal
    const modal = new bootstrap.Modal(document.getElementById('enrollmentDetailsModal'));
    modal.show();

    // Fetch all students data
    fetch(`${window.chartDetailsUrl}?chartType=allStudents`)
        .then(response => response.json())
        .then(data => {
            if (data && data.students) {
                populateEnrollmentModal(data.students);
            } else {
                showEnrollmentModalError('No students found');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showEnrollmentModalError('Failed to load student data');
        });
}

// ==========================================
// HELPER FUNCTIONS
// ==========================================

function getOrdinal(n) {
    const num = parseInt(n);
    if (num === 1) return '1st';
    if (num === 2) return '2nd';
    if (num === 3) return '3rd';
    if (num === 4) return '4th';
    return n;
}

function getStatusClass(status) {
    if (!status) return 'pending';
    const s = status.toLowerCase();
    if (s === 'paid') return 'paid';
    if (s === 'pending' || s === 'unpaid') return 'pending';
    return 'unpaid';
}

// ==========================================
// EXPORT FUNCTIONS
// ==========================================

function exportEnrollmentModalData() {
    alert('Export functionality - This will export enrollment data to Excel.');
}

function exportFinancialModalData() {
    alert('Export functionality - This will export financial data to Excel.');
}

// ==========================================
// SEND NOTICE FUNCTIONS (UPDATED)
// ==========================================

function sendNoticeToEnrollmentStudents() {
    const modalTitle = document.getElementById('enrollmentModalTitle')?.textContent || '';
    openSendNoticeModal(modalTitle);
}

function sendNoticeToFinancialStudents() {
    const modalTitle = document.getElementById('financialModalTitle')?.textContent || '';
    openSendNoticeModal(modalTitle);
}

function openSendNoticeModal(contextTitle) {
    const sendNoticeModal = new bootstrap.Modal(document.getElementById('sendNoticeModal'));
    const subjectInput = document.getElementById('noticeSubject');

    if (subjectInput && contextTitle) {
        subjectInput.value = `Regarding: ${contextTitle}`;
    }

    updateRecipientCount(); // Initial count
    sendNoticeModal.show();
}

$(document).ready(function () {
    // 1. Handle Radio/Select Changes to update Recipient Count
    $('input[name="recipientFilter"], #noticeProgram, #noticeYear').on('change', function () {
        const val = $('input[name="recipientFilter"]:checked').val();

        // Show/Hide filter rows based on selection
        $('#programFilterRow').toggle(val === 'program' || val === 'year');
        $('#yearFilterRow').toggle(val === 'year');

        updateRecipientCount();
    });

    // 2. AJAX Form Submission
    $('#sendNoticeForm').on('submit', function (e) {
        e.preventDefault();

        const btn = $('#sendNoticeBtn');
        const originalText = btn.html();

        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Processing...');

        $.ajax({
            url: window.sendNotificationUrl, // Defined in Index.cshtml
            type: 'POST',
            data: $(this).serialize(),
            success: function (response) {
                if (response.success) {
                    bootstrap.Modal.getInstance(document.getElementById('sendNoticeModal')).hide();
                    alert(response.message); // Replace with a toast if you have one
                    $('#sendNoticeForm')[0].reset();
                } else {
                    alert('Error: ' + response.message);
                }
            },
            error: function () {
                alert('Connection error. Please try again.');
            },
            complete: function () {
                btn.prop('disabled', false).html(originalText);
            }
        });
    });
});

// Helper to update the "X students will receive this" text in the modal
function updateRecipientCount() {
    const filter = $('input[name="recipientFilter"]:checked').val();
    const program = $('#noticeProgram').val();
    const year = $('#noticeYear').val();

    $.get(window.notificationPreviewUrl, { filter, program, year }, function (data) {
        $('#recipientCount').text(data.count);
        $('#recipientSample').text(data.sample);
    });
}

// ==========================================
// QUICK ACTIONS PANEL TOGGLE
// ==========================================

document.addEventListener('DOMContentLoaded', function () {
    const quickActionsContent = document.getElementById('quickActionsContent');
    const quickActionsChevron = document.getElementById('quickActionsChevron');

    if (quickActionsContent) {
        quickActionsContent.addEventListener('shown.bs.collapse', function () {
            if (quickActionsChevron) quickActionsChevron.style.transform = 'rotate(0deg)';
        });

        quickActionsContent.addEventListener('hidden.bs.collapse', function () {
            if (quickActionsChevron) quickActionsChevron.style.transform = 'rotate(-90deg)';
        });
    }

    // Initialize Send Notice Modal
    initSendNoticeModal();
});

// ==========================================
// SEND NOTICE MODAL FUNCTIONALITY
// ==========================================

function initSendNoticeModal() {
    const recipientRadios = document.querySelectorAll('input[name="recipientFilter"]');
    const programFilterRow = document.getElementById('programFilterRow');
    const yearFilterRow = document.getElementById('yearFilterRow');

    recipientRadios.forEach(radio => {
        radio.addEventListener('change', function () {
            const value = this.value;

            if (value === 'program') {
                programFilterRow.style.display = 'block';
                yearFilterRow.style.display = 'none';
            } else if (value === 'year') {
                programFilterRow.style.display = 'block';
                yearFilterRow.style.display = 'block';
            } else {
                programFilterRow.style.display = 'none';
                yearFilterRow.style.display = 'none';
            }

            updateNotificationPreview();
        });
    });

    document.getElementById('noticeProgram')?.addEventListener('change', updateNotificationPreview);
    document.getElementById('noticeYear')?.addEventListener('change', updateNotificationPreview);

    updateNotificationPreview();

    const sendNoticeForm = document.getElementById('sendNoticeForm');
    if (sendNoticeForm) {
        sendNoticeForm.addEventListener('submit', handleSendNotice);
    }
}

function updateNotificationPreview() {
    const recipientFilter = document.querySelector('input[name="recipientFilter"]:checked')?.value || 'all';
    const programFilter = document.getElementById('noticeProgram')?.value;
    const yearFilter = document.getElementById('noticeYear')?.value;

    let url = `${window.notificationPreviewUrl}?recipientFilter=${recipientFilter}`;
    if (recipientFilter === 'program' || recipientFilter === 'year') {
        url += `&programFilter=${programFilter}`;
    }
    if (recipientFilter === 'year') {
        url += `&yearFilter=${yearFilter}`;
    }

    fetch(url)
        .then(response => response.json())
        .then(data => {
            const countEl = document.getElementById('recipientCount');
            const sampleEl = document.getElementById('recipientSample');

            if (countEl) countEl.textContent = data.count || 0;

            if (sampleEl && data.sampleNames) {
                let sampleText = data.sampleNames.join(', ');
                if (data.hasMore) sampleText += `, +${data.count - 3} more`;
                sampleEl.textContent = sampleText;
            }
        })
        .catch(error => {
            console.error('Error fetching preview:', error);
        });
}

async function handleSendNotice(e) {
    e.preventDefault();

    const form = e.target;
    const submitBtn = document.getElementById('sendNoticeBtn');
    const originalBtnText = submitBtn.innerHTML;

    const count = parseInt(document.getElementById('recipientCount')?.textContent) || 0;
    if (count === 0) {
        alert('No students match the selected criteria.');
        return;
    }

    if (!confirm(`Send this notice to ${count} students?`)) {
        return;
    }

    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Sending...';

    try {
        const formData = new FormData(form);

        const response = await fetch(window.sendNotificationUrl, {
            method: 'POST',
            body: formData
        });

        const data = await response.json();

        if (data.success) {
            alert(data.message);
            bootstrap.Modal.getInstance(document.getElementById('sendNoticeModal'))?.hide();
            form.reset();
            updateNotificationPreview();
        } else {
            alert(data.message || 'Failed to send notification');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Failed to send notification. Please try again.');
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalBtnText;
    }
}



