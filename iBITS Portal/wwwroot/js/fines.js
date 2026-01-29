document.addEventListener('DOMContentLoaded', function () {

    // =========================================================
    // CHART INITIALIZATION
    // =========================================================
    const createDoughnutChart = (canvasId, noDataId, data, label) => {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        const chartData = window.finesChartsData[data];
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
                    backgroundColor: ['#3b82f6', '#10b981', '#ef4444', '#f97316', '#8b5cf6', '#14b8a6', '#ec4899'],
                    borderColor: 'rgba(255, 255, 255, 0.1)',
                    borderWidth: 2,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom', labels: { color: '#94a3b8', font: { size: 12 } } }, tooltip: { callbacks: { label: (c) => `${c.label}: ₱${c.raw.toFixed(2)}` } } },
                cutout: '70%'
            }
        });
    };

    if (window.finesChartsData) {
        createDoughnutChart('paidChart', 'paidNoData', 'paid', 'Paid Fines');
        createDoughnutChart('unpaidChart', 'unpaidNoData', 'unpaid', 'Unpaid Fines');
    }

    // =========================================================
    // CLIENT-SIDE FILTERING LOGIC (FIXED)
    // =========================================================
    const finesFilterForm = document.getElementById('finesFilterForm');
    if (!finesFilterForm) return;

    // IMPORTANT: Prevent the form from submitting and reloading the page
    finesFilterForm.addEventListener('submit', e => e.preventDefault());

    const finesTableRows = document.querySelectorAll("#finesTable tbody tr");
    const filterFineType = document.getElementById('filterFineType');
    const eventFilterContainer = document.getElementById('eventFilterContainer');
    const reasonFilterContainer = document.getElementById('reasonFilterContainer');
    const filterEvent = document.getElementById('filterEvent');
    const filterReason = document.getElementById('filterReason');

    // Function to show/hide Event or Reason dropdowns
    const toggleDynamicFilters = () => {
        if (!filterFineType) return;
        const selectedType = filterFineType.value;

        eventFilterContainer.style.display = selectedType === 'event' ? 'block' : 'none';
        reasonFilterContainer.style.display = selectedType === 'manual' ? 'block' : 'none';

        if (selectedType !== 'event' && filterEvent) filterEvent.value = '';
        if (selectedType !== 'manual' && filterReason) filterReason.value = '';
    };

    // Main function to apply all filters to the table
    const applyTableFilters = () => {
        const searchVal = document.getElementById('filterSearch').value.toLowerCase();
        const fineTypeVal = document.getElementById('filterFineType').value;
        const eventVal = document.getElementById('filterEvent').value;
        const reasonVal = document.getElementById('filterReason').value;
        const statusVal = document.getElementById('filterStatus').value.toLowerCase() || '';
        const programVal = document.getElementById('filterProgram').value.toLowerCase();
        const yearLevelVal = document.getElementById('filterYearLevel').value;

        finesTableRows.forEach(row => {
            const rowData = row.dataset;
            let isVisible = true;

            const student = rowData.student || '';
            const studentNum = rowData.studentnum || '';

            if (searchVal && !(student.includes(searchVal) || studentNum.includes(searchVal)))
                isVisible = false;

            if (isVisible && fineTypeVal && rowData.finetype !== fineTypeVal)
                isVisible = false;

            if (
                isVisible &&
                fineTypeVal === 'event' &&
                eventVal &&
                Number(rowData.event) !== Number(eventVal)
            )
                isVisible = false;

            if (
                isVisible &&
                fineTypeVal === 'manual' &&
                reasonVal &&
                !rowData.reason.includes(reasonVal.toLowerCase())
            )
                isVisible = false;

            if (isVisible && statusVal && rowData.status && rowData.status !== statusVal)
                isVisible = false;

            if (isVisible && programVal && rowData.program.toLowerCase() !== programVal)
                isVisible = false;

            if (isVisible && yearLevelVal && rowData.yearlevel !== yearLevelVal)
                isVisible = false;

            row.style.display = isVisible ? '' : 'none';
        });
    };

    // Set initial state of dynamic filters on page load
    toggleDynamicFilters();

    // Attach event listeners to all filter controls
    const allFilterInputs = finesFilterForm.querySelectorAll('input, select');
    allFilterInputs.forEach(input => {
        const eventType = input.tagName === 'INPUT' && input.type === 'text' ? 'input' : 'change';
        input.addEventListener(eventType, (e) => {
            e.preventDefault(); // Prevent any default behavior

            if (input.id === 'filterFineType') {
                toggleDynamicFilters();
            }

            if (eventType === 'input') {
                clearTimeout(input.searchTimeout);
                input.searchTimeout = setTimeout(applyTableFilters, 300);
            } else {
                applyTableFilters();
            }
        });
    });

    // Add functionality to the "Clear Filters" button
    const clearFinesFilterBtn = document.getElementById('clearFinesFilterBtn');
    if (clearFinesFilterBtn) {
        clearFinesFilterBtn.addEventListener('click', (e) => {
            e.preventDefault();
            finesFilterForm.reset();
            toggleDynamicFilters();
            applyTableFilters();
        });
    }

    // =========================================================
    // CREATE FINE PREVIEW LOGIC
    // =========================================================
    const createFineForm = document.getElementById('createFineForm');
    if (createFineForm) {
        const programFilters = createFineForm.querySelectorAll('input[name="programFilter"]');
        const yearFilters = createFineForm.querySelectorAll('input[name="yearFilter"]');
        const previewTotal = document.getElementById('previewTotal');
        const previewBSIT = document.getElementById('previewBSIT');
        const previewDIT = document.getElementById('previewDIT');
        const previewLoading = document.getElementById('previewLoading');

        const updatePreview = () => {
            const selectedProgram = createFineForm.querySelector('input[name="programFilter"]:checked').value;
            const selectedYear = createFineForm.querySelector('input[name="yearFilter"]:checked').value;
            previewLoading.style.display = 'flex';

            fetch(`/Admin/PreviewFineStudentCount?programFilter=${selectedProgram}&yearFilter=${selectedYear}`)
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        previewTotal.textContent = data.total;
                        previewBSIT.textContent = data.bsit;
                        previewDIT.textContent = data.dit;
                    }
                })
                .catch(error => console.error('Error fetching preview:', error))
                .finally(() => {
                    previewLoading.style.display = 'none';
                });
        };

        if (programFilters.length > 0) {
            programFilters.forEach(radio => radio.addEventListener('change', updatePreview));
            yearFilters.forEach(radio => radio.addEventListener('change', updatePreview));
            updatePreview(); // Initial check
        }
    }
});

