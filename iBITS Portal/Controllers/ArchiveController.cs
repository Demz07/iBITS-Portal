// Controllers/ArchiveController.cs

using iBITS_Portal.Helpers;
using iBITS_Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace iBITS_Portal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ArchiveController : Controller
    {
        private readonly PortaliBitsContext _context;
        private readonly ILogger<ArchiveController> _logger;

        public ArchiveController(PortaliBitsContext context, ILogger<ArchiveController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =========================================================
        // GLOBAL OVERRIDE: EXECUTES BEFORE EVERY ACTION
        // =========================================================
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var aySetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "CurrentAcademicYear");
            ViewBag.CurrentAcademicYear = aySetting?.SettingValue ?? "Not Set";

            var semSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "CurrentSemester");
            ViewBag.CurrentSemester = semSetting?.SettingValue ?? "1st Semester";

            ViewBag.AllExistingYears = await _context.Students
                .Where(s => !string.IsNullOrEmpty(s.SchoolYearEnrolled))
                .Select(s => s.SchoolYearEnrolled)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            await next();
        }

        // ==========================================
        // MAIN VIEW
        // ==========================================
        public async Task<IActionResult> Index()
        {
            // Get Distinct Years from ArchiveDate (Student, Fees, Events)
            var studentYears = await _context.Students
                .Where(s => s.ArchiveDate.HasValue)
                .Select(s => s.ArchiveDate.Value.Year)
                .Distinct()
                .ToListAsync();

            // Merge with other archive tables if necessary, or just rely on Student archives
            var allYears = studentYears
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            ViewBag.ArchiveYears = allYears;

            // Default to current year if list is empty
            ViewBag.CurrentArchiveYear = allYears.Any() ? allYears.First() : PhTime.Now.Year;

            return View();
        }

        // ==========================================
        // AJAX: Get Archived Students (Updated with Search & Archive Year)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetArchivedStudents(string searchString, int? archiveYear)
        {
            try
            {
                var query = _context.Students
                    .Where(s => s.IsArchived == true ||
                                s.Classification == "Archived" ||
                                s.Classification == "Graduated" ||
                                s.Classification == "Dropped" ||
                                s.Classification == "Inactive");

                // Filter by Archive Year
                if (archiveYear.HasValue)
                {
                    query = query.Where(s => s.ArchiveDate.HasValue && s.ArchiveDate.Value.Year == archiveYear.Value);
                }

                // Filter by Search String
                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.ToLower();
                    query = query.Where(s =>
                        s.StudentNum.ToLower().Contains(searchString) ||
                        s.StudentFn.ToLower().Contains(searchString) ||
                        s.StudentLn.ToLower().Contains(searchString) ||
                        (s.Course != null && s.Course.ToLower().Contains(searchString)) ||
                        (s.YearLevelSection != null && s.YearLevelSection.ToLower().Contains(searchString))
                    );
                }

                var students = await query
                    .Select(s => new {
                        s.StudentNum,
                        FullName = s.StudentFn + " " + s.StudentLn,
                        s.Course,
                        s.YearLevelSection,
                        s.Classification,
                        s.ArchiveStatus,
                        s.ArchiveDate
                    })
                    .OrderByDescending(s => s.ArchiveDate)
                    .ThenBy(s => s.FullName)
                    .ToListAsync();

                return Json(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching archived students");
                return StatusCode(500, "Internal Server Error");
            }
        }

        // ==========================================
        // AJAX: Get Archived Payments
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetArchivedPayments(string searchString, int? archiveYear, string status)
        {
            try
            {
                var query = _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    // Assume fees for archived students are archived payments
                    .Where(f => f.StudentNumNavigation.IsArchived == true)
                    .AsQueryable();

                // Filter by Archive Year (Based on when the student was archived, or you could add an ArchivedDate to Fee)
                if (archiveYear.HasValue)
                {
                    query = query.Where(f => f.StudentNumNavigation.ArchiveDate.HasValue && f.StudentNumNavigation.ArchiveDate.Value.Year == archiveYear.Value);
                }

                // Search Filter
                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.ToLower();
                    query = query.Where(f =>
                        f.FeeName.ToLower().Contains(searchString) ||
                        f.StudentNum.ToLower().Contains(searchString) ||
                        (f.StudentNumNavigation.StudentFn + " " + f.StudentNumNavigation.StudentLn).ToLower().Contains(searchString)
                    );
                }

                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    if (status == "paid")
                        query = query.Where(f => f.FeeStatus == "Paid" || f.FeeStatus == "Completed");
                    else if (status == "unpaid")
                        query = query.Where(f => f.FeeStatus == "Pending" || f.FeeStatus == "Unpaid" || f.FeeStatus == null);
                }

                var payments = await query
                    .Select(f => new {
                        f.FeeId,
                        f.FeeName,
                        f.Amount,
                        f.FeesDueDate,
                        f.FeeStatus,
                        StudentName = f.StudentNumNavigation != null
                            ? f.StudentNumNavigation.StudentFn + " " + f.StudentNumNavigation.StudentLn
                            : "Unknown",
                        StudentNum = f.StudentNum
                    })
                    .OrderByDescending(f => f.FeesDueDate)
                    .ToListAsync();

                return Json(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching archived payments");
                return StatusCode(500, "Internal Server Error");
            }
        }

        // ==========================================
        // AJAX: Get Archived Events
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetArchivedEvents(int? archiveYear)
        {
            try
            {
                // We use a very simple query first to see if the table is even readable
                var query = _context.ArchivedEvents.AsNoTracking().AsQueryable();

                if (archiveYear.HasValue && archiveYear.Value > 0)
                {
                    // Use EF.Functions to be safe with SQL dates
                    query = query.Where(e => e.ArchivedDate.Year == archiveYear.Value);
                }

                var events = await query
                    .OrderByDescending(e => e.ArchivedDate)
                    .Select(e => new {
                        eventName = e.EventName ?? "Unnamed Event",
                        eventDate = e.EventDate,
                        eventLocation = e.EventLocation ?? "N/A",
                        archiveReason = e.ArchiveReason ?? "Manual Archive"
                    })
                    .ToListAsync();

                return Json(events);
            }
            catch (Exception ex)
            {
                // This is the most important part: 
                // It sends the ACTUAL error message to the browser console.
                var innerError = ex.InnerException != null ? ex.InnerException.Message : "";
                return StatusCode(500, new { message = ex.Message, details = innerError });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchiveStudent(string studentNum)
        {
            var currentUser = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Admin";
            var student = await _context.Students.FindAsync(studentNum);

            if (student != null)
            {
                student.IsArchived = false;
                student.Classification = "Active";
                student.ArchiveStatus = null;
                student.ArchiveDate = null;

                var logEntry = new ActivityLog
                {
                    Action = "Student Unarchive",
                    Description = $"Restored student: {student.StudentFn} {student.StudentLn}",
                    PerformedBy = currentUser,
                    Timestamp = PhTime.Now
                };
                _context.ActivityLogs.Add(logEntry);

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Student restored successfully." });
            }
            return Json(new { success = false, message = "Student not found." });
        }
    }
}