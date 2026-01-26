// ============================================================
// FILE PATH: wwwroot/js/officer-fees.js
// Officer Fees Management - Charts, Filters, and Preview Logic
// ============================================================

document.addEventListener('DOMContentLoaded', function () {

    // =========================================================
    // CHART INITIALIZATION
    // =========================================================
    const createDoughnutChart = (canvasId, noDataId, dataKey, label) => {
        const ctx = document.getElementById(canvasId);
        if (!ctx || !window.feesChartsData) return;

        const chartData = window.feesChartsData[dataKey];
        if (!chartData) return;

        const labels = Object.keys(chartData);
        const values = Object.values(chartData);
        const total = values.reduce((acc, val) => acc + val, 0);

        const noDataEl = document.getElementById(noDataId);

        if (total === 0) {
            if (noDataEl) noDataEl.style.display = 'flex';
            if (ctx) ctx.style.display = 'none';
            return;
        } else {
            if (noDataEl) noDataEl.style.display = 'none';
            if (ctx) ctx.style.display = 'block';
        }

        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: values,
                    backgroundColor: ['#3b82f6', '#10b981', '#ef4444', '#f97316', '#8b5cf6', '#14b8a6', '#ec4899', '#6366f1', '#84cc16', '#f59e0b'],
                    borderColor: 'rgba(255, 255, 255, 0.1)',
                    borderWidth: 2,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            color: '#94a3b8',
                            font: { size: 12 },
                            padding: 15
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: (context) => `${context.label}: ₱${context.raw.toLocaleString('en-US', { minimumFractionDigits: 2 })}`
                        }
                    }
                },
                cutout: '70%'
            }
        });
    };

    // Initialize charts
    if (window.feesChartsData) {
        createDoughnutChart('collectedChart', 'collectedNoData', 'collected', 'Collected Fees');
        createDoughnutChart('pendingChart', 'pendingNoData', 'pending', 'Pending Fees');
    }

    // =========================================================
    // TABLE FILTERING
    // =========================================================
    const filterSearch = document.getElementById('filterSearch');
    const filterFeeName = document.getElementById('filterFeeName');
    const filterProgram = document.getElementById('filterProgram');
    const filterSection = document.getElementById('filterSection');
    const filterStatus = document.getElementById('filterStatus');
    const clearFiltersBtn = document.getElementById('clearFiltersBtn');
    const feesTableBody = document.getElementById('feesTable')?.getElementsByTagName('tbody')[0];

    const applyFilters = () => {
        if (!feesTableBody) return;

        const searchVal = (filterSearch?.value || '').toLowerCase();
        const feeNameVal = (filterFeeName?.value || '').toLowerCase();
        const programVal = (filterProgram?.value || '').toUpperCase();
        const sectionVal = (filterSection?.value || '');
        const statusVal = (filterStatus?.value || '').toLowerCase();

        const rows = feesTableBody.querySelectorAll('tr.fee-row');

        rows.forEach(row => {
            const studentName = row.dataset.studentName || '';
            const studentId = row.dataset.studentId || '';
            const feeName = row.dataset.feeName || '';
            const program = row.dataset.program || '';
            const section = row.dataset.section || '';
            const status = row.dataset.status || '';

            const searchMatch = !searchVal || 
                studentName.includes(searchVal) || 
                studentId.includes(searchVal) ||
                row.dataset.refId?.toString().includes(searchVal);

            const feeNameMatch = !feeNameVal || feeName.includes(feeNameVal);
            const programMatch = !programVal || program.toUpperCase().includes(programVal);
            const sectionMatch = !sectionVal || section === sectionVal;
            
            let statusMatch = true;
            if (statusVal === 'paid') {
                statusMatch = status === 'paid';
            } else if (statusVal === 'unpaid') {
                statusMatch = status !== 'paid';
            }

            row.style.display = (searchMatch && feeNameMatch && programMatch && sectionMatch && statusMatch) ? '' : 'none';
        });

        // Update visible count (optional)
        const visibleRows = feesTableBody.querySelectorAll('tr.fee-row:not([style*="display: none"])');
        console.log(`[Fees Filter] Showing ${visibleRows.length} of ${rows.length} records`);
    };

    // Attach event listeners to filters
    if (filterSearch) filterSearch.addEventListener('input', applyFilters);
    if (filterFeeName) filterFeeName.addEventListener('change', applyFilters);
    if (filterProgram) filterProgram.addEventListener('change', applyFilters);
    if (filterSection) filterSection.addEventListener('change', applyFilters);
    if (filterStatus) filterStatus.addEventListener('change', applyFilters);

    // Clear Filters
    if (clearFiltersBtn) {
        clearFiltersBtn.addEventListener('click', () => {
            if (filterSearch) filterSearch.value = '';
            if (filterFeeName) filterFeeName.value = '';
            if (filterProgram) filterProgram.value = '';
            if (filterSection) filterSection.value = '';
            if (filterStatus) filterStatus.value = '';
            applyFilters();
        });
    }

    // =========================================================
    // CREATE FEE PREVIEW LOGIC (For Org Treasurer)
    // =========================================================
    const createFeeForm = document.getElementById('createFeeForm');
    if (createFeeForm) {
        const programFilters = createFeeForm.querySelectorAll('input[name="programFilter"]');
        const yearFilters = createFeeForm.querySelectorAll('input[name="yearFilter"]');
        const previewTotal = document.getElementById('previewTotal');
        const previewBSIT = document.getElementById('previewBSIT');
        const previewDIT = document.getElementById('previewDIT');
        const previewLoading = document.getElementById('previewLoading');
        const year4Option = document.getElementById('year4Option');

        // Handle DIT program selection (disable 4th year)
        const handleProgramChange = () => {
            const selectedProgram = createFeeForm.querySelector('input[name="programFilter"]:checked')?.value;
            
            if (selectedProgram === 'dit') {
                // Disable 4th year option for DIT
                if (year4Option) {
                    year4Option.style.opacity = '0.5';
                    year4Option.style.pointerEvents = 'none';
                    const year4Radio = document.getElementById('year4');
                    if (year4Radio && year4Radio.checked) {
                        document.getElementById('yearAll').checked = true;
                    }
                }
            } else {
                // Enable 4th year option
                if (year4Option) {
                    year4Option.style.opacity = '1';
                    year4Option.style.pointerEvents = 'auto';
                }
            }
        };

        const updatePreview = () => {
            const selectedProgram = createFeeForm.querySelector('input[name="programFilter"]:checked')?.value || 'all';
            const selectedYear = createFeeForm.querySelector('input[name="yearFilter"]:checked')?.value || 'all';

            if (previewLoading) previewLoading.style.display = 'flex';

            fetch(`/Officer/PreviewStudentCount?programFilter=${selectedProgram}&yearFilter=${selectedYear}`)
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        if (previewTotal) previewTotal.textContent = data.total;
                        if (previewBSIT) previewBSIT.textContent = data.bsit;
                        if (previewDIT) previewDIT.textContent = data.dit;
                    }
                })
                .catch(error => {
                    console.error('Error fetching preview:', error);
                    if (previewTotal) previewTotal.textContent = '?';
                })
                .finally(() => {
                    if (previewLoading) previewLoading.style.display = 'none';
                });
        };

        if (programFilters.length > 0) {
            programFilters.forEach(radio => {
                radio.addEventListener('change', () => {
                    handleProgramChange();
                    updatePreview();
                });
            });
            yearFilters.forEach(radio => radio.addEventListener('change', updatePreview));
            
            // Initial setup
            handleProgramChange();
            updatePreview();
        }
    }

    // =========================================================
    // FORM SUBMISSION HANDLING
    // =========================================================
    const feeForm = document.getElementById('createFeeForm');
    if (feeForm) {
        feeForm.addEventListener('submit', function(e) {
            const submitBtn = document.getElementById('submitFeeBtn');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span> Creating...';
            }
        });
    }

    console.log('[Officer Fees] JavaScript initialized successfully');
});
