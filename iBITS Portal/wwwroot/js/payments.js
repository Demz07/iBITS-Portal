/* ============================================================
   FILE PATH: wwwroot/js/payments.js
   ============================================================
   UPDATED: Implemented Fee Name Dropdown Filter & Robust Matching
   ============================================================ */

document.addEventListener('DOMContentLoaded', function () {

    // =========================================================
    // CHART INITIALIZATION (EXACT MATCH TO FINES)
    // =========================================================
    const createDoughnutChart = (canvasId, noDataId, data, label) => {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        const chartData = window.paymentsChartsData[data];
        const labels = Object.keys(chartData);
        const values = Object.values(chartData);
        const total = values.reduce((acc, val) => acc + val, 0);

        const noDataEl = document.getElementById(noDataId);

        // Show "No Data" state if no values
        if (total === 0) {
            if (noDataEl) noDataEl.style.display = 'flex';
            if (ctx) ctx.style.display = 'none';
            return;
        } else {
            if (noDataEl) noDataEl.style.display = 'none';
            if (ctx) ctx.style.display = 'block';
        }

        // Create the chart - SAME colors as Fines for consistency
        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: values,
                    backgroundColor: ['#3b82f6', '#10b981', '#ef4444', '#f97316', '#8b5cf6', '#14b8a6', '#ec4899'],
                    borderColor: 'rgba(255, 255, 255, 0.1)',
                    borderWidth: 2,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom', // Legend at bottom like Fines
                        labels: {
                            color: '#94a3b8',
                            font: { size: 12 }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: (context) => `${context.label}: ₱${context.raw.toFixed(2)}`
                        }
                    }
                },
                cutout: '70%' // Creates donut hole
            }
        });
    };

    // Initialize charts
    if (window.paymentsChartsData) {
        createDoughnutChart('paymentsBreakdownChart', 'paymentsNoData', 'collected', 'Collected Payments');
        createDoughnutChart('pendingBreakdownChart', 'pendingNoData', 'pending', 'Pending Payments');
    }

    // =========================================================
    // REST OF PAYMENTS.JS CODE
    // =========================================================
    console.log("💰 Payments & Fees Manager Initialized");

    // =========================================================
    // 1. FEE PREVIEW (Real-time Student Count)
    // =========================================================
    const programRadios = document.querySelectorAll('input[name="programFilter"]');
    const yearRadios = document.querySelectorAll('input[name="yearFilter"]');
    const previewUrl = '/Admin/PreviewFeeStudentCount';

    function updateFeePreview() {
        const program = document.querySelector('input[name="programFilter"]:checked')?.value || 'all';
        const year = document.querySelector('input[name="yearFilter"]:checked')?.value || 'all';

        // Show loading state
        const loader = document.getElementById('previewLoading');
        if (loader) loader.style.display = 'block';

        document.getElementById('previewTotal').textContent = '...';
        document.getElementById('previewBSIT').textContent = '...';
        document.getElementById('previewDIT').textContent = '...';

        // Fetch student count
        fetch(`${previewUrl}?programFilter=${program}&yearFilter=${year}`)
            .then(response => response.json())
            .then(data => {
                document.getElementById('previewTotal').textContent = data.total || 0;
                document.getElementById('previewBSIT').textContent = data.bsit || 0;
                document.getElementById('previewDIT').textContent = data.dit || 0;
                if (loader) loader.style.display = 'none';
            })
            .catch(error => {
                console.error('Error fetching preview:', error);
                document.getElementById('previewTotal').textContent = '0';
                document.getElementById('previewBSIT').textContent = '0';
                document.getElementById('previewDIT').textContent = '0';
                if (loader) loader.style.display = 'none';
            });
    }

    // Attach event listeners for preview updates
    programRadios.forEach(radio => radio.addEventListener('change', updateFeePreview));
    yearRadios.forEach(radio => radio.addEventListener('change', updateFeePreview));

    // Initial preview load
    if (programRadios.length > 0) updateFeePreview();

    // =========================================================
    // 2. DYNAMIC YEAR LEVEL VISIBILITY (DIT has no 4th year)
    // =========================================================
    programRadios.forEach(radio => {
        radio.addEventListener('change', function () {
            const year4Option = document.getElementById('year4Option');
            const year4Radio = document.getElementById('year4');

            if (year4Option && year4Radio) {
                if (this.value === 'dit') {
                    // Hide 4th year for DIT
                    year4Option.style.opacity = '0.3';
                    year4Option.style.pointerEvents = 'none';
                    year4Radio.disabled = true;

                    // If 4th year was selected, reset to "All Years"
                    if (year4Radio.checked) {
                        document.getElementById('yearAll').checked = true;
                        updateFeePreview();
                    }
                } else {
                    // Show 4th year for BSIT or All Programs
                    year4Option.style.opacity = '1';
                    year4Option.style.pointerEvents = 'auto';
                    year4Radio.disabled = false;
                }
            }
        });
    });

    // =========================================================
    // 3. FILTERS FUNCTIONALITY (Updated with Search)
    // =========================================================
    const filterSearch = document.getElementById('filterSearch'); // NEW
    const filterFeeName = document.getElementById('filterFeeName');
    const filterProgram = document.getElementById('filterProgram');
    const filterYear = document.getElementById('filterYear');
    const filterStatus = document.getElementById('filterStatus');
    const filterAcadYear = document.getElementById('filterAcadYear');
    const clearFiltersBtn = document.getElementById('clearFiltersBtn');
    const feeRows = document.querySelectorAll('.fee-row');
    const totalRecordsEl = document.getElementById('totalRecords');

    function applyFilters() {
        // Get filter values
        const searchValue = filterSearch ? filterSearch.value.toLowerCase().trim() : '';
        const feeNameValue = filterFeeName ? filterFeeName.value.toLowerCase().trim() : '';
        const programValue = filterProgram ? filterProgram.value.toLowerCase().trim() : '';
        const yearValue = filterYear ? filterYear.value.trim() : '';
        const statusValue = filterStatus ? filterStatus.value.toLowerCase().trim() : '';
        const acadYearValue = filterAcadYear ? filterAcadYear.value.toLowerCase().trim() : '';

        let visibleCount = 0;

        feeRows.forEach(row => {
            // Get data attributes
            const rowRefId = row.getAttribute('data-ref-id') || '';
            const rowStudentName = row.getAttribute('data-student-name') || '';
            const rowStudentId = row.getAttribute('data-student-id') || '';

            const rowFeeName = (row.getAttribute('data-fee-name') || '').toLowerCase();
            const rowProgram = (row.getAttribute('data-program') || '').toLowerCase();
            const rowYear = row.getAttribute('data-year') || '';
            const rowStatus = (row.getAttribute('data-status') || '').toLowerCase();
            const rowAcadYear = (row.getAttribute('data-acad-year') || '').toLowerCase();

            // 1. Search Check (Ref ID OR Name OR Student ID)
            const matchesSearch = !searchValue ||
                rowRefId.includes(searchValue) ||
                rowStudentName.includes(searchValue) ||
                rowStudentId.includes(searchValue);

            // 2. Fee Name Check
            const matchesFeeName = !feeNameValue || rowFeeName === feeNameValue;

            // 3. Program Check
            const matchesProgram = !programValue || rowProgram.includes(programValue);

            // 4. Year Level Check
            const matchesYear = !yearValue ||
                rowYear.includes(yearValue) ||
                rowYear.startsWith(`${yearValue}-`) ||
                rowYear.includes(` ${yearValue}-`);

            // 5. Status Check
            let matchesStatus = false;
            if (!statusValue) matchesStatus = true;
            else if (statusValue === 'pending') matchesStatus = rowStatus === 'pending' || rowStatus === 'unpaid' || rowStatus === '';
            else if (statusValue === 'paid') matchesStatus = rowStatus === 'paid' || rowStatus === 'completed';
            else matchesStatus = rowStatus === statusValue;

            // 6. Academic Year Check
            const matchesAcadYear = !acadYearValue || rowAcadYear.includes(acadYearValue);

            // Final Visibility Decision
            if (matchesSearch && matchesFeeName && matchesProgram && matchesYear && matchesStatus && matchesAcadYear) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        });

        // Update UI Counter
        if (totalRecordsEl) {
            totalRecordsEl.textContent = visibleCount;
        }
    }

    // Attach filter event listeners
    if (filterSearch) filterSearch.addEventListener('input', applyFilters); // NEW
    if (filterFeeName) filterFeeName.addEventListener('change', applyFilters);
    if (filterProgram) filterProgram.addEventListener('change', applyFilters);
    if (filterYear) filterYear.addEventListener('change', applyFilters);
    if (filterStatus) filterStatus.addEventListener('change', applyFilters);
    if (filterAcadYear) filterAcadYear.addEventListener('input', applyFilters);

    if (clearFiltersBtn) {
        clearFiltersBtn.addEventListener('click', function () {
            if (filterSearch) filterSearch.value = '';
            if (filterFeeName) filterFeeName.value = '';
            if (filterProgram) filterProgram.value = '';
            if (filterYear) filterYear.value = '';
            if (filterStatus) filterStatus.value = '';
            if (filterAcadYear) filterAcadYear.value = '';
            applyFilters();
        });
    }

    // =========================================================
    // 4. FORM VALIDATION & SUBMIT
    // =========================================================
    const createFeeForm = document.getElementById('createFeeForm');
    const submitFeeBtn = document.getElementById('submitFeeBtn');

    if (createFeeForm) {
        createFeeForm.addEventListener('submit', function (e) {
            const acadYear = document.querySelector('input[name="AcadYear"]').value.trim();
            const feeName = document.querySelector('input[name="FeeName"]').value.trim();
            const amount = document.querySelector('input[name="Amount"]').value;

            if (!acadYear || !feeName || !amount) {
                e.preventDefault();
                alert('⚠️ Please fill in all required fields (Academic Year, Fee Name, Amount).');
                return false;
            }

            // Show loading state on submit button
            if (submitFeeBtn) {
                submitFeeBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Creating...';
                submitFeeBtn.disabled = true;
            }
        });
    }

    // =========================================================
    // 5. MODAL RESET ON CLOSE
    // =========================================================
    const createFeeModal = document.getElementById('createFeeModal');
    if (createFeeModal) {
        createFeeModal.addEventListener('hidden.bs.modal', function () {
            if (createFeeForm) {
                createFeeForm.reset();
                // Reset to default selections
                const progAll = document.getElementById('programAll');
                const yearAll = document.getElementById('yearAll');
                if (progAll) progAll.checked = true;
                if (yearAll) yearAll.checked = true;

                updateFeePreview();

                // Reset submit button
                if (submitFeeBtn) {
                    submitFeeBtn.innerHTML = '<i class="bi bi-check-lg me-1"></i> Create and Assign Fee';
                    submitFeeBtn.disabled = false;
                }
            }
        });
    }

    console.log("✅ Payments.js fully loaded with Dropdown filters!");
});

// =========================================================
// 6. INITIALIZE FINANCIAL PIE CHARTS (Updated: Dynamic Filtering)
// =========================================================
if (window.paymentChartsData) {
    initPaymentCharts(window.paymentChartsData);
}

function initPaymentCharts(data) {
    // 1. Define all possible labels and colors mapped by index
    const rawLabels = ['BSIT 1', 'BSIT 2', 'BSIT 3', 'BSIT 4', 'DIT 1', 'DIT 2', 'DIT 3'];
    const rawColors = [
        '#D4AF37', // BSIT 1 (Gold)
        '#F59E0B', // BSIT 2
        '#EAB308', // BSIT 3
        '#CA8A04', // BSIT 4
        '#3B82F6', // DIT 1 (Blue)
        '#60A5FA', // DIT 2
        '#93C5FD'  // DIT 3
    ];

    // Helper: Filter out zero values to clean up the chart
    function prepareChartData(rawDataObj) {
        // Map raw object keys to the specific order of rawLabels
        const values = [
            rawDataObj["BSIT1"] || 0,
            rawDataObj["BSIT2"] || 0,
            rawDataObj["BSIT3"] || 0,
            rawDataObj["BSIT4"] || 0,
            rawDataObj["DIT1"] || 0,
            rawDataObj["DIT2"] || 0,
            rawDataObj["DIT3"] || 0
        ];

        const filteredLabels = [];
        const filteredData = [];
        const filteredColors = [];

        values.forEach((val, index) => {
            if (val > 0) {
                filteredLabels.push(rawLabels[index]);
                filteredData.push(val);
                filteredColors.push(rawColors[index]);
            }
        });

        return {
            labels: filteredLabels,
            data: filteredData,
            colors: filteredColors,
            total: values.reduce((a, b) => a + b, 0)
        };
    }

    // Helper: Render Chart
    function renderChart(canvasId, noDataId, chartDataObj) {
        const canvas = document.getElementById(canvasId);
        const noDataEl = document.getElementById(noDataId);

        if (!canvas || !noDataEl) return;

        // Use the filtered data
        const { labels, data, colors, total } = prepareChartData(chartDataObj);

        if (total === 0) {
            // No Data: Hide Canvas, Show Big Icon
            canvas.style.display = 'none';
            noDataEl.style.display = 'flex';
        } else {
            // Has Data: Show Canvas
            canvas.style.display = 'block';
            noDataEl.style.display = 'none';

            new Chart(canvas, {
                type: 'doughnut',
                data: {
                    labels: labels, // Only shows relevant years
                    datasets: [{
                        data: data,
                        backgroundColor: colors,
                        borderWidth: 1,
                        borderColor: 'rgba(11, 26, 51, 0.8)'
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'right',
                            labels: {
                                boxWidth: 12,
                                color: '#aaa',
                                font: { size: 11 }
                            }
                        },
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    return ` ${context.label}: ₱${Number(context.raw).toLocaleString()}`;
                                }
                            }
                        }
                    }
                }
            });
        }
    }

    // Render Charts
    renderChart('paymentsBreakdownChart', 'paymentsNoData', data.paid);
    renderChart('pendingBreakdownChart', 'pendingNoData', data.pending);
}