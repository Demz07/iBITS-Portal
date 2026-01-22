/**
 * ============================================================
 * FILE PATH: wwwroot/js/admin-dashboard.js
 * ============================================================
 * iBITS Portal - Admin Dashboard Charts
 * Gold & Navy Blue Theme with Chart.js
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

let studentsPerProgramChart = null;
let activeStudentsLineChart = null;
let activeInactivePieChart = null;
let paymentsPieChart = null;
let pendingPieChart = null;
let finesPieChart = null;
let eventsLineChart = null;
let eventStatusDoughnutChart = null;

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
    initActiveStudentsLineChart(data);
    initActiveInactivePieChart(data);
    initPaymentsPieChart(data);
    initPendingPieChart(data);
    initFinesPieChart(data);
    initEventsLineChart(data);
    initEventStatusDoughnutChart(data);
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

    // Archive count
    animateCounter('archiveCount', data.archiveCount || 0);

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
// 2. ACTIVE STUDENTS (Line Chart)
// ==========================================

function initActiveStudentsLineChart(data) {
    const ctx = document.getElementById('activeStudentsLineChart');
    if (!ctx) return;

    const monthlyData = data.monthlyActiveStudents || new Array(12).fill(0);

    activeStudentsLineChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: months,
            datasets: [{
                label: 'Active Students',
                data: monthlyData,
                borderColor: chartColors.green,
                backgroundColor: chartColors.greenLight,
                borderWidth: 3,
                fill: true,
                tension: 0.4,
                pointBackgroundColor: chartColors.green,
                pointBorderColor: '#fff',
                pointBorderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: 'rgba(11, 26, 51, 0.95)',
                    titleColor: chartColors.gold,
                    bodyColor: chartColors.textStrong,
                    borderColor: chartColors.green,
                    borderWidth: 1,
                    padding: 12
                }
            },
            scales: {
                x: {
                    grid: { color: chartColors.gridColor },
                    ticks: { color: chartColors.textLight }
                },
                y: {
                    beginAtZero: true,
                    grid: { color: chartColors.gridColor },
                    ticks: {
                        color: chartColors.textLight,
                        stepSize: 10
                    }
                }
            }
        }
    });
}

// ==========================================
// 3. ACTIVE VS INACTIVE (Pie Chart)
// ==========================================

function initActiveInactivePieChart(data) {
    const ctx = document.getElementById('activeInactivePieChart');
    if (!ctx) return;

    const status = data.activeInactive || {};
    const activeCount = status.active || status.Active || 0;
    const inactiveCount = status.inactive || status.Inactive || 0;

    activeInactivePieChart = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: ['Active', 'Inactive'],
            datasets: [{
                data: [activeCount, inactiveCount],
                backgroundColor: [chartColors.green, chartColors.red],
                borderColor: ['rgba(34, 197, 94, 0.8)', 'rgba(239, 68, 68, 0.8)'],
                borderWidth: 2,
                hoverOffset: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
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
// 4. PAYMENTS BY PROGRAM (Pie Chart)
// ==========================================

function initPaymentsPieChart(data) {
    const ctx = document.getElementById('paymentsPieChart');
    if (!ctx) return;

    const payments = data.paymentsByProgram || {};

    // Get all payment values
    const paymentData = [
        payments.bsit1 || payments.BSIT1 || 0,
        payments.bsit2 || payments.BSIT2 || 0,
        payments.bsit3 || payments.BSIT3 || 0,
        payments.bsit4 || payments.BSIT4 || 0,
        payments.dit1 || payments.DIT1 || 0,
        payments.dit2 || payments.DIT2 || 0,
        payments.dit3 || payments.DIT3 || 0
    ];

    // Check if all values are 0 (no data)
    const hasData = paymentData.some(val => val > 0);

    if (!hasData) {
        // Show "No Data" message instead of empty chart
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
            onClick: (event, elements) => handleChartClick(event, elements, 'payments', 'paid', paymentsPieChart),
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

// ==========================================
// 5. PENDING PAYMENTS (Pie Chart)
// ==========================================

function initPendingPieChart(data) {
    const ctx = document.getElementById('pendingPieChart');
    if (!ctx) return;

    const pending = data.pendingByProgram || {};

    // Get all pending values
    const pendingData = [
        pending.bsit1 || pending.BSIT1 || 0,
        pending.bsit2 || pending.BSIT2 || 0,
        pending.bsit3 || pending.BSIT3 || 0,
        pending.bsit4 || pending.BSIT4 || 0,
        pending.dit1 || pending.DIT1 || 0,
        pending.dit2 || pending.DIT2 || 0,
        pending.dit3 || pending.DIT3 || 0
    ];

    // Check if all values are 0 (no data)
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
            onClick: (event, elements) => handleChartClick(event, elements, 'payments', 'pending', pendingPieChart),
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

// ==========================================
// 6. FINES BY PROGRAM (Pie Chart)
// ==========================================

function initFinesPieChart(data) {
    const ctx = document.getElementById('finesPieChart');
    if (!ctx) return;

    const fines = data.finesByProgram || {};

    // Get all fine values
    const fineData = [
        fines.bsit1 || fines.BSIT1 || 0,
        fines.bsit2 || fines.BSIT2 || 0,
        fines.bsit3 || fines.BSIT3 || 0,
        fines.bsit4 || fines.BSIT4 || 0,
        fines.dit1 || fines.DIT1 || 0,
        fines.dit2 || fines.DIT2 || 0,
        fines.dit3 || fines.DIT3 || 0
    ];

    // Check if all values are 0 (no data)
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
            onClick: (event, elements) => handleChartClick(event, elements, 'fines', null, finesPieChart),
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
// 6. EVENTS LINE CHART
// ==========================================

function initEventsLineChart(data) {
    const ctx = document.getElementById('eventsLineChart');
    if (!ctx) return;

    const monthlyData = data.monthlyEvents || new Array(12).fill(0);

    eventsLineChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: months,
            datasets: [{
                label: 'Events',
                data: monthlyData,
                borderColor: chartColors.cyan,
                backgroundColor: chartColors.cyanLight,
                borderWidth: 3,
                fill: true,
                tension: 0.4,
                pointBackgroundColor: chartColors.cyan,
                pointBorderColor: '#fff',
                pointBorderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: 'rgba(11, 26, 51, 0.95)',
                    titleColor: chartColors.gold,
                    bodyColor: chartColors.textStrong,
                    borderColor: chartColors.cyan,
                    borderWidth: 1,
                    padding: 12
                }
            },
            scales: {
                x: {
                    grid: { color: chartColors.gridColor },
                    ticks: { color: chartColors.textLight }
                },
                y: {
                    beginAtZero: true,
                    grid: { color: chartColors.gridColor },
                    ticks: {
                        color: chartColors.textLight,
                        stepSize: 1
                    }
                }
            }
        }
    });
}

// ==========================================
// 7. EVENT STATUS (Doughnut Chart)
// ==========================================

function initEventStatusDoughnutChart(data) {
    const ctx = document.getElementById('eventStatusDoughnutChart');
    if (!ctx) return;

    const status = data.eventStatus || {};
    const completed = status.completed || status.Completed || 0;
    const upcoming = status.upcoming || status.Upcoming || 0;

    eventStatusDoughnutChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Completed', 'Upcoming'],
            datasets: [{
                data: [completed, upcoming],
                backgroundColor: [chartColors.gold, chartColors.cyan],
                borderColor: ['rgba(212, 175, 55, 0.8)', 'rgba(6, 182, 212, 0.8)'],
                borderWidth: 2,
                hoverOffset: 10
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '65%',
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
// REFRESH DASHBOARD
// ==========================================

function refreshDashboard() {
    const $btn = $('#btnRefresh');
    $btn.find('i').addClass('spin-animation');
    $btn.prop('disabled', true);

    // If refresh URL is available, fetch new data
    if (window.dashboardRefreshUrl) {
        $.ajax({
            url: window.dashboardRefreshUrl,
            type: 'GET',
            success: function (data) {
                window.dashboardData = data;

                // Update counters
                updateCounters(data);

                // Update all charts
                updateStudentsPerProgramChart(data);
                updateActiveStudentsLineChart(data);
                updateActiveInactivePieChart(data);
                updatePaymentsPieChart(data);
                updateFinesPieChart(data);
                updateEventsLineChart(data);
                updateEventStatusDoughnutChart(data);
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
        // Just re-initialize with existing data
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

function updateActiveStudentsLineChart(data) {
    if (!activeStudentsLineChart) return;
    activeStudentsLineChart.data.datasets[0].data = data.monthlyActiveStudents || new Array(12).fill(0);
    activeStudentsLineChart.update();
}

function updateActiveInactivePieChart(data) {
    if (!activeInactivePieChart) return;
    const status = data.activeInactive || {};
    activeInactivePieChart.data.datasets[0].data = [
        status.active || status.Active || 0,
        status.inactive || status.Inactive || 0
    ];
    activeInactivePieChart.update();
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

function updateEventsLineChart(data) {
    if (!eventsLineChart) return;
    eventsLineChart.data.datasets[0].data = data.monthlyEvents || new Array(12).fill(0);
    eventsLineChart.update();
}

function updateEventStatusDoughnutChart(data) {
    if (!eventStatusDoughnutChart) return;
    const status = data.eventStatus || {};
    eventStatusDoughnutChart.data.datasets[0].data = [
        status.completed || status.Completed || 0,
        status.upcoming || status.Upcoming || 0
    ];
    eventStatusDoughnutChart.update();
}

// ==========================================
// CHART CLICK HANDLER
// ==========================================

function handleChartClick(event, elements, chartType, status, chart) {
    if (!elements || elements.length === 0) return;

    const index = elements[0].index;
    const label = chart.data.labels[index]; // e.g., "BSIT3"
    const value = chart.data.datasets[0].data[index];

    // Don't open modal for zero values
    if (value === 0) return;

    // Open the details modal
    showChartDetailsModal(chartType, label, status);
}

// ==========================================
// SHOW CHART DETAILS MODAL
// ==========================================

function showChartDetailsModal(chartType, segment, status) {
    const modalEl = document.getElementById('chartDetailsModal');
    if (!modalEl) {
        console.error('chartDetailsModal not found');
        return;
    }

    const modal = new bootstrap.Modal(modalEl);
    const modalTitle = document.getElementById('modalTitle');
    const modalLoading = document.getElementById('modalLoading');
    const modalContent = document.getElementById('modalContent');
    const breakdownTitle = document.getElementById('breakdownTitle');
    const breakdownColName = document.getElementById('breakdownColName');
    const studentColDetail = document.getElementById('studentColDetail');
    const goToPaymentsBtn = document.getElementById('goToPaymentsBtn');

    // Set modal title based on chart type and status
    const yearLevel = segment.replace('BSIT', '').replace('DIT', '');
    const program = segment.includes('BSIT') ? 'BSIT' : 'DIT';

    let titleText = '';
    if (chartType === 'payments') {
        titleText = status === 'paid'
            ? `${program} ${getOrdinal(yearLevel)} Year - Payments Collected`
            : `${program} ${getOrdinal(yearLevel)} Year - Payments Pending`;
        if (breakdownTitle) breakdownTitle.innerHTML = '<i class="bi bi-list-ul me-2"></i>Fee Breakdown';
        if (breakdownColName) breakdownColName.textContent = 'Fee Name';
        if (studentColDetail) studentColDetail.textContent = 'Fee';
        if (goToPaymentsBtn) goToPaymentsBtn.style.display = 'inline-block';
    } else if (chartType === 'fines') {
        titleText = `${program} ${getOrdinal(yearLevel)} Year - Fines Issued`;
        if (breakdownTitle) breakdownTitle.innerHTML = '<i class="bi bi-list-ul me-2"></i>Event Breakdown';
        if (breakdownColName) breakdownColName.textContent = 'Event Name';
        if (studentColDetail) studentColDetail.textContent = 'Event';
        if (goToPaymentsBtn) goToPaymentsBtn.style.display = 'inline-block';
    }

    if (modalTitle) modalTitle.textContent = titleText;

    // Show loading state
    if (modalLoading) modalLoading.style.display = 'block';
    if (modalContent) modalContent.style.display = 'none';

    // Show modal
    modal.show();

    // Fetch data based on chart type
    const url = chartType === 'fines'
        ? `${window.finesDetailsUrl}?segment=${segment}`
        : `${window.chartDetailsUrl}?chartType=${chartType}&segment=${segment}&status=${status}`;

    fetch(url)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                populateModalContent(data, chartType);
            } else {
                showModalError(data.message || 'Failed to load details');
            }
        })
        .catch(error => {
            console.error('Error fetching chart details:', error);
            showModalError('Failed to load details. Please try again.');
        })
        .finally(() => {
            if (modalLoading) modalLoading.style.display = 'none';
            if (modalContent) modalContent.style.display = 'block';
        });
}

// ==========================================
// POPULATE MODAL CONTENT
// ==========================================

function populateModalContent(data, chartType) {
    const summary = data.summary || {};

    // Update summary cards
    document.getElementById('detailTotalAmount').textContent = `₱${formatCurrency(summary.totalAmount || 0)}`;
    document.getElementById('detailStudentCount').textContent = summary.studentCount || 0;
    document.getElementById('detailAverage').textContent = `₱${formatCurrency(summary.averagePerStudent || 0)}`;

    // Populate breakdown table
    const breakdownBody = document.getElementById('breakdownBody');
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
    const studentListBody = document.getElementById('studentListBody');
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

// ==========================================
// SHOW MODAL ERROR
// ==========================================

function showModalError(message) {
    const modalContent = document.getElementById('modalContent');
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
// HELPER: Get Ordinal Suffix
// ==========================================

function getOrdinal(n) {
    const num = parseInt(n);
    if (num === 1) return '1st';
    if (num === 2) return '2nd';
    if (num === 3) return '3rd';
    if (num === 4) return '4th';
    return n;
}

// ==========================================
// HELPER: Get Status Class
// ==========================================

function getStatusClass(status) {
    if (!status) return 'pending';
    const s = status.toLowerCase();
    if (s === 'paid') return 'paid';
    if (s === 'pending' || s === 'unpaid') return 'pending';
    return 'unpaid';
}

// ==========================================
// YEAR LEVEL COUNTER CLICK MODAL
// ==========================================

function showYearLevelModal(program, yearLevel) {
    const modal = new bootstrap.Modal(document.getElementById('chartDetailsModal'));
    const modalTitle = document.getElementById('modalTitle');
    const modalLoading = document.getElementById('modalLoading');
    const modalContent = document.getElementById('modalContent');

    // Set modal title
    modalTitle.textContent = `${program} ${getOrdinal(yearLevel)} Year Students`;

    // Show correct navigation button
    showNavigationButton('students', `?program=${program}&year=${yearLevel}`);

    // Show loading state
    modalLoading.style.display = 'block';
    modalContent.style.display = 'none';

    modal.show();

    fetch(`${window.studentsByYearLevelUrl}?program=${program}&yearLevel=${yearLevel}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                populateStudentYearLevelModal(data);
            } else {
                showModalError(data.message || 'Failed to load details');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showModalError('Failed to load details');
        })
        .finally(() => {
            modalLoading.style.display = 'none';
            modalContent.style.display = 'block';
        });
}

function populateStudentYearLevelModal(data) {
    const summary = data.summary || {};

    // Update summary cards
    const totalAmountEl = document.getElementById('detailTotalAmount');
    const studentCountEl = document.getElementById('detailStudentCount');
    const averageEl = document.getElementById('detailAverage');

    if (totalAmountEl) {
        totalAmountEl.textContent = summary.totalStudents || 0;
        const label1 = document.querySelector('.details-summary .summary-item:first-child .summary-label');
        if (label1) label1.textContent = 'Total Students';
    }

    if (studentCountEl) {
        studentCountEl.textContent = summary.activeStudents || 0;
        const label2 = document.querySelector('.details-summary .summary-item:nth-child(2) .summary-label');
        if (label2) label2.textContent = 'Active Students';
    }

    if (averageEl) {
        // Show Total Pending (Fees + Fines)
        const totalPending = (summary.totalPendingFees || 0) + (summary.totalPendingFines || 0);
        averageEl.textContent = `₱${formatCurrency(totalPending)}`;
        const label3 = document.querySelector('.details-summary .summary-item:nth-child(3) .summary-label');
        if (label3) label3.textContent = 'Total Balance Due';
    }

    // Populate student list table
    const breakdownBody = document.getElementById('breakdownBody');
    const breakdownTitle = document.getElementById('breakdownTitle');

    if (!breakdownBody) {
        console.error('breakdownBody element not found');
        return;
    }

    if (breakdownTitle) {
        breakdownTitle.innerHTML = '<i class="bi bi-people me-2"></i>Student List';
    }

    breakdownBody.innerHTML = '';

    // Update table headers to include Pending Fines
    const headerRow = breakdownBody.closest('table')?.querySelector('thead tr');
    if (headerRow) {
        headerRow.innerHTML = `
            <th>Student ID</th>
            <th>Name</th>
            <th>Section</th>
            <th>Pending Fees</th>
            <th>Pending Fines</th>
        `;
    }

    if (data.students && data.students.length > 0) {
        data.students.forEach(student => {
            const row = document.createElement('tr');

            const feeClass = student.pendingFees > 0 ? 'text-warning' : 'text-success';
            const fineClass = student.pendingFines > 0 ? 'text-danger' : 'text-success';

            row.innerHTML = `
                <td><code>${student.studentNum}</code></td>
                <td>${student.name}</td>
                <td>${student.section}</td>
                <td class="${feeClass}">₱${formatCurrency(student.pendingFees)}</td>
                <td class="${fineClass}">₱${formatCurrency(student.pendingFines)}</td>
            `;
            breakdownBody.appendChild(row);
        });
    } else {
        const headerRowCheck = breakdownBody.closest('table')?.querySelector('thead tr');
        const colspan = headerRowCheck ? headerRowCheck.querySelectorAll('th').length : 5;
        breakdownBody.innerHTML = `<tr><td colspan="${colspan}" class="text-center text-muted">No students found</td></tr>`;
    }

    // Hide the other collapsible section meant for charts
    const studentListSection = document.getElementById('studentListCollapse')?.closest('.details-section');
    if (studentListSection) {
        studentListSection.style.display = 'none';
    }
}

// ==========================================
// ARCHIVED STUDENTS MODAL
// ==========================================

function showArchivedStudentsModal() {
    const modal = new bootstrap.Modal(document.getElementById('chartDetailsModal'));
    const modalTitle = document.getElementById('modalTitle');
    const modalLoading = document.getElementById('modalLoading');
    const modalContent = document.getElementById('modalContent');

    modalTitle.textContent = 'Archived Students';
    showNavigationButton('students', '?status=archived');

    modalLoading.style.display = 'block';
    modalContent.style.display = 'none';

    modal.show();

    fetch(window.archivedStudentsUrl)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                populateArchivedStudentsModal(data);
            } else {
                showModalError(data.message || 'Failed to load details');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showModalError('Failed to load details');
        })
        .finally(() => {
            modalLoading.style.display = 'none';
            modalContent.style.display = 'block';
        });
}

function populateArchivedStudentsModal(data) {
    const summary = data.summary || {};

    // Update summary cards with null checks
    const totalAmountEl = document.getElementById('detailTotalAmount');
    const studentCountEl = document.getElementById('detailStudentCount');
    const averageEl = document.getElementById('detailAverage');

    if (totalAmountEl) {
        totalAmountEl.textContent = summary.totalArchived || 0;
        const label1 = document.querySelector('.details-summary .summary-item:first-child .summary-label');
        if (label1) label1.textContent = 'Total Archived';
    }

    if (studentCountEl) {
        studentCountEl.textContent = summary.bsitCount || 0;
        const label2 = document.querySelector('.details-summary .summary-item:nth-child(2) .summary-label');
        if (label2) label2.textContent = 'BSIT';
    }

    if (averageEl) {
        averageEl.textContent = summary.ditCount || 0;
        const label3 = document.querySelector('.details-summary .summary-item:nth-child(3) .summary-label');
        if (label3) label3.textContent = 'DIT';
    }

    const breakdownBody = document.getElementById('breakdownBody');
    const breakdownTitle = document.getElementById('breakdownTitle');

    if (!breakdownBody) {
        console.error('breakdownBody element not found');
        return;
    }

    if (breakdownTitle) {
        breakdownTitle.innerHTML = '<i class="bi bi-archive me-2"></i>Archived Student List';
    }

    breakdownBody.innerHTML = '';

    const headerRow = breakdownBody.closest('table')?.querySelector('thead tr');
    if (headerRow) {
        headerRow.innerHTML = `
            <th>Student ID</th>
            <th>Name</th>
            <th>Program</th>
            <th>Status</th>
        `;
    }

    if (data.students && data.students.length > 0) {
        data.students.forEach(student => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td><code>${student.studentNum}</code></td>
                <td>${student.name}</td>
                <td>${student.program}</td>
                <td><span class="status-badge unpaid">${student.status}</span></td>
            `;
            breakdownBody.appendChild(row);
        });
    } else {
        const headerRowCheck = breakdownBody.closest('table')?.querySelector('thead tr');
        const colspan = headerRowCheck ? headerRowCheck.querySelectorAll('th').length : 4;
        breakdownBody.innerHTML = `<tr><td colspan="${colspan}" class="text-center text-muted">No archived students</td></tr>`;
    }

    const studentListSection = document.getElementById('studentListCollapse')?.closest('.details-section');
    if (studentListSection) {
        studentListSection.style.display = 'none';
    }
}

// ==========================================
// SHOW NAVIGATION BUTTON HELPER
// ==========================================

function showNavigationButton(type, queryParams = '') {
    const paymentsBtn = document.getElementById('goToPaymentsBtn');
    const studentsBtn = document.getElementById('goToStudentsBtn');
    const eventsBtn = document.getElementById('goToEventsBtn');

    paymentsBtn.style.display = 'none';
    studentsBtn.style.display = 'none';
    eventsBtn.style.display = 'none';

    if (type === 'payments') {
        paymentsBtn.style.display = 'inline-block';
        paymentsBtn.href = window.studentRecordsUrl?.replace('StudentRecords', 'Payments') + queryParams;
    } else if (type === 'students') {
        studentsBtn.style.display = 'inline-block';

        // ⭐ NEW: Add autoApply parameter if query params exist
        if (queryParams && queryParams !== '') {
            const separator = queryParams.includes('?') ? '&' : '?';
            queryParams += separator + 'autoApply=true';
        }

        studentsBtn.href = window.studentRecordsUrl + queryParams;
    } else if (type === 'events') {
        eventsBtn.style.display = 'inline-block';
        eventsBtn.href = window.eventsUrl + queryParams;
    }
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

            // Show/hide filter rows
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

            // Update preview
            updateNotificationPreview();
        });
    });

    // Program and Year filter changes
    document.getElementById('noticeProgram')?.addEventListener('change', updateNotificationPreview);
    document.getElementById('noticeYear')?.addEventListener('change', updateNotificationPreview);

    // Initial preview load
    updateNotificationPreview();

    // Form submission
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

    // Validate
    const count = parseInt(document.getElementById('recipientCount')?.textContent) || 0;
    if (count === 0) {
        alert('No students match the selected criteria.');
        return;
    }

    // Confirm
    if (!confirm(`Send this notice to ${count} students?`)) {
        return;
    }

    // Show loading
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

// ==========================================
// SEND NOTICE TO MODAL STUDENTS
// ==========================================

function sendNoticeToModalStudents() {
    // Pre-populate the send notice modal based on current modal context
    const modalTitle = document.getElementById('modalTitle')?.textContent || '';

    // Open send notice modal
    const sendNoticeModal = new bootstrap.Modal(document.getElementById('sendNoticeModal'));

    // Pre-fill subject based on context
    const subjectInput = document.getElementById('noticeSubject');
    if (subjectInput && modalTitle) {
        subjectInput.value = `Regarding: ${modalTitle}`;
    }

    sendNoticeModal.show();
}

// ==========================================
// EXPORT MODAL DATA (Placeholder)
// ==========================================

function exportModalData() {
    // Get current modal data and export to Excel
    alert('Export functionality - This will export the currently displayed data to Excel.');
    // In a full implementation, this would collect the table data and trigger a download
}
