/* ============================================================ */
/* FILE PATH: wwwroot/js/student-records.js                     */
/* ============================================================ */
/* UPDATED: Auto-submit filters on dropdown change              */
/* - Removed need for "Apply" button                            */
/* - Filters automatically submit when changed                  */
/* - Debounced search input for better UX                       */
/* ============================================================ */

$(document).ready(function () {

    // ==========================================
    // AUTO-SUBMIT FILTERS ON CHANGE (NEW)
    // ==========================================

    // Debounce timer for search input
    let searchDebounceTimer = null;

    // Auto-submit when filter dropdowns change
    $('.auto-submit-filter').on('change', function () {
        console.log('Filter changed:', $(this).attr('id'), '=', $(this).val());
        // Small delay to ensure Select2 has updated
        setTimeout(() => {
            $('#filterForm').submit();
        }, 100);
    });

    // Auto-submit when sort order changes
    $('#sortOrder').on('change', function () {
        console.log('Sort order changed:', $(this).val());
        setTimeout(() => {
            $('#filterForm').submit();
        }, 100);
    });


    // Also submit on Enter key in search
    $('#searchString').on('keypress', function (e) {
        if (e.which === 13) { // Enter key
            e.preventDefault();
            clearTimeout(searchDebounceTimer);
            $('#filterForm').submit();
        }
    });

    // ==========================================
    // AUTO-APPLY FILTERS FROM DASHBOARD
    // ==========================================
    const urlParams = new URLSearchParams(window.location.search);
    const autoApply = urlParams.get('autoApply');
    const programParam = urlParams.get('program');
    const yearParam = urlParams.get('year');


    if (autoApply === 'true') {
        // Auto-apply filters based on URL parameters

        // Set Program filter
        if (programParam) {
            const programSelect = document.getElementById('programFilter');
            if (programSelect) {
                programSelect.value = programParam;
                $(programSelect).trigger('change');
            }
        }

        // Set Year filter
        if (yearParam) {
            setTimeout(() => {
                const yearSelect = document.getElementById('yearFilter');
                if (yearSelect) {
                    yearSelect.value = yearParam;
                    $(yearSelect).trigger('change');
                }
            }, 100);
        }

        // Auto-trigger the form submit after a short delay
        setTimeout(() => {
            $('#filterForm').submit();
            showToastNotification('success', 'Filters applied automatically from Dashboard');
        }, 300);
    }

    // ==========================================
    // DYNAMIC FILTER VISIBILITY MANAGEMENT
    // ==========================================
    const FILTER_STORAGE_KEY = 'ibits_student_filters';
    const allFilters = ['search', 'sortOrder', 'program', 'year', 'section', 'type', 'role', 'status'];
    const alwaysVisibleFilters = ['search'];
    let visibleFilters = JSON.parse(localStorage.getItem(FILTER_STORAGE_KEY)) || {};

    if (Object.keys(visibleFilters).length === 0) {
        allFilters.forEach(f => visibleFilters[f] = true);
        localStorage.setItem(FILTER_STORAGE_KEY, JSON.stringify(visibleFilters));
    }

    function applyFilterVisibility() {
        allFilters.forEach(filter => {
            const $filterItem = $(`.filter-item[data-filter="${filter}"]`);
            const $checkbox = $(`.filter-toggle[value="${filter}"]`);

            if (alwaysVisibleFilters.includes(filter)) {
                $filterItem.show();
                $checkbox.prop('checked', true).prop('disabled', true);
            } else {
                if (visibleFilters[filter]) {
                    $filterItem.removeClass('filter-hidden').show();
                    $checkbox.prop('checked', true);
                } else {
                    $filterItem.addClass('filter-hidden').hide();
                    $checkbox.prop('checked', false);
                }
            }
        });

        updateActiveFiltersPills();
    }

    $('.filter-toggle').on('change', function () {
        const filterName = $(this).val();

        if (alwaysVisibleFilters.includes(filterName)) {
            $(this).prop('checked', true);
            return;
        }

        const isVisible = $(this).is(':checked');
        visibleFilters[filterName] = isVisible;
        localStorage.setItem(FILTER_STORAGE_KEY, JSON.stringify(visibleFilters));

        const $filterItem = $(`.filter-item[data-filter="${filterName}"]`);
        if (isVisible) {
            $filterItem.removeClass('filter-hidden').fadeIn(200);
        } else {
            $filterItem.addClass('filter-hidden').fadeOut(200);
            clearFilterValue(filterName);
        }

        updateActiveFiltersPills();
    });

    $('#btnResetFiltersInside').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();

        allFilters.forEach(f => visibleFilters[f] = true);
        localStorage.setItem(FILTER_STORAGE_KEY, JSON.stringify(visibleFilters));

        $('.filter-toggle').each(function () {
            if (!alwaysVisibleFilters.includes($(this).val())) {
                $(this).prop('checked', true);
            }
        });

        $('.filter-item').removeClass('filter-hidden').show();
        updateActiveFiltersPills();
    });

    function clearFilterValue(filterName) {
        const filterMap = {
            'search': '#searchString',
            'sortOrder': '#sortOrder',
            'program': '#programFilter',
            'year': '#yearFilter',
            'section': '#sectionFilter',
            'type': '#typeFilter',
            'role': '#roleFilter'
        };

        const $element = $(filterMap[filterName]);
        if ($element.length) {
            if ($element.is('select')) {
                $element.val('').trigger('change');
            } else {
                $element.val('');
            }
            // Auto-submit after clearing
            setTimeout(() => {
                $('#filterForm').submit();
            }, 100);
        }
    }

    function updateActiveFiltersPills() {
        const $container = $('#activeFiltersPills');
        const $list = $('#activeFiltersList');
        $list.empty();

        const filterDisplayNames = {
            'search': 'Search',
            'sortOrder': 'Sort',
            'program': 'Program',
            'year': 'Year',
            'section': 'Section',
            'type': 'Type',
            'role': 'Role'
        };

        const filterSelectors = {
            'search': '#searchString',
            'sortOrder': '#sortOrder',
            'program': '#programFilter',
            'year': '#yearFilter',
            'section': '#sectionFilter',
            'type': '#typeFilter',
            'role': '#roleFilter'
        };

        let hasActiveFilters = false;

        allFilters.forEach(filter => {
            if (!visibleFilters[filter]) return;

            const $element = $(filterSelectors[filter]);
            let value = $element.val();

            if (value && value.trim() !== '') {
                hasActiveFilters = true;

                let displayValue = value;
                if ($element.is('select')) {
                    displayValue = $element.find('option:selected').text();
                }

                const $pill = $(`
                    <span class="filter-pill" data-filter="${filter}">
                        <span class="filter-pill-label">${filterDisplayNames[filter]}:</span>
                        <span class="filter-pill-value">${displayValue}</span>
                        <button type="button" class="filter-pill-remove" onclick="removeFilterPill('${filter}')" title="Remove filter">
                            <i class="bi bi-x"></i>
                        </button>
                    </span>
                `);
                $list.append($pill);
            }
        });

        if (hasActiveFilters) {
            $container.slideDown(200);
        } else {
            $container.slideUp(200);
        }
    }

    applyFilterVisibility();

    // Only listen for changes on 'select' elements (your dropdowns).
    $('#filterForm').on('change', 'select', function () {
        updateActiveFiltersPills();
    });

    $('.filter-settings-dropdown').on('click', (e) => e.stopPropagation());

    // ==========================================
    // SEARCH BAR CLEAR FUNCTIONALITY
    // ==========================================
    const $searchInput = $('#searchString');
    const $clearBtn = $('#btnClearSearch');

    function toggleClearButton() {
        if ($searchInput.val() && $searchInput.val().length > 0) {
            $clearBtn.addClass('show').fadeIn(150);
        } else {
            $clearBtn.removeClass('show').fadeOut(150);
        }
    }

    $searchInput.on('input keyup', toggleClearButton);

    $clearBtn.on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        $searchInput.val('');
        toggleClearButton();
        // Auto-submit after clearing
        $('#filterForm').submit();
        $searchInput.focus();
    });

    toggleClearButton();

    // ==========================================
    // INITIALIZE SELECT2
    // ==========================================
    $('.select2-enable').select2({
        minimumResultsForSearch: Infinity,
        width: '100%'
    });

    $('#createStudentModal').on('shown.bs.modal', function () {
        $('.select2-modal').select2({
            dropdownParent: $('#createStudentModal'),
            minimumResultsForSearch: Infinity,
            width: '100%'
        });
    });

    $('#roleModal').on('shown.bs.modal', function () {
        $('#modalRoleSelect').select2({
            dropdownParent: $('#roleModal'),
            minimumResultsForSearch: Infinity,
            width: '100%'
        });
    });

    // ==========================================
    // MODAL FORM RESET
    // ==========================================
    $('#createStudentModal').on('hidden.bs.modal', function () {
        $(this).find('form').trigger('reset');
        $('.select2-modal').val(null).trigger('change');
    });

    $('#roleModal').on('hidden.bs.modal', function () {
        $('#adminPasswordInput').val('');
    });

    // ==========================================
    // COLUMN VISIBILITY LOGIC
    // ==========================================
    const STORAGE_KEY = 'ibits_student_cols';
    const allColumns = ['col-id', 'col-name', 'col-program', 'col-section', 'col-year', 'col-type', 'col-role'];
    let visibleColumns = JSON.parse(localStorage.getItem(STORAGE_KEY)) || {};

    if (Object.keys(visibleColumns).length === 0 || visibleColumns['col-program'] === undefined) {
        allColumns.forEach(c => visibleColumns[c] = true);
    }

    function applyCols() {
        allColumns.forEach(col => {
            const el = $('.' + col);
            visibleColumns[col] ? el.show() : el.hide();
        });

        if (document.getElementById('chkId')) {
            document.getElementById('chkId').checked = visibleColumns['col-id'];
            document.getElementById('chkName').checked = visibleColumns['col-name'];
            document.getElementById('chkProgram').checked = visibleColumns['col-program'];
            document.getElementById('chkSec').checked = visibleColumns['col-section'];
            document.getElementById('chkYear').checked = visibleColumns['col-year'];
            document.getElementById('chkType').checked = visibleColumns['col-type'];
            document.getElementById('chkRole').checked = visibleColumns['col-role'];
        }
    }
    applyCols();

    $('.col-toggle').on('change', function () {
        const colClass = $(this).val();
        visibleColumns[colClass] = $(this).is(':checked');
        localStorage.setItem(STORAGE_KEY, JSON.stringify(visibleColumns));
        visibleColumns[colClass] ? $('.' + colClass).fadeIn(200) : $('.' + colClass).fadeOut(200);
    });

    $('#btnResetColumnsInside').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        allColumns.forEach(c => visibleColumns[c] = true);
        localStorage.setItem(STORAGE_KEY, JSON.stringify(visibleColumns));
        $('.col-toggle').prop('checked', true);
        allColumns.forEach(col => $('.' + col).show());
    });

    $('.dropdown-menu').on('click', (e) => e.stopPropagation());

    // ==========================================
    // DOUBLE-CLICK FOR STUDENT DETAILS
    // ==========================================
    $('.student-row').dblclick(function () {
        const studentId = $(this).data('id');
        console.log('Double-click detected. Student ID:', studentId);

        const modalContent = $('#detailsModalContent');
        const detailsModal = new bootstrap.Modal(document.getElementById('detailsModal'));

        modalContent.html('<div class="modal-body text-center p-5"><div class="spinner-border text-warning" role="status"></div><p class="mt-2">Loading details...</p></div>');
        detailsModal.show();

        if (window.getStudentDetailsUrl) {
            console.log('Fetching from:', window.getStudentDetailsUrl + '?id=' + studentId);
            $.get(`${window.getStudentDetailsUrl}?id=${studentId}`, function (data) {
                console.log('Data received successfully');
                modalContent.html(data);
            }).fail(function (xhr, status, error) {
                console.error('AJAX Error:', status, error);
                console.error('Response:', xhr.responseText);
                modalContent.html('<div class="modal-body text-center p-5"><i class="bi bi-x-circle-fill text-danger fs-1"></i><p class="mt-2">Failed to load student details.</p><small class="text-muted">' + error + '</small></div>');
            });
        } else {
            console.error('window.getStudentDetailsUrl is not defined!');
            modalContent.html('<div class="modal-body text-center p-5"><i class="bi bi-x-circle-fill text-danger fs-1"></i><p class="mt-2">Configuration error: URL not defined.</p></div>');
        }
    });

    $('#backToMappingBtn').on('click', function () {
        const previewModalEl = document.getElementById('previewModal');
        const previewModalInstance = bootstrap.Modal.getInstance(previewModalEl);

        const mappingModalEl = document.getElementById('mappingModal');
        const mappingModalInstance = bootstrap.Modal.getOrCreateInstance(mappingModalEl);

        if (previewModalInstance) {
            previewModalInstance.hide();
        }

        mappingModalInstance.show();
    });

});

// ==========================================
// GLOBAL HELPER FUNCTIONS
// ==========================================

function resetFilters() {
    $('#searchString').val('');
    $('#sortOrder, #yearFilter, #roleFilter, #programFilter, #sectionFilter, #typeFilter').val('').trigger('change');
    if (window.studentRecordsUrl) {
        window.location.href = window.studentRecordsUrl;
    } else {
        $('#filterForm').submit();
    }
}

function removeFilterPill(filterName) {
    const filterMap = {
        'search': '#searchString',
        'sortOrder': '#sortOrder',
        'program': '#programFilter',
        'year': '#yearFilter',
        'section': '#sectionFilter',
        'type': '#typeFilter',
        'role': '#roleFilter'
    };

    const $element = $(filterMap[filterName]);
    if ($element.length) {
        if ($element.is('select')) {
            $element.val('').trigger('change');
        } else {
            $element.val('');
        }
    }

    updateActiveFiltersPillsGlobal();

    // Auto-submit after removing pill
    setTimeout(() => {
        $('#filterForm').submit();
    }, 100);
}

function clearAllActiveFilters() {
    $('#searchString').val('');
    $('#sortOrder, #programFilter, #yearFilter, #sectionFilter, #typeFilter, #roleFilter').val('').trigger('change');

    if (window.studentRecordsUrl) {
        window.location.href = window.studentRecordsUrl;
    } else {
        $('#filterForm').submit();
    }
}

function updateActiveFiltersPillsGlobal() {
    const $container = $('#activeFiltersPills');
    const $list = $('#activeFiltersList');
    $list.empty();

    const filterDisplayNames = {
        'search': 'Search',
        'sortOrder': 'Sort',
        'program': 'Program',
        'year': 'Year',
        'section': 'Section',
        'type': 'Type',
        'role': 'Role',
    };

    const filterSelectors = {
        'search': '#searchString',
        'sortOrder': '#sortOrder',
        'program': '#programFilter',
        'year': '#yearFilter',
        'section': '#sectionFilter',
        'type': '#typeFilter',
        'role': '#roleFilter',
    };

    const allFilters = ['search', 'sortOrder', 'program', 'year', 'section', 'type', 'role'];
    let hasActiveFilters = false;

    allFilters.forEach(filter => {
        const $element = $(filterSelectors[filter]);
        let value = $element.val();

        if (value && value.trim() !== '') {
            hasActiveFilters = true;

            let displayValue = value;
            if ($element.is('select')) {
                displayValue = $element.find('option:selected').text();
            }

            const $pill = $(`
                <span class="filter-pill" data-filter="${filter}">
                    <span class="filter-pill-label">${filterDisplayNames[filter]}:</span>
                    <span class="filter-pill-value">${displayValue}</span>
                    <button type="button" class="filter-pill-remove" onclick="removeFilterPill('${filter}')" title="Remove filter">
                        <i class="bi bi-x"></i>
                    </button>
                </span>
            `);
            $list.append($pill);
        }
    });

    if (hasActiveFilters) {
        $container.slideDown(200);
    } else {
        $container.slideUp(200);
    }
}

// ==========================================
// REMAINING FUNCTIONS (UNCHANGED)
// ==========================================

function setRoleModal(event, studentId, studentName, currentRole) {
    if (event) event.stopPropagation();

    $('#modalStudentNum').val(studentId);
    $('#modalStudentName').text(studentName);

    if (currentRole) {
        var $select = $('#modalRoleSelect');
        var found = false;
        $select.find('option').each(function () {
            if ($(this).val() === currentRole || $(this).text() === currentRole) {
                $select.val($(this).val());
                found = true;
                return false;
            }
        });

        if ($select.hasClass('select2-hidden-accessible')) {
            $select.trigger('change');
        }
    }

    $('#adminPasswordInput').val('');
    new bootstrap.Modal(document.getElementById('roleModal')).show();
}

function openEditModal(event, id, fn, mn, ln, email, course, section, type, birthday, schoolYearEnrolled) {
    if (event) event.stopPropagation();
    const form = $('#editStudentForm');
    form.find('#originalStudentNum').val(id);
    form.find('[name="StudentNum"]').val(id);
    form.find('[name="StudentFn"]').val(fn);
    form.find('[name="StudentMn"]').val(mn);
    form.find('[name="StudentLn"]').val(ln);
    form.find('[name="StudentEmail"]').val(email);
    form.find('[name="Course"]').val(course);
    form.find('[name="YearLevelSection"]').val(section);
    form.find('[name="Birthday"]').val(birthday);

    const $studentTypeSelect = $('#editStudentType');
    if (type && type.trim() !== '') {
        $studentTypeSelect.val(type).trigger('change');
    } else {
        $studentTypeSelect.val('').trigger('change');
    }

    const $schoolYearSelect = $('#editSchoolYearEnrolled');
    if (schoolYearEnrolled && schoolYearEnrolled.trim() !== '') {
        $schoolYearSelect.val(schoolYearEnrolled).trigger('change');
    } else {
        $schoolYearSelect.val('').trigger('change');
    }

    const editModal = new bootstrap.Modal(document.getElementById('editStudentModal'));
    editModal.show();

    $('#editStudentModal').on('shown.bs.modal', function () {
        $('#editStudentType').select2({
            dropdownParent: $('#editStudentModal'),
            minimumResultsForSearch: Infinity,
            placeholder: '-- Select Type --',
            allowClear: false
        });

        $('#editSchoolYearEnrolled').select2({
            dropdownParent: $('#editStudentModal'),
            minimumResultsForSearch: Infinity,
            placeholder: '-- Select Year --',
            allowClear: false
        });

        if (type && type.trim() !== '') {
            $('#editStudentType').val(type).trigger('change');
        }
        if (schoolYearEnrolled && schoolYearEnrolled.trim() !== '') {
            $('#editSchoolYearEnrolled').val(schoolYearEnrolled).trigger('change');
        }
    });

    $('#editStudentModal').on('hidden.bs.modal', function () {
        if ($('#editStudentType').hasClass('select2-hidden-accessible')) {
            $('#editStudentType').select2('destroy');
        }
        if ($('#editSchoolYearEnrolled').hasClass('select2-hidden-accessible')) {
            $('#editSchoolYearEnrolled').select2('destroy');
        }
        $('#editStudentModal').off('shown.bs.modal');
        $('#editStudentModal').off('hidden.bs.modal');
    });
}

function openResetPasswordModal(event, id, name) {
    if (event) event.stopPropagation();

    $('#resetModalStudentId').val(id);
    $('#resetModalStudentName').text(name);
    $('#resetModalDefaultPassword').text(id);

    new bootstrap.Modal(document.getElementById('resetPasswordModal')).show();
}

function exportToExcelWithColumns() {
    const searchString = $('#searchString').val() || '';
    const programFilter = $('#programFilter').val() || '';
    const yearFilter = $('#yearFilter').val() || '';
    const sectionFilter = $('#sectionFilter').val() || '';
    const typeFilter = $('#typeFilter').val() || '';
    const roleFilter = $('#roleFilter').val() || '';

    const STORAGE_KEY = 'ibits_student_cols';
    const visibleColumns = JSON.parse(localStorage.getItem(STORAGE_KEY)) || {};

    const columnMapping = {
        'col-id': 'Id',
        'col-name': 'Name',
        'col-program': 'Program',
        'col-section': 'Section',
        'col-year': 'Year',
        'col-type': 'Type',
        'col-role': 'Role'
    };

    const columns = [];
    Object.keys(columnMapping).forEach(colKey => {
        if (Object.keys(visibleColumns).length === 0 || visibleColumns[colKey] !== false) {
            columns.push(columnMapping[colKey]);
        }
    });

    const baseUrl = window.exportToExcelUrl || '/Admin/ExportStudentsToExcel';
    const params = new URLSearchParams({
        searchString: searchString,
        programFilter: programFilter,
        yearFilter: yearFilter,
        sectionFilter: sectionFilter,
        typeFilter: typeFilter,
        roleFilter: roleFilter,
        columns: columns.join(',')
    });

    window.location.href = `${baseUrl}?${params.toString()}`;
}

function showToastNotification(type, message) {
    let toastContainer = document.getElementById('toastContainer');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toastContainer';
        toastContainer.style.cssText = 'position: fixed; top: 80px; right: 20px; z-index: 9999;';
        document.body.appendChild(toastContainer);
    }

    const toastId = 'toast_' + Date.now();
    const iconClass = type === 'success' ? 'bi-check-circle-fill' : 'bi-info-circle-fill';
    const bgClass = type === 'success' ? 'bg-success' : 'bg-info';

    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi ${iconClass} me-2"></i>${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;

    toastContainer.insertAdjacentHTML('beforeend', toastHtml);

    const toastEl = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
    toast.show();

    toastEl.addEventListener('hidden.bs.toast', function () {
        this.remove();
    });
}

// ==========================================
// CSV IMPORT FUNCTIONALITY
// ==========================================
let currentFile = null;
let columnMap = {};

window.uploadAndAnalyze = function () {
    const fileInput = document.getElementById('csvFileUpload');
    if (!fileInput.files || fileInput.files.length === 0) {
        alert('Please select a CSV or XLSX file first.');
        return;
    }
    currentFile = fileInput.files[0];

    const formData = new FormData();
    formData.append('file', currentFile);

    $.ajax({
        url: window.analyzeCsvUrl,
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (response) {
            if (response.success) {
                window.currentFileName = response.fileName;
                window.currentFileType = response.fileType;
                showMappingModal(response.headers);
            } else {
                alert('Error: ' + response.message);
            }
        },
        error: function () {
            alert('Failed to upload file. Please try again.');
        }
    });
};

function showMappingModal(headers) {
    const systemFields = ['StudentNum', 'StudentFn', 'StudentLn', 'StudentMn', 'StudentEmail', 'Course', 'Year', 'Section', 'StudentType', 'Birthday'];
    const tableBody = $('#mappingTableBody');
    tableBody.empty();

    systemFields.forEach(field => {
        let options = '<option value="-1">-- Skip this field --</option>';
        headers.forEach((h, i) => {
            options += `<option value="${i}">${h}</option>`;
        });

        const row = `
            <tr>
                <td>${field} <span class="text-danger">${field === 'StudentNum' || field === 'StudentFn' || field === 'StudentLn' ? '*' : ''}</span></td>
                <td>
                    <select class="form-select map-select" data-field="${field}">${options}</select>
                </td>
            </tr>`;
        tableBody.append(row);
    });

    new bootstrap.Modal(document.getElementById('mappingModal')).show();
}

window.generatePreview = function () {
    columnMap = {};
    $('.map-select').each(function () {
        const field = $(this).data('field');
        const index = parseInt($(this).val());
        if (index >= 0) {
            columnMap[field] = index;
        }
    });

    if (!columnMap.hasOwnProperty('StudentNum') || !columnMap.hasOwnProperty('StudentFn') || !columnMap.hasOwnProperty('StudentLn')) {
        alert('Student ID, First Name, and Last Name must be mapped.');
        return;
    }

    $.ajax({
        url: window.previewImportUrl,
        type: 'POST',
        data: { fileName: window.currentFileName, map: columnMap },
        success: function (html) {
            $('#previewContent').html(html);
            bootstrap.Modal.getInstance(document.getElementById('mappingModal')).hide();
            new bootstrap.Modal(document.getElementById('previewModal')).show();
        },
        error: function () {
            alert('Failed to generate preview. Please check file format.');
        }
    });
};
window.confirmUpload = function () {
    // Hide preview modal
    const previewModalEl = document.getElementById('previewModal');
    if (previewModalEl) {
        const inst = bootstrap.Modal.getInstance(previewModalEl);
        if (inst) inst.hide();
    }

    // Show progress modal
    const progressModal = new bootstrap.Modal(document.getElementById('importProgressModal'), { backdrop: 'static', keyboard: false });
    progressModal.show();

    // State
    // Get selected date format from mapping modal dropdown
    const dateFormat = document.getElementById('birthdateFormatSelect')?.value || 'auto';
    let importedCount = 0;
    let skippedCount = 0;
    let allErrors = [];
    let aborted = false;
    let allRows = [];
    let currentIndex = 0;
    let totalRows = 0;

    // UI Elements
    const progressBar = document.getElementById('importProgressBar');
    const progressCounter = document.getElementById('progressCounter');
    const progressPercent = document.getElementById('progressPercent');
    const progressLabel = document.getElementById('progressLabel');
    const progressSuccess = document.getElementById('progressSuccess');
    const progressSkipped = document.getElementById('progressSkipped');
    const progressTotal = document.getElementById('progressTotal');
    const liveErrorContainer = document.getElementById('liveErrorContainer');
    const liveErrorList = document.getElementById('liveErrorList');
    const errorBadge = document.getElementById('errorBadge');
    const currentRowInfo = document.getElementById('currentRowInfo');
    const currentRowText = document.getElementById('currentRowText');

    function updateUI() {
        const pct = totalRows > 0 ? Math.round((currentIndex / totalRows) * 100) : 0;
        progressBar.style.width = pct + '%';
        progressCounter.textContent = currentIndex + ' / ' + totalRows;
        progressPercent.textContent = '(' + pct + '%)';
        progressSuccess.textContent = importedCount;
        progressSkipped.textContent = skippedCount;
        progressTotal.textContent = totalRows;
    }

    function addLiveError(msg) {
        allErrors.push(msg);
        liveErrorContainer.style.display = 'block';
        const li = document.createElement('li');
        li.textContent = msg;
        liveErrorList.appendChild(li);
        errorBadge.textContent = allErrors.length;
        // Auto-scroll to bottom
        liveErrorList.parentElement.scrollTop = liveErrorList.parentElement.scrollHeight;
    }

    function showPauseDialog(rowNum, errorMsg, rowData, onSkip, onAbort) {
        document.getElementById('pauseErrorRowNum').textContent = 'Row #' + rowNum;
        document.getElementById('pauseErrorMessage').textContent = errorMsg;
        document.getElementById('pauseRowData').textContent = JSON.stringify(rowData, null, 2);

        const pauseModal = new bootstrap.Modal(document.getElementById('importErrorPauseModal'), { backdrop: 'static', keyboard: false });
        pauseModal.show();

        document.getElementById('skipRowBtn').onclick = function () {
            pauseModal.hide();
            setTimeout(onSkip, 300);
        };
        document.getElementById('abortImportBtn').onclick = function () {
            pauseModal.hide();
            setTimeout(onAbort, 300);
        };
    }

    function finishImport() {
        // Complete progress bar
        progressBar.style.width = '100%';
        progressBar.classList.remove('progress-bar-animated');
        progressBar.classList.add('bg-success');
        progressCounter.textContent = totalRows + ' / ' + totalRows;
        progressPercent.textContent = '(100%)';
        progressLabel.textContent = aborted ? 'Import aborted!' : 'Import complete!';

        setTimeout(function () {
            progressModal.hide();
            setTimeout(function () { showResultModal(importedCount, skippedCount, totalRows, allErrors, aborted); }, 400);
        }, 1200);
    }

    function processNextRow() {
        if (aborted || currentIndex >= totalRows) {
            finishImport();
            return;
        }

        const row = allRows[currentIndex];
        const rowNum = row._rowNum || (currentIndex + 2);

        // Update current row display
        currentRowInfo.style.display = 'block';
        currentRowText.textContent = 'Row ' + rowNum + ': ' + (row.StudentFn || '') + ' ' + (row.StudentLn || '') + ' (' + (row.StudentNum || 'No ID') + ')';
        progressLabel.textContent = 'Processing row ' + rowNum + '...';
        updateUI();

        $.ajax({
            url: window.importSingleRowUrl,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                rowNum: parseInt(rowNum),
                studentNum: row.StudentNum || '',
                studentLn: row.StudentLn || '',
                studentFn: row.StudentFn || '',
                studentMn: row.StudentMn || '',
                yearLevelSection: row.YearLevelSection || '',
                year: row.Year || '',
                section: row.Section || '',
                course: row.Course || '',
                studentEmail: row.StudentEmail || '',
                studentType: row.StudentType || '',
                birthday: row.Birthday || '',
                dateFormat: dateFormat
            }),
            success: function (response) {
                if (response.success) {
                    importedCount++;
                    currentIndex++;
                    updateUI();
                    // Small delay for UI visibility
                    setTimeout(processNextRow, 50);
                } else {
                    // Error found - pause and show dialog
                    addLiveError(response.error || 'Row ' + rowNum + ': Unknown error.');
                    skippedCount++;
                    currentIndex++;
                    updateUI();

                    showPauseDialog(
                        rowNum,
                        response.error || 'Unknown error',
                        row,
                        function () { // Skip
                            setTimeout(processNextRow, 100);
                        },
                        function () { // Abort
                            aborted = true;
                            finishImport();
                        }
                    );
                }
            },
            error: function (xhr) {
                addLiveError('Row ' + rowNum + ': Server error — ' + (xhr.responseText || 'Unknown'));
                skippedCount++;
                currentIndex++;
                updateUI();

                showPauseDialog(
                    rowNum,
                    'Server error: ' + (xhr.responseText || 'Unknown'),
                    row,
                    function () { setTimeout(processNextRow, 100); },
                    function () { aborted = true; finishImport(); }
                );
            }
        });
    }

    // Step 1: Get all rows first
    progressLabel.textContent = 'Loading file data...';
    $.ajax({
        url: window.getImportRowsUrl,
        type: 'POST',
        data: { fileName: window.currentFileName, map: columnMap, dateFormat: dateFormat },
        success: function (response) {
            if (!response.success) {
                progressModal.hide();
                alert('Error loading file: ' + response.message);
                return;
            }
            allRows = response.rows;
            totalRows = response.total;
            progressTotal.textContent = totalRows;
            progressCounter.textContent = '0 / ' + totalRows;
            progressLabel.textContent = 'Starting import...';

            // Start processing row by row
            setTimeout(processNextRow, 500);
        },
        error: function () {
            progressModal.hide();
            alert('Failed to load import data. Please try again.');
        }
    });
};

// Store errors globally for download
window.lastImportErrors = [];

function showResultModal(imported, skipped, total, errors, aborted) {
    const icon = document.getElementById('importResultIcon');
    const summary = document.getElementById('importResultSummary');
    const detail = document.getElementById('importResultDetail');
    const errorContainer = document.getElementById('importErrorContainer');
    const errorList = document.getElementById('importErrorList');

    icon.className = 'bi';
    errorContainer.style.display = 'none';
    errorList.innerHTML = '';
    window.lastImportErrors = errors || [];

    if (aborted) {
        icon.className = 'bi bi-x-circle-fill text-danger';
        summary.textContent = 'Import Aborted';
        detail.textContent = imported + ' students imported before abort. ' + skipped + ' rows were skipped.';
    } else if (skipped > 0) {
        icon.className = 'bi bi-info-circle-fill text-warning';
        summary.textContent = 'Import Partially Complete';
        detail.textContent = imported + ' imported successfully, ' + skipped + ' rows skipped out of ' + total + ' total.';
    } else {
        icon.className = 'bi bi-check-circle-fill text-success';
        summary.textContent = 'Import Successful! 🎉';
        detail.textContent = 'All ' + imported + ' student records were imported successfully.';
    }

    if (errors && errors.length > 0) {
        errors.forEach(function (err) {
            const li = document.createElement('li');
            li.textContent = err;
            errorList.appendChild(li);
        });
        errorContainer.style.display = 'block';
    }

    new bootstrap.Modal(document.getElementById('importResultModal')).show();
}

function downloadErrorLog() {
    if (!window.lastImportErrors || window.lastImportErrors.length === 0) return;
    const content = window.lastImportErrors.join('\n');
    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'import_error_log_' + new Date().toISOString().slice(0, 10) + '.txt';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

// ==========================================
// MULTI-SELECTION FUNCTIONALITY
// ==========================================
let isSelectionMode = false;

function toggleSelectionMode() {
    isSelectionMode = !isSelectionMode;
    const $checkboxCols = $('.col-checkbox');
    const $btnSelect = $('#btnSelectMode');
    const $bulkBar = $('#bulkActionsBar');
    const $table = $('#studentTable');

    if (isSelectionMode) {
        $checkboxCols.show();
        $btnSelect.addClass('active');
        $btnSelect.html('<i class="bi bi-x-lg me-1"></i> Cancel');
        $table.addClass('selection-mode');
        $('.student-row').off('dblclick');
    } else {
        exitSelectionMode();
    }
}

function exitSelectionMode() {
    isSelectionMode = false;
    const $checkboxCols = $('.col-checkbox');
    const $btnSelect = $('#btnSelectMode');
    const $bulkBar = $('#bulkActionsBar');
    const $table = $('#studentTable');

    $checkboxCols.hide();
    $btnSelect.removeClass('active');
    $btnSelect.html('<i class="bi bi-ui-checks me-1"></i> Select');
    $bulkBar.slideUp(200);
    $table.removeClass('selection-mode');
    $('.row-checkbox').prop('checked', false);
    $('#selectAllCheckbox').prop('checked', false);
    $('.student-row').removeClass('selected');

    $('.student-row').dblclick(function () {
        const studentId = $(this).data('id');
        const modalContent = $('#detailsModalContent');
        const detailsModal = new bootstrap.Modal(document.getElementById('detailsModal'));

        modalContent.html('<div class="modal-body text-center p-5"><div class="spinner-border text-warning" role="status"></div><p class="mt-2">Loading details...</p></div>');
        detailsModal.show();

        if (window.getStudentDetailsUrl) {
            $.get(`${window.getStudentDetailsUrl}?id=${studentId}`, function (data) {
                modalContent.html(data);
            }).fail(function (xhr, status, error) {
                modalContent.html('<div class="modal-body text-center p-5"><i class="bi bi-x-circle-fill text-danger fs-1"></i><p class="mt-2">Failed to load student details.</p></div>');
            });
        }
    });
}

function updateSelectionCount() {
    const selectedCount = $('.row-checkbox:checked').length;
    const totalCount = $('.row-checkbox').length;
    const $bulkBar = $('#bulkActionsBar');

    $('#selectedCountText').text(selectedCount);

    if (selectedCount > 0) {
        $bulkBar.slideDown(200);
    } else {
        $bulkBar.slideUp(200);
    }

    if (selectedCount === totalCount && totalCount > 0) {
        $('#selectAllCheckbox').prop('checked', true).prop('indeterminate', false);
    } else if (selectedCount > 0) {
        $('#selectAllCheckbox').prop('checked', false).prop('indeterminate', true);
    } else {
        $('#selectAllCheckbox').prop('checked', false).prop('indeterminate', false);
    }

    $('.row-checkbox').each(function () {
        if ($(this).is(':checked')) {
            $(this).closest('.student-row').addClass('selected');
        } else {
            $(this).closest('.student-row').removeClass('selected');
        }
    });
}

function selectAllVisible() {
    $('.row-checkbox').prop('checked', true);
    updateSelectionCount();
}

function deselectAll() {
    $('.row-checkbox').prop('checked', false);
    updateSelectionCount();
}

function getSelectedIds() {
    const ids = [];
    $('.row-checkbox:checked').each(function () {
        ids.push($(this).val());
    });
    return ids;
}

function exportSelectedToExcel() {
    const selectedIds = getSelectedIds();
    if (selectedIds.length === 0) {
        alert('Please select at least one student to export.');
        return;
    }

    const STORAGE_KEY = 'ibits_student_cols';
    const visibleColumns = JSON.parse(localStorage.getItem(STORAGE_KEY)) || {};

    const columnMapping = {
        'col-id': 'Id',
        'col-name': 'Name',
        'col-program': 'Program',
        'col-section': 'Section',
        'col-year': 'Year',
        'col-type': 'Type',
        'col-role': 'Role',
        'col-status': 'Status'
    };

    const columns = [];
    Object.keys(columnMapping).forEach(colKey => {
        if (Object.keys(visibleColumns).length === 0 || visibleColumns[colKey] !== false) {
            columns.push(columnMapping[colKey]);
        }
    });

    const baseUrl = window.exportSelectedUrl || '/Admin/ExportSelectedStudents';
    const params = new URLSearchParams({
        studentIds: selectedIds.join(','),
        columns: columns.join(',')
    });

    window.location.href = `${baseUrl}?${params.toString()}`;
}

function archiveSelected() {
    const selectedIds = getSelectedIds();
    if (selectedIds.length === 0) {
        alert('Please select at least one student to archive.');
        return;
    }

    if (confirm(`Are you sure you want to archive ${selectedIds.length} selected student(s)?`)) {
        $('#bulkActionsBar .btn').prop('disabled', true);

        $.ajax({
            url: window.archiveSelectedUrl || '/Admin/ArchiveSelectedStudents',
            type: 'POST',
            data: { studentIds: selectedIds },
            traditional: true,
            success: function (response) {
                if (response.success) {
                    alert(`Successfully archived ${response.count} student(s).`);
                    location.reload();
                } else {
                    alert('Error: ' + (response.message || 'Failed to archive students.'));
                }
            },
            error: function () {
                alert('An error occurred while archiving students.');
            },
            complete: function () {
                $('#bulkActionsBar .btn').prop('disabled', false);
            }
        });
    }
}

function resetPasswordSelectedConfirm() {
    const selectedIds = getSelectedIds();
    if (selectedIds.length === 0) {
        alert('Please select at least one student to reset password.');
        return;
    }

    const confirmHtml = `
        <div class="modal fade" id="resetPasswordConfirmModal" tabindex="-1">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content glass-modal">
                    <div class="modal-header border-0">
                        <h5 class="modal-title" style="color: var(--gold-primary);"><i class="bi bi-key-fill me-2"></i>Reset Password</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body text-center py-4">
                        <i class="bi bi-key" style="font-size: 3rem; color: var(--gold-primary);"></i>
                        <p class="mt-3 mb-0" style="color: var(--text-main);">
                            Are you sure you want to reset the password for 
                            <span class="badge" style="background: var(--gold-primary); color: #000;">${selectedIds.length}</span> selected student(s)?
                        </p>
                        <p class="text-muted small mt-2">
                            <i class="bi bi-info-circle me-1"></i>
                            Passwords will be reset to their respective Student IDs.
                        </p>
                    </div>
                    <div class="modal-footer border-0 justify-content-center">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                            <i class="bi bi-x-lg me-1"></i> Cancel
                        </button>
                        <button type="button" class="btn btn-gold" onclick="executeResetPasswordSelected()">
                            <i class="bi bi-key me-1"></i> Reset ${selectedIds.length} Password(s)
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;

    $('#resetPasswordConfirmModal').remove();
    $('body').append(confirmHtml);

    new bootstrap.Modal(document.getElementById('resetPasswordConfirmModal')).show();
}

function executeResetPasswordSelected() {
    const selectedIds = getSelectedIds();

    bootstrap.Modal.getInstance(document.getElementById('resetPasswordConfirmModal')).hide();

    $('#bulkActionsBar .btn').prop('disabled', true);

    $.ajax({
        url: window.resetPasswordSelectedUrl || '/Admin/ResetPasswordSelected',
        type: 'POST',
        data: { studentIds: selectedIds },
        traditional: true,
        success: function (response) {
            if (response.success) {
                alert(`Successfully reset password for ${response.count} student(s).`);
                exitSelectionMode();
            } else {
                alert('Error: ' + (response.message || 'Failed to reset passwords.'));
            }
        },
        error: function () {
            alert('An error occurred while resetting passwords.');
        },
        complete: function () {
            $('#bulkActionsBar .btn').prop('disabled', false);
        }
    });
}

$(document).ready(function () {
    $('#selectAllCheckbox').on('change', function () {
        const isChecked = $(this).is(':checked');
        $('.row-checkbox').prop('checked', isChecked);
        updateSelectionCount();
    });

    $('.student-row').on('click', function (e) {
        if (isSelectionMode && !$(e.target).is('input, button, a, i')) {
            const $checkbox = $(this).find('.row-checkbox');
            $checkbox.prop('checked', !$checkbox.is(':checked'));
            updateSelectionCount();
        }
    });


    // StudentNum Change: Intercept Submit & Require Admin Password
    $('#editStudentForm').on('submit', function (e) {
        var originalNum = $('#originalStudentNum').val();
        var newNum = $('#editStudentNum').val().trim();

        if (newNum !== originalNum) {
            e.preventDefault();
            $('#adminConfirmPassword').val('');
            $('#adminPasswordError').addClass('d-none');
            var pwModal = new bootstrap.Modal(document.getElementById('confirmStudentNumModal'));
            pwModal.show();
        }
    });

    $('#btnConfirmStudentNumChange').on('click', function () {
        var password = $('#adminConfirmPassword').val();
        if (!password) {
            $('#adminPasswordError').text('Please enter your password.').removeClass('d-none');
            return;
        }

        $('#adminPasswordError').addClass('d-none');
        $('#btnConfirmStudentNumChange').prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>Verifying...');

        $.ajax({
            url: '/Admin/VerifyAdminPassword',
            type: 'POST',
            data: {
                password: password,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').first().val()
            },
            success: function (res) {
                if (res.success) {
                    $('<input>').attr({ type: 'hidden', name: 'AdminPassword', value: password }).appendTo('#editStudentForm');
                    bootstrap.Modal.getInstance(document.getElementById('confirmStudentNumModal')).hide();
                    $('#editStudentForm')[0].submit();
                } else {
                    $('#adminPasswordError').text('Incorrect password. Please try again.').removeClass('d-none');
                    $('#btnConfirmStudentNumChange').prop('disabled', false).html('<i class="fas fa-check me-1"></i>Confirm & Save');
                }
            },
            error: function () {
                $('#adminPasswordError').text('Verification failed. Please try again.').removeClass('d-none');
                $('#btnConfirmStudentNumChange').prop('disabled', false).html('<i class="fas fa-check me-1"></i>Confirm & Save');
            }
        });
    });
});

