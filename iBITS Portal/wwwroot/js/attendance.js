document.addEventListener('DOMContentLoaded', function () {
    console.log("✅ Attendance Oversight Initialized");

    const adjustAttendanceModal = document.getElementById('adjustAttendanceModal');

    if (adjustAttendanceModal) {
        adjustAttendanceModal.addEventListener('show.bs.modal', function (event) {
            // Button that triggered the modal
            const button = event.relatedTarget;

            // Extract info from data-* attributes
            const attendanceId = button.getAttribute('data-attendance-id');
            const studentName = button.getAttribute('data-student-name');
            const currentStatus = button.getAttribute('data-current-status');

            // Update the modal's content
            const modalAttendanceIdInput = document.getElementById('modalAttendanceId');
            const modalStudentNameElement = document.getElementById('modalStudentName');
            const modalStatusSelect = document.getElementById('modalStatus');

            if (modalAttendanceIdInput) modalAttendanceIdInput.value = attendanceId;
            if (modalStudentNameElement) modalStudentNameElement.textContent = studentName;

            // Set the select dropdown to the current status
            if (modalStatusSelect) {
                // Capitalize first letter to match option values (e.g., "present" -> "Present")
                const capitalizedStatus = currentStatus.charAt(0).toUpperCase() + currentStatus.slice(1);
                modalStatusSelect.value = capitalizedStatus;
            }
        });
    }
});