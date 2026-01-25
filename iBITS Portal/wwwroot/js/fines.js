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
    // DYNAMIC & TABLE FILTERING
    // =========================================================
    const filterFineType = document.getElementById('filterFineType');
    const eventFilterContainer = document.getElementById('eventFilterContainer');
    const reasonFilterContainer = document.getElementById('reasonFilterContainer');
    const filterEvent = document.getElementById('filterEvent');
    const filterReason = document.getElementById('filterReason');
    const finesTableBody = document.getElementById('finesTable')?.getElementsByTagName('tbody')[0];

    const toggleDynamicFilters = () => {
        if (!filterFineType) return;
        const selectedType = filterFineType.value;

        eventFilterContainer.style.display = (selectedType === 'event') ? 'block' : 'none';
        reasonFilterContainer.style.display = (selectedType === 'manual') ? 'block' : 'none';

        if (selectedType !== 'event') filterEvent.value = '';
        if (selectedType !== 'manual') filterReason.value = '';
    };

    const applyClientSideFilters = () => {
        if (!finesTableBody) return;

        const searchVal = document.getElementById('filterSearch').value.toLowerCase();
        const statusVal = document.getElementById('filterStatus').value;
        const programVal = document.getElementById('filterProgram').value;
        const fineTypeVal = filterFineType.value;
        const eventVal = filterEvent.value;
        const reasonVal = filterReason.value;

        for (let row of finesTableBody.rows) {
            const student = row.dataset.student || '';
            const studentNum = row.dataset.studentnum || '';
            const status = row.dataset.status || '';
            const program = row.dataset.program || '';
            const fineType = row.dataset.finetype || '';
            const eventId = row.dataset.event || '';
            const reason = row.dataset.reason || '';

            const searchMatch = student.includes(searchVal) || studentNum.includes(searchVal);
            const statusMatch = !statusVal || statusVal === status;
            const programMatch = !programVal || program === programVal;
            const fineTypeMatch = !fineTypeVal || fineTypeVal === fineType;
            const eventMatch = !eventVal || eventVal === eventId;
            const reasonMatch = !reasonVal || reasonVal === reason;

            row.style.display = (searchMatch && statusMatch && programMatch && fineTypeMatch && eventMatch && reasonMatch) ? '' : 'none';
        }
    };

    if (filterFineType) {
        filterFineType.addEventListener('change', () => {
            toggleDynamicFilters();
            // Do not submit form, just apply client-side filter
            applyClientSideFilters();
        });
        toggleDynamicFilters(); // Run on page load
    }

    // Attach live client-side filtering to all inputs
    const allFilterInputs = document.querySelectorAll('#filterSearch, #filterFineType, #filterEvent, #filterReason, #filterStatus, #filterProgram');
    allFilterInputs.forEach(input => {
        const eventType = input.tagName === 'INPUT' ? 'input' : 'change';
        input.addEventListener(eventType, applyClientSideFilters);
    });

    // Note: The "Clear Filters" button is now an <a> tag that reloads the page, so no JS is needed for it.
    // The "Apply Filters" button submits the form to the server.

    // =========================================================
    // MODAL TRIGGER FUNCTIONS
    // =========================================================
    function openMarkPaidModal(id) {
        document.getElementById('markPaidFineId').value = id;
        document.getElementById('markPaidFineIdDisplay').innerText = id;
        new bootstrap.Modal(document.getElementById('markPaidModal')).show();
    }

    function openWaiveModal(id, name, amount) {
        document.getElementById('waiveFineId').value = id;
        document.getElementById('waiveStudentName').innerText = name;
        document.getElementById('waiveFineAmount').innerText = parseFloat(amount).toFixed(2);
        new bootstrap.Modal(document.getElementById('waiveFineModal')).show();
    }

    function openAdjustModal(id, name, amount) {
        document.getElementById('adjustFineId').value = id;
        document.getElementById('adjustStudentName').innerText = name;
        document.getElementById('adjustCurrentAmount').innerText = parseFloat(amount).toFixed(2);
        document.getElementById('newAmount').value = parseFloat(amount).toFixed(2);
        new bootstrap.Modal(document.getElementById('adjustFineModal')).show();
    }

    function openDeleteModal(id, name, amount) {
        document.getElementById('deleteFineId').value = id;
        document.getElementById('deleteStudentName').innerText = name;
        document.getElementById('deleteFineAmount').innerText = parseFloat(amount).toFixed(2);
        new bootstrap.Modal(document.getElementById('deleteFineModal')).show();
    }

    // Make modal functions globally accessible
    window.openMarkPaidModal = openMarkPaidModal;
    window.openWaiveModal = openWaiveModal;
    window.openAdjustModal = openAdjustModal;
    window.openDeleteModal = openDeleteModal;


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