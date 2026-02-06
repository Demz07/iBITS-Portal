// ============================================================
// iBITS Portal AdminController - SEMESTER-AWARE VERSION
// Updated for semester system with time-in/time-out tracking
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using iBITS_Portal.Models;
using iBITS_Portal.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.IO;
using iBITS_Portal.Helpers;

namespace iBITS_Portal.Controllers
{
    [Authorize(Roles = "Admin")]
    public partial class AdminController : Controller
    {

        // ============================================================
        // SEMESTER MANAGEMENT
        // ============================================================

        public async Task<IActionResult> Semesters()
        {
            var semesters = await _context.Semesters
                .Include(s => s.AcademicYear)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            ViewBag.AcademicYears = await _context.AcademicYears
                .OrderByDescending(ay => ay.StartDate)
                .ToListAsync();

            return View(semesters);
        }

        [HttpPost]
        public async Task<IActionResult> SetCurrentSemester(int semesterId)
        {
            var semester = await _context.Semesters.FindAsync(semesterId);
            if (semester == null)
            {
                return Json(new { success = false, message = "Semester not found" });
            }

            // Remove current flag from all semesters
            var allSemesters = await _context.Semesters.ToListAsync();
            allSemesters.ForEach(s => s.IsCurrent = false);
            
            // Set current flag
            semester.IsCurrent = true;
            await _context.SaveChangesAsync();

            await LogAction("Set Current Semester", $"Set current semester to: {semester.SemesterName}");

            return Json(new { success = true, currentSemester = semester.SemesterName });
        }

        // ============================================================
        // STUDENT MANAGEMENT WITH SEMESTER FILTERING
        // ============================================================

        public async Task<IActionResult> Students(string? semesterFilter = null, string? programFilter = null, 
            string? yearFilter = null, string? sectionFilter = null, int page = 1)
        {
            int pageSize = 10;
            
            // Start with base query including semester data
            var studentsQuery = _context.Students
                .Include(s => s.StudentSemesters)
                .ThenInclude(ss => ss.Semester)
                .Where(s => !s.IsArchived == true);

            // Apply filters
            if (!string.IsNullOrEmpty(semesterFilter))
            {
                var semesterId = int.Parse(semesterFilter);
                studentsQuery = studentsQuery
                    .Where(s => s.StudentSemesters.Any(ss => ss.SemesterId == semesterId));
            }

            if (!string.IsNullOrEmpty(programFilter))
            {
                studentsQuery = studentsQuery
                    .Where(s => s.Course.ToLower().Contains(programFilter.ToLower()));
            }

            if (!string.IsNullOrEmpty(yearFilter))
            {
                var yearLevel = int.Parse(yearFilter);
                studentsQuery = studentsQuery
                    .Where(s => s.StudentSemesters.Any(ss => ss.YearLevel == yearLevel));
            }

            if (!string.IsNullOrEmpty(sectionFilter))
            {
                studentsQuery = studentsQuery
                    .Where(s => s.StudentSemesters.Any(ss => ss.Section.ToLower().Contains(sectionFilter.ToLower())));
            }

            var totalStudents = await studentsQuery.CountAsync();
            var students = await studentsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get filter options for dropdowns
            var semesters = await _context.Semesters
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            var currentSemesterId = await _context.Semesters
                .Where(s => s.IsCurrent)
                .Select(s => s.SemesterId)
                .FirstOrDefaultAsync();

            ViewBag.Semesters = semesters;
            ViewBag.CurrentSemesterId = currentSemesterId;
            ViewBag.ProgramFilter = programFilter;
            ViewBag.YearFilter = yearFilter;
            ViewBag.SectionFilter = sectionFilter;
            ViewBag.TotalStudents = totalStudents;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;

            return View(students);
        }

        // ============================================================
        // TIME-IN/TIME-OUT ATTENDANCE TRACKING
        // ============================================================

        public async Task<IActionResult> TimeAttendance()
        {
            var currentSemester = await _context.Semesters
                .FirstOrDefaultAsync(s => s.IsCurrent);

            var attendanceToday = await _context.Attendances
                .Include(a => a.StudentNumNavigation)
                .ThenInclude(s => s.StudentSemesters)
                .Where(a => a.TimeIn.HasValue && a.TimeIn.Value.Date == DateTime.Today)
                .OrderByDescending(a => a.TimeIn)
                .ToListAsync();

            var recentAttendance = await _context.Attendances
                .Include(a => a.StudentNumNavigation)
                .ThenInclude(s => s.StudentSemesters)
                .Where(a => a.TimeIn.HasValue)
                .OrderByDescending(a => a.TimeIn)
                .Take(100)
                .ToListAsync();

            ViewBag.CurrentSemester = currentSemester?.SemesterName;
            return View(recentAttendance);
        }

        [HttpPost]
        public async Task<IActionResult> RecordTimeIn(string studentNum)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentNum == studentNum);

            if (student == null)
            {
                return Json(new { success = false, message = "Student not found" });
            }

            var currentSemester = await _context.Semesters
                .FirstOrDefaultAsync(s => s.IsCurrent);

            if (currentSemester == null)
            {
                return Json(new { success = false, message = "No current semester set" });
            }

            // Check if already timed in today
            var existingTimeIn = await _context.Attendances
                .FirstOrDefaultAsync(a => a.StudentNum == studentNum 
                    && a.TimeIn.HasValue 
                    && a.TimeIn.Value.Date == DateTime.Today);

            if (existingTimeIn != null)
            {
                return Json(new { success = false, message = "Student already timed in today" });
            }

            // Create attendance record for TimeIn
            var attendance = new Attendance
            {
                StudentNum = studentNum,
                TimeIn = DateTime.Now,
                ScanDevice = "Manual Admin Time-In",
                Location = "Admin Office",
                SemesterId = currentSemester.SemesterId,
                AttendanceStatus = "Present"
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            await LogAction("Manual Time-In", $"Manual Time-In recorded for student: {studentNum} at {DateTime.Now}");

            return Json(new { 
                success = true, 
                studentName = student.FullName,
                timeIn = attendance.TimeIn,
                semesterName = currentSemester.SemesterName
            });
        }

        [HttpPost]
        public async Task<IActionResult> RecordTimeOut(string studentNum)
        {
            var attendance = await _context.Attendances
                .Include(a => a.StudentNumNavigation)
                .FirstOrDefaultAsync(a => a.StudentNum == studentNum 
                    && a.TimeIn.HasValue 
                    && !a.TimeOut.HasValue 
                    && a.TimeIn.Value.Date == DateTime.Today);

            if (attendance == null)
            {
                return Json(new { success = false, message = "No active Time-In record found" });
            }

            // Calculate duration
            attendance.TimeOut = DateTime.Now;
            attendance.DurationMinutes = (int)(DateTime.Now - attendance.TimeIn.Value).TotalMinutes;

            await _context.SaveChangesAsync();

            await LogAction("Time-Out", $"Time-Out recorded for student: {studentNum} at {DateTime.Now} (Duration: {attendance.DurationMinutes} minutes)");

            return Json(new { 
                success = true, 
                studentName = attendance.StudentNumNavigation?.FullName,
                timeOut = attendance.TimeOut,
                duration = attendance.DurationMinutes
            });
        }

        // ============================================================
        // SEMESTER-BASED ATTENDANCE REPORTS
        // ============================================================

        public async Task<IActionResult> AttendanceReport(int? semesterId = null)
        {
            IQueryable<Attendance> query = _context.Attendances
                .Include(a => a.StudentNumNavigation)
                .ThenInclude(s => s.StudentSemesters);

            if (semesterId.HasValue)
            {
                query = query.Where(a => a.SemesterId == semesterId.Value);
            }
            else
            {
                var currentSemester = await _context.Semesters
                    .FirstOrDefaultAsync(s => s.IsCurrent);
                if (currentSemester != null)
                {
                    query = query.Where(a => a.SemesterId == currentSemester.SemesterId);
                }
            }

            var attendanceRecords = await query
                .OrderByDescending(a => a.TimeIn)
                .ToListAsync();

            ViewBag.Semesters = await _context.Semesters
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();

            return View(attendanceRecords);
        }

        public async Task<IActionResult> ExportAttendanceReport(int? semesterId = null)
        {
            IQueryable<Attendance> query = _context.Attendances
                .Include(a => a.StudentNumNavigation)
                .ThenInclude(s => s.StudentSemesters);

            if (semesterId.HasValue)
            {
                query = query.Where(a => a.SemesterId == semesterId.Value);
            }
            else
            {
                var currentSemester = await _context.Semesters
                    .FirstOrDefaultAsync(s => s.IsCurrent);
                if (currentSemester != null)
                {
                    query = query.Where(a => a.SemesterId == currentSemester.SemesterId);
                }
            }

            var attendanceRecords = await query
                .OrderByDescending(a => a.TimeIn)
                .ToListAsync();

            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Attendance Report");

            // Headers
            worksheet.Cell(1, 1).Value = "Student Number";
            worksheet.Cell(1, 2).Value = "Student Name";
            worksheet.Cell(1, 3).Value = "Course";
            worksheet.Cell(1, 4).Value = "Time In";
            worksheet.Cell(1, 5).Value = "Time Out";
            worksheet.Cell(1, 6).Value = "Duration (Min)";
            worksheet.Cell(1, 7).Value = "Location";
            worksheet.Cell(1, 8).Value = "Device";

            // Data
            int row = 2;
            foreach (var record in attendanceRecords)
            {
                worksheet.Cell(row, 1).Value = record.StudentNum;
                worksheet.Cell(row, 2).Value = record.StudentNumNavigation?.FullName;
                worksheet.Cell(row, 3).Value = record.StudentNumNavigation?.Course;
                worksheet.Cell(row, 4).Value = record.TimeIn?.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 5).Value = record.TimeOut?.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 6).Value = record.DurationMinutes?.ToString();
                worksheet.Cell(row, 7).Value = record.Location;
                worksheet.Cell(row, 8).Value = record.ScanDevice;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"Attendance_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

    }
}