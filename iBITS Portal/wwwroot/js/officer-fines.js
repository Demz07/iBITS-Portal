// ============================================================
// FILE PATH: wwwroot/js/officer-fines.js
// Officer Fines Management - Charts, Filters, and Preview Logic
// ============================================================

document.addEventListener('DOMContentLoaded', function () {

    // =========================================================
    // CHART INITIALIZATION
    // =========================================================
    let paidChartInstance = null;
    let unpaidChartInstance = null;

    const createDoughnutChart = (canvasId, noDataId, dataKey, label) => {
        const ctx = document.getElementById(canvasId);
        if (!ctx || !window.finesChartsData) return null;

        const chartData = window.finesChartsData[dataKey];
        if (!chartData) return null;

        const labels = Object.keys(chartData);
        const values = Object.values(chartData);
        const total = values.reduce((acc, val) => acc + val, 0);

        const noDataEl = document.getElementById(noDataId);

        if (total === 0) {
            if (noDataEl) noDataEl.style.display = 'flex';
            if (ctx) ctx.style.display = 'none';
            return null;
        } else {
            if (noDataEl) noDataEl.style.display = 'none';
            if (ctx) ctx.style.display = 'block';
        }

        return new Chart(ctx, {
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

    const updateChart = (canvasId, noDataId, data, label) => {
        const ctx = document.getElementById(canvasId);
        const noDataEl = document.getElementById(noDataId);
        if (!ctx) return;

        const labels = Object.keys(data);
        const values = Object.values(data);
        const total = values.reduce((acc, val) => acc + val, 0);

        if (total === 0) {
            if (noDataEl) noDataEl.style.display = 'flex';
            if (ctx) ctx.style.display = 'none';
            // Destroy existing chart
            if (canvasId === 'paidChart' && paidChartInstance) {
                paidChartInstance.destroy();
                paidChartInstance = null;
            }
            if (canvasId === 'unpaidChart' && unpaidChartInstance) {
                unpaidChartInstance.destroy();
                unpaidChartInstance = null;
            }
            return;
        } else {
            if (noDataEl) noDataEl.style.display = 'none';
            if (ctx) ctx.style.display = 'block';
        }

        // Update or create chart
        if (canvasId === 'paidChart') {
            if (paidChartInstance) {
                paidChartInstance.data.labels = labels;
                paidChartInstance.data.datasets[0].data = values;
                paidChartInstance.update();
            } else {
                paidChartInstance = new Chart(ctx, {
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
            }
        } else if (canvasId === 'unpaidChart') {
            if (unpaidChartInstance) {
                unpaidChartInstance.data.labels = labels;
                unpaidChartInstance.data.datasets[0].data = values;
                unpaidChartInstance.update();
            } else {
                unpaidChartInstance = new Chart(ctx, {
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
            }
        }
    };

    // Initialize charts
    if (window.finesChartsData) {
        paidChartInstance = createDoughnutChart('paidChart', 'paidNoData', 'paid', 'Paid Fines');
        unpaidChartInstance = createDoughnutChart('unpaidChart', 'unpaidNoData', 'unpaid', 'Unpaid Fines');
    }

    // =========================================================
    // TABLE FILTERING
    // =========================================================
    const filterSearch = document.getElementById('filterSearch');
    const filterFineType = document.getElementById('filterFineType');
    const filterStatus = document.getElementById('filterStatus');
    const filterProgram = document.getElementById('filterProgram');
    const filterYear = document.getElementById('filterYear');
    const filterEvent = document.getElementById('filterEvent');
    const filterDescription = document.getElementById('filterDescription');
    const clearFiltersBtn = document.getElementById('clearFiltersBtn');
    const finesTableBody = document.getElementById('finesTable')?.getElementsByTagName('tbody')[0];
    
    // Conditional filter containers
    const conditionalFiltersRow = document.getElementById('conditionalFiltersRow');
    const filterEventContainer = document.getElementById('filterEventContainer');
    const filterDescriptionContainer = document.getElementById('filterDescriptionContainer');

    const applyFilters = () => {
        if (!finesTableBody) return;

        const searchVal = (filterSearch?.value || '').toLowerCase();
        const fineTypeVal = (filterFineType?.value || '').toLowerCase();
        const statusVal = (filterStatus?.value || '').toLowerCase();
        const programVal = (filterProgram?.value || '').toUpperCase();
        const yearVal = (filterYear?.value || '');
        const eventVal = (filterEvent?.value || '').toLowerCase();
        const descriptionVal = (filterDescription?.value || '').toLowerCase();

        const rows = finesTableBody.querySelectorAll('tr.fine-row');

        let totalExpected = 0;
        let totalPaid = 0;
        let totalUnpaid = 0;
        const paidByProgramYear = {};
        const unpaidByProgramYear = {};

        rows.forEach(row => {
            const student = row.dataset.student || '';
            const studentNum = row.dataset.studentnum || '';
            const fineType = row.dataset.finetype || '';
            const event = row.dataset.event || '';
            const description = row.dataset.description || '';
            const section = row.dataset.section || '';
            const status = row.dataset.status || '';
            const program = row.dataset.program || '';
            const year = row.dataset.year || '';
            const amount = parseFloat(row.dataset.amount) || 0;
            const paid = parseFloat(row.dataset.paid) || 0;
            const balance = parseFloat(row.dataset.balance) || 0;

            const searchMatch = !searchVal || 
                student.includes(searchVal) || 
                studentNum.includes(searchVal);

            const fineTypeMatch = !fineTypeVal || fineType === fineTypeVal;
            
            // Conditional filtering based on fine type
            const eventMatch = !eventVal || event.includes(eventVal);
            const descriptionMatch = !descriptionVal || description.includes(descriptionVal);
            
            // Handle excused/waived interchangeably
            let statusMatch = !statusVal;
            if (statusVal === 'excused') {
                statusMatch = status === 'excused' || status === 'waived';
            } else if (statusVal) {
                statusMatch = status === statusVal;
            }
            const programMatch = !programVal || program.toUpperCase().includes(programVal);
            const yearMatch = !yearVal || year === yearVal;

            const isVisible = searchMatch && fineTypeMatch && statusMatch && programMatch && yearMatch && eventMatch && descriptionMatch;
            row.style.display = isVisible ? '' : 'none';

            // Calculate totals for visible rows
            if (isVisible) {
                totalExpected += amount;
                totalPaid += paid;
                totalUnpaid += balance;

                // Create Program + Year label (e.g., "BSIT Year 3", "DIT Year 2")
                let programYearLabel = 'Unknown';
                if (program && year) {
                    programYearLabel = `${program} Year ${year}`;
                } else if (program) {
                    programYearLabel = program;
                } else if (year) {
                    programYearLabel = `Year ${year}`;
                }

                if (status === 'paid') {
                    paidByProgramYear[programYearLabel] = (paidByProgramYear[programYearLabel] || 0) + amount;
                } else {
                    unpaidByProgramYear[programYearLabel] = (unpaidByProgramYear[programYearLabel] || 0) + amount;
                }
            }
        });

        // Update summary cards
        const totalExpectedEl = document.getElementById('totalExpected');
        const totalCollectedEl = document.getElementById('totalCollected');
        const totalPendingEl = document.getElementById('totalPending');
        
        if (totalExpectedEl) totalExpectedEl.textContent = totalExpected.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        if (totalCollectedEl) totalCollectedEl.textContent = totalPaid.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        if (totalPendingEl) totalPendingEl.textContent = totalUnpaid.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

        // Update charts
        updateChart('paidChart', 'paidNoData', paidByProgramYear, 'Paid Fines');
        updateChart('unpaidChart', 'unpaidNoData', unpaidByProgramYear, 'Unpaid Fines');

        // Update visible count
        const visibleRows = finesTableBody.querySelectorAll('tr.fine-row:not([style*="display: none"])');
        console.log(`[Fines Filter] Showing ${visibleRows.length} of ${rows.length} records`);
    };

    // Handle Fine Type Change to show/hide conditional filters
    if (filterFineType) {
        filterFineType.addEventListener('change', function() {
            const selectedType = this.value;
            
            if (selectedType === 'event') {
                // Show event filter, hide description filter
                if (conditionalFiltersRow) conditionalFiltersRow.style.display = '';
                if (filterEventContainer) filterEventContainer.style.display = 'block';
                if (filterDescriptionContainer) filterDescriptionContainer.style.display = 'none';
                if (filterDescription) filterDescription.value = '';
                
                // Populate event dropdown from table data
                populateEventFilter();
            } else if (selectedType === 'manual') {
                // Show description filter, hide event filter
                if (conditionalFiltersRow) conditionalFiltersRow.style.display = '';
                if (filterEventContainer) filterEventContainer.style.display = 'none';
                if (filterDescriptionContainer) filterDescriptionContainer.style.display = 'block';
                if (filterEvent) filterEvent.value = '';
                
                // Populate description dropdown from table data
                populateDescriptionFilter();
            } else {
                // Hide all conditional filters
                if (conditionalFiltersRow) conditionalFiltersRow.style.display = 'none';
                if (filterEventContainer) filterEventContainer.style.display = 'none';
                if (filterDescriptionContainer) filterDescriptionContainer.style.display = 'none';
                if (filterEvent) filterEvent.value = '';
                if (filterDescription) filterDescription.value = '';
            }
            
            applyFilters();
        });
    }

    // Populate Event Filter Dropdown
    function populateEventFilter() {
        if (!filterEvent || !finesTableBody) return;
        
        const events = new Set();
        const rows = finesTableBody.querySelectorAll('tr.fine-row');
        
        rows.forEach(row => {
            const fineType = row.dataset.finetype || '';
            const event = row.dataset.event || '';
            
            if (fineType === 'event' && event) {
                events.add(event);
            }
        });
        
        // Clear and repopulate
        filterEvent.innerHTML = '<option value="">All Events</option>';
        Array.from(events).sort().forEach(event => {
            const option = document.createElement('option');
            option.value = event;
            option.textContent = event.charAt(0).toUpperCase() + event.slice(1);
            filterEvent.appendChild(option);
        });
    }

    // Populate Description Filter Dropdown
    function populateDescriptionFilter() {
        if (!filterDescription || !finesTableBody) return;
        
        const descriptions = new Set();
        const rows = finesTableBody.querySelectorAll('tr.fine-row');
        
        rows.forEach(row => {
            const fineType = row.dataset.finetype || '';
            const description = row.dataset.description || '';
            
            if (fineType === 'manual' && description) {
                descriptions.add(description);
            }
        });
        
        // Clear and repopulate
        filterDescription.innerHTML = '<option value="">All Descriptions</option>';
        Array.from(descriptions).sort().forEach(desc => {
            const option = document.createElement('option');
            option.value = desc;
            option.textContent = desc.charAt(0).toUpperCase() + desc.slice(1);
            filterDescription.appendChild(option);
        });
    }

    // Attach event listeners to filters
    if (filterSearch) filterSearch.addEventListener('input', applyFilters);
    if (filterStatus) filterStatus.addEventListener('change', applyFilters);
    if (filterProgram) filterProgram.addEventListener('change', applyFilters);
    if (filterYear) filterYear.addEventListener('change', applyFilters);
    if (filterEvent) filterEvent.addEventListener('change', applyFilters);
    if (filterDescription) filterDescription.addEventListener('change', applyFilters);

    // Clear Filters
    if (clearFiltersBtn) {
        clearFiltersBtn.addEventListener('click', () => {
            if (filterSearch) filterSearch.value = '';
            if (filterFineType) filterFineType.value = '';
            if (filterStatus) filterStatus.value = '';
            if (filterProgram) filterProgram.value = '';
            if (filterYear) filterYear.value = '';
            if (filterEvent) filterEvent.value = '';
            if (filterDescription) filterDescription.value = '';
            
            // Hide conditional filters
            if (conditionalFiltersRow) conditionalFiltersRow.style.display = 'none';
            if (filterEventContainer) filterEventContainer.style.display = 'none';
            if (filterDescriptionContainer) filterDescriptionContainer.style.display = 'none';
            
            applyFilters();
        });
    }

    // Initialize charts on page load by running applyFilters with no filters active
    applyFilters();

    // =========================================================
    // CREATE FINE PREVIEW LOGIC (For Org Treasurer)
    // =========================================================
    const createFineForm = document.getElementById('createFineForm');
    if (createFineForm) {
        const programFilters = createFineForm.querySelectorAll('input[name="programFilter"]');
        const yearFilters = createFineForm.querySelectorAll('input[name="yearFilter"]');
        const previewTotal = document.getElementById('previewTotal');
        const previewBSIT = document.getElementById('previewBSIT');
        const previewDIT = document.getElementById('previewDIT');
        const previewLoading = document.getElementById('previewLoading');
        const year4Option = document.getElementById('year4Option');

        // Handle DIT program selection (disable 4th year)
        const handleProgramChange = () => {
            const selectedProgram = createFineForm.querySelector('input[name="programFilter"]:checked')?.value;
            
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
            const selectedProgram = createFineForm.querySelector('input[name="programFilter"]:checked')?.value || 'all';
            const selectedYear = createFineForm.querySelector('input[name="yearFilter"]:checked')?.value || 'all';

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
    const fineForm = document.getElementById('createFineForm');
    if (fineForm) {
        fineForm.addEventListener('submit', function(e) {
            const submitBtn = document.getElementById('submitFineBtn');
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span> Creating...';
            }
        });
    }

    console.log('[Officer Fines] JavaScript initialized successfully');
});


// ============================================================
// MARK FINE AS PAID & REVOKE PAYMENT FUNCTIONS
// ============================================================

let selectedFineId = null;

// Show Mark as Paid Modal
function showMarkFinePaidModal(fineId, studentName, description, amount) {
    selectedFineId = fineId;
    document.getElementById('markPaidStudentName').textContent = studentName;
    document.getElementById('markPaidDescription').textContent = description;
    document.getElementById('markPaidAmount').textContent = parseFloat(amount).toFixed(2);
    
    const modal = new bootstrap.Modal(document.getElementById('markFinePaidModal'));
    modal.show();
}

// Confirm Mark Fine as Paid
async function confirmMarkFinePaid() {
    if (!selectedFineId) return;
    
    const btn = document.getElementById('confirmMarkPaidBtn');
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Processing...';
    
    try {
        const response = await fetch(`/Officer/MarkFineAsPaid/${selectedFineId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            }
        });
        
        const result = await response.json();
        
        if (result.success) {
            bootstrap.Modal.getInstance(document.getElementById('markFinePaidModal')).hide();
            showToast('success', result.message || 'Fine marked as paid successfully');
            setTimeout(() => location.reload(), 1500);
        } else {
            showToast('error', result.message || 'Failed to mark fine as paid');
        }
    } catch (error) {
        console.error('Error:', error);
        showToast('error', 'An error occurred while processing the payment');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-check-circle me-1"></i> Confirm Payment';
    }
}

// Show Revoke Payment Modal
function showRevokeFinePaymentModal(fineId, studentName, description, amount) {
    selectedFineId = fineId;
    document.getElementById('revokeStudentName').textContent = studentName;
    document.getElementById('revokeDescription').textContent = description;
    document.getElementById('revokeAmount').textContent = parseFloat(amount).toFixed(2);
    
    const modal = new bootstrap.Modal(document.getElementById('revokeFinePaymentModal'));
    modal.show();
}

// Confirm Revoke Fine Payment
async function confirmRevokePayment() {
    if (!selectedFineId) return;
    
    const btn = document.getElementById('confirmRevokeBtn');
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Processing...';
    
    try {
        const response = await fetch(`/Officer/RevokeFinePayment/${selectedFineId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            }
        });
        
        const result = await response.json();
        
        if (result.success) {
            bootstrap.Modal.getInstance(document.getElementById('revokeFinePaymentModal')).hide();
            showToast('success', result.message || 'Fine payment revoked successfully');
            setTimeout(() => location.reload(), 1500);
        } else {
            showToast('error', result.message || 'Failed to revoke fine payment');
        }
    } catch (error) {
        console.error('Error:', error);
        showToast('error', 'An error occurred while revoking the payment');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-x-circle me-1"></i> Revoke Payment';
    }
}