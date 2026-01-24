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
            noDataEl.style.display = 'flex';
            ctx.style.display = 'none';
            return;
        } else {
            noDataEl.style.display = 'none';
            ctx.style.display = 'block';
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
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            color: '#94a3b8',
                            font: { size: 12 }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                return `${context.label}: ₱${context.raw.toFixed(2)}`;
                            }
                        }
                    }
                },
                cutout: '70%'
            }
        });
    };

    createDoughnutChart('paidChart', 'paidNoData', 'paid', 'Paid Fines');
    createDoughnutChart('unpaidChart', 'unpaidNoData', 'unpaid', 'Unpaid Fines');


    // =========================================================
    // TABLE FILTERING
    // =========================================================
    const filterInputs = document.querySelectorAll('#filterSearch, #filterEvent, #filterStatus, #filterProgram, #filterOverdue');
    const finesTable = document.getElementById('finesTable').getElementsByTagName('tbody')[0];

    const applyFilters = () => {
        const searchVal = document.getElementById('filterSearch').value.toLowerCase();
        const eventVal = document.getElementById('filterEvent').value;
        const statusVal = document.getElementById('filterStatus').value.toLowerCase();
        const programVal = document.getElementById('filterProgram').value.toUpperCase();
        const overdueVal = document.getElementById('filterOverdue').value;

        for (let row of finesTable.rows) {
            const student = row.dataset.student || '';
            const studentNum = row.dataset.studentnum || '';
            const eventId = row.dataset.event || '';
            const status = row.dataset.status || '';
            const program = row.dataset.program || '';
            const isOverdue = row.dataset.overdue || '';

            const searchMatch = student.includes(searchVal) || studentNum.includes(searchVal);
            const eventMatch = !eventVal || eventVal === eventId;
            const statusMatch = !statusVal || statusVal === status;
            const programMatch = !programVal || (program && program.includes(programVal));
            const overdueMatch = !overdueVal || overdueVal === isOverdue;

            if (searchMatch && eventMatch && statusMatch && programMatch && overdueMatch) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        }
    };

    filterInputs.forEach(input => {
        input.addEventListener('keyup', applyFilters);
        input.addEventListener('change', applyFilters);
    });

    window.clearFilters = () => {
        document.getElementById('filterSearch').value = '';
        document.getElementById('filterEvent').value = '';
        document.getElementById('filterStatus').value = '';
        document.getElementById('filterProgram').value = '';
        document.getElementById('filterOverdue').value = '';
        applyFilters();
    };


    // =========================================================
    // MODAL TRIGGER FUNCTIONS
    // =========================================================
    const markPaidModal = new bootstrap.Modal(document.getElementById('markPaidModal'));
    const waiveFineModal = new bootstrap.Modal(document.getElementById('waiveFineModal'));
    const adjustFineModal = new bootstrap.Modal(document.getElementById('adjustFineModal'));
    const deleteFineModal = new bootstrap.Modal(document.getElementById('deleteFineModal'));

    window.markAsPaid = (fineId) => {
        document.getElementById('markPaidFineId').value = fineId;
        document.getElementById('markPaidFineIdDisplay').textContent = fineId;
        markPaidModal.show();
    };

    window.waiveFine = (fineId, studentName, amount) => {
        document.getElementById('waiveFineId').value = fineId;
        document.getElementById('waiveStudentName').textContent = studentName;
        document.getElementById('waiveFineAmount').textContent = parseFloat(amount).toFixed(2);
        waiveFineModal.show();
    };

    window.adjustFine = (fineId, studentName, currentAmount) => {
        document.getElementById('adjustFineId').value = fineId;
        document.getElementById('adjustStudentName').textContent = studentName;
        document.getElementById('adjustCurrentAmount').textContent = parseFloat(currentAmount).toFixed(2);
        document.getElementById('newAmount').value = parseFloat(currentAmount).toFixed(2);
        adjustFineModal.show();
    };

    window.deleteFine = (fineId, studentName, amount) => {
        document.getElementById('deleteFineId').value = fineId;
        document.getElementById('deleteStudentName').textContent = studentName;
        document.getElementById('deleteFineAmount').textContent = parseFloat(amount).toFixed(2);
        deleteFineModal.show();
    };
});