/* ======================================================= */
/* FILE PATH: wwwroot/js/archive.js                        */
/* ======================================================= */

$(document).ready(function () {
    // === ELEMENTS ===
    const $search = $('#archiveSearch');
    const $yearFilter = $('#archiveYearFilter');
    const $statusFilter = $('#paymentStatusFilter');
    const $statusContainer = $('#paymentStatusContainer');
    const $btnApply = $('#btnApplyFilter');
    const $btnReset = $('#btnResetFilter');

    // === STATE ===
    let currentTab = 'students'; // students, payments, events

    // === INIT ===
    loadData();

    // === EVENT LISTENERS ===

    // Tab Switching
    $('button[data-bs-toggle="pill"]').on('shown.bs.tab', function (e) {
        const targetId = $(e.target).attr('id');

        if (targetId === 'students-tab') {
            currentTab = 'students';
            $statusContainer.hide();
            $search.attr('placeholder', 'Search ID, Name, Program or Section...');
        } else if (targetId === 'payments-tab') {
            currentTab = 'payments';
            $statusContainer.show();
            $search.attr('placeholder', 'Search Fee Name or Student...');
        } else if (targetId === 'events-tab') {
            currentTab = 'events';
            $statusContainer.hide();
            $search.attr('placeholder', 'Search Event Name...');
        }

        loadData();
    });

    // Filters
    $btnApply.on('click', loadData);

    // Enter key on search
    $search.on('keypress', function (e) {
        if (e.which === 13) loadData();
    });

    $btnReset.on('click', function () {
        $search.val('');
        $yearFilter.prop('selectedIndex', 0);
        $statusFilter.val('all');
        loadData();
    });

    // === DATA LOADING FUNCTIONS ===

    function loadData() {
        const search = $search.val();
        const year = $yearFilter.val();

        if (currentTab === 'students') loadStudents(search, year);
        else if (currentTab === 'payments') loadPayments(search, year);
        else if (currentTab === 'events') loadEvents(search, year);
    }

    function loadStudents(search, year) {
        const $tbody = $('#studentsTableBody');
        $tbody.html('<tr><td colspan="6" class="text-center py-5 text-muted"><div class="spinner-border spinner-border-sm text-gold"></div> Loading...</td></tr>');

        $.get('/Archive/GetArchivedStudents', { searchString: search, archiveYear: year }, function (data) {
            $tbody.empty();
            if (!data || data.length === 0) {
                $tbody.html('<tr><td colspan="6" class="text-center py-5 text-muted">No records found.</td></tr>');
                return;
            }

            data.forEach(s => {
                const date = s.archiveDate ? new Date(s.archiveDate).toLocaleDateString('en-PH', { timeZone: 'Asia/Manila' }) : '-';
                const row = `
                    <tr>
                        <td class="font-monospace text-gold fw-bold">${s.studentNum}</td>
                        <td class="fw-bold text-white">${s.fullName}</td>
                        <td><span class="badge badge-gray">${s.course || '-'}</span> <span class="small text-muted">${s.yearLevelSection || ''}</span></td>
                        <td><span class="badge bg-secondary">${s.classification || s.archiveStatus}</span></td>
                        <td class="text-muted small">${date}</td>
                        <td class="text-end">
                            <button class="btn btn-sm btn-glass unarchive-btn" data-id="${s.studentNum}" title="Restore">
                                <i class="bi bi-arrow-counterclockwise"></i>
                            </button>
                        </td>
                    </tr>`;
                $tbody.append(row);
            });
        });
    }

    function loadPayments(search, year) {
        const $tbody = $('#paymentsTableBody');
        const status = $statusFilter.val();
        $tbody.html('<tr><td colspan="5" class="text-center py-5 text-muted"><div class="spinner-border spinner-border-sm text-gold"></div> Loading...</td></tr>');

        $.get('/Archive/GetArchivedPayments', { searchString: search, archiveYear: year, status: status }, function (data) {
            $tbody.empty();
            if (!data || data.length === 0) {
                $tbody.html('<tr><td colspan="5" class="text-center py-5 text-muted">No records found.</td></tr>');
                return;
            }

            data.forEach(p => {
                const date = p.feesDueDate ? new Date(p.feesDueDate).toLocaleDateString('en-PH', { timeZone: 'Asia/Manila' }) : '-';
                const isPaid = (p.feeStatus && (p.feeStatus.toLowerCase() === 'paid' || p.feeStatus.toLowerCase() === 'completed'));
                const badge = isPaid
                    ? '<span class="badge bg-success bg-opacity-25 text-success border border-success">PAID</span>'
                    : '<span class="badge bg-danger bg-opacity-25 text-danger border border-danger">UNPAID</span>';

                const row = `
                    <tr>
                        <td class="fw-bold">${p.feeName}</td>
                        <td>
                            <div class="text-white fw-bold">${p.studentName}</div>
                            <small class="text-muted font-monospace">${p.studentNum}</small>
                        </td>
                        <td class="text-gold fw-bold">?${(p.amount || 0).toLocaleString('en-PH', { minimumFractionDigits: 2 })}</td>
                        <td class="text-muted small">${date}</td>
                        <td>${badge}</td>
                    </tr>`;
                $tbody.append(row);
            });
        });
    }

    function loadEvents(search, year) {
        const $tbody = $('#eventsTableBody');
        $tbody.html('<tr><td colspan="4" class="text-center py-5 text-muted"><div class="spinner-border spinner-border-sm text-gold"></div> Loading...</td></tr>');

        // Note: Basic event fetch doesn't support text search in Controller yet, filtering by year mainly
        $.get('/Archive/GetArchivedEvents', { archiveYear: year }, function (data) {
            $tbody.empty();
            if (!data || data.length === 0) {
                $tbody.html('<tr><td colspan="4" class="text-center py-5 text-muted">No records found.</td></tr>');
                return;
            }

            data.forEach(e => {
                const date = e.eventDate ? new Date(e.eventDate).toLocaleDateString('en-PH', { timeZone: 'Asia/Manila' }) : '-';
                const row = `
                    <tr>
                        <td class="fw-bold text-white">${e.eventName}</td>
                        <td class="text-muted">${date}</td>
                        <td><i class="bi bi-geo-alt me-1 text-gold"></i>${e.eventLocation}</td>
                        <td class="small fst-italic text-muted">${e.archiveReason}</td>
                    </tr>`;
                $tbody.append(row);
            });
        });
    }

    // Restore Action
    $(document).on('click', '.unarchive-btn', function () {
        const id = $(this).data('id');
        if (confirm('Restore this student to Active status?')) {
            $.post('/Archive/UnarchiveStudent', {
                studentNum: id,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            }, function (res) {
                if (res.success) {
                    loadStudents($search.val(), $yearFilter.val());
                } else {
                    alert('Error: ' + res.message);
                }
            });
        }
    });
});