// ============================================================
// FILE PATH: Controllers/AdminController.cs
// ============================================================
// UPDATED: Added working ExportStudentsToExcel functionality
// with ClosedXML library. Exports filtered student data to Excel.
// UPDATED: Added Current Academic Year management and
// SchoolYearEnrolled field for students.
// ============================================================
// NEW UPDATE: Attendance View-Only with Filters
// ============================================================
// - Modified Attendance() method to accept filters (event, program, year)
// - Added PopulateAttendanceFilters() helper method
// - Updated ExportAttendanceToExcel() to support filters and added Program/Year columns
// - Added NEW ExportAttendanceToCSV() method with filters
// - Added EscapeCsvField() helper method for CSV export
// ============================================================

using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using iBITS_Portal.Helpers;
using iBITS_Portal.Models;
using iBITS_Portal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iBITS_Portal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly PortaliBitsContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AdminController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            PortaliBitsContext context,
            ILogger<AdminController> logger,
            IWebHostEnvironment env,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
            _env = env;
            _signInManager = signInManager;
        }

        // =========================================================
        // HELPER: POPULATE DROPDOWNS DYNAMICALLY
        // =========================================================
        // Replace the existing PopulateFilterDropdowns method

        // =========================================================
        // HELPER: POPULATE DROPDOWNS DYNAMICALLY
        // =========================================================
        private async Task PopulateFilterDropdowns()
        {
            ViewBag.Roles = _roleManager.Roles
                .Where(r => r.Name != "Admin" && r.Name != "Student")
                .Select(r => r.Name)
                .OrderBy(n => n)
                .ToList();

            ViewBag.Programs = await _context.Students
                .Where(s => s.Course != null)
                .Select(s => s.Course)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.StudentTypes = await _context.Students
                .Where(s => s.StudentType != null)
                .Select(s => s.StudentType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            // DYNAMIC: Populate Year filter options from actual data using the new helper
            var allYearSections = await _context.Students
                .Where(s => s.YearLevelSection != null)
                .Select(s => s.YearLevelSection)
                .Distinct()
                .ToListAsync();

            ViewBag.Years = allYearSections
      .Select(ys => StringHelper.ExtractYearLevel(ys))
      .Where(y => y > 0)
      .Distinct()
      .OrderBy(y => y)
      .Select(y => y.ToString())
      .ToList();


            // This logic is simplified as section-only filtering is less common
            // and can be achieved with the main search bar.
            ViewBag.Sections = allYearSections
                .Select(ys => System.Text.RegularExpressions.Regex.Match(ys, @"-(\d+)").Groups[1].Value)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            ViewBag.AvailableRoles = new List<string>
            {
                "Member",
                "Officer",
                "Org Secretary",
                "Class Secretary",
                "Org Treasurer",
                "Class Treasurer"
            };
        }

        /// <summary>
        /// Extract year-section pattern (e.g., "3-1") from YearLevelSection string
        /// Handles formats: "1-1", "BSIT 3-1", "DIT 3-1", etc.
        /// Used for Class officer duplicate validation
        /// </summary>
        private string ExtractYearSection(string yearLevelSection)
        {
            if (string.IsNullOrEmpty(yearLevelSection)) return "";

            // Extract pattern like "3-1" from "BSIT 3-1" or return "3-1" as-is
            var match = System.Text.RegularExpressions.Regex.Match(yearLevelSection, @"(\d+-\d+)");
            return match.Success ? match.Value : yearLevelSection;
        }

        /// <summary>
        /// Extract year number from YearLevelSection string
        /// Handles formats: "1-1", "2-2", "BSIT 3-1", "DIT 3-1", etc.
        /// </summary>
        private string ExtractYearFromYearLevelSection(string yearLevelSection)
        {
            if (string.IsNullOrEmpty(yearLevelSection)) return null;

            // Try to find a digit followed by a dash (e.g., "1-", "3-")
            var match = System.Text.RegularExpressions.Regex.Match(yearLevelSection, @"(\d)-");
            if (match.Success)
            {
                return match.Groups[1].Value; // Return the digit before the dash
            }

            return null;
        }

        /// <summary>
        /// Extract section number from YearLevelSection string
        /// Handles formats: "1-1", "2-2", "BSIT 3-1", "DIT 3-1", etc.
        /// </summary>
        private string ExtractSectionFromYearLevelSection(string yearLevelSection)
        {
            if (string.IsNullOrEmpty(yearLevelSection)) return null;

            // Try to find a dash followed by a digit (e.g., "-1", "-2")
            var match = System.Text.RegularExpressions.Regex.Match(yearLevelSection, @"-(\d+)");
            if (match.Success)
            {
                return match.Groups[1].Value; // Return the digit(s) after the dash
            }

            return null;
        }

        private async Task LogAction(string action, string description)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var logEntry = new ActivityLog
            {
                Action = action,
                Description = description,
                PerformedBy = currentUser?.UserName ?? "System",
                Timestamp = DateTime.Now
            };
            _context.ActivityLogs.Add(logEntry);
            await _context.SaveChangesAsync();
        }


        // =========================================================
        // HELPER: GET FILTERED STUDENTS QUERY (CONSOLIDATED)
        // =========================================================
        private IQueryable<Student> GetFilteredStudentsQuery(
            string? searchString,
            string? programFilter,
            string? yearFilter,
            string? sectionFilter,
            string? typeFilter,
            string? statusFilter,
            string? roleFilter)
        {
            var studentsQuery = _context.Students
                .Include(s => s.Officer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                studentsQuery = studentsQuery.Where(s =>
                    s.StudentNum.Contains(searchString) ||
                    s.StudentFn.Contains(searchString) ||
                    s.StudentLn.Contains(searchString) ||
                    (s.StudentMn != null && s.StudentMn.Contains(searchString)) ||
                    (s.StudentEmail != null && s.StudentEmail.Contains(searchString)) ||
                    (s.Course != null && s.Course.Contains(searchString)) ||
                    (s.YearLevelSection != null && s.YearLevelSection.Contains(searchString))
                );
            }

            if (!string.IsNullOrEmpty(programFilter))
            {
                studentsQuery = studentsQuery.Where(s => s.Course == programFilter);
            }

            // Improved Year/Section filtering that can be translated to SQL
            if (!string.IsNullOrEmpty(yearFilter) && !string.IsNullOrEmpty(sectionFilter))
            {
                string exactMatch = $"{yearFilter}-{sectionFilter}";
                studentsQuery = studentsQuery.Where(s =>
                    s.YearLevelSection != null &&
                    (s.YearLevelSection == exactMatch || s.YearLevelSection.EndsWith(" " + exactMatch)));
            }
            else if (!string.IsNullOrEmpty(yearFilter))
            {
                string yearPattern = yearFilter + "-";
                studentsQuery = studentsQuery.Where(s =>
                    s.YearLevelSection != null &&
                    (s.YearLevelSection.StartsWith(yearPattern) || s.YearLevelSection.Contains(" " + yearPattern)));
            }
            else if (!string.IsNullOrEmpty(sectionFilter))
            {
                string sectionPattern = "-" + sectionFilter;
                studentsQuery = studentsQuery.Where(s =>
                    s.YearLevelSection != null && s.YearLevelSection.EndsWith(sectionPattern));
            }

            if (!string.IsNullOrEmpty(typeFilter))
            {
                studentsQuery = studentsQuery.Where(s => s.StudentType == typeFilter);
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                studentsQuery = studentsQuery.Where(s => s.Classification == statusFilter);
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                switch (roleFilter)
                {
                    case "Org Officer":
                        // Filters for students who ARE officers AND their type is "Org"
                        studentsQuery = studentsQuery.Where(s => s.Officer != null && s.Officer.Classification == "Org Officer");
                        break;

                    case "Class Officer":
                        // Filters for students who ARE officers AND their type is "Class"
                        studentsQuery = studentsQuery.Where(s => s.Officer != null && s.Officer.Classification == "Class Officer");
                        break;

                    case "Member":
                        // Filters for students who are NOT officers
                        studentsQuery = studentsQuery.Where(s => s.Officer == null);
                        break;
                }
            }


            return studentsQuery;
        }


        // =========================================================
        // HELPER: GET CURRENT ACADEMIC YEAR FROM SYSTEM SETTINGS
        // =========================================================
        private async Task<string> GetCurrentAcademicYear()
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "CurrentAcademicYear");

            if (setting != null && !string.IsNullOrEmpty(setting.SettingValue))
            {
                return setting.SettingValue;
            }

            // Default: Generate based on current date
            int currentYear = DateTime.Now.Year;
            int currentMonth = DateTime.Now.Month;
            // Academic year typically starts in June/August
            if (currentMonth >= 6)
            {
                return $"A.Y. {currentYear}-{currentYear + 1}";
            }
            return $"A.Y. {currentYear - 1}-{currentYear}";
        }

        // =========================================================
        // HELPER: GENERATE ACADEMIC YEAR OPTIONS FOR DROPDOWNS
        // =========================================================
        private List<string> GenerateAcademicYearOptions()
        {
            var options = new List<string>();
            int currentYear = DateTime.Now.Year;

            // Generate years from 5 years ago to 2 years ahead
            for (int year = currentYear - 5; year <= currentYear + 2; year++)
            {
                options.Add($"A.Y. {year}-{year + 1}");
            }

            return options;
        }

        // =========================================================
        // ACTION: SET CURRENT ACADEMIC YEAR (AJAX - For Navbar Dropdown)
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> SetCurrentAcademicYear(string academicYear)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(academicYear))
                {
                    return Json(new { success = false, message = "Academic year cannot be empty." });
                }

                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "CurrentAcademicYear");

                if (setting != null)
                {
                    setting.SettingValue = academicYear;
                    setting.LastUpdated = DateTime.Now;
                    setting.UpdatedBy = User.Identity?.Name ?? "Admin";
                }
                else
                {
                    setting = new SystemSetting
                    {
                        SettingKey = "CurrentAcademicYear",
                        SettingValue = academicYear,
                        Description = "The current academic year for student enrollment",
                        LastUpdated = DateTime.Now,
                        UpdatedBy = User.Identity?.Name ?? "Admin"
                    };
                    _context.SystemSettings.Add(setting);
                }

                await _context.SaveChangesAsync();
                await LogAction("Academic Year Changed", $"Academic year set to {academicYear}");

                return Json(new { success = true, message = "Academic year updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating academic year");
                return Json(new { success = false, message = "An error occurred while updating the academic year." });
            }
        }

        // =========================================================
        // ACTION: UPDATE CURRENT ACADEMIC YEAR (Form Post - Legacy)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAcademicYear(string newAcademicYear)
        {
            if (string.IsNullOrWhiteSpace(newAcademicYear))
            {
                TempData["Error"] = "Academic year cannot be empty.";
                return RedirectToAction("StudentRecords");
            }

            try
            {
                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "CurrentAcademicYear");

                var currentUser = await _userManager.GetUserAsync(User);

                if (setting == null)
                {
                    // Create new setting
                    setting = new SystemSetting
                    {
                        SettingKey = "CurrentAcademicYear",
                        SettingValue = newAcademicYear,
                        Description = "The current academic year for the system",
                        LastUpdated = DateTime.Now,
                        UpdatedBy = currentUser?.UserName
                    };
                    _context.SystemSettings.Add(setting);
                }
                else
                {
                    // Update existing setting
                    setting.SettingValue = newAcademicYear;
                    setting.LastUpdated = DateTime.Now;
                    setting.UpdatedBy = currentUser?.UserName;
                    _context.SystemSettings.Update(setting);
                }

                await _context.SaveChangesAsync();
                await LogAction("Update Academic Year", $"Changed current academic year to {newAcademicYear}");
                TempData["Message"] = $"Academic year updated to {newAcademicYear}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating academic year");
                TempData["Error"] = "An error occurred while updating the academic year.";
            }

            return RedirectToAction("StudentRecords");
        }

        // =========================================================
        // 1. DASHBOARD - Charts & Statistics Overview
        // =========================================================
        public async Task<IActionResult> Index()
        {
            await PopulateDashboardData();
            return View();
        }

        // =========================================================
        // AJAX: Get Dashboard Data for Refresh
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetDashboardData(string? feeCategory, string? fineCategory)
        {
            var data = await BuildDashboardData(feeCategory, fineCategory);
            return Json(data);
        }

        // =========================================================
        // AJAX: Get Chart Details on Click
        // Returns detailed breakdown when user clicks a chart segment
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetChartDetails(string chartType, string segment, string status, string? category)
        {
            try
            {
                // ADDED: Explicitly handle only 'payments' chart type for this action
                if (chartType.ToLower() != "payments")
                {
                    return Json(new { success = false, message = "Invalid chart type specified for this endpoint." });
                }

                if (string.IsNullOrEmpty(segment))
                    return Json(new { success = false, message = "Invalid segment data" });

                var program = segment.StartsWith("BSIT", StringComparison.OrdinalIgnoreCase) ? "BSIT" :
                              segment.StartsWith("DIT", StringComparison.OrdinalIgnoreCase) ? "DIT" : "";

                var yearStr = segment.Replace("BSIT", "", StringComparison.OrdinalIgnoreCase)
                                     .Replace("DIT", "", StringComparison.OrdinalIgnoreCase);

                int.TryParse(yearStr, out int yearLevel);

                if (string.IsNullOrEmpty(program) || yearLevel == 0)
                {
                    return Json(new { success = false, message = "Could not determine program or year level." });
                }

                var query = _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .AsQueryable();

                query = query.Where(f => f.StudentNumNavigation != null &&
                                         f.StudentNumNavigation.Course != null &&
                                         f.StudentNumNavigation.Course.Contains(program));

                if (!string.IsNullOrEmpty(category) && category.ToLower() != "all")
                {
                    query = query.Where(f => f.FeeName == category);
                }

                if (status.ToLower() == "paid")
                {
                    query = query.Where(f => f.FeeStatus == "Paid" || f.FeeStatus == "Completed");
                }
                else
                {
                    query = query.Where(f => f.FeeStatus != "Paid" && f.FeeStatus != "Completed");
                }

                var fees = await query.ToListAsync();

                // In-memory filter for year level
                fees = fees.Where(f => StringHelper.ExtractYearLevel(f.StudentNumNavigation?.YearLevelSection) == yearLevel).ToList();

                var feeBreakdown = fees
                    .GroupBy(f => f.FeeName ?? "Unnamed Fee")
                    .Select(g => new
                    {
                        FeeName = g.Key,
                        Amount = g.FirstOrDefault()?.Amount ?? 0,
                        StudentCount = g.Select(f => f.StudentNum).Distinct().Count(),
                        TotalAmount = g.Sum(f => f.Amount ?? 0)
                    })
                    .OrderByDescending(f => f.TotalAmount)
                    .ToList();

                var students = fees
                    .Select(f => new
                    {
                        StudentNum = f.StudentNumNavigation?.StudentNum ?? f.StudentNum,
                        Name = f.StudentNumNavigation?.FullName ?? "Unknown",
                        FeeName = f.FeeName ?? "Unnamed Fee",
                        Amount = f.Amount ?? 0,
                        Status = f.FeeStatus ?? "Pending"
                    })
                    .OrderBy(s => s.Name)
                    .Take(100)
                    .ToList();

                var totalAmount = fees.Sum(f => f.Amount ?? 0);
                var studentCount = fees.Select(f => f.StudentNum).Distinct().Count();

                return Json(new
                {
                    success = true,
                    summary = new
                    {
                        totalAmount = totalAmount,
                        studentCount = studentCount,
                        averagePerStudent = studentCount > 0 ? totalAmount / studentCount : 0
                    },
                    feeBreakdown = feeBreakdown,
                    students = students
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart details for segment {Segment}", segment);
                return Json(new { success = false, message = "Error loading details" });
            }
        }

        // =========================================================
        // AJAX: Get Fines Details on Click
        // Returns detailed breakdown when user clicks fines chart segment
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetFinesDetails(string segment, string status, string? category)
        {
            try
            {
                var program = segment.StartsWith("BSIT") ? "BSIT" : segment.StartsWith("DIT") ? "DIT" : "";
                int.TryParse(segment.Replace("BSIT", "").Replace("DIT", ""), out int yearLevel);

                var query = _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .Include(f => f.Attendance).ThenInclude(a => a.StudentNumNavigation)
                    .Include(f => f.Attendance).ThenInclude(a => a.Event)
                    .AsQueryable();

                // 1. Category Filter
                if (!string.IsNullOrEmpty(category) && category != "all")
                {
                    if (category == "manual")
                        query = query.Where(f => f.AttendanceId == null);
                    else if (category == "event")
                        query = query.Where(f => f.AttendanceId != null);
                    else
                        query = query.Where(f =>
                            (f.Attendance != null && f.Attendance.Event.EventName == category) ||
                            (f.AttendanceId == null && f.Description == category));
                }

                // 2. Status Filter
                if (status == "paid")
                    query = query.Where(f => f.FinesStatus == "Paid");
                else
                    query = query.Where(f => f.FinesStatus != "Paid");

                var fines = await query.ToListAsync();

                // 3. Year/Program Filter (Memory)
                int ExtractYearLevel(string? yls)
                {
                    if (string.IsNullOrWhiteSpace(yls)) return 0;
                    var input = yls.Trim().ToUpper();
                    foreach (char c in input) { if (char.IsDigit(c)) { int year = c - '0'; if (year >= 1 && year <= 4) return year; } }
                    return 0;
                }

                bool ProgramMatch(Student? s) => s != null && s.Course != null && s.Course.Contains(program);

                fines = fines.Where(f => {
                    var s = f.StudentNumNavigation ?? f.Attendance?.StudentNumNavigation;
                    return ProgramMatch(s) && ExtractYearLevel(s?.YearLevelSection) == yearLevel;
                }).ToList();

                // 4. Response
                var breakdown = fines
                    .GroupBy(f => f.Attendance?.Event?.EventName ?? f.Description ?? "Unknown")
                    .Select(g => new { EventName = g.Key, FineAmount = g.First().Amount ?? 0, StudentCount = g.Count(), TotalAmount = g.Sum(f => f.Amount ?? 0) })
                    .ToList();

                var students = fines.Select(f => new {
                    StudentNum = (f.StudentNumNavigation ?? f.Attendance?.StudentNumNavigation)?.StudentNum,
                    Name = (f.StudentNumNavigation ?? f.Attendance?.StudentNumNavigation)?.FullName,
                    EventName = f.Attendance?.Event?.EventName ?? f.Description,
                    Amount = f.Amount,
                    Status = f.FinesStatus
                }).Take(50).ToList();

                return Json(new
                {
                    success = true,
                    summary = new { totalAmount = fines.Sum(f => f.Amount ?? 0), studentCount = fines.Count },
                    eventBreakdown = breakdown,
                    students = students
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fines details");
                return Json(new { success = false, message = "Error loading details" });
            }
        }


        // =========================================================
        // AJAX: Get Students by Program (for Program Doughnut Chart)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetStudentsByProgram(string program)
        {
            try
            {
                int ExtractYearLevel(string? yls)
                {
                    if (string.IsNullOrWhiteSpace(yls)) return 0;
                    var input = yls.Trim().ToUpper();
                    if (input.Contains("FIRST") || input.Contains("1ST")) return 1;
                    if (input.Contains("SECOND") || input.Contains("2ND")) return 2;
                    if (input.Contains("THIRD") || input.Contains("3RD")) return 3;
                    if (input.Contains("FOURTH") || input.Contains("4TH")) return 4;
                    foreach (char c in input)
                        if (char.IsDigit(c) && c >= '1' && c <= '4') return c - '0';
                    return 0;
                }

                var students = await _context.Students
                    .Where(s => s.IsArchived != true && s.Course != null && s.Course.ToUpper().Contains(program.ToUpper()))
                    .ToListAsync();

                var fees = await _context.Fees.Include(f => f.StudentNumNavigation).ToListAsync();
                var maxYear = program.ToUpper() == "DIT" ? 3 : 4;

                var yearBreakdown = Enumerable.Range(1, maxYear).Select(year => new
                {
                    YearLevel = year,
                    StudentCount = students.Count(s => ExtractYearLevel(s.YearLevelSection) == year),
                    ActiveCount = students.Count(s => ExtractYearLevel(s.YearLevelSection) == year &&
                        (string.IsNullOrWhiteSpace(s.Classification) || s.Classification.ToUpper() == "ACTIVE")),
                    PendingFees = fees.Where(f => f.StudentNumNavigation != null &&
                        f.StudentNumNavigation.Course != null && f.StudentNumNavigation.Course.ToUpper().Contains(program.ToUpper()) &&
                        ExtractYearLevel(f.StudentNumNavigation.YearLevelSection) == year &&
                        (string.IsNullOrWhiteSpace(f.FeeStatus) || f.FeeStatus.ToUpper() != "PAID")).Sum(f => f.Amount ?? 0)
                }).ToList();

                return Json(new
                {
                    success = true,
                    program = program,
                    summary = new
                    {
                        totalStudents = students.Count,
                        activeStudents = students.Count(s => string.IsNullOrWhiteSpace(s.Classification) || s.Classification.ToUpper() == "ACTIVE"),
                        withPendingFees = fees.Where(f => f.StudentNumNavigation != null &&
                            f.StudentNumNavigation.Course != null && f.StudentNumNavigation.Course.ToUpper().Contains(program.ToUpper()) &&
                            (string.IsNullOrWhiteSpace(f.FeeStatus) || f.FeeStatus.ToUpper() != "PAID"))
                            .Select(f => f.StudentNum).Distinct().Count()
                    },
                    yearBreakdown = yearBreakdown
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students by program");
                return Json(new { success = false, message = "Error loading details" });
            }
        }

        // =========================================================
        // AJAX: Get Students by Year Level (for Counter Cards)
        // =========================================================
        // =========================================================
        // AJAX: Get Students by Year Level (for Counter Cards)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetStudentsByYearLevel(string program, int yearLevel)
        {
            try
            {
                int ExtractYearLevel(string? yls)
                {
                    if (string.IsNullOrWhiteSpace(yls)) return 0;
                    var input = yls.Trim().ToUpper();
                    if (input.Contains("FIRST") || input.Contains("1ST")) return 1;
                    if (input.Contains("SECOND") || input.Contains("2ND")) return 2;
                    if (input.Contains("THIRD") || input.Contains("3RD")) return 3;
                    if (input.Contains("FOURTH") || input.Contains("4TH")) return 4;
                    foreach (char c in input)
                        if (char.IsDigit(c) && c >= '1' && c <= '4') return c - '0';
                    return 0;
                }

                var allStudents = await _context.Students
                    .Where(s => s.IsArchived != true && s.Course != null && s.Course.ToUpper().Contains(program.ToUpper()))
                    .ToListAsync();

                var students = allStudents.Where(s => ExtractYearLevel(s.YearLevelSection) == yearLevel).ToList();
                var studentIds = students.Select(s => s.StudentNum).ToList();

                // Get Fees
                var fees = await _context.Fees.Where(f => studentIds.Contains(f.StudentNum)).ToListAsync();

                // NEW: Get Fines (via Attendance)
                var fines = await _context.Fines
                    .Include(f => f.Attendance)
                    .Where(f => f.Attendance != null && studentIds.Contains(f.Attendance.StudentNum))
                    .ToListAsync();

                var studentList = students.Select(s => new
                {
                    studentNum = s.StudentNum,
                    name = $"{s.StudentFn} {s.StudentLn}",
                    section = s.YearLevelSection ?? "N/A",
                    status = s.Classification ?? "Active",
                    // Calculate Pending Fees
                    pendingFees = fees.Where(f => f.StudentNum == s.StudentNum &&
                        (string.IsNullOrWhiteSpace(f.FeeStatus) || f.FeeStatus.ToUpper() != "PAID")).Sum(f => f.Amount ?? 0),
                    // Calculate Pending Fines
                    pendingFines = fines.Where(f => f.Attendance != null && f.Attendance.StudentNum == s.StudentNum &&
                        (string.IsNullOrWhiteSpace(f.FinesStatus) || f.FinesStatus.ToUpper() != "PAID")).Sum(f => f.Amount ?? 0)
                }).OrderBy(s => s.name).ToList();

                return Json(new
                {
                    success = true,
                    program = program,
                    yearLevel = yearLevel,
                    summary = new
                    {
                        totalStudents = students.Count,
                        activeStudents = students.Count(s => string.IsNullOrWhiteSpace(s.Classification) || s.Classification.ToUpper() == "ACTIVE"),
                        totalPendingFees = fees.Where(f => string.IsNullOrWhiteSpace(f.FeeStatus) || f.FeeStatus.ToUpper() != "PAID").Sum(f => f.Amount ?? 0),
                        totalPendingFines = fines.Where(f => string.IsNullOrWhiteSpace(f.FinesStatus) || f.FinesStatus.ToUpper() != "PAID").Sum(f => f.Amount ?? 0)
                    },
                    students = studentList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students by year level");
                return Json(new { success = false, message = "Error loading details" });
            }
        }

        // =========================================================
        // AJAX: Get Archived Students (for Archive Counter)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetArchivedStudents()
        {
            try
            {
                var students = await _context.Students
                    .Where(s => s.IsArchived == true ||
                        (s.Classification != null && (s.Classification.ToUpper() == "ARCHIVED" || s.Classification.ToUpper() == "INACTIVE")))
                    .ToListAsync();

                var bsitCount = students.Count(s => s.Course != null && s.Course.ToUpper().Contains("BSIT"));
                var ditCount = students.Count(s => s.Course != null && s.Course.ToUpper().Contains("DIT"));

                var studentList = students.Select(s => new
                {
                    studentNum = s.StudentNum,
                    name = $"{s.StudentFn} {s.StudentLn}",
                    program = s.Course ?? "N/A",
                    yearLevel = s.YearLevelSection ?? "N/A",
                    status = s.Classification ?? "Archived"
                }).OrderBy(s => s.name).Take(50).ToList();

                return Json(new
                {
                    success = true,
                    summary = new
                    {
                        totalArchived = students.Count,
                        bsitCount = bsitCount,
                        ditCount = ditCount
                    },
                    students = studentList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting archived students");
                return Json(new { success = false, message = "Error loading details" });
            }
        }

        // =========================================================
        // AJAX: Get Students by Status (for Status Distribution Chart)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetStudentsByStatus(string status)
        {
            try
            {
                int ExtractYearLevel(string? yls)
                {
                    if (string.IsNullOrWhiteSpace(yls)) return 0;
                    var input = yls.Trim().ToUpper();
                    if (input.Contains("FIRST") || input.Contains("1ST")) return 1;
                    if (input.Contains("SECOND") || input.Contains("2ND")) return 2;
                    if (input.Contains("THIRD") || input.Contains("3RD")) return 3;
                    if (input.Contains("FOURTH") || input.Contains("4TH")) return 4;
                    foreach (char c in input)
                        if (char.IsDigit(c) && c >= '1' && c <= '4') return c - '0';
                    return 0;
                }

                bool IsBSIT(Student s) => s.Course != null && s.Course.ToUpper().Contains("BSIT");
                bool IsDIT(Student s) => s.Course != null && s.Course.ToUpper().Contains("DIT");

                var allStudents = await _context.Students.Where(s => s.IsArchived != true).ToListAsync();

                List<Student> students;
                if (status.ToLower() == "active")
                {
                    students = allStudents.Where(s => string.IsNullOrWhiteSpace(s.Classification) || s.Classification.ToUpper() == "ACTIVE").ToList();
                }
                else
                {
                    students = allStudents.Where(s => !string.IsNullOrWhiteSpace(s.Classification) && s.Classification.ToUpper() != "ACTIVE").ToList();
                }

                var bsitStudents = students.Where(IsBSIT).ToList();
                var ditStudents = students.Where(IsDIT).ToList();

                var breakdown = new List<object>();
                for (int year = 1; year <= 4; year++)
                {
                    var bsitCount = bsitStudents.Count(s => ExtractYearLevel(s.YearLevelSection) == year);
                    if (bsitCount > 0 || year <= 4)
                    {
                        breakdown.Add(new { programYear = $"BSIT {year}", count = bsitCount, percentage = students.Count > 0 ? Math.Round((double)bsitCount / students.Count * 100, 1) : 0 });
                    }
                }
                for (int year = 1; year <= 3; year++)
                {
                    var ditCount = ditStudents.Count(s => ExtractYearLevel(s.YearLevelSection) == year);
                    if (ditCount > 0)
                    {
                        breakdown.Add(new { programYear = $"DIT {year}", count = ditCount, percentage = students.Count > 0 ? Math.Round((double)ditCount / students.Count * 100, 1) : 0 });
                    }
                }

                return Json(new
                {
                    success = true,
                    status = status,
                    summary = new
                    {
                        totalStudents = students.Count,
                        bsitCount = bsitStudents.Count,
                        ditCount = ditStudents.Count
                    },
                    breakdown = breakdown.Where(b => ((dynamic)b).count > 0).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students by status");
                return Json(new { success = false, message = "Error loading details" });
            }
        }

        // =========================================================
        // AJAX: Get Events by Month (for Monthly Events Line Chart)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetEventsByMonth(int month, int year)
        {
            try
            {
                var events = await _context.Events
                    .Where(e => e.EventDate.HasValue && e.EventDate.Value.Month == month && e.EventDate.Value.Year == year)
                    .ToListAsync();

                var today = DateOnly.FromDateTime(DateTime.Now);
                var attendances = await _context.Attendances
                    .Where(a => events.Select(e => e.EventId).Contains(a.EventId ?? 0))
                    .ToListAsync();

                var fines = await _context.Fines
                    .Where(f => attendances.Select(a => a.AttendanceId).Contains(f.AttendanceId ?? 0))
                    .ToListAsync();

                var eventList = events.Select(e => new
                {
                    eventId = e.EventId,
                    eventName = e.EventName ?? "Unnamed Event",
                    eventDate = e.EventDate.HasValue ? e.EventDate.Value.ToString("MMM dd, yyyy") : "N/A",
                    attended = attendances.Count(a => a.EventId == e.EventId && a.AttendanceStatus?.ToUpper() == "PRESENT"),
                    absent = attendances.Count(a => a.EventId == e.EventId && a.AttendanceStatus?.ToUpper() == "ABSENT"),
                    finesGenerated = fines.Where(f => attendances.Any(a => a.AttendanceId == f.AttendanceId && a.EventId == e.EventId)).Sum(f => f.Amount ?? 0),
                    status = e.EventDate.HasValue && e.EventDate.Value < today ? "Completed" : "Upcoming"
                }).OrderBy(e => e.eventDate).ToList();

                var totalAttended = eventList.Sum(e => e.attended);
                var totalExpected = eventList.Sum(e => e.attended + e.absent);
                var avgAttendance = totalExpected > 0 ? Math.Round((double)totalAttended / totalExpected * 100, 1) : 0;

                return Json(new
                {
                    success = true,
                    month = month,
                    year = year,
                    monthName = new DateTime(year, month, 1).ToString("MMMM yyyy"),
                    summary = new
                    {
                        totalEvents = events.Count,
                        avgAttendance = avgAttendance,
                        totalFines = eventList.Sum(e => e.finesGenerated)
                    },
                    events = eventList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events by month");
                return Json(new { success = false, message = "Error loading details" });
            }
        }

        // =========================================================
        // AJAX: Get Events by Status (for Event Status Doughnut)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetEventsByStatus(string status)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                var events = await _context.Events.ToListAsync();

                List<Event> filteredEvents;
                if (status.ToLower() == "completed")
                {
                    filteredEvents = events.Where(e => e.EventDate.HasValue && e.EventDate.Value < today).ToList();
                }
                else
                {
                    filteredEvents = events.Where(e => e.EventDate.HasValue && e.EventDate.Value >= today).ToList();
                }

                var attendances = await _context.Attendances
                    .Where(a => filteredEvents.Select(e => e.EventId).Contains(a.EventId ?? 0))
                    .ToListAsync();

                var fines = await _context.Fines
                    .Where(f => attendances.Select(a => a.AttendanceId).Contains(f.AttendanceId ?? 0))
                    .ToListAsync();

                var eventList = filteredEvents.Select(e => new
                {
                    eventId = e.EventId,
                    eventName = e.EventName ?? "Unnamed Event",
                    eventDate = e.EventDate.HasValue ? e.EventDate.Value.ToString("MMM dd, yyyy") : "N/A",
                    attended = attendances.Count(a => a.EventId == e.EventId && a.AttendanceStatus?.ToUpper() == "PRESENT"),
                    absent = attendances.Count(a => a.EventId == e.EventId && a.AttendanceStatus?.ToUpper() == "ABSENT"),
                    finesGenerated = fines.Where(f => attendances.Any(a => a.AttendanceId == f.AttendanceId && a.EventId == e.EventId)).Sum(f => f.Amount ?? 0)
                }).OrderByDescending(e => e.eventDate).Take(20).ToList();

                var totalAttended = eventList.Sum(e => e.attended);
                var totalExpected = eventList.Sum(e => e.attended + e.absent);
                var avgAttendance = totalExpected > 0 ? Math.Round((double)totalAttended / totalExpected * 100, 1) : 0;

                return Json(new
                {
                    success = true,
                    status = status,
                    summary = new
                    {
                        totalEvents = filteredEvents.Count,
                        avgAttendance = avgAttendance,
                        totalFines = eventList.Sum(e => e.finesGenerated)
                    },
                    events = eventList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events by status");
                return Json(new { success = false, message = "Error loading details" });
            }
        }

        // =========================================================
        // AJAX: Send Notification to Students
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotification(string recipientFilter, string? programFilter, string? yearFilter, string subject, string message)
        {
            try
            {
                var adminUser = await _userManager.GetUserAsync(User);
                string posterName = adminUser?.UserName ?? "Administrator";

                var query = _context.Students.Where(s => s.IsArchived != true);

                if (!string.IsNullOrEmpty(programFilter) && (recipientFilter == "program" || recipientFilter == "year"))
                    query = query.Where(s => s.Course != null && s.Course.Contains(programFilter));

                if (recipientFilter == "pending")
                {
                    var debtors = await _context.Fees.Where(f => f.FeeStatus != "Paid").Select(f => f.StudentNum).ToListAsync();
                    var fineDebtors = await _context.Attendances.SelectMany(a => a.Fines).Where(f => f.FinesStatus != "Paid").Select(f => f.Attendance.StudentNum).ToListAsync();
                    var allDebtors = debtors.Union(fineDebtors).Distinct().ToList();
                    query = query.Where(s => allDebtors.Contains(s.StudentNum));
                }

                var studentList = await query.ToListAsync();
                if (recipientFilter == "year" && !string.IsNullOrEmpty(yearFilter))
                {
                    int.TryParse(yearFilter, out int yr);
                    studentList = studentList.Where(s => ExtractYearLevel(s.YearLevelSection) == yr).ToList();
                }

                // THIS LINE PREVENTS MULTIPLE SENDS TO THE SAME STUDENT
                studentList = studentList.GroupBy(s => s.StudentNum).Select(g => g.First()).ToList();

                if (!studentList.Any()) return Json(new { success = false, message = "No recipients found." });

                // NOTICE: NO ANNOUNCEMENT TABLE ADD HERE. ONLY NOTIFICATIONS.
                foreach (var student in studentList)
                {
                    _context.Notifications.Add(new Notification
                    {
                        StudentNum = student.StudentNum,
                        Title = subject,
                        Message = message,
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        NotificationType = "Admin Notice",
                        SentBy = posterName
                    });
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Notice sent to {studentList.Count} recipients." });
            }
            catch (Exception ex) { return Json(new { success = false, message = "Error: " + ex.Message }); }
        }

        // =========================================================
        // AJAX: Get Notification Preview (recipient count)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetNotificationPreview(string recipientFilter, string? programFilter, string? yearFilter)
        {
            try
            {
                // Use the shared private method instead of defining it inside here
                var students = await _context.Students
                    .Where(s => s.IsArchived != true)
                    .ToListAsync();

                // Apply filters (Ensure this is 1:1 with SendNotification)
                if (recipientFilter == "program" && !string.IsNullOrEmpty(programFilter))
                {
                    students = students.Where(s => s.Course != null && s.Course.ToUpper().Contains(programFilter.ToUpper())).ToList();
                }
                else if (recipientFilter == "year" && !string.IsNullOrEmpty(programFilter) && !string.IsNullOrEmpty(yearFilter))
                {
                    int.TryParse(yearFilter, out int year);
                    students = students.Where(s => s.Course != null && s.Course.ToUpper().Contains(programFilter.ToUpper()) && ExtractYearLevel(s.YearLevelSection) == year).ToList();
                }
                else if (recipientFilter == "pending")
                {
                    var feesWithPending = await _context.Fees
                        .Where(f => string.IsNullOrWhiteSpace(f.FeeStatus) || f.FeeStatus.ToUpper() == "PENDING" || f.FeeStatus.ToUpper() == "UNPAID")
                        .Select(f => f.StudentNum)
                        .Distinct()
                        .ToListAsync();
                    students = students.Where(s => feesWithPending.Contains(s.StudentNum)).ToList();
                }

                var sampleNames = students.Take(3).Select(s => $"{s.StudentFn} {s.StudentLn}").ToList();

                return Json(new
                {
                    success = true,
                    count = students.Count,
                    sampleNames = sampleNames,
                    hasMore = students.Count > 3
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification preview");
                return Json(new { success = false, count = 0 });
            }
        }

        // =========================================================
        // SHARED HELPER METHOD (Put this at the bottom of the class)
        // =========================================================
        private int ExtractYearLevel(string? yls)
        {
            if (string.IsNullOrWhiteSpace(yls)) return 0;
            var input = yls.Trim().ToUpper();
            if (input.Contains("FIRST") || input.Contains("1ST")) return 1;
            if (input.Contains("SECOND") || input.Contains("2ND")) return 2;
            if (input.Contains("THIRD") || input.Contains("3RD")) return 3;
            if (input.Contains("FOURTH") || input.Contains("4TH")) return 4;
            foreach (char c in input)
                if (char.IsDigit(c) && c >= '1' && c <= '4') return c - '0';
            return 0;
        }

        // =========================================================
        // AJAX: Export Dashboard Report
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ExportDashboardReport(string format, string reportType)
        {
            try
            {
                // FIX: Pass null, null to match the new signature
                var data = await BuildDashboardData(null, null);

                var students = await _context.Students.Where(s => s.IsArchived != true).ToListAsync();
                var fees = await _context.Fees.Include(f => f.StudentNumNavigation).ToListAsync();
                var attendances = await _context.Attendances.Include(a => a.StudentNumNavigation).Include(a => a.Event).ToListAsync();

                using var workbook = new XLWorkbook();

                if (reportType == "students" || reportType == "full")
                {
                    var ws = workbook.Worksheets.Add("Students");
                    ws.Cell(1, 1).Value = "Student ID";
                    ws.Cell(1, 2).Value = "First Name";
                    ws.Cell(1, 3).Value = "Last Name";
                    ws.Cell(1, 4).Value = "Program";
                    ws.Cell(1, 5).Value = "Year & Section";
                    ws.Cell(1, 6).Value = "Status";
                    ws.Range(1, 1, 1, 6).Style.Font.Bold = true;
                    ws.Range(1, 1, 1, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#D4AF37");

                    int row = 2;
                    foreach (var s in students.OrderBy(s => s.Course).ThenBy(s => s.YearLevelSection))
                    {
                        ws.Cell(row, 1).Value = s.StudentNum;
                        ws.Cell(row, 2).Value = s.StudentFn;
                        ws.Cell(row, 3).Value = s.StudentLn;
                        ws.Cell(row, 4).Value = s.Course;
                        ws.Cell(row, 5).Value = s.YearLevelSection;
                        ws.Cell(row, 6).Value = s.Classification ?? "Active";
                        row++;
                    }
                    ws.Columns().AdjustToContents();
                }

                if (reportType == "financial" || reportType == "full")
                {
                    var ws = workbook.Worksheets.Add("Financial");
                    ws.Cell(1, 1).Value = "Student ID";
                    ws.Cell(1, 2).Value = "Student Name";
                    ws.Cell(1, 3).Value = "Program";
                    ws.Cell(1, 4).Value = "Fee Name";
                    ws.Cell(1, 5).Value = "Amount";
                    ws.Cell(1, 6).Value = "Status";
                    ws.Cell(1, 7).Value = "Due Date";
                    ws.Range(1, 1, 1, 7).Style.Font.Bold = true;
                    ws.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#D4AF37");

                    int row = 2;
                    foreach (var f in fees.OrderBy(f => f.FeeStatus).ThenBy(f => f.StudentNum))
                    {
                        ws.Cell(row, 1).Value = f.StudentNum;
                        ws.Cell(row, 2).Value = f.StudentNumNavigation != null ? $"{f.StudentNumNavigation.StudentFn} {f.StudentNumNavigation.StudentLn}" : "N/A";
                        ws.Cell(row, 3).Value = f.StudentNumNavigation?.Course ?? "N/A";
                        ws.Cell(row, 4).Value = f.FeeName;
                        ws.Cell(row, 5).Value = f.Amount ?? 0;
                        ws.Cell(row, 6).Value = f.FeeStatus ?? "Pending";
                        ws.Cell(row, 7).Value = f.FeesDueDate.HasValue ? f.FeesDueDate.Value.ToString("yyyy-MM-dd") : "N/A";
                        row++;
                    }
                    ws.Columns().AdjustToContents();
                }

                if (reportType == "attendance" || reportType == "full")
                {
                    var ws = workbook.Worksheets.Add("Attendance");
                    ws.Cell(1, 1).Value = "Event Name";
                    ws.Cell(1, 2).Value = "Event Date";
                    ws.Cell(1, 3).Value = "Student ID";
                    ws.Cell(1, 4).Value = "Student Name";
                    ws.Cell(1, 5).Value = "Status";
                    ws.Range(1, 1, 1, 5).Style.Font.Bold = true;
                    ws.Range(1, 1, 1, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#D4AF37");

                    int row = 2;
                    foreach (var a in attendances.OrderBy(a => a.Event?.EventDate).ThenBy(a => a.StudentNum))
                    {
                        ws.Cell(row, 1).Value = a.Event?.EventName ?? "N/A";
                        ws.Cell(row, 2).Value = a.Event?.EventDate.HasValue == true ? a.Event.EventDate.Value.ToString("yyyy-MM-dd") : "N/A";
                        ws.Cell(row, 3).Value = a.StudentNum;
                        ws.Cell(row, 4).Value = a.StudentNumNavigation != null ? $"{a.StudentNumNavigation.StudentFn} {a.StudentNumNavigation.StudentLn}" : "N/A";
                        ws.Cell(row, 5).Value = a.AttendanceStatus ?? "N/A";
                        row++;
                    }
                    ws.Columns().AdjustToContents();
                }

                if (reportType == "full")
                {
                    var ws = workbook.Worksheets.Add("Summary");
                    ws.Cell(1, 1).Value = "Dashboard Summary Report";
                    ws.Cell(1, 1).Style.Font.Bold = true;
                    ws.Cell(1, 1).Style.Font.FontSize = 16;
                    ws.Cell(2, 1).Value = $"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm}";

                    ws.Cell(4, 1).Value = "Metric";
                    ws.Cell(4, 2).Value = "Value";
                    ws.Range(4, 1, 4, 2).Style.Font.Bold = true;
                    ws.Range(4, 1, 4, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#D4AF37");

                    ws.Cell(5, 1).Value = "Total Students";
                    ws.Cell(5, 2).Value = data.TotalStudents;
                    ws.Cell(6, 1).Value = "Total Expected Fees";
                    ws.Cell(6, 2).Value = data.FeesOverview.TotalExpected;
                    ws.Cell(7, 1).Value = "Total Collected Fees";
                    ws.Cell(7, 2).Value = data.FeesOverview.TotalCollected;
                    ws.Cell(8, 1).Value = "Total Pending Fees";
                    ws.Cell(8, 2).Value = data.FeesOverview.TotalPending;
                    ws.Cell(9, 1).Value = "Fee Collection Rate";
                    ws.Cell(9, 2).Value = $"{data.FeesOverview.CollectionRate}%";
                    ws.Cell(10, 1).Value = "Total Expected Fines";
                    ws.Cell(10, 2).Value = data.FinesOverview.TotalExpected;
                    ws.Cell(11, 1).Value = "Total Collected Fines";
                    ws.Cell(11, 2).Value = data.FinesOverview.TotalCollected;

                    ws.Columns().AdjustToContents();
                }

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"iBITS_Dashboard_{reportType}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting dashboard report");
                TempData["Error"] = "Error generating report. Please try again.";
                return RedirectToAction("Index");
            }
        }


        // =========================================================
        // DEBUG: Check Database Values (REMOVE IN PRODUCTION)
        // Access via: /Admin/DebugDashboardData
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> DebugDashboardData()
        {
            var students = await _context.Students.Where(s => s.IsArchived != true).ToListAsync();
            var fines = await _context.Fines
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.StudentNumNavigation)
                .ToListAsync();

            // Helper function to extract year level (same as in BuildDashboardData)
            int ExtractYearLevel(string? yearLevelSection)
            {
                if (string.IsNullOrWhiteSpace(yearLevelSection))
                    return 0;

                var input = yearLevelSection.Trim().ToUpper();

                if (input.Contains("FIRST") || input.Contains("1ST")) return 1;
                if (input.Contains("SECOND") || input.Contains("2ND")) return 2;
                if (input.Contains("THIRD") || input.Contains("3RD")) return 3;
                if (input.Contains("FOURTH") || input.Contains("4TH")) return 4;

                foreach (char c in input)
                {
                    if (char.IsDigit(c))
                    {
                        int year = c - '0';
                        if (year >= 1 && year <= 4)
                            return year;
                    }
                }
                return 0;
            }

            bool IsBSIT(Student s) => !string.IsNullOrWhiteSpace(s.Course) && s.Course.ToUpper().Contains("BSIT");
            bool IsDIT(Student s) => !string.IsNullOrWhiteSpace(s.Course) && s.Course.ToUpper().Contains("DIT");

            var debugInfo = new
            {
                TotalStudents = students.Count,

                // Sample of first 10 students with their Course and YearLevelSection
                SampleStudents = students.Take(10).Select(s => new
                {
                    s.StudentNum,
                    s.StudentFn,
                    s.StudentLn,
                    Course = s.Course ?? "(NULL)",
                    YearLevelSection = s.YearLevelSection ?? "(NULL)",
                    Classification = s.Classification ?? "(NULL)",
                    IsArchived = s.IsArchived,
                    // Add calculated values
                    ExtractedYearLevel = ExtractYearLevel(s.YearLevelSection),
                    IsBSIT = IsBSIT(s),
                    IsDIT = IsDIT(s)
                }).ToList(),

                // Distinct Course values in database
                DistinctCourses = students
                    .Where(s => s.Course != null)
                    .Select(s => s.Course)
                    .Distinct()
                    .ToList(),

                // Distinct YearLevelSection values in database
                DistinctYearLevelSections = students
                    .Where(s => s.YearLevelSection != null)
                    .Select(s => s.YearLevelSection)
                    .Distinct()
                    .ToList(),

                // Count by Course
                CountByCourse = students
                    .GroupBy(s => s.Course ?? "(NULL)")
                    .Select(g => new { Course = g.Key, Count = g.Count() })
                    .ToList(),

                // Count by YearLevelSection
                CountByYearLevelSection = students
                    .GroupBy(s => s.YearLevelSection ?? "(NULL)")
                    .Select(g => new { YearLevelSection = g.Key, Count = g.Count() })
                    .ToList(),

                // *** CALCULATED DASHBOARD VALUES ***
                CalculatedDashboardData = new
                {
                    StudentsPerProgram = new
                    {
                        BSIT = students.Count(IsBSIT),
                        DIT = students.Count(IsDIT)
                    },
                    YearLevelCounts = new
                    {
                        BSIT1 = students.Count(s => IsBSIT(s) && ExtractYearLevel(s.YearLevelSection) == 1),
                        BSIT2 = students.Count(s => IsBSIT(s) && ExtractYearLevel(s.YearLevelSection) == 2),
                        BSIT3 = students.Count(s => IsBSIT(s) && ExtractYearLevel(s.YearLevelSection) == 3),
                        BSIT4 = students.Count(s => IsBSIT(s) && ExtractYearLevel(s.YearLevelSection) == 4),
                        DIT1 = students.Count(s => IsDIT(s) && ExtractYearLevel(s.YearLevelSection) == 1),
                        DIT2 = students.Count(s => IsDIT(s) && ExtractYearLevel(s.YearLevelSection) == 2),
                        DIT3 = students.Count(s => IsDIT(s) && ExtractYearLevel(s.YearLevelSection) == 3)
                    },
                    FinesByProgram = new
                    {
                        BSIT3 = fines.Where(f => f.Attendance?.StudentNumNavigation != null && IsBSIT(f.Attendance.StudentNumNavigation) && ExtractYearLevel(f.Attendance.StudentNumNavigation.YearLevelSection) == 3).Sum(f => f.Amount ?? 0),
                        BSIT4 = fines.Where(f => f.Attendance?.StudentNumNavigation != null && IsBSIT(f.Attendance.StudentNumNavigation) && ExtractYearLevel(f.Attendance.StudentNumNavigation.YearLevelSection) == 4).Sum(f => f.Amount ?? 0),
                        DIT3 = fines.Where(f => f.Attendance?.StudentNumNavigation != null && IsDIT(f.Attendance.StudentNumNavigation) && ExtractYearLevel(f.Attendance.StudentNumNavigation.YearLevelSection) == 3).Sum(f => f.Amount ?? 0),
                        TotalFinesAmount = fines.Sum(f => f.Amount ?? 0)
                    }
                },

                // Fines Info
                TotalFines = fines.Count,
                TotalFinesAmount = fines.Sum(f => f.Amount ?? 0),
                FinesWithAttendance = fines.Count(f => f.Attendance != null),
                FinesWithStudent = fines.Count(f => f.Attendance?.StudentNumNavigation != null),

                // Sample Fines with calculated values
                SampleFines = fines.Take(5).Select(f => new
                {
                    f.FineId,
                    f.Amount,
                    HasAttendance = f.Attendance != null,
                    AttendanceId = f.AttendanceId,
                    StudentNum = f.Attendance?.StudentNum ?? "(NULL)",
                    StudentName = f.Attendance?.StudentNumNavigation != null
                        ? $"{f.Attendance.StudentNumNavigation.StudentFn} {f.Attendance.StudentNumNavigation.StudentLn}"
                        : "(No Student Linked)",
                    StudentCourse = f.Attendance?.StudentNumNavigation?.Course ?? "(NULL)",
                    StudentYearLevel = f.Attendance?.StudentNumNavigation?.YearLevelSection ?? "(NULL)",
                    ExtractedYear = f.Attendance?.StudentNumNavigation != null ? ExtractYearLevel(f.Attendance.StudentNumNavigation.YearLevelSection) : 0,
                    IsBSIT = f.Attendance?.StudentNumNavigation != null && IsBSIT(f.Attendance.StudentNumNavigation),
                    IsDIT = f.Attendance?.StudentNumNavigation != null && IsDIT(f.Attendance.StudentNumNavigation)
                }).ToList()
            };

            return Json(debugInfo);
        }

        // =========================================================
        // HELPER: Populate ViewBag with Dashboard Data
        // =========================================================
        private async Task PopulateDashboardData()
        {
            // Populate Dropdowns
            ViewBag.FeeCategories = await _context.Fees.Select(f => f.FeeName).Distinct().OrderBy(n => n).ToListAsync();
            var events = await _context.Events.Select(e => e.EventName).Distinct().ToListAsync();
            var manual = await _context.Fines.Where(f => f.AttendanceId == null).Select(f => f.Description).Distinct().ToListAsync();
            ViewBag.FineCategories = events.Concat(manual).Distinct().OrderBy(n => n).ToList();

            // Get Data
            var data = await BuildDashboardData(null, null);

            // Populate Enrollment Data (Strictly preserved)
            ViewBag.StudentsPerProgram = data.StudentsPerProgram;
            ViewBag.TotalStudents = data.TotalStudents;
            ViewBag.YearLevelCounts = data.YearLevelCounts;

            // Populate New Financial Data
            ViewBag.FeesData = data.FeesOverview;
            ViewBag.FinesData = data.FinesOverview;

            ViewBag.CurrentAcademicYear = await GetCurrentAcademicYear();
        }


        // =========================================================
        // HELPER: Build Dashboard Data Object
        // =========================================================
        // =========================================================
        // HELPER: Build Dashboard Data Object (UPDATED FOR MANUAL FINES)
        // =========================================================
        private async Task<DashboardDataModel> BuildDashboardData(string? feeCategory, string? fineCategory)
        {
            // 1. FETCH ALL STUDENTS (Unfiltered for Enrollment Section)
            // This ensures Student Enrollment numbers NEVER change based on fee filters
            var allStudents = await _context.Students.Where(s => s.IsArchived != true).ToListAsync();

            // 2. FETCH FINANCIAL DATA (Filtered)
            var feesQuery = _context.Fees.Include(f => f.StudentNumNavigation).AsQueryable();
            if (!string.IsNullOrEmpty(feeCategory) && feeCategory != "all")
            {
                feesQuery = feesQuery.Where(f => f.FeeName == feeCategory);
            }
            var fees = await feesQuery.ToListAsync();

            var finesQuery = _context.Fines
                .Include(f => f.StudentNumNavigation)
                .Include(f => f.Attendance).ThenInclude(a => a.StudentNumNavigation)
                .Include(f => f.Attendance).ThenInclude(a => a.Event)
                .AsQueryable();

            if (!string.IsNullOrEmpty(fineCategory) && fineCategory != "all")
            {
                if (fineCategory == "manual") finesQuery = finesQuery.Where(f => f.AttendanceId == null);
                else if (fineCategory == "event") finesQuery = finesQuery.Where(f => f.AttendanceId != null);
                else finesQuery = finesQuery.Where(f => (f.Attendance != null && f.Attendance.Event.EventName == fineCategory) || (f.AttendanceId == null && f.Description == fineCategory));
            }
            var fines = await finesQuery.ToListAsync();

            // 3. HELPERS
            int ExtractYearLevel(string? yearLevelSection)
            {
                if (string.IsNullOrWhiteSpace(yearLevelSection)) return 0;
                var input = yearLevelSection.Trim().ToUpper();
                if (input.Contains("FIRST") || input.Contains("1ST")) return 1;
                if (input.Contains("SECOND") || input.Contains("2ND")) return 2;
                if (input.Contains("THIRD") || input.Contains("3RD")) return 3;
                if (input.Contains("FOURTH") || input.Contains("4TH")) return 4;
                foreach (char c in input) { if (char.IsDigit(c)) { int year = c - '0'; if (year >= 1 && year <= 4) return year; } }
                return 0;
            }

            bool IsBSIT(Student s) => !string.IsNullOrWhiteSpace(s.Course) && s.Course.ToUpper().Contains("BSIT");
            bool IsDIT(Student s) => !string.IsNullOrWhiteSpace(s.Course) && s.Course.ToUpper().Contains("DIT");

            // 4. CALCULATE ENROLLMENT (Logic Preserved)
            var bsitCount = allStudents.Count(IsBSIT);
            var ditCount = allStudents.Count(IsDIT);

            var yearLevelCounts = new Dictionary<string, int>
    {
        { "BSIT1", allStudents.Count(s => IsBSIT(s) && ExtractYearLevel(s.YearLevelSection) == 1) },
        { "BSIT2", allStudents.Count(s => IsBSIT(s) && ExtractYearLevel(s.YearLevelSection) == 2) },
        { "BSIT3", allStudents.Count(s => IsBSIT(s) && ExtractYearLevel(s.YearLevelSection) == 3) },
        { "BSIT4", allStudents.Count(s => IsBSIT(s) && ExtractYearLevel(s.YearLevelSection) == 4) },
        { "DIT1", allStudents.Count(s => IsDIT(s) && ExtractYearLevel(s.YearLevelSection) == 1) },
        { "DIT2", allStudents.Count(s => IsDIT(s) && ExtractYearLevel(s.YearLevelSection) == 2) },
        { "DIT3", allStudents.Count(s => IsDIT(s) && ExtractYearLevel(s.YearLevelSection) == 3) }
    };

            // 5. CALCULATE FEES (Paid vs Unpaid)
            decimal GetFeeAmount(Func<Student, bool> programCheck, int yearLevel, bool isPaid)
            {
                return fees.Where(f => f.StudentNumNavigation != null && programCheck(f.StudentNumNavigation) &&
                    ExtractYearLevel(f.StudentNumNavigation.YearLevelSection) == yearLevel &&
                    (isPaid
                        ? (f.FeeStatus == "Paid" || f.FeeStatus == "Completed")
                        : (f.FeeStatus != "Paid" && f.FeeStatus != "Completed")))
                    .Sum(f => f.Amount ?? 0);
            }

            var feesOverview = new FinancialOverviewModel
            {
                PaidByProgram = new Dictionary<string, decimal> {
            { "BSIT1", GetFeeAmount(IsBSIT, 1, true) }, { "BSIT2", GetFeeAmount(IsBSIT, 2, true) }, { "BSIT3", GetFeeAmount(IsBSIT, 3, true) }, { "BSIT4", GetFeeAmount(IsBSIT, 4, true) },
            { "DIT1", GetFeeAmount(IsDIT, 1, true) }, { "DIT2", GetFeeAmount(IsDIT, 2, true) }, { "DIT3", GetFeeAmount(IsDIT, 3, true) }
        },
                UnpaidByProgram = new Dictionary<string, decimal> {
            { "BSIT1", GetFeeAmount(IsBSIT, 1, false) }, { "BSIT2", GetFeeAmount(IsBSIT, 2, false) }, { "BSIT3", GetFeeAmount(IsBSIT, 3, false) }, { "BSIT4", GetFeeAmount(IsBSIT, 4, false) },
            { "DIT1", GetFeeAmount(IsDIT, 1, false) }, { "DIT2", GetFeeAmount(IsDIT, 2, false) }, { "DIT3", GetFeeAmount(IsDIT, 3, false) }
        }
            };
            feesOverview.TotalExpected = fees.Sum(f => f.Amount ?? 0);
            feesOverview.TotalCollected = fees.Where(f => f.FeeStatus == "Paid" || f.FeeStatus == "Completed").Sum(f => f.Amount ?? 0);
            feesOverview.TotalPending = feesOverview.TotalExpected - feesOverview.TotalCollected;
            feesOverview.CollectionRate = feesOverview.TotalExpected > 0 ? Math.Round((feesOverview.TotalCollected / feesOverview.TotalExpected) * 100, 1) : 0;

            // 6. CALCULATE FINES (Paid vs Unpaid)
            decimal GetFineAmount(Func<Student, bool> programCheck, int yearLevel, bool isPaid)
            {
                return fines.Where(f => {
                    var s = f.StudentNumNavigation ?? f.Attendance?.StudentNumNavigation;
                    if (s == null) return false;
                    bool finePaid = (f.FinesStatus == "Paid" || f.FinesStatus == "Completed");
                    return programCheck(s) && ExtractYearLevel(s.YearLevelSection) == yearLevel && (isPaid == finePaid);
                }).Sum(f => f.Amount ?? 0);
            }

            var finesOverview = new FinancialOverviewModel
            {
                PaidByProgram = new Dictionary<string, decimal> {
            { "BSIT1", GetFineAmount(IsBSIT, 1, true) }, { "BSIT2", GetFineAmount(IsBSIT, 2, true) }, { "BSIT3", GetFineAmount(IsBSIT, 3, true) }, { "BSIT4", GetFineAmount(IsBSIT, 4, true) },
            { "DIT1", GetFineAmount(IsDIT, 1, true) }, { "DIT2", GetFineAmount(IsDIT, 2, true) }, { "DIT3", GetFineAmount(IsDIT, 3, true) }
        },
                UnpaidByProgram = new Dictionary<string, decimal> {
            { "BSIT1", GetFineAmount(IsBSIT, 1, false) }, { "BSIT2", GetFineAmount(IsBSIT, 2, false) }, { "BSIT3", GetFineAmount(IsBSIT, 3, false) }, { "BSIT4", GetFineAmount(IsBSIT, 4, false) },
            { "DIT1", GetFineAmount(IsDIT, 1, false) }, { "DIT2", GetFineAmount(IsDIT, 2, false) }, { "DIT3", GetFineAmount(IsDIT, 3, false) }
        }
            };
            finesOverview.TotalExpected = fines.Sum(f => f.Amount ?? 0);
            finesOverview.TotalCollected = fines.Where(f => f.FinesStatus == "Paid" || f.FinesStatus == "Completed").Sum(f => f.Amount ?? 0);
            finesOverview.TotalPending = finesOverview.TotalExpected - finesOverview.TotalCollected;
            finesOverview.CollectionRate = finesOverview.TotalExpected > 0 ? Math.Round((finesOverview.TotalCollected / finesOverview.TotalExpected) * 100, 1) : 0;

            return new DashboardDataModel
            {
                StudentsPerProgram = new Dictionary<string, int> { { "BSIT", bsitCount }, { "DIT", ditCount } },
                TotalStudents = bsitCount + ditCount,
                YearLevelCounts = yearLevelCounts,
                FeesOverview = feesOverview,
                FinesOverview = finesOverview,
                // Fill other properties to prevent null errors
                MonthlyEvents = new int[0],
                EventStatus = new Dictionary<string, int>(),
                MonthlyActiveStudents = new int[0],
                ActiveInactive = new Dictionary<string, int>(),
                ArchiveCount = 0
            };
        }

        // Dashboard Data Model - Using Dictionary for consistent JSON property casing
        private class DashboardDataModel
        {
            public Dictionary<string, int> StudentsPerProgram { get; set; }
            public int TotalStudents { get; set; }
            public Dictionary<string, int> YearLevelCounts { get; set; }
            public int[] MonthlyActiveStudents { get; set; }
            public int ArchiveCount { get; set; }
            public Dictionary<string, int> ActiveInactive { get; set; }

            // Replaced old Payments/Fines dictionaries with these objects
            public FinancialOverviewModel FeesOverview { get; set; }
            public FinancialOverviewModel FinesOverview { get; set; }

            public int[] MonthlyEvents { get; set; }
            public Dictionary<string, int> EventStatus { get; set; }
        }

        public class FinancialOverviewModel
        {
            public Dictionary<string, decimal> PaidByProgram { get; set; }
            public Dictionary<string, decimal> UnpaidByProgram { get; set; }
            public decimal TotalExpected { get; set; }
            public decimal TotalCollected { get; set; }
            public decimal TotalPending { get; set; }
            public decimal CollectionRate { get; set; }
        }


        // =========================================================
        // 2. STUDENT RECORDS PAGE
        // =========================================================
        public async Task<IActionResult> StudentRecords(
            string searchString,
            string programFilter,
            string yearFilter,
            string sectionFilter,
            string typeFilter,
            string statusFilter,
            string roleFilter,
            string sortOrder,
            int pageNumber = 1,
            int pageSize = 10)
        {
            ViewBag.CurrentFilter = searchString;
            ViewBag.ProgramFilter = programFilter;
            ViewBag.YearFilter = yearFilter;
            ViewBag.SectionFilter = sectionFilter;
            ViewBag.TypeFilter = typeFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.PageSize = pageSize;

            // Use the consolidated helper method to get the base query
            var studentsQuery = GetFilteredStudentsQuery(searchString, programFilter, yearFilter, sectionFilter, typeFilter, statusFilter, roleFilter);

            // Apply sorting
            switch (sortOrder)
            {
                case "name_desc": studentsQuery = studentsQuery.OrderByDescending(s => s.StudentLn); break;
                case "id_asc": studentsQuery = studentsQuery.OrderBy(s => s.StudentNum); break;
                case "id_desc": studentsQuery = studentsQuery.OrderByDescending(s => s.StudentNum); break;
                default: studentsQuery = studentsQuery.OrderBy(s => s.StudentLn); break;
            }

            var pagedStudents = await PagedList<Student>.CreateAsync(studentsQuery, pageNumber, pageSize);
            await PopulateFilterDropdowns();

            // The summary counts can be simplified or adjusted as needed
            ViewBag.TotalStudents = await _context.Students.CountAsync();

            ViewBag.TotalOfficers = await _context.Students.CountAsync(s => s.Officer != null && s.Officer.Classification == "Org Officer");


            ViewBag.TotalClassOfficers = await _context.Students.CountAsync(s => s.Officer != null && s.Officer.Classification == "Class Officer");
            ViewBag.ActiveMembers = await _context.Students.CountAsync(s => s.Classification == "Active");
            ViewBag.ArchivedMembers = await _context.Students.CountAsync(s => s.Classification == "Archived");

            ViewBag.CurrentAcademicYear = await GetCurrentAcademicYear();
            ViewBag.AcademicYearOptions = GenerateAcademicYearOptions();

            return View(pagedStudents);

        }

        [HttpGet]
        public async Task<IActionResult> GetStudentDetails(string id)
        {
            var student = await _context.Students
                .Include(s => s.Officer)
                .Include(s => s.Fees)
                .Include(s => s.Attendances).ThenInclude(a => a.Event)
                .Include(s => s.Attendances).ThenInclude(a => a.Fines)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentNum == id);

            if (student == null) return NotFound();

            var allEventsCount = await _context.Events.CountAsync();
            var attendedCount = student.Attendances.Count(a => a.AttendanceStatus == "Present");
            var allFines = student.Attendances.SelectMany(a => a.Fines).ToList();

            var totalFeesAmount = student.Fees.Sum(f => f.Amount ?? 0);
            var totalFinesAmount = allFines.Sum(f => f.Amount ?? 0);
            var paidFees = student.Fees.Where(f => f.FeeStatus == "Paid").Sum(f => f.Amount ?? 0);
            var paidFines = allFines.Where(f => f.FinesStatus == "Paid").Sum(f => f.Amount ?? 0);
            var totalPaid = paidFees + paidFines;

            var viewModel = new StudentDetailsViewModel
            {
                Student = student,
                FullName = student.FullName,
                Role = student.Officer?.Position ?? "Member",
                TotalEvents = allEventsCount,
                EventsAttended = attendedCount,
                AttendanceRate = allEventsCount > 0 ? ((double)attendedCount / allEventsCount) * 100 : 0,
                Fees = student.Fees.ToList(),
                TotalFees = totalFeesAmount,
                TotalFines = totalFinesAmount,
                TotalPaid = totalPaid,
                Balance = (totalFeesAmount + totalFinesAmount) - totalPaid,
                AttendanceHistory = student.Attendances.Select(a => new AttendanceRecord
                {
                    EventName = a.Event?.EventName ?? "N/A",
                    Date = a.Event?.EventDate,
                    Status = a.AttendanceStatus ?? "N/A",
                    HasFine = a.Fines.Any()
                }).OrderByDescending(a => a.Date).ToList(),
                Fines = allFines.Select(f => new FineDisplay
                {
                    EventName = f.Attendance?.Event?.EventName ?? "N/A",
                    Amount = f.Amount ?? 0,
                    Status = f.FinesStatus ?? "N/A",
                    DueDate = f.FinesDueDate
                }).ToList()
            };

            return PartialView("_StudentDetailsPartial", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStudent(Student student)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid data submitted.";
                return RedirectToAction(nameof(StudentRecords));
            }

            var studentToUpdate = await _context.Students.FindAsync(student.StudentNum);
            if (studentToUpdate == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToAction(nameof(StudentRecords));
            }

            studentToUpdate.StudentFn = student.StudentFn;
            studentToUpdate.StudentMn = student.StudentMn;
            studentToUpdate.StudentLn = student.StudentLn;
            studentToUpdate.StudentEmail = student.StudentEmail;
            studentToUpdate.Course = student.Course;
            studentToUpdate.YearLevelSection = student.YearLevelSection;
            studentToUpdate.StudentType = student.StudentType;
            studentToUpdate.Birthday = student.Birthday;

            try
            {
                _context.Students.Update(studentToUpdate);
                await _context.SaveChangesAsync();
                await LogAction("Update Student", $"Updated details for {student.StudentNum}");
                TempData["Message"] = "Student record updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student {StudentNum}", student.StudentNum);
                TempData["Error"] = "An error occurred while saving the record.";
            }

            return RedirectToAction(nameof(StudentRecords));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            if (string.IsNullOrEmpty(id)) { TempData["Error"] = "Student ID was not provided."; return RedirectToAction("Index"); }
            var user = await _userManager.FindByNameAsync(id);
            if (user == null) { TempData["Error"] = $"User with ID '{id}' not found."; return RedirectToAction("Index"); }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, id);

            if (result.Succeeded)
            {
                await LogAction("Reset Password", $"Reset password for user {id}.");
                TempData["Message"] = $"Password for {id} has been reset to default.";
            }
            else
            {
                var errorDescriptions = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Error resetting password for {UserId}: {Errors}", id, errorDescriptions);
                TempData["Error"] = $"Could not reset password for {id}. Errors: {errorDescriptions}";
            }

            string referringUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referringUrl) && (referringUrl.Contains("/Admin/StudentRecords") || referringUrl.Contains("/Admin/Index")))
            {
                return Redirect(referringUrl);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(Student student)
        {
            if (string.IsNullOrWhiteSpace(student.StudentNum))
            {
                TempData["Error"] = "Student Number is required.";
                return RedirectToAction(nameof(StudentRecords));
            }

            if (await _context.Students.AnyAsync(s => s.StudentNum == student.StudentNum))
            {
                TempData["Error"] = $"Student ID {student.StudentNum} already exists.";
                return RedirectToAction(nameof(StudentRecords));
            }

            try
            {
                var user = new IdentityUser { UserName = student.StudentNum, Email = student.StudentEmail };
                var result = await _userManager.CreateAsync(user, student.StudentNum);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Member");

                    student.Classification = "Active";
                    student.IsArchived = false;
                    student.Qrcode = student.StudentNum;

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();
                    await LogAction("Create Student", $"Created student {student.StudentNum}");
                    TempData["Message"] = "Student registered successfully.";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Failed to create user account: {errors}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student {StudentNum}", student.StudentNum);
                TempData["Error"] = "An error occurred while creating the student.";
            }

            return RedirectToAction(nameof(StudentRecords));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveStudent(string id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToAction(nameof(StudentRecords));
            }

            // Get Current Academic Year to tag the archive record
            var currentAcadYear = await GetCurrentAcademicYear();

            if (student.Classification == "Active" || string.IsNullOrEmpty(student.Classification))
            {
                // ARCHIVE ACTION
                student.Classification = "Archived";
                student.IsArchived = true;
                student.ArchiveStatus = "Manually Archived";
                student.ArchiveDate = DateOnly.FromDateTime(DateTime.Now);

                // IMPORTANT: Ensure SchoolYearEnrolled matches the current filter logic
                // If it's null, set it to current so it shows up in the latest archive list
                if (string.IsNullOrEmpty(student.SchoolYearEnrolled))
                {
                    student.SchoolYearEnrolled = currentAcadYear;
                }

                await LogAction("Archive Student", $"Archived student {id}");
                TempData["Message"] = $"Student {id} has been archived.";
            }
            else
            {
                // ACTIVATE ACTION
                student.Classification = "Active";
                student.IsArchived = false;
                student.ArchiveStatus = null;
                student.ArchiveDate = null;

                await LogAction("Activate Student", $"Activated student {id}");
                TempData["Message"] = $"Student {id} has been activated.";
            }

            _context.Students.Update(student);
            await _context.SaveChangesAsync();

            string referringUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referringUrl)) return Redirect(referringUrl);
            return RedirectToAction(nameof(StudentRecords));
        }

        // =========================================================
        // CSV IMPORT METHODS
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> AnalyzeCsv(IFormFile file)
        {
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file selected." });
            try
            {
                var fileName = Guid.NewGuid().ToString() + ".csv";
                var filePath = Path.Combine(Path.GetTempPath(), fileName);
                using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }

                string[] headers;
                using (var reader = new StreamReader(filePath))
                {
                    var headerLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(headerLine)) return Json(new { success = false, message = "CSV file is empty." });
                    headers = headerLine.Split(',');
                }
                return Json(new { success = true, fileName = fileName, headers = headers });
            }
            catch (Exception ex) { return Json(new { success = false, message = "Error analyzing file: " + ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> PreviewImport(string fileName, Dictionary<string, int> map)
        {
            var filePath = Path.Combine(Path.GetTempPath(), fileName);
            if (!System.IO.File.Exists(filePath)) return NotFound("File session expired.");

            var viewModel = new ImportPreviewViewModel();

            try
            {
                viewModel.Headers = map.Keys.ToList();

                if (viewModel.Headers.Contains("Year") || viewModel.Headers.Contains("Section"))
                {
                    viewModel.Headers.Remove("Year");
                    viewModel.Headers.Remove("Section");
                    if (!viewModel.Headers.Contains("YearLevelSection"))
                    {
                        viewModel.Headers.Add("YearLevelSection");
                    }
                }

                var lines = await System.IO.File.ReadAllLinesAsync(filePath);
                var previewLines = lines.Skip(1).Take(5);

                foreach (var line in previewLines)
                {
                    var cols = line.Split(',');
                    var rowData = new List<string>();
                    var combinedYearSection = "";

                    if (map.ContainsKey("Year") || map.ContainsKey("Section"))
                    {
                        var year = GetValue(cols, map, "Year") ?? "";
                        var section = GetValue(cols, map, "Section") ?? "";
                        combinedYearSection = $"{year}-{section}".Trim('-');
                    }

                    foreach (var header in viewModel.Headers)
                    {
                        if (header == "YearLevelSection")
                        {
                            rowData.Add(string.IsNullOrEmpty(combinedYearSection) ? (GetValue(cols, map, "YearLevelSection") ?? "-") : combinedYearSection);
                        }
                        else
                        {
                            rowData.Add(GetValue(cols, map, header) ?? "-");
                        }
                    }
                    viewModel.PreviewRows.Add(rowData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating import preview for file {FileName}", fileName);
                return StatusCode(500, "Error reading preview. Please check file format and mapping.");
            }

            return PartialView("_ImportPreviewPartial", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteImport(string fileName, Dictionary<string, int> map)
        {
            var filePath = Path.Combine(Path.GetTempPath(), fileName);
            if (!System.IO.File.Exists(filePath)) return Json(new { success = false, message = "Session expired." });

            int successCount = 0;
            int errorCount = 0;
            List<string> errorMessages = new List<string>();

            try
            {
                var lines = await System.IO.File.ReadAllLinesAsync(filePath);

                for (int i = 1; i < lines.Length; i++)
                {
                    string currentRowData = "";
                    try
                    {
                        currentRowData = lines[i];
                        var cols = currentRowData.Split(',');
                        string studentNum = GetValue(cols, map, "StudentNum");

                        if (string.IsNullOrWhiteSpace(studentNum))
                        {
                            errorCount++;
                            errorMessages.Add($"Row {i + 1}: Missing Student Number.");
                            continue;
                        }

                        if (await _context.Students.AnyAsync(s => s.StudentNum == studentNum))
                        {
                            errorCount++;
                            errorMessages.Add($"Row {i + 1}: Student ID {studentNum} already exists.");
                            continue;
                        }

                        var user = new IdentityUser { UserName = studentNum, Email = GetValue(cols, map, "StudentEmail") };
                        var result = await _userManager.CreateAsync(user, studentNum);

                        if (result.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, "Member");

                            string yearLevelSection;
                            if (map.ContainsKey("Year") || map.ContainsKey("Section"))
                            {
                                var year = GetValue(cols, map, "Year") ?? "";
                                var section = GetValue(cols, map, "Section") ?? "";
                                yearLevelSection = $"{year}-{section}".Trim('-');
                            }
                            else
                            {
                                yearLevelSection = GetValue(cols, map, "YearLevelSection");
                            }

                            var student = new Student
                            {
                                StudentNum = studentNum,
                                StudentLn = GetValue(cols, map, "StudentLn") ?? "Unknown",
                                StudentFn = GetValue(cols, map, "StudentFn") ?? "Unknown",
                                StudentMn = GetValue(cols, map, "StudentMn"),
                                YearLevelSection = yearLevelSection,
                                Course = GetValue(cols, map, "Course"),
                                StudentEmail = user.Email,
                                StudentType = GetValue(cols, map, "StudentType") ?? "Regular",
                                Classification = "Active",
                                IsArchived = false,
                                Qrcode = studentNum
                            };

                            if (map.ContainsKey("Birthday"))
                            {
                                string bdayStr = GetValue(cols, map, "Birthday");
                                if (DateOnly.TryParse(bdayStr, out DateOnly bday)) student.Birthday = bday;
                            }

                            _context.Students.Add(student);
                            successCount++;
                        }
                        else
                        {
                            errorCount++;
                            string identityErrors = string.Join(", ", result.Errors.Select(e => e.Description));
                            errorMessages.Add($"Row {i + 1}: System Account Error ({identityErrors})");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errorMessages.Add($"Row {i + 1}: Data Format Error.");
                        _logger.LogError(ex, "Error processing row {RowNumber} during import.", i + 1);
                    }
                }

                await _context.SaveChangesAsync();
                await LogAction("Batch Import", $"Imported {successCount} students. Failed: {errorCount}.");

                System.IO.File.Delete(filePath);

                return Json(new { success = true, imported = successCount, failed = errorCount, errors = errorMessages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during ExecuteImport for file {FileName}", fileName);
                return Json(new { success = false, message = "Critical Server Error: " + ex.Message });
            }
        }

        private string GetValue(string[] cols, Dictionary<string, int> map, string key)
        {
            if (map.ContainsKey(key) && map[key] < cols.Length)
                return cols[map[key]].Trim().Replace("\"", "");
            return null;
        }


        // =========================================================
        // EVENT MANAGEMENT
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Events(int pageNumber = 1, int pageSize = 10)
        {
            // --- ADDITION: Run the Auto-Close Logic ---
            // This checks for expired events and assigns fines immediately when the page loads.
            await ProcessAutoCloseEvents();
            // ------------------------------------------

            ViewBag.PageSize = pageSize;
            var eventsQuery = _context.Events
                .Include(e => e.Attendances)
                .OrderByDescending(e => e.EventDate)
                .AsQueryable();

            var pagedEvents = await PagedList<Event>.CreateAsync(eventsQuery, pageNumber, pageSize);
            return View(pagedEvents);
        }

        private async Task ProcessAutoCloseEvents()
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var timeNow = TimeOnly.FromDateTime(now);

            // 1. Find events that are NOT closed yet, but SHOULD be.
            // Logic: (EndDate is in the past) OR (EndDate is today AND EndTime has passed)
            // Note: If EndDate is null, we fallback to EventDate.
            var eventsToClose = await _context.Events
                .Where(e => !e.IsClosed &&
                       ((e.EndDate ?? e.EventDate) < today ||
                       ((e.EndDate ?? e.EventDate) == today && e.EndTime < timeNow)))
                .ToListAsync();

            if (!eventsToClose.Any()) return; // No events to process

            // 2. Get all active students (Same filter as your CloseEvent action)
            var activeStudents = await _context.Students
                .Where(s => s.IsArchived != true && (s.Classification == null || s.Classification == "Active"))
                .ToListAsync();

            int totalFinesGenerated = 0;

            foreach (var evt in eventsToClose)
            {
                // Get existing attendance for this specific event
                var existingAttendance = await _context.Attendances
                    .Where(a => a.EventId == evt.EventId)
                    .ToListAsync();

                foreach (var student in activeStudents)
                {
                    var attendance = existingAttendance.FirstOrDefault(a => a.StudentNum == student.StudentNum);

                    // A. If no record exists, create one as "Absent"
                    if (attendance == null)
                    {
                        attendance = new Attendance
                        {
                            StudentNum = student.StudentNum,
                            EventId = evt.EventId,
                            AttendanceStatus = "Absent"
                        };
                        _context.Attendances.Add(attendance);
                        // We save immediately so we get an AttendanceId for the fine generation
                        await _context.SaveChangesAsync();
                    }

                    // B. Generate fine if status is "Absent"
                    // We assume GenerateFineForAttendance exists in your controller based on your provided code
                    if (attendance.AttendanceStatus == "Absent" || attendance.AttendanceStatus == "Excused")
                    {
                        bool fineCreated = await GenerateFineForAttendance(attendance.AttendanceId, notifyStudent: true);
                        if (fineCreated) totalFinesGenerated++;
                    }
                }

                // C. Mark the event as closed in DB so we don't process it again
                evt.IsClosed = true;
                _context.Events.Update(evt);

                // Log the auto-closure
                await LogAction("Auto-Close Event", $"System automatically closed event '{evt.EventName}' and assigned fines.");
            }

            // Final save to commit the IsClosed status
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event newEvent)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    newEvent.FineForMember ??= 0;
                    newEvent.FineForClassOfficer ??= 0;
                    newEvent.FineForOrgOfficer ??= 0;
                    newEvent.NonIbitsFineForMember ??= 0;
                    newEvent.NonIbitsFineForClassOfficer ??= 0;
                    newEvent.NonIbitsFineForOrgOfficer ??= 0;

                    _context.Events.Add(newEvent);
                    await _context.SaveChangesAsync();
                    await LogAction("Create Event", $"Created event: {newEvent.EventName}");
                    TempData["Message"] = "Event created successfully.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating event");
                    TempData["Error"] = "Failed to create event. Please check inputs.";
                }
            }
            else
            {
                TempData["Error"] = "Invalid data. Please check required fields.";
            }
            return RedirectToAction("Events");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(Event updatedEvent)
        {
            try
            {
                var eventToUpdate = await _context.Events.FindAsync(updatedEvent.EventId);
                if (eventToUpdate == null)
                {
                    TempData["Error"] = "Event not found.";
                    return RedirectToAction("Events");
                }

                eventToUpdate.EventName = updatedEvent.EventName;
                eventToUpdate.EventLocation = updatedEvent.EventLocation;
                eventToUpdate.EventDate = updatedEvent.EventDate;
                eventToUpdate.StartTime = updatedEvent.StartTime;
                eventToUpdate.EndDate = updatedEvent.EndDate;
                eventToUpdate.EndTime = updatedEvent.EndTime;
                eventToUpdate.EventDuration = updatedEvent.EventDuration;
                eventToUpdate.AcadYear = updatedEvent.AcadYear;
                eventToUpdate.EventDesc = updatedEvent.EventDesc;
                eventToUpdate.EventType = updatedEvent.EventType;
                eventToUpdate.FineForMember = updatedEvent.FineForMember;
                eventToUpdate.FineForClassOfficer = updatedEvent.FineForClassOfficer;
                eventToUpdate.FineForOrgOfficer = updatedEvent.FineForOrgOfficer;
                eventToUpdate.NonIbitsFineForMember = updatedEvent.NonIbitsFineForMember;
                eventToUpdate.NonIbitsFineForClassOfficer = updatedEvent.NonIbitsFineForClassOfficer;
                eventToUpdate.NonIbitsFineForOrgOfficer = updatedEvent.NonIbitsFineForOrgOfficer;

                _context.Events.Update(eventToUpdate);
                await _context.SaveChangesAsync();
                await LogAction("Update Event", $"Updated event ID: {updatedEvent.EventId}");
                TempData["Message"] = "Event updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event.");
                TempData["Error"] = "An error occurred while updating the event.";
            }
            return RedirectToAction("Events");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int eventId, AdminActionConfirmation confirmation)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || string.IsNullOrEmpty(confirmation.Password))
            {
                TempData["Error"] = "Password is required to delete an event.";
                return RedirectToAction("Events");
            }

            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(currentUser, confirmation.Password, false);
            if (!passwordCheck.Succeeded)
            {
                TempData["Error"] = "Invalid password. Event deletion cancelled.";
                return RedirectToAction("Events");
            }

            var eventToDelete = await _context.Events.Include(e => e.Attendances).FirstOrDefaultAsync(e => e.EventId == eventId);
            if (eventToDelete == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("Events");
            }

            try
            {
                int attendanceCount = eventToDelete.Attendances?.Count ?? 0;
                _context.Events.Remove(eventToDelete);
                await _context.SaveChangesAsync();
                await LogAction("Delete Event", $"Deleted event: {eventToDelete.EventName} (affected {attendanceCount} attendees)");
                TempData["Message"] = "Event deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event.");
                TempData["Error"] = "Error deleting event. It may be referenced by other records.";
            }
            return RedirectToAction("Events");
        }


        // =========================================================
        // ACTION: CLOSE EVENT & ASSIGN FINES
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseEvent(int eventId, string password)
        {
            // 1. Password Verification
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Session expired.";
                return RedirectToAction("Events");
            }

            if (string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Password is required to close an event.";
                return RedirectToAction("Events");
            }

            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            if (!passwordCheck.Succeeded)
            {
                TempData["Error"] = "Invalid password. Action cancelled.";
                return RedirectToAction("Events");
            }

            try
            {
                var eventToClose = await _context.Events.FindAsync(eventId);
                if (eventToClose == null)
                {
                    TempData["Error"] = "Event not found.";
                    return RedirectToAction("Events");
                }

                // 2. Mark Event as Closed (Updates Status to Completed)
                eventToClose.IsClosed = true;
                _context.Events.Update(eventToClose);

                // 3. Get all active students
                var activeStudents = await _context.Students
                    .Include(s => s.Officer)
                    .Where(s => s.IsArchived != true && (s.Classification == null || s.Classification == "Active"))
                    .ToListAsync();

                // 4. Get existing attendance records for this event
                var existingAttendance = await _context.Attendances
                    .Where(a => a.EventId == eventId)
                    .ToListAsync();

                int finesGenerated = 0;
                int attendanceCreated = 0;

                foreach (var student in activeStudents)
                {
                    var attendance = existingAttendance.FirstOrDefault(a => a.StudentNum == student.StudentNum);

                    // If no record exists, create one as "Absent"
                    if (attendance == null)
                    {
                        attendance = new Attendance
                        {
                            StudentNum = student.StudentNum,
                            EventId = eventId,
                            AttendanceStatus = "Absent"
                        };
                        _context.Attendances.Add(attendance);
                        // Save individually to get ID for fine generation
                        await _context.SaveChangesAsync();
                        attendanceCreated++;
                    }

                    // Generate fine if status is "Absent" or "Excused"
                    if (attendance.AttendanceStatus == "Absent" || attendance.AttendanceStatus == "Excused")
                    {
                        bool fineCreated = await GenerateFineForAttendance(attendance.AttendanceId, notifyStudent: true);
                        if (fineCreated) finesGenerated++;
                    }
                }

                // Save the Event Update (IsClosed)
                await _context.SaveChangesAsync();

                await LogAction("Close Event",
                    $"Closed event '{eventToClose.EventName}'. Marked {attendanceCreated} absent and issued {finesGenerated} fines.");

                TempData["Message"] = $"Event closed. Status set to Completed. {attendanceCreated} students marked absent. {finesGenerated} fines issued.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing event and assigning fines");
                TempData["Error"] = "An error occurred while closing the event.";
            }

            return RedirectToAction("Events");
        }


        // =========================================================
        // FINE MANAGEMENT - ENHANCED AUTO-GENERATION
        // =========================================================

        /// <summary>
        /// Generates a fine for an attendance record if the student is absent or excused.
        /// </summary>
        private async Task<bool> GenerateFineForAttendance(int attendanceId, bool notifyStudent = true)
        {
            try
            {
                // Load attendance with all necessary relationships for fine calculation
                var attendance = await _context.Attendances
                    .Include(a => a.StudentNumNavigation).ThenInclude(s => s.Officer)
                    .Include(a => a.Event)
                    .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

                if (attendance == null || (attendance.AttendanceStatus != "Absent" && attendance.AttendanceStatus != "Excused"))
                    return false;

                // Prevent duplicate fines for the same attendance record
                bool alreadyHasFine = await _context.Fines.AnyAsync(f => f.AttendanceId == attendanceId);
                if (alreadyHasFine) return false;

                decimal fineAmount = CalculateFineAmount(attendance);
                if (fineAmount <= 0) return false;

                var fine = new Fine
                {
                    AttendanceId = attendanceId,
                    StudentNum = attendance.StudentNum, // FIX: Set StudentNum for proper navigation
                    Amount = fineAmount,
                    FinesStatus = "Unpaid",
                    FinesStartDate = DateOnly.FromDateTime(DateTime.Now),
                    FinesDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(14)) // Default 2 weeks due date
                };

                _context.Fines.Add(fine);
                await _context.SaveChangesAsync();

                if (notifyStudent && !string.IsNullOrEmpty(attendance.StudentNum))
                {
                    await SendFineNotification(attendance.StudentNum, fine, attendance.Event?.EventName ?? "Event");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fine generation failed for Attendance {attendanceId}");
                return false;
            }
        }

        private decimal CalculateFineAmount(Attendance attendance)
        {
            if (attendance.Event == null || attendance.StudentNumNavigation == null)
                return 0;

            var student = attendance.StudentNumNavigation;
            var evt = attendance.Event;

            // Determine the student's role classification
            string roleType = "Member";
            if (student.Officer != null)
            {
                // Use the classification from the Officer table
                if (student.Officer.Classification == "Class Officer")
                    roleType = "Class Officer";
                else if (student.Officer.Classification == "Org Officer")
                    roleType = "Org Officer";
            }

            // Assign fine based on Event Type and Role Type
            bool isIbits = evt.EventType == "iBITS Event";

            return roleType switch
            {
                "Org Officer" => isIbits ? (evt.FineForOrgOfficer ?? 0) : (evt.NonIbitsFineForOrgOfficer ?? 0),
                "Class Officer" => isIbits ? (evt.FineForClassOfficer ?? 0) : (evt.NonIbitsFineForClassOfficer ?? 0),
                _ => isIbits ? (evt.FineForMember ?? 0) : (evt.NonIbitsFineForMember ?? 0)
            };
        }

        private async Task SendFineNotification(string studentNum, Fine fine, string eventName)
        {
            try
            {
                var notification = new Notification
                {
                    StudentNum = studentNum,
                    Title = "Fine Issued",
                    Message = $"A fine of ₱{fine.Amount} has been issued for your absence at '{eventName}'. " +
                             $"Due date: {fine.FinesDueDate?.ToString("MMMM dd, yyyy")}. Please settle this at your earliest convenience.",
                    NotificationType = "Fine",
                    NotificationDate = DateTime.Now,
                    IsRead = false,
                    SentBy = "System"
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending fine notification to {studentNum}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttendanceStatus(int attendanceId, string newStatus)
        {
            try
            {
                var attendance = await _context.Attendances
                    .Include(a => a.StudentNumNavigation)
                    .Include(a => a.Event)
                    .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

                if (attendance == null)
                    return Json(new { success = false, message = "Attendance record not found." });

                string oldStatus = attendance.AttendanceStatus ?? "Unknown";
                attendance.AttendanceStatus = newStatus;
                _context.Update(attendance);
                await _context.SaveChangesAsync();

                await LogAction("Update Attendance",
                    $"Changed attendance for {attendance.StudentNumNavigation?.FullName} " +
                    $"at '{attendance.Event?.EventName}' from '{oldStatus}' to '{newStatus}'");

                bool fineGenerated = false;
                if (newStatus == "Absent" || newStatus == "Excused")
                {
                    fineGenerated = await GenerateFineForAttendance(attendanceId, true);
                }
                else if (oldStatus == "Absent" || oldStatus == "Excused")
                {
                    var existingFine = await _context.Fines
                        .FirstOrDefaultAsync(f => f.AttendanceId == attendanceId && f.FinesStatus == "Unpaid");

                    if (existingFine != null)
                    {
                        _context.Fines.Remove(existingFine);
                        await _context.SaveChangesAsync();
                        await LogAction("Remove Fine", $"Removed unpaid fine after status changed to '{newStatus}'");
                    }
                }

                string message = $"Attendance status updated to '{newStatus}'.";
                if (fineGenerated)
                    message += " A fine has been automatically generated.";

                return Json(new { success = true, message, fineGenerated });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating attendance status for ID {attendanceId}");
                return Json(new { success = false, message = "An error occurred while updating attendance." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateFinesForEvent(int eventId)
        {
            try
            {
                var eventRecord = await _context.Events.FindAsync(eventId);
                if (eventRecord == null)
                    return Json(new { success = false, message = "Event not found." });

                var absentAttendances = await _context.Attendances
                    .Where(a => a.EventId == eventId &&
                               (a.AttendanceStatus == "Absent" || a.AttendanceStatus == "Excused"))
                    .ToListAsync();

                int finesGenerated = 0;
                foreach (var attendance in absentAttendances)
                {
                    bool created = await GenerateFineForAttendance(attendance.AttendanceId, true);
                    if (created) finesGenerated++;
                }

                await LogAction("Bulk Generate Fines", $"Generated {finesGenerated} fines for event '{eventRecord.EventName}'");
                return Json(new { success = true, message = $"Successfully generated {finesGenerated} fine(s).", finesGenerated });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating fines for event ID {eventId}");
                return Json(new { success = false, message = "An error occurred while generating fines." });
            }
        }

        // =========================================================
        // OTHER PAGES & UTILITIES
        // =========================================================
        public async Task<IActionResult> ActivityLogs()
        {
            var logs = await _context.ActivityLogs.OrderByDescending(l => l.Timestamp).ToListAsync();
            return View(logs);
        }

        public async Task<IActionResult> Announcements()
        {
            var announcements = await _context.Announcements.OrderByDescending(a => a.Timestamp).ToListAsync();
            return View(announcements);
        }

        // =========================================================
        // ATTENDANCE - VIEW ONLY WITH FILTERS (OPTIMIZED)
        // =========================================================
        public async Task<IActionResult> Attendance(string? eventFilter, string? programFilter, string? yearFilter)
        {
            // Start with base query including navigation properties
            var query = _context.Attendances
                .Include(a => a.Event)
                .Include(a => a.StudentNumNavigation)
                .AsQueryable();

            // Apply Event filter (database-side)
            if (!string.IsNullOrEmpty(eventFilter) && int.TryParse(eventFilter, out int eventId))
            {
                query = query.Where(a => a.EventId == eventId);
            }

            // Apply Program filter (database-side)
            if (!string.IsNullOrEmpty(programFilter))
            {
                query = query.Where(a => a.StudentNumNavigation != null &&
                                            a.StudentNumNavigation.Course != null &&
                                            a.StudentNumNavigation.Course.ToUpper().Contains(programFilter.ToUpper()));
            }

            // Apply Year filter (database-side)
            if (!string.IsNullOrEmpty(yearFilter))
            {
                // This pattern matches year levels like "3-", "BSIT 3-", "DIT 3-", etc.
                string yearPattern = yearFilter + "-";
                query = query.Where(a => a.StudentNumNavigation != null &&
                                            a.StudentNumNavigation.YearLevelSection != null &&
                                            (a.StudentNumNavigation.YearLevelSection.StartsWith(yearPattern) ||
                                            a.StudentNumNavigation.YearLevelSection.Contains(" " + yearPattern)));
            }

            // Execute the final, filtered query on the database
            var result = await query
                .OrderByDescending(a => a.Event != null ? a.Event.EventDate : DateOnly.MinValue)
                .ToListAsync();

            // Populate dropdowns for the view
            await PopulateAttendanceFilters();
            ViewData["EventFilter"] = eventFilter;
            ViewData["ProgramFilter"] = programFilter;
            ViewData["YearFilter"] = yearFilter;

            return View(result);
        }

        // Helper method to populate attendance filter dropdowns
        private async Task PopulateAttendanceFilters()
        {
            // Get all events for dropdown
            ViewBag.Events = await _context.Events
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            // Get distinct programs
            ViewBag.Programs = await _context.Students
                .Where(s => s.Course != null)
                .Select(s => s.Course)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Get distinct years
            var allYearSections = await _context.Students
                .Where(s => s.YearLevelSection != null)
                .Select(s => s.YearLevelSection)
                .Distinct()
                .ToListAsync();

            ViewBag.Years = allYearSections
                .Select(ys => ExtractYearFromYearLevelSection(ys))
                .Where(y => !string.IsNullOrEmpty(y))
                .Distinct()
                .OrderBy(y => y)
                .ToList();
        }

        // =========================================================
        // PAYMENTS PAGE (FEES ONLY) (OPTIMIZED)
        // =========================================================
        public async Task<IActionResult> Payments()
        {
            // Fetch Fees for the main table view
            var fees = await _context.Fees
                .Include(f => f.StudentNumNavigation)
                .OrderBy(f => f.FeeStatus)
                .ThenByDescending(f => f.FeesDueDate)
                .ToListAsync();

            // --- OPTIMIZED CHART DATA & SUMMARY CALCULATION ---
            // Perform aggregation directly on the database
            var paymentStats = await _context.Fees
                .Where(f => f.StudentNumNavigation != null && f.StudentNumNavigation.Course != null)
                .GroupBy(f => new
                {
                    Course = f.StudentNumNavigation.Course,
                    YearLevelSection = f.StudentNumNavigation.YearLevelSection,
                    IsPaid = f.FeeStatus == "Paid" || f.FeeStatus == "Completed"
                })
                .Select(g => new
                {
                    g.Key.Course,
                    g.Key.YearLevelSection,
                    g.Key.IsPaid,
                    TotalAmount = g.Sum(f => f.Amount ?? 0)
                })
                .ToListAsync();

            // Process the aggregated results in memory - Group by Program + Year (matching Fines)
            var collectedBreakdown = new Dictionary<string, decimal>();
            var pendingBreakdown = new Dictionary<string, decimal>();

            foreach (var stat in paymentStats)
            {
                string program = stat.Course.ToUpper().Contains("BSIT") ? "BSIT" :
                                 stat.Course.ToUpper().Contains("DIT") ? "DIT" : "Other";
                int year = StringHelper.ExtractYearLevel(stat.YearLevelSection);

                if (year > 0)
                {
                    string key = $"{program}{year}"; // Program + Year: "BSIT1", "DIT2", etc.

                    if (stat.IsPaid)
                    {
                        if (!collectedBreakdown.ContainsKey(key)) collectedBreakdown[key] = 0;
                        collectedBreakdown[key] += stat.TotalAmount;
                    }
                    else
                    {
                        if (!pendingBreakdown.ContainsKey(key)) pendingBreakdown[key] = 0;
                        pendingBreakdown[key] += stat.TotalAmount;
                    }
                }
            }

            ViewBag.CollectedBreakdown = collectedBreakdown;
            ViewBag.PendingBreakdown = pendingBreakdown;
            ViewBag.TotalCollections = paymentStats.Where(s => s.IsPaid).Sum(s => s.TotalAmount);
            ViewBag.TotalExpected = paymentStats.Sum(s => s.TotalAmount);

            // Populate Fee Name Dropdown for filtering
            ViewBag.FeeNames = await _context.Fees
                .Where(f => !string.IsNullOrEmpty(f.FeeName))
                .Select(f => f.FeeName)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            return View(fees);
        }


        // =========================================================
        // ACTION: UPDATE FINE STATUS - DISABLED FOR ADMIN
        // Admin cannot change payment status - handled by Treasurers
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFineStatus(int fineId, string status)
        {
            // DISABLED: Admin cannot change payment status - Treasurers handle this
            TempData["Error"] = "Admin cannot change payment status. Payment collection is handled by Class Treasurers and validated by Org Treasurer.";
            return RedirectToAction(nameof(Fines));

            /* ORIGINAL CODE - DISABLED
            var fine = await _context.Fines.FindAsync(fineId);
            if (fine == null)
            {
                TempData["Error"] = "Fine record not found.";
                return RedirectToAction(nameof(Payments));
            }

            fine.FinesStatus = status;
            _context.Fines.Update(fine);
            await _context.SaveChangesAsync();

            await LogAction("Update Fine Status", $"Updated fine ID {fineId} status to {status}");
            TempData["Message"] = "Fine status updated successfully.";

            return RedirectToAction(nameof(Fines));
            END OF DISABLED CODE */
        }

        // =========================================================
        // FINES MANAGEMENT PAGE (FIXED SEARCH & FILTERING)
        // =========================================================
        public async Task<IActionResult> Fines(string searchString, string fineType, int? eventId, string manualFineReason, string statusFilter, string programFilter, string yearLevelFilter, string overdueFilter)
        {
            // --- 1. Pass filters to the View ---
            ViewData["SearchFilter"] = searchString;
            ViewData["FineTypeFilter"] = fineType;
            ViewData["EventFilter"] = eventId;
            ViewData["ReasonFilter"] = manualFineReason;
            ViewData["StatusFilter"] = statusFilter;
            ViewData["ProgramFilter"] = programFilter;
            ViewData["YearLevelFilter"] = yearLevelFilter;
            ViewData["OverdueFilter"] = overdueFilter;

            // --- 2. Build the base query ---
            var query = _context.Fines
                .Include(f => f.StudentNumNavigation) // Manual Fines
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.StudentNumNavigation) // Event Fines
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.Event) // Event Fines
                .AsQueryable();

            // --- 3. Apply Fine Type & Logic-Based Filters ---
            if (!string.IsNullOrEmpty(fineType))
            {
                if (fineType == "event")
                {
                    query = query.Where(f => f.AttendanceId != null);
                    if (eventId.HasValue)
                    {
                        query = query.Where(f => f.Attendance.EventId == eventId);
                    }
                }
                else if (fineType == "manual")
                {
                    query = query.Where(f => f.AttendanceId == null);
                    if (!string.IsNullOrEmpty(manualFineReason))
                    {
                        query = query.Where(f => f.Description == manualFineReason);
                    }
                }
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(f => f.FinesStatus == statusFilter);
            }

            if (!string.IsNullOrEmpty(programFilter))
            {
                // Check program on either Manual Student link or Event Student link
                query = query.Where(f =>
                    (f.StudentNumNavigation != null && f.StudentNumNavigation.Course == programFilter) ||
                    (f.Attendance != null && f.Attendance.StudentNumNavigation != null && f.Attendance.StudentNumNavigation.Course == programFilter));
            }

            if (!string.IsNullOrEmpty(yearLevelFilter))
            {
                query = query.Where(f =>
                    (f.StudentNumNavigation != null &&
                     f.StudentNumNavigation.YearLevelSection == yearLevelFilter) ||

                    (f.Attendance != null &&
                     f.Attendance.StudentNumNavigation != null &&
                     f.Attendance.StudentNumNavigation.YearLevelSection == yearLevelFilter)
                );
            }


            if (!string.IsNullOrEmpty(overdueFilter) && overdueFilter == "overdue")
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                query = query.Where(f => f.FinesStatus == "Unpaid" && f.FinesDueDate.HasValue && f.FinesDueDate.Value < today);
            }

            // --- 4. FIXED SEARCH STRING LOGIC ---
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim().ToLower();

                query = query.Where(f =>
                    // Search in Manual Fines
                    (f.StudentNumNavigation != null && (
                        f.StudentNumNavigation.StudentNum.Contains(searchString) ||
                        f.StudentNumNavigation.StudentFn.ToLower().Contains(searchString) ||
                        f.StudentNumNavigation.StudentLn.ToLower().Contains(searchString)
                    )) ||
                    // Search in Event Fines
                    (f.Attendance != null && f.Attendance.StudentNumNavigation != null && (
                        f.Attendance.StudentNumNavigation.StudentNum.Contains(searchString) ||
                        f.Attendance.StudentNumNavigation.StudentFn.ToLower().Contains(searchString) ||
                        f.Attendance.StudentNumNavigation.StudentLn.ToLower().Contains(searchString)
                    )) ||
                    // Search in Fine Description/Reason
                    (f.Description != null && f.Description.ToLower().Contains(searchString))
                );
            }

            var allFines = await query.OrderByDescending(f => f.FineId).ToListAsync();

            // --- 5. Prepare chart data (Optimized) ---
            var paidBreakdown = new Dictionary<string, decimal>();
            var unpaidBreakdown = new Dictionary<string, decimal>();

            foreach (var fine in allFines)
            {
                var student = fine.StudentNumNavigation ?? fine.Attendance?.StudentNumNavigation;
                if (student == null) continue;

                string program = !string.IsNullOrEmpty(student.Course) && student.Course.ToUpper().Contains("BSIT") ? "BSIT" : "DIT";
                int year = StringHelper.ExtractYearLevel(student.YearLevelSection);
                string key = $"{program}{year}";

                if (year > 0)
                {
                    decimal amount = fine.Amount ?? 0;
                    if (fine.FinesStatus?.ToLower() == "paid")
                    {
                        if (!paidBreakdown.ContainsKey(key)) paidBreakdown[key] = 0;
                        paidBreakdown[key] += amount;
                    }
                    else if (fine.FinesStatus?.ToLower() == "unpaid")
                    {
                        if (!unpaidBreakdown.ContainsKey(key)) unpaidBreakdown[key] = 0;
                        unpaidBreakdown[key] += amount;
                    }
                }
            }

            ViewBag.PaidBreakdown = paidBreakdown;
            ViewBag.UnpaidBreakdown = unpaidBreakdown;

            decimal totalExpected = allFines.Sum(f => f.Amount ?? 0);
            decimal totalCollected = allFines.Where(f => f.FinesStatus?.ToLower() == "paid").Sum(f => f.Amount ?? 0);
            ViewBag.CollectionRate = totalExpected > 0 ? Math.Round((totalCollected / totalExpected) * 100, 1) : 0;

            ViewBag.Events = await _context.Events.OrderByDescending(e => e.EventDate).ToListAsync();
            ViewBag.ManualFineReasons = await _context.Fines
                .Where(f => f.AttendanceId == null && f.Description != null)
                .Select(f => f.Description)
                .Distinct()
                .ToListAsync();

            return View(allFines);
        }


        // =========================================================
        // DEDICATED VIEW FOR MANUAL FINES LOG
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ManualFines(string searchString)
        {
            ViewData["CurrentSearch"] = searchString;

            var manualFinesQuery = _context.Fines
                .Include(f => f.StudentNumNavigation)
                .Where(f => f.AttendanceId == null); // The key filter for manual fines

            if (!string.IsNullOrEmpty(searchString))
            {
                manualFinesQuery = manualFinesQuery.Where(f => (f.StudentNumNavigation != null && (f.StudentNumNavigation.FullName.Contains(searchString) || f.StudentNumNavigation.StudentNum.Contains(searchString))) || (f.Description != null && f.Description.Contains(searchString)));
            }

            var fines = await manualFinesQuery.OrderByDescending(f => f.FineId).ToListAsync();
            return View(fines);
        }

        // =========================================================
        // SECURE DELETE FOR A SINGLE MANUAL FINE
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteManualFine(int fineId, string adminPassword)
        {
            var adminUser = await _userManager.GetUserAsync(User);
            if (string.IsNullOrEmpty(adminPassword) || !await _userManager.CheckPasswordAsync(adminUser, adminPassword))
            {
                TempData["Error"] = "Invalid password. Deletion cancelled.";
                return RedirectToAction("ManualFines");
            }

            var fineToDelete = await _context.Fines.FindAsync(fineId);

            if (fineToDelete == null)
            {
                TempData["Error"] = "Fine record not found.";
                return RedirectToAction("ManualFines");
            }

            if (fineToDelete.AttendanceId != null)
            {
                TempData["Error"] = "Error: This fine is linked to an event and cannot be deleted from this page.";
                return RedirectToAction("ManualFines");
            }

            _context.Fines.Remove(fineToDelete);
            await _context.SaveChangesAsync();

            await LogAction("Delete Manual Fine", $"Permanently deleted manual fine ID {fineToDelete.FineId}.");
            TempData["Message"] = $"Manual fine #{fineToDelete.FineId} has been permanently deleted.";

            return RedirectToAction("ManualFines");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFineAsPaidAdmin(int fineId, string returnUrl)
        {
            var fine = await _context.Fines.FindAsync(fineId);
            if (fine == null)
            {
                TempData["Error"] = "Fine not found.";
                return LocalRedirect(returnUrl ?? "/Admin/Fines");
            }

            fine.FinesStatus = "Paid";
            await _context.SaveChangesAsync();
            await LogAction("Mark Fine Paid", $"Marked fine ID {fineId} as Paid.");
            TempData["Message"] = "Fine has been marked as Paid.";

            return LocalRedirect(returnUrl ?? "/Admin/Fines");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WaiveFine(int fineId, string reason, string returnUrl)
        {
            var fine = await _context.Fines.FindAsync(fineId);
            if (fine == null)
            {
                TempData["Error"] = "Fine not found.";
                return LocalRedirect(returnUrl ?? "/Admin/Fines");
            }

            fine.FinesStatus = "Waived";
            await _context.SaveChangesAsync();
            await LogAction("Waive Fine", $"Waived fine ID {fineId}. Reason: {reason}");
            TempData["Message"] = "Fine has been waived.";

            return LocalRedirect(returnUrl ?? "/Admin/Fines");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustFineAmount(int fineId, decimal newAmount, string reason, string returnUrl)
        {
            var fine = await _context.Fines.FindAsync(fineId);
            if (fine == null)
            {
                TempData["Error"] = "Fine not found.";
                return LocalRedirect(returnUrl ?? "/Admin/Fines");
            }

            fine.Amount = newAmount;
            await _context.SaveChangesAsync();
            await LogAction("Adjust Fine", $"Adjusted fine ID {fineId} to {newAmount:C}. Reason: {reason}");
            TempData["Message"] = "Fine amount has been adjusted.";

            return LocalRedirect(returnUrl ?? "/Admin/Fines");
        }

        // =========================================================
        // ACTION: CREATE MANUAL FINE (UPDATED with BatchId)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFine(string FineReason, decimal Amount, DateOnly? FinesDueDate, string programFilter, string yearFilter)
        {
            try
            {
                var students = await GetFilteredStudentsForFee(programFilter, yearFilter);

                if (!students.Any())
                {
                    TempData["Error"] = "No students found matching the selected criteria.";
                    return RedirectToAction("Fines");
                }

                int count = 0;
                var dueDate = FinesDueDate ?? DateOnly.FromDateTime(DateTime.Now.AddDays(15));

                // Generate a single, unique ID for this entire batch
                var batchId = Guid.NewGuid().ToString();

                foreach (var student in students)
                {
                    var fine = new Fine
                    {
                        Amount = Amount,
                        Description = FineReason,
                        StudentNum = student.StudentNum,
                        FinesStatus = "Unpaid",
                        FinesStartDate = DateOnly.FromDateTime(DateTime.Now),
                        FinesDueDate = dueDate,
                        AttendanceId = null,
                        BatchId = batchId // Assign the same BatchId to all fines in this group
                    };
                    _context.Fines.Add(fine);

                    var notification = new Notification
                    {
                        StudentNum = student.StudentNum,
                        Title = "New Fine Issued",
                        Message = $"You have been issued a fine of ₱{Amount:N2} for '{FineReason}'. Due date: {dueDate:MMM dd, yyyy}.",
                        NotificationType = "Fine",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = "Admin"
                    };
                    _context.Notifications.Add(notification);
                    count++;
                }

                await _context.SaveChangesAsync();

                await LogAction("Create Manual Fine Batch", $"Created fine batch '{FineReason}' ({batchId}) for {count} students.");
                TempData["Message"] = $"Successfully assigned fine to {count} students.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manual fine batch");
                TempData["Error"] = "An error occurred while creating the fine.";
            }

            return RedirectToAction("Fines");
        }

        // =========================================================
        // NEW PAGE: VIEW AND MANAGE MANUAL FINE BATCHES
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ManualFineBatches()
        {
            // Fetch only manual fines that have a BatchId
            var manualFines = await _context.Fines
                .Where(f => f.AttendanceId == null && f.BatchId != null)
                .ToListAsync();

            // Group the fines by their BatchId to create a summary view
            var fineBatches = manualFines
                .GroupBy(f => f.BatchId)
                .Select(g => new ManualFineBatchViewModel
                {
                    BatchId = g.Key,
                    Reason = g.First().Description,
                    Amount = g.First().Amount ?? 0,
                    DateCreated = g.First().FinesStartDate,
                    StudentCount = g.Count()
                })
                .OrderByDescending(b => b.DateCreated)
                .ToList();

            return View(fineBatches);
        }

        // =========================================================
        // NEW ACTION: SECURELY DELETE AN ENTIRE FINE BATCH
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFineBatch(string batchId, string adminPassword)
        {
            var adminUser = await _userManager.GetUserAsync(User);
            if (string.IsNullOrEmpty(adminPassword) || !await _userManager.CheckPasswordAsync(adminUser, adminPassword))
            {
                TempData["Error"] = "Invalid password. Deletion cancelled.";
                return RedirectToAction("ManualFineBatches");
            }

            if (string.IsNullOrEmpty(batchId))
            {
                TempData["Error"] = "Batch ID was not provided.";
                return RedirectToAction("ManualFineBatches");
            }

            // Find all fines associated with this batch
            var finesToDelete = await _context.Fines
                .Where(f => f.BatchId == batchId)
                .ToListAsync();

            if (!finesToDelete.Any())
            {
                TempData["Warning"] = "This batch may have already been deleted.";
                return RedirectToAction("ManualFineBatches");
            }

            var reason = finesToDelete.First().Description;
            var count = finesToDelete.Count;

            _context.Fines.RemoveRange(finesToDelete);
            await _context.SaveChangesAsync();

            await LogAction("Delete Fine Batch", $"Deleted manual fine batch '{reason}' ({batchId}), affecting {count} records.");
            TempData["Message"] = $"Successfully deleted the entire '{reason}' fine batch ({count} records).";

            return RedirectToAction("ManualFineBatches");
        }

        // =========================================================
        // AJAX: Preview Fine Student Count
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> PreviewFineStudentCount(string programFilter, string yearFilter)
        {
            // Reuse logic from Fee Preview since the filtering logic is identical
            return await PreviewFeeStudentCount(programFilter, yearFilter);
        }

        // =========================================================
        // NEW: GET STUDENTS IN FINE BATCH (AJAX for double-click)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetStudentsInFineBatch(string batchId)
        {
            var students = await _context.Fines
                .Where(f => f.BatchId == batchId)
                .Include(f => f.StudentNumNavigation)
                .Select(f => new
                {
                    studentNum = f.StudentNum,
                    fullName = f.StudentNumNavigation.FullName,
                    program = f.StudentNumNavigation.Course,
                    yearLevel = f.StudentNumNavigation.YearLevelSection,
                    amount = f.Amount,
                    status = f.FinesStatus
                })
                .ToListAsync();

            return Json(new { success = true, students });
        }

        // =========================================================
        // NEW: VIEW AND MANAGE MANUAL PAYMENT BATCHES
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ManualPaymentBatches()
        {
            // Fetch only fees that have a BatchId (manual batch fees)
            var manualFees = await _context.Fees
                .Where(f => f.BatchId != null)
                .Include(f => f.StudentNumNavigation)
                .ToListAsync();

            // Group the fees by their BatchId to create a summary view
            var paymentBatches = manualFees
                .GroupBy(f => f.BatchId)
                .Select(g => new ManualPaymentBatchViewModel
                {
                    BatchId = g.Key,
                    Description = g.First().FeeName,
                    Amount = g.First().Amount ?? 0,
                    DateCreated = g.First().DateCreated,
                    StudentCount = g.Count()
                })
                .OrderByDescending(b => b.DateCreated)
                .ToList();

            return View(paymentBatches);
        }

        // =========================================================
        // NEW: SECURELY DELETE AN ENTIRE PAYMENT BATCH
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePaymentBatch(string batchId, string adminPassword)
        {
            var adminUser = await _userManager.GetUserAsync(User);
            if (string.IsNullOrEmpty(adminPassword) || !await _userManager.CheckPasswordAsync(adminUser, adminPassword))
            {
                TempData["Error"] = "Invalid password. Deletion cancelled.";
                return RedirectToAction("ManualPaymentBatches");
            }

            if (string.IsNullOrEmpty(batchId))
            {
                TempData["Error"] = "Batch ID was not provided.";
                return RedirectToAction("ManualPaymentBatches");
            }

            // Find all fees associated with this batch
            var feesToDelete = await _context.Fees
                .Where(f => f.BatchId == batchId)
                .ToListAsync();

            if (!feesToDelete.Any())
            {
                TempData["Warning"] = "This batch may have already been deleted.";
                return RedirectToAction("ManualPaymentBatches");
            }

            var description = feesToDelete.First().FeeName;
            var count = feesToDelete.Count;

            _context.Fees.RemoveRange(feesToDelete);
            await _context.SaveChangesAsync();

            await LogAction("Delete Payment Batch", $"Deleted manual payment batch '{description}' ({batchId}), affecting {count} records.");
            TempData["Message"] = $"Successfully deleted the entire '{description}' payment batch ({count} records).";

            return RedirectToAction("ManualPaymentBatches");
        }

        // =========================================================
        // NEW: GET STUDENTS IN PAYMENT BATCH (AJAX for double-click)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetStudentsInPaymentBatch(string batchId)
        {
            var students = await _context.Fees
                .Where(f => f.BatchId == batchId)
                .Include(f => f.StudentNumNavigation)
                .Select(f => new
                {
                    studentNum = f.StudentNum,
                    fullName = f.StudentNumNavigation.FullName,
                    program = f.StudentNumNavigation.Course,
                    yearLevel = f.StudentNumNavigation.YearLevelSection,
                    amount = f.Amount,
                    status = f.FeeStatus
                })
                .ToListAsync();

            return Json(new { success = true, students });
        }


        [HttpGet]
        public async Task<IActionResult> ExportFinesToExcel()
        {
            var allFines = await _context.Fines
                .Include(f => f.Attendance.StudentNumNavigation)
                .Include(f => f.Attendance.Event)
                .OrderBy(f => f.FineId)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Fines");
                var currentRow = 1;

                // Headers
                worksheet.Cell(currentRow, 1).Value = "Fine ID";
                worksheet.Cell(currentRow, 2).Value = "Student ID";
                worksheet.Cell(currentRow, 3).Value = "Student Name";
                worksheet.Cell(currentRow, 4).Value = "Event";
                worksheet.Cell(currentRow, 5).Value = "Event Date";
                worksheet.Cell(currentRow, 6).Value = "Amount";
                worksheet.Cell(currentRow, 7).Value = "Status";
                worksheet.Cell(currentRow, 8).Value = "Due Date";
                worksheet.Row(currentRow).Style.Font.Bold = true;

                // Body
                foreach (var fine in allFines)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = fine.FineId;
                    worksheet.Cell(currentRow, 2).Value = fine.Attendance?.StudentNumNavigation?.StudentNum;
                    worksheet.Cell(currentRow, 3).Value = fine.Attendance?.StudentNumNavigation?.FullName;
                    worksheet.Cell(currentRow, 4).Value = fine.Attendance?.Event?.EventName;
                    worksheet.Cell(currentRow, 5).Value = fine.Attendance?.Event?.EventDate?.ToString("yyyy-MM-dd");
                    worksheet.Cell(currentRow, 6).Value = fine.Amount;
                    worksheet.Cell(currentRow, 7).Value = fine.FinesStatus;
                    worksheet.Cell(currentRow, 8).Value = fine.FinesDueDate?.ToString("yyyy-MM-dd");
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Fines_Export_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }

        // =========================================================
        // CREATE FEE - Apply to students by Program and Year Level
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFee(string FeeName, decimal Amount, DateOnly? FeesDueDate, string AcadYear, string programFilter, string yearFilter)
        {
            try
            {
                // Get filtered students based on program and year
                var students = await GetFilteredStudentsForFee(programFilter, yearFilter);

                if (!students.Any())
                {
                    TempData["Error"] = "No students found matching the selected criteria.";
                    return RedirectToAction("Payments");
                }

                // Generate unique BatchId for this batch of fees
                string batchId = $"FEE-{DateTime.Now:yyyyMMddHHmmss}";

                // Create fee records for each matching student
                var feesCreated = 0;
                foreach (var student in students)
                {
                    var fee = new Fee
                    {
                        FeeName = FeeName,
                        Amount = Amount,
                        FeesStartDate = DateOnly.FromDateTime(DateTime.Now),
                        FeesDueDate = FeesDueDate,
                        FeeStatus = "Pending",
                        AcadYear = AcadYear,
                        StudentNum = student.StudentNum,
                        BatchId = batchId,  // NEW: Assign BatchId
                        DateCreated = DateTime.Now  // NEW: Set DateCreated
                    };
                    _context.Fees.Add(fee);
                    feesCreated++;
                }

                await _context.SaveChangesAsync();

                // Build success message with details
                var programDesc = programFilter switch
                {
                    "bsit" => "BSIT",
                    "dit" => "DIT",
                    _ => "All Programs"
                };
                var yearDesc = yearFilter switch
                {
                    "1" => "1st Year",
                    "2" => "2nd Year",
                    "3" => "3rd Year",
                    "4" => "4th Year",
                    _ => "All Year Levels"
                };

                TempData["Message"] = $"Fee '{FeeName}' (₱{Amount:N2}) for {AcadYear} successfully assigned to {feesCreated} students ({programDesc} - {yearDesc}).";
                _logger.LogInformation($"Fee batch created: {FeeName}, BatchId: {batchId}, Amount: {Amount}, AcadYear: {AcadYear}, Applied to: {feesCreated} students ({programDesc} - {yearDesc})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fee");
                TempData["Error"] = "An error occurred while creating the fee. Please try again.";
            }

            return RedirectToAction("Payments");
        }

        // =========================================================
        // AJAX: Preview Fee Student Count
        // Returns the count of students that would be affected
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> PreviewFeeStudentCount(string programFilter, string yearFilter)
        {
            try
            {
                var students = await GetFilteredStudentsForFee(programFilter, yearFilter);

                // Count by program
                var bsitCount = students.Count(s => !string.IsNullOrWhiteSpace(s.Course) && s.Course.ToUpper().Contains("BSIT"));
                var ditCount = students.Count(s => !string.IsNullOrWhiteSpace(s.Course) && s.Course.ToUpper().Contains("DIT"));

                return Json(new
                {
                    success = true,
                    total = students.Count,
                    bsit = bsitCount,
                    dit = ditCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing fee student count");
                return Json(new { success = false, total = 0, bsit = 0, dit = 0 });
            }
        }

        // =========================================================
        // HELPER: Get Filtered Students for Fee Creation
        // =========================================================
        private async Task<List<Student>> GetFilteredStudentsForFee(string programFilter, string yearFilter)
        {
            // Helper to extract year level
            int ExtractYearLevel(string? yearLevelSection)
            {
                if (string.IsNullOrWhiteSpace(yearLevelSection))
                    return 0;

                var input = yearLevelSection.Trim().ToUpper();

                if (input.Contains("FIRST") || input.Contains("1ST")) return 1;
                if (input.Contains("SECOND") || input.Contains("2ND")) return 2;
                if (input.Contains("THIRD") || input.Contains("3RD")) return 3;
                if (input.Contains("FOURTH") || input.Contains("4TH")) return 4;

                foreach (char c in input)
                {
                    if (char.IsDigit(c))
                    {
                        int year = c - '0';
                        if (year >= 1 && year <= 4)
                            return year;
                    }
                }
                return 0;
            }

            // Get all active BSIT/DIT students (non-archived)
            var students = await _context.Students
                .Where(s => s.IsArchived != true &&
                            (s.Classification == null ||
                             s.Classification == "" ||
                             s.Classification.ToUpper() == "ACTIVE") &&
                            s.Course != null &&
                            (s.Course.ToUpper().Contains("BSIT") || s.Course.ToUpper().Contains("DIT")))
                .ToListAsync();

            // Filter by Program
            if (programFilter == "bsit")
            {
                students = students.Where(s => s.Course!.ToUpper().Contains("BSIT")).ToList();
            }
            else if (programFilter == "dit")
            {
                students = students.Where(s => s.Course!.ToUpper().Contains("DIT")).ToList();
            }

            // Filter by Year Level
            if (!string.IsNullOrEmpty(yearFilter) && yearFilter != "all")
            {
                if (int.TryParse(yearFilter, out int targetYear))
                {
                    students = students.Where(s => ExtractYearLevel(s.YearLevelSection) == targetYear).ToList();
                }
            }

            return students;
        }

        // =========================================================
        // EXPORT TO EXCEL - WYSIWYG (What You See What You Get)
        // =========================================================
        public async Task<IActionResult> ExportStudentsToExcel(
            string searchString,
            string programFilter,
            string yearFilter,
            string sectionFilter,
            string typeFilter,
            string statusFilter,
            string roleFilter,
            string columns)
        {
            try
            {
                // Use the consolidated helper method to get the exact same filtered data
                var studentsQuery = GetFilteredStudentsQuery(searchString, programFilter, yearFilter, sectionFilter, typeFilter, statusFilter, roleFilter);

                // Order the results for the export file
                var students = await studentsQuery.OrderBy(s => s.StudentLn).ToListAsync();

                // Parse visible columns (default to all if not specified)
                var visibleColumns = string.IsNullOrEmpty(columns)
                    ? new[] { "Id", "Name", "Program", "Section", "Year", "Type", "Role", "Status" }
                    : columns.Split(',', StringSplitOptions.RemoveEmptyEntries);

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Student Records");

                    // Build dynamic headers based on visible columns
                    var headersList = new List<string>();
                    if (visibleColumns.Contains("Id")) headersList.Add("Student ID");
                    if (visibleColumns.Contains("Name")) { headersList.Add("Full Name"); headersList.Add("Email"); }
                    if (visibleColumns.Contains("Program")) headersList.Add("Course/Program");
                    if (visibleColumns.Contains("Section")) headersList.Add("Year & Section");
                    if (visibleColumns.Contains("Year")) headersList.Add("School Year Enrolled");
                    if (visibleColumns.Contains("Type")) headersList.Add("Student Type");
                    if (visibleColumns.Contains("Role")) headersList.Add("Role");
                    if (visibleColumns.Contains("Status")) headersList.Add("Status");

                    // Style header row
                    for (int i = 0; i < headersList.Count; i++)
                    {
                        var cell = worksheet.Cell(1, i + 1);
                        cell.Value = headersList[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D4AF37"); // Gold color
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }

                    // Populate data rows
                    int row = 2;
                    foreach (var student in students)
                    {
                        int col = 1;
                        if (visibleColumns.Contains("Id")) worksheet.Cell(row, col++).Value = student.StudentNum;
                        if (visibleColumns.Contains("Name"))
                        {
                            string middleInitial = !string.IsNullOrEmpty(student.StudentMn) ? student.StudentMn.Substring(0, 1) + "." : "";
                            string fullName = $"{student.StudentLn ?? ""}, {student.StudentFn ?? ""} {middleInitial}".Trim();
                            worksheet.Cell(row, col++).Value = fullName;
                            worksheet.Cell(row, col++).Value = student.StudentEmail ?? "";
                        }
                        if (visibleColumns.Contains("Program")) worksheet.Cell(row, col++).Value = student.Course ?? "";
                        if (visibleColumns.Contains("Section")) worksheet.Cell(row, col++).Value = student.YearLevelSection ?? "";
                        if (visibleColumns.Contains("Year")) worksheet.Cell(row, col++).Value = student.SchoolYearEnrolled ?? "";
                        if (visibleColumns.Contains("Type")) worksheet.Cell(row, col++).Value = student.StudentType ?? "";
                        if (visibleColumns.Contains("Role")) worksheet.Cell(row, col++).Value = student.Officer?.Position ?? "Member";
                        if (visibleColumns.Contains("Status")) worksheet.Cell(row, col++).Value = student.Classification ?? "";

                        for (int c = 1; c < col; c++)
                        {
                            worksheet.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        }
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();

                    // Add summary
                    row++;
                    worksheet.Cell(row, 1).Value = $"Total Records: {students.Count}";
                    worksheet.Cell(row, 1).Style.Font.Bold = true;
                    worksheet.Range(row, 1, row, 3).Merge();

                    // Generate file
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Position = 0;
                        string fileName = $"iBITS_StudentRecords_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        await LogAction("Export Excel", $"Exported {students.Count} student records to Excel.");
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting students to Excel");
                TempData["Error"] = "An error occurred while exporting to Excel.";
                return RedirectToAction(nameof(StudentRecords));
            }
        }

        // =========================================================
        // EXPORT ATTENDANCE TO EXCEL - WITH FILTERS
        // =========================================================
        public async Task<IActionResult> ExportAttendanceToExcel(string? eventFilter, string? programFilter, string? yearFilter)
        {
            try
            {
                // Apply same filters as Attendance view
                var query = _context.Attendances
                    .Include(a => a.Event)
                    .Include(a => a.StudentNumNavigation)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(eventFilter) && int.TryParse(eventFilter, out int eventId))
                {
                    query = query.Where(a => a.EventId == eventId);
                }

                if (!string.IsNullOrEmpty(programFilter))
                {
                    query = query.Where(a => a.StudentNumNavigation != null &&
                                             a.StudentNumNavigation.Course != null &&
                                             a.StudentNumNavigation.Course.ToUpper().Contains(programFilter.ToUpper()));
                }

                var attendances = await query
                    .OrderByDescending(a => a.Event != null ? a.Event.EventDate : DateOnly.MinValue)
                    .ToListAsync();

                // Apply year filter in memory
                if (!string.IsNullOrEmpty(yearFilter))
                {
                    attendances = attendances.Where(a => ExtractYearFromYearLevelSection(a.StudentNumNavigation?.YearLevelSection) == yearFilter).ToList();
                }

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Attendance Records");

                    // Headers
                    var headers = new[] { "Student ID", "Student Name", "Program", "Year Level", "Event", "Event Date", "Status" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cell(1, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D4AF37");
                        cell.Style.Font.FontColor = XLColor.White;
                    }

                    // Data
                    int row = 2;
                    foreach (var att in attendances)
                    {
                        worksheet.Cell(row, 1).Value = att.StudentNum ?? "";
                        worksheet.Cell(row, 2).Value = att.StudentNumNavigation?.FullName ?? "";
                        worksheet.Cell(row, 3).Value = att.StudentNumNavigation?.Course ?? "";
                        worksheet.Cell(row, 4).Value = ExtractYearFromYearLevelSection(att.StudentNumNavigation?.YearLevelSection) ?? "";
                        worksheet.Cell(row, 5).Value = att.Event?.EventName ?? "";
                        worksheet.Cell(row, 6).Value = att.Event?.EventDate.ToString() ?? "";
                        worksheet.Cell(row, 7).Value = att.AttendanceStatus ?? "";
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Position = 0;
                        string fileName = $"iBITS_Attendance_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        await LogAction("Export Excel", $"Exported {attendances.Count} attendance records to Excel.");
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting attendance to Excel");
                TempData["Error"] = "An error occurred while exporting attendance.";
                return RedirectToAction(nameof(Attendance));
            }
        }

        // =========================================================
        // EXPORT ATTENDANCE TO CSV - WITH FILTERS
        // =========================================================
        public async Task<IActionResult> ExportAttendanceToCSV(string? eventFilter, string? programFilter, string? yearFilter)
        {
            try
            {
                // Apply same filters as Attendance view
                var query = _context.Attendances
                    .Include(a => a.Event)
                    .Include(a => a.StudentNumNavigation)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(eventFilter) && int.TryParse(eventFilter, out int eventId))
                {
                    query = query.Where(a => a.EventId == eventId);
                }

                if (!string.IsNullOrEmpty(programFilter))
                {
                    query = query.Where(a => a.StudentNumNavigation != null &&
                                             a.StudentNumNavigation.Course != null &&
                                             a.StudentNumNavigation.Course.ToUpper().Contains(programFilter.ToUpper()));
                }

                var attendances = await query
                    .OrderByDescending(a => a.Event != null ? a.Event.EventDate : DateOnly.MinValue)
                    .ToListAsync();

                // Apply year filter in memory
                if (!string.IsNullOrEmpty(yearFilter))
                {
                    attendances = attendances.Where(a => ExtractYearFromYearLevelSection(a.StudentNumNavigation?.YearLevelSection) == yearFilter).ToList();
                }

                // Build CSV content
                var csv = new System.Text.StringBuilder();

                // Headers
                csv.AppendLine("Student ID,Student Name,Program,Year Level,Event,Event Date,Status");

                // Data rows
                foreach (var att in attendances)
                {
                    var studentId = EscapeCsvField(att.StudentNum ?? "");
                    var studentName = EscapeCsvField(att.StudentNumNavigation?.FullName ?? "");
                    var program = EscapeCsvField(att.StudentNumNavigation?.Course ?? "");
                    var yearLevel = EscapeCsvField(ExtractYearFromYearLevelSection(att.StudentNumNavigation?.YearLevelSection) ?? "");
                    var eventName = EscapeCsvField(att.Event?.EventName ?? "");
                    var eventDate = EscapeCsvField(att.Event?.EventDate.ToString() ?? "");
                    var status = EscapeCsvField(att.AttendanceStatus ?? "");

                    csv.AppendLine($"{studentId},{studentName},{program},{yearLevel},{eventName},{eventDate},{status}");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                string fileName = $"iBITS_Attendance_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                await LogAction("Export CSV", $"Exported {attendances.Count} attendance records to CSV.");

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting attendance to CSV");
                TempData["Error"] = "An error occurred while exporting attendance to CSV.";
                return RedirectToAction(nameof(Attendance));
            }
        }

        // Helper method to escape CSV fields
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // If field contains comma, quote, or newline, wrap it in quotes and escape internal quotes
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        // =========================================================
        // UPDATE USER ROLE - WITH PASSWORD VERIFICATION
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(string studentNum, string newRole, string adminPassword)
        {
            string returnAction = "Index";
            string referringUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referringUrl) && referringUrl.Contains("/Admin/StudentRecords"))
            {
                returnAction = "StudentRecords";
            }

            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null)
            {
                TempData["Error"] = "Admin session expired. Please login again.";
                return RedirectToAction(returnAction);
            }

            if (string.IsNullOrEmpty(adminPassword))
            {
                TempData["Error"] = "Admin password is required to change roles.";
                return RedirectToAction(returnAction);
            }

            var passwordValid = await _userManager.CheckPasswordAsync(adminUser, adminPassword);
            if (!passwordValid)
            {
                TempData["Error"] = "Invalid admin password. Role change cancelled.";
                return RedirectToAction(returnAction);
            }

            var user = await _userManager.FindByNameAsync(studentNum);
            if (user == null)
            {
                TempData["Error"] = $"User {studentNum} not found.";
                return RedirectToAction(returnAction);
            }

            var student = await _context.Students.Include(s => s.Officer).FirstOrDefaultAsync(s => s.StudentNum == studentNum);
            if (student == null)
            {
                TempData["Error"] = $"Student record for {studentNum} not found.";
                return RedirectToAction(returnAction);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            string currentRole = currentRoles.FirstOrDefault() ?? "Member";

            if (currentRole == newRole)
            {
                TempData["Error"] = $"{student.StudentFn} {student.StudentLn} is already assigned to {newRole}.";
                return RedirectToAction(returnAction);
            }

            // UPDATED VALIDATION: Only 1 Class Secretary & 1 Class Treasurer per section
            // Handles both "3-1" and "BSIT 3-1" formats
            // UPDATED VALIDATION: Only 1 Class Secretary & 1 Class Treasurer per Program + Section
            if (newRole == "Class Secretary" || newRole == "Class Treasurer")
            {
                if (!string.IsNullOrEmpty(student.YearLevelSection) && !string.IsNullOrEmpty(student.Course))
                {
                    // 1. Get current student's details
                    var studentSection = ExtractYearSection(student.YearLevelSection);
                    var studentProgram = student.Course; // e.g., "BSIT" or "DIT"

                    // 2. Get all users who currently hold this role in the Identity system
                    var usersInRole = await _userManager.GetUsersInRoleAsync(newRole);

                    foreach (var u in usersInRole)
                    {
                        if (u.UserName == studentNum) continue; // Skip the student being edited

                        // 3. Fetch the Student profile for the other officer to compare Program and Section
                        var otherStudent = await _context.Students
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.StudentNum == u.UserName);

                        if (otherStudent != null)
                        {
                            var otherSection = ExtractYearSection(otherStudent.YearLevelSection);
                            var otherProgram = otherStudent.Course;

                            // 4. CRITICAL CHECK: Does the Program AND the Section match?
                            if (otherProgram == studentProgram && otherSection == studentSection)
                            {
                                TempData["Error"] = $"Action Denied: {studentProgram} {studentSection} already has a {newRole} ({otherStudent.StudentFn} {otherStudent.StudentLn}).";
                                return RedirectToAction(returnAction);
                            }
                        }
                    }

                    // 5. Check for PENDING role changes
                    // We use .ToListAsync() to bring the small list into memory so we can use ExtractYearSection
                    var pendingChanges = await _context.PendingRoleChanges
                        .Where(p => p.NewRole == newRole && !p.IsConfirmed && !p.IsDeclined && p.StudentNumber != studentNum)
                        .Join(_context.Students, p => p.StudentNumber, s => s.StudentNum, (p, s) => s)
                        .ToListAsync(); // Move to memory here

                    var pendingConflict = pendingChanges
                        .FirstOrDefault(s => s.Course == studentProgram && ExtractYearSection(s.YearLevelSection) == studentSection);

                    if (pendingConflict != null)
                    {
                        TempData["Error"] = $"Action Denied: A role change for {newRole} in {studentProgram} {studentSection} is already pending for {pendingConflict.StudentFn} {pendingConflict.StudentLn}.";
                        return RedirectToAction(returnAction);
                    }
                }
            }

            if (newRole == "Org Secretary" || newRole == "Org Treasurer")
            {
                // 1. Check Confirmed Officers (Already assigned in Identity)
                var usersInOrgRole = await _userManager.GetUsersInRoleAsync(newRole);
                var existingConfirmed = usersInOrgRole.FirstOrDefault(u => u.UserName != studentNum);

                if (existingConfirmed != null)
                {
                    var otherS = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentNum == existingConfirmed.UserName);
                    TempData["Error"] = $"Action Denied: The organization already has a confirmed {newRole} ({otherS?.StudentFn} {otherS?.StudentLn}).";
                    return RedirectToAction(returnAction);
                }

                // 2. Check Pending Changes (Assigned but not yet confirmed by the student)
                var pendingOrgConflict = await _context.PendingRoleChanges
                    .Where(p => p.NewRole == newRole && !p.IsConfirmed && !p.IsDeclined && p.StudentNumber != studentNum)
                    .Join(_context.Students, p => p.StudentNumber, s => s.StudentNum, (p, s) => s)
                    .FirstOrDefaultAsync();

                if (pendingOrgConflict != null)
                {
                    TempData["Error"] = $"Action Denied: A role change for {newRole} is already pending for {pendingOrgConflict.StudentFn} {pendingOrgConflict.StudentLn}.";
                    return RedirectToAction(returnAction);
                }
            }

            var existingPending = await _context.PendingRoleChanges
                .Where(p => p.StudentNumber == studentNum && !p.IsConfirmed && !p.IsDeclined)
                .FirstOrDefaultAsync();

            if (existingPending != null)
            {
                existingPending.NewRole = newRole;
                existingPending.AssignedByAdminId = adminUser.Id;
                existingPending.AssignedByAdminName = adminUser.UserName;
                existingPending.AssignedDate = DateTime.Now;
                _context.PendingRoleChanges.Update(existingPending);
            }
            else
            {
                var pendingChange = new PendingRoleChange
                {
                    StudentNumber = studentNum,
                    OldRole = currentRole,
                    NewRole = newRole,
                    AssignedByAdminId = adminUser.Id,
                    AssignedByAdminName = adminUser.UserName,
                    AssignedDate = DateTime.Now,
                    IsConfirmed = false,
                    IsDeclined = false
                };
                _context.PendingRoleChanges.Add(pendingChange);
            }

            await _context.SaveChangesAsync();
            await LogAction("Pending Role Change", $"Admin {adminUser.UserName} assigned {newRole} to {studentNum} (pending confirmation)");

            TempData["Message"] = $"Role change for {student.StudentFn} {student.StudentLn} to '{newRole}' is pending their confirmation upon next login.";
            return RedirectToAction(returnAction);
        }

        // =========================================================
        // BULK ACTIONS FOR STUDENT SELECTION
        // =========================================================

        /// <summary>
        /// Export selected students to Excel with specified columns
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportSelectedStudents(string studentIds, string columns)
        {
            try
            {
                if (string.IsNullOrEmpty(studentIds))
                {
                    TempData["Error"] = "No students selected for export.";
                    return RedirectToAction("StudentRecords");
                }

                var idList = studentIds.Split(',').ToList();
                var columnList = !string.IsNullOrEmpty(columns) ? columns.Split(',').ToList() : new List<string>();

                var students = await _context.Students
                    .Include(s => s.Officer)
                    .Where(s => idList.Contains(s.StudentNum))
                    .OrderBy(s => s.StudentLn)
                    .ToListAsync();

                if (!students.Any())
                {
                    TempData["Error"] = "No matching students found.";
                    return RedirectToAction("StudentRecords");
                }

                // Build CSV content
                var csv = new System.Text.StringBuilder();

                // Add header row based on selected columns
                var headers = new List<string>();
                if (columnList.Count == 0 || columnList.Contains("Id")) headers.Add("Student ID");
                if (columnList.Count == 0 || columnList.Contains("Name")) headers.Add("Full Name");
                if (columnList.Count == 0 || columnList.Contains("Program")) headers.Add("Program");
                if (columnList.Count == 0 || columnList.Contains("Section")) headers.Add("Section");
                if (columnList.Count == 0 || columnList.Contains("Year")) headers.Add("Year");
                if (columnList.Count == 0 || columnList.Contains("Type")) headers.Add("Student Type");
                if (columnList.Count == 0 || columnList.Contains("Role")) headers.Add("Role");
                if (columnList.Count == 0 || columnList.Contains("Status")) headers.Add("Status");

                csv.AppendLine(string.Join(",", headers));

                // Add data rows
                foreach (var student in students)
                {
                    var row = new List<string>();

                    if (columnList.Count == 0 || columnList.Contains("Id"))
                        row.Add($"\"{student.StudentNum}\"");

                    if (columnList.Count == 0 || columnList.Contains("Name"))
                        row.Add($"\"{student.FullName}\"");

                    if (columnList.Count == 0 || columnList.Contains("Program"))
                        row.Add($"\"{student.Course ?? "N/A"}\"");

                    if (columnList.Count == 0 || columnList.Contains("Section"))
                        row.Add($"\"{student.YearLevelSection ?? "N/A"}\"");

                    if (columnList.Count == 0 || columnList.Contains("Year"))
                    {
                        var year = ExtractYearFromYearLevelSection(student.YearLevelSection);
                        row.Add($"\"{year ?? "N/A"}\"");
                    }

                    if (columnList.Count == 0 || columnList.Contains("Type"))
                        row.Add($"\"{student.StudentType ?? "N/A"}\"");

                    if (columnList.Count == 0 || columnList.Contains("Role"))
                        row.Add($"\"{student.Officer?.Position ?? "Member"}\"");

                    if (columnList.Count == 0 || columnList.Contains("Status"))
                        row.Add($"\"{student.Classification ?? "Active"}\"");

                    csv.AppendLine(string.Join(",", row));
                }

                // Return CSV file
                var fileName = $"iBITS_StudentRecords_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());

                await LogAction("Export Selected Students", $"Exported {students.Count} selected students to Excel");

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting selected students");
                TempData["Error"] = "An error occurred while exporting students.";
                return RedirectToAction("StudentRecords");
            }
        }

        /// <summary>
        /// Archive multiple selected students
        /// </summary>
        /// <summary>
        /// Archive multiple selected students
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ArchiveSelectedStudents([FromForm] string[] studentIds)
        {
            try
            {
                if (studentIds == null || !studentIds.Any())
                {
                    return Json(new { success = false, message = "No students selected." });
                }

                var students = await _context.Students
                    .Where(s => studentIds.Contains(s.StudentNum))
                    .ToListAsync();

                if (!students.Any())
                {
                    return Json(new { success = false, message = "No matching students found." });
                }

                var currentAcadYear = await GetCurrentAcademicYear();
                int count = 0;

                foreach (var student in students)
                {
                    if (student.Classification == "Active" || string.IsNullOrEmpty(student.Classification))
                    {
                        student.Classification = "Archived";
                        student.IsArchived = true;
                        student.ArchiveStatus = "Bulk Archived";
                        student.ArchiveDate = DateOnly.FromDateTime(DateTime.Now);

                        if (string.IsNullOrEmpty(student.SchoolYearEnrolled))
                        {
                            student.SchoolYearEnrolled = currentAcadYear;
                        }
                    }
                    else
                    {
                        // Toggle back to active
                        student.Classification = "Active";
                        student.IsArchived = false;
                        student.ArchiveStatus = null;
                        student.ArchiveDate = null;
                    }
                    count++;
                }

                await _context.SaveChangesAsync();
                await LogAction("Bulk Archive", $"Toggled archive status for {count} selected students");

                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving selected students");
                return Json(new { success = false, message = "An error occurred while archiving students." });
            }
        }

        /// <summary>
        /// Reset password for multiple selected students to their Student ID
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ResetPasswordSelected([FromForm] string[] studentIds)
        {
            try
            {
                if (studentIds == null || !studentIds.Any())
                {
                    return Json(new { success = false, message = "No students selected." });
                }

                int count = 0;
                var errors = new List<string>();

                foreach (var studentId in studentIds)
                {
                    var user = await _userManager.FindByNameAsync(studentId);
                    if (user == null)
                    {
                        errors.Add($"User {studentId} not found");
                        continue;
                    }

                    // Remove existing password and set new one (Student ID)
                    var removeResult = await _userManager.RemovePasswordAsync(user);
                    if (!removeResult.Succeeded)
                    {
                        errors.Add($"Failed to reset password for {studentId}");
                        continue;
                    }

                    var addResult = await _userManager.AddPasswordAsync(user, studentId);
                    if (!addResult.Succeeded)
                    {
                        errors.Add($"Failed to set new password for {studentId}");
                        continue;
                    }

                    count++;
                }

                await LogAction("Bulk Password Reset", $"Reset password for {count} selected students");

                if (errors.Any())
                {
                    return Json(new
                    {
                        success = true,
                        count = count,
                        message = $"Reset {count} passwords. Errors: {string.Join(", ", errors)}"
                    });
                }

                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting passwords for selected students");
                return Json(new { success = false, message = "An error occurred while resetting passwords." });
            }
        }

        // =========================================================
        // ACTION: UPDATE FEE STATUS - DISABLED FOR ADMIN
        // Admin cannot change payment status - handled by Treasurers
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFeeStatus(int feeId, string status)
        {
            // DISABLED: Admin cannot change payment status - Treasurers handle this
            TempData["Error"] = "Admin cannot change payment status. Payment collection is handled by Class Treasurers and validated by Org Treasurer.";
            return RedirectToAction(nameof(Payments));

            /* ORIGINAL CODE - DISABLED
            var fee = await _context.Fees.Include(f => f.StudentNumNavigation).FirstOrDefaultAsync(f => f.FeeId == feeId);
            if (fee == null)
            {
                TempData["Error"] = "Fee record not found.";
                return RedirectToAction(nameof(Payments));
            }

            // Normalize status to proper casing
            var normalizedStatus = status?.Trim();
            if (string.Equals(normalizedStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                normalizedStatus = "Paid";
            }
            else if (string.Equals(normalizedStatus, "unpaid", StringComparison.OrdinalIgnoreCase))
            {
                normalizedStatus = "Unpaid";
            }

            var previousStatus = fee.FeeStatus;
            fee.FeeStatus = normalizedStatus;
            _context.Fees.Update(fee);

            // If marking as Paid, create a PaymentTransaction record for audit trail
            if (normalizedStatus == "Paid" && previousStatus?.ToLower() != "paid")
            {
                // Get the current admin user
                var adminUser = await _userManager.GetUserAsync(User);
                var adminName = adminUser?.UserName ?? "Admin";

                // Create payment transaction record
                var transaction = new PaymentTransaction
                {
                    FeeId = feeId,
                    StudentNum = fee.StudentNum ?? "",
                    Amount = fee.Amount ?? 0,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "Admin Override",
                    ProcessedBy = adminName,
                    TransactionReference = $"ADMIN-{DateTime.Now:yyyyMMddHHmmss}",
                    Notes = $"Payment confirmed by Admin ({adminName})"
                };

                _context.PaymentTransactions.Add(transaction);

                // Send notification to student
                if (!string.IsNullOrEmpty(fee.StudentNum))
                {
                    _context.Notifications.Add(new Notification
                    {
                        StudentNum = fee.StudentNum,
                        Title = "Payment Confirmed",
                        Message = $"Your payment of ₱{fee.Amount:N2} for '{fee.FeeName}' has been confirmed by the administrator.",
                        NotificationType = "Payment",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = adminName
                    });
                }
            }

            await _context.SaveChangesAsync();

            await LogAction("Update Fee Status", $"Updated fee ID {feeId} status from '{previousStatus}' to '{normalizedStatus}'");
            TempData["Message"] = $"Fee status updated to '{normalizedStatus}' successfully.";

            return RedirectToAction(nameof(Payments));
            END OF DISABLED CODE */
        }

        // =========================================================
        // ACTION: RECORD PARTIAL FEE PAYMENT - DISABLED FOR ADMIN
        // Admin cannot record payments - handled by Treasurers
        // Partial payments have been removed per business requirements
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordFeePayment(int feeId, decimal paymentAmount, string? paymentMethod, string? transactionRef, string? notes)
        {
            // DISABLED: Admin cannot record payments and partial payments are removed
            TempData["Error"] = "Admin cannot record payments. Payment collection is handled by Class Treasurers (full payment only) and validated by Org Treasurer.";
            return RedirectToAction(nameof(Payments));

            /* ORIGINAL CODE - DISABLED
            try
            {
                var fee = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);

                if (fee == null)
                {
                    TempData["Error"] = $"Fee with ID {feeId} not found.";
                    return RedirectToAction(nameof(Payments));
                }

                var remainingBalance = (fee.Amount ?? 0) - fee.AmountPaid;
                if (paymentAmount <= 0)
                {
                    TempData["Error"] = "Payment amount must be greater than zero.";
                    return RedirectToAction(nameof(Payments));
                }

                if (paymentAmount > remainingBalance)
                {
                    TempData["Error"] = $"Payment amount (?{paymentAmount:N2}) cannot exceed the remaining balance (?{remainingBalance:N2}).";
                    return RedirectToAction(nameof(Payments));
                }

                var adminUser = await _userManager.GetUserAsync(User);
                var adminName = adminUser?.UserName ?? "Admin";

                fee.AmountPaid += paymentAmount;

                if (fee.AmountPaid >= (fee.Amount ?? 0))
                {
                    fee.FeeStatus = "Paid";
                }
                else if (fee.AmountPaid > 0)
                {
                    fee.FeeStatus = "Partial";
                }

                _context.Fees.Update(fee);

                var transaction = new PaymentTransaction
                {
                    FeeId = feeId,
                    StudentNum = fee.StudentNum ?? "",
                    Amount = paymentAmount,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod.Trim(),
                    ProcessedBy = adminName,
                    TransactionReference = string.IsNullOrWhiteSpace(transactionRef) ? null : transactionRef.Trim(),
                    Notes = string.IsNullOrWhiteSpace(notes) ? "Partial payment recorded by Admin" : notes.Trim()
                };
                _context.PaymentTransactions.Add(transaction);

                if (!string.IsNullOrEmpty(fee.StudentNum))
                {
                    var newBalance = (fee.Amount ?? 0) - fee.AmountPaid;
                    var message = newBalance <= 0
                        ? $"Your payment of ₱{paymentAmount:N2} for '{fee.FeeName}' has been confirmed. This fee is now FULLY PAID."
                        : $"Your payment of ₱{paymentAmount:N2} for '{fee.FeeName}' has been confirmed. Remaining balance: ₱{newBalance:N2}.";

                    _context.Notifications.Add(new Notification
                    {
                        StudentNum = fee.StudentNum,
                        Title = "Payment Recorded",
                        Message = message,
                        NotificationType = "Payment",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = adminName
                    });
                }

                await _context.SaveChangesAsync();
                await LogAction("Record Fee Payment", $"Recorded payment of ₱{paymentAmount:N2} for fee ID {feeId}");

                var statusMsg = fee.FeeStatus == "Paid" ? "FULLY PAID" : $"Partial (Balance: ₱{(fee.Amount ?? 0) - fee.AmountPaid:N2})";
                TempData["Message"] = $"Payment of ₱{paymentAmount:N2} recorded successfully. Status: {statusMsg}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error recording payment: {ex.Message}";
            }

            return RedirectToAction(nameof(Payments));
            END OF DISABLED CODE */
        }

        // =========================================================
        // ACTION: RECORD PARTIAL FINE PAYMENT - DISABLED FOR ADMIN
        // Admin cannot record payments - handled by Treasurers
        // Partial payments have been removed per business requirements
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordFinePayment(int fineId, decimal paymentAmount, string? paymentMethod, string? transactionRef, string? notes)
        {
            // DISABLED: Admin cannot record payments and partial payments are removed
            TempData["Error"] = "Admin cannot record payments. Payment collection is handled by Class Treasurers (full payment only) and validated by Org Treasurer.";
            return RedirectToAction(nameof(Fines));

            /* ORIGINAL CODE - DISABLED
            try
            {
                var fine = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FineId == fineId);

                if (fine == null)
                {
                    TempData["Error"] = $"Fine with ID {fineId} not found.";
                    return RedirectToAction(nameof(Fines));
                }

                if (fine.FinesStatus?.ToLower() == "excused" || fine.FinesStatus?.ToLower() == "waived")
                {
                    TempData["Error"] = "Cannot record payment for an excused fine.";
                    return RedirectToAction(nameof(Fines));
                }

                var remainingBalance = (fine.Amount ?? 0) - fine.AmountPaid;
                if (paymentAmount <= 0)
                {
                    TempData["Error"] = "Payment amount must be greater than zero.";
                    return RedirectToAction(nameof(Fines));
                }

                if (paymentAmount > remainingBalance)
                {
                    TempData["Error"] = $"Payment amount (₱{paymentAmount:N2}) cannot exceed the remaining balance (₱{remainingBalance:N2}).";
                    return RedirectToAction(nameof(Fines));
                }

                var adminUser = await _userManager.GetUserAsync(User);
                var adminName = adminUser?.UserName ?? "Admin";

                fine.AmountPaid += paymentAmount;

                if (fine.AmountPaid >= (fine.Amount ?? 0))
                {
                    fine.FinesStatus = "Paid";
                }
                else if (fine.AmountPaid > 0)
                {
                    fine.FinesStatus = "Partial";
                }

                _context.Fines.Update(fine);

                var transaction = new FinePaymentTransaction
                {
                    FineId = fineId,
                    StudentNum = fine.StudentNum ?? "",
                    Amount = paymentAmount,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod.Trim(),
                    ProcessedBy = adminName,
                    TransactionReference = string.IsNullOrWhiteSpace(transactionRef) ? null : transactionRef.Trim(),
                    Notes = string.IsNullOrWhiteSpace(notes) ? "Partial payment recorded by Admin" : notes.Trim()
                };
                _context.FinePaymentTransactions.Add(transaction);

                if (!string.IsNullOrEmpty(fine.StudentNum))
                {
                    var newBalance = (fine.Amount ?? 0) - fine.AmountPaid;
                    var message = newBalance <= 0
                        ? $"Your payment of ₱{paymentAmount:N2} for '{fine.Description ?? "Fine"}' has been confirmed. This fine is now FULLY PAID."
                        : $"Your payment of ₱{paymentAmount:N2} for '{fine.Description ?? "Fine"}' has been confirmed. Remaining balance: ₱{newBalance:N2}.";

                    _context.Notifications.Add(new Notification
                    {
                        StudentNum = fine.StudentNum,
                        Title = "Fine Payment Recorded",
                        Message = message,
                        NotificationType = "Payment",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = adminName
                    });
                }

                await _context.SaveChangesAsync();
                await LogAction("Record Fine Payment", $"Recorded payment of ₱{paymentAmount:N2} for fine ID {fineId}");

                var statusMsg = fine.FinesStatus == "Paid" ? "FULLY PAID" : $"Partial (Balance: ₱{(fine.Amount ?? 0) - fine.AmountPaid:N2})";
                TempData["Message"] = $"Payment of ₱{paymentAmount:N2} recorded successfully. Status: {statusMsg}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error recording payment: {ex.Message}";
            }

            return RedirectToAction(nameof(Fines));
            END OF DISABLED CODE */
        }

        // =========================================================
        // ACTION: EXPORT PAYMENTS TO EXCEL
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ExportPayments(string feeNameFilter, string programFilter, string yearFilter, string statusFilter, string acadYearFilter)
        {
            try
            {
                var query = _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .AsQueryable();

                // Apply Filters
                if (!string.IsNullOrEmpty(feeNameFilter))
                    query = query.Where(f => f.FeeName == feeNameFilter);

                if (!string.IsNullOrEmpty(programFilter))
                    query = query.Where(f => f.StudentNumNavigation.Course.Contains(programFilter));

                if (!string.IsNullOrEmpty(yearFilter))
                    query = query.Where(f => f.StudentNumNavigation.YearLevelSection.Contains(yearFilter));

                if (!string.IsNullOrEmpty(statusFilter))
                {
                    if (statusFilter == "paid") query = query.Where(f => f.FeeStatus == "Paid" || f.FeeStatus == "Completed");
                    else if (statusFilter == "pending") query = query.Where(f => f.FeeStatus == "Pending" || f.FeeStatus == "Unpaid" || f.FeeStatus == null);
                }

                if (!string.IsNullOrEmpty(acadYearFilter))
                    query = query.Where(f => f.AcadYear.Contains(acadYearFilter));

                var fees = await query.OrderBy(f => f.FeeStatus).ThenBy(f => f.StudentNum).ToListAsync();

                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Payments");

                    // Headers
                    ws.Cell(1, 1).Value = "Ref ID";
                    ws.Cell(1, 2).Value = "Student ID";
                    ws.Cell(1, 3).Value = "Student Name";
                    ws.Cell(1, 4).Value = "Program";
                    ws.Cell(1, 5).Value = "Year/Section";
                    ws.Cell(1, 6).Value = "Fee Name";
                    ws.Cell(1, 7).Value = "Academic Year";
                    ws.Cell(1, 8).Value = "Amount";
                    ws.Cell(1, 9).Value = "Status";
                    ws.Cell(1, 10).Value = "Due Date";

                    // Styling
                    var headerRange = ws.Range(1, 1, 1, 10);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D4AF37");
                    headerRange.Style.Font.FontColor = XLColor.White;

                    // Data
                    int row = 2;
                    foreach (var f in fees)
                    {
                        ws.Cell(row, 1).Value = f.FeeId;
                        ws.Cell(row, 2).Value = f.StudentNum;
                        ws.Cell(row, 3).Value = f.StudentNumNavigation?.FullName ?? "Unknown";
                        ws.Cell(row, 4).Value = f.StudentNumNavigation?.Course ?? "";
                        ws.Cell(row, 5).Value = f.StudentNumNavigation?.YearLevelSection ?? "";
                        ws.Cell(row, 6).Value = f.FeeName;
                        ws.Cell(row, 7).Value = f.AcadYear;
                        ws.Cell(row, 8).Value = f.Amount;
                        ws.Cell(row, 9).Value = f.FeeStatus;
                        ws.Cell(row, 10).Value = f.FeesDueDate?.ToString("yyyy-MM-dd");
                        row++;
                    }

                    ws.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Position = 0;
                        string fileName = $"iBITS_Payments_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting payments");
                TempData["Error"] = "Failed to export data.";
                return RedirectToAction(nameof(Payments));
            }
        }

        // =========================================================
        // AJAX: TOGGLE FEE PAYMENT STATUS (Checkbox)
        // =========================================================
        /// <summary>
        /// DISABLED: Admin cannot change payment status.
        /// Payment status changes are handled by Treasurers only.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeePayment(int feeId, bool markAsPaid)
        {
            // DISABLED: Admin cannot change payment status - Treasurers handle this
            return Json(new { success = false, message = "Admin cannot change payment status. Please contact the Org Treasurer." });

            /* ORIGINAL CODE - DISABLED
            try
            {
                var fee = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);

                if (fee == null)
                {
                    return Json(new { success = false, message = "Fee record not found." });
                }

                var adminUser = await _userManager.GetUserAsync(User);
                var adminName = adminUser?.UserName ?? "Admin";

                if (markAsPaid)
                {
                    var remainingBalance = (fee.Amount ?? 0) - fee.AmountPaid;
                    
                    if (remainingBalance <= 0)
                    {
                        return Json(new { success = false, message = "This fee is already fully paid." });
                    }

                    fee.AmountPaid = fee.Amount ?? 0;
                    fee.FeeStatus = "Paid";
                    _context.Fees.Update(fee);

                    var transaction = new PaymentTransaction
                    {
                        FeeId = feeId,
                        StudentNum = fee.StudentNum ?? "",
                        Amount = remainingBalance,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "Cash",
                        ProcessedBy = adminName,
                        TransactionReference = $"CHK-{DateTime.Now:yyyyMMddHHmmss}",
                        Notes = "Payment marked via checkbox"
                    };
                    _context.PaymentTransactions.Add(transaction);

                    if (!string.IsNullOrEmpty(fee.StudentNum))
                    {
                        _context.Notifications.Add(new Notification
                        {
                            StudentNum = fee.StudentNum,
                            Title = "Payment Confirmed",
                            Message = $"Your payment of ₱{remainingBalance:N2} for '{fee.FeeName}' has been confirmed.",
                            NotificationType = "Payment",
                            NotificationDate = DateTime.Now,
                            IsRead = false,
                            SentBy = adminName
                        });
                    }

                    await _context.SaveChangesAsync();
                    await LogAction("Toggle Fee Payment", $"Marked fee ID {feeId} as Paid via checkbox");

                    return Json(new { 
                        success = true, 
                        message = "Payment confirmed successfully.",
                        newStatus = "Paid",
                        amountPaid = fee.AmountPaid,
                        balance = 0
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Use the Revoke function to undo payments." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling fee payment");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
            END OF DISABLED CODE */
        }

        // =========================================================
        // AJAX: REVOKE FEE PAYMENT - DISABLED FOR ADMIN
        // =========================================================
        /// <summary>
        /// DISABLED: Admin cannot revoke payment status.
        /// Payment revocation is handled by Class Treasurers (before remittance only).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeFeePayment(int feeId)
        {
            // DISABLED: Admin cannot revoke payments - Class Treasurers handle this
            return Json(new { success = false, message = "Admin cannot revoke payments. Please contact the Class Treasurer." });

            /* ORIGINAL CODE - DISABLED
            try
            {
                var fee = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);

                if (fee == null)
                {
                    return Json(new { success = false, message = "Fee record not found." });
                }

                var adminUser = await _userManager.GetUserAsync(User);
                var adminName = adminUser?.UserName ?? "Admin";

                var previousAmountPaid = fee.AmountPaid;

                var transactions = await _context.PaymentTransactions
                    .Where(t => t.FeeId == feeId)
                    .ToListAsync();

                if (transactions.Any())
                {
                    _context.PaymentTransactions.RemoveRange(transactions);
                }

                fee.AmountPaid = 0;
                fee.FeeStatus = "Unpaid";
                _context.Fees.Update(fee);

                if (!string.IsNullOrEmpty(fee.StudentNum))
                {
                    _context.Notifications.Add(new Notification
                    {
                        StudentNum = fee.StudentNum,
                        Title = "Payment Revoked",
                        Message = $"Your payment record for '{fee.FeeName}' (₱{previousAmountPaid:N2}) has been revoked by the administrator.",
                        NotificationType = "Payment",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = adminName
                    });
                }

                await _context.SaveChangesAsync();
                await LogAction("Revoke Fee Payment", $"Revoked payment for fee ID {feeId}. Previous: ₱{previousAmountPaid:N2}, {transactions.Count} transaction(s) deleted.");

                return Json(new { 
                    success = true, 
                    message = $"Payment revoked. {transactions.Count} transaction(s) deleted.",
                    newStatus = "Unpaid",
                    amountPaid = 0,
                    balance = fee.Amount ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking fee payment");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
            END OF DISABLED CODE */
        }

        // =========================================================
        // AJAX: TOGGLE FINE PAYMENT STATUS - DISABLED FOR ADMIN
        // =========================================================
        /// <summary>
        /// DISABLED: Admin cannot change fine payment status.
        /// Payment status changes are handled by Treasurers only.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFinePayment(int fineId, bool markAsPaid)
        {
            // DISABLED: Admin cannot change payment status - Treasurers handle this
            return Json(new { success = false, message = "Admin cannot change payment status. Please contact the Org Treasurer." });

            /* ORIGINAL CODE - DISABLED
            try
            {
                var fine = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FineId == fineId);

                if (fine == null)
                {
                    return Json(new { success = false, message = "Fine record not found." });
                }

                if (fine.FinesStatus?.ToLower() == "excused" || fine.FinesStatus?.ToLower() == "waived")
                {
                    return Json(new { success = false, message = "Cannot modify an excused fine." });
                }

                var adminUser = await _userManager.GetUserAsync(User);
                var adminName = adminUser?.UserName ?? "Admin";

                if (markAsPaid)
                {
                    var remainingBalance = (fine.Amount ?? 0) - fine.AmountPaid;
                    
                    if (remainingBalance <= 0)
                    {
                        return Json(new { success = false, message = "This fine is already fully paid." });
                    }

                    fine.AmountPaid = fine.Amount ?? 0;
                    fine.FinesStatus = "Paid";
                    _context.Fines.Update(fine);

                    var transaction = new FinePaymentTransaction
                    {
                        FineId = fineId,
                        StudentNum = fine.StudentNum ?? "",
                        Amount = remainingBalance,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "Cash",
                        ProcessedBy = adminName,
                        TransactionReference = $"CHK-{DateTime.Now:yyyyMMddHHmmss}",
                        Notes = "Payment marked via checkbox"
                    };
                    _context.FinePaymentTransactions.Add(transaction);

                    if (!string.IsNullOrEmpty(fine.StudentNum))
                    {
                        _context.Notifications.Add(new Notification
                        {
                            StudentNum = fine.StudentNum,
                            Title = "Fine Payment Confirmed",
                            Message = $"Your payment of ₱{remainingBalance:N2} for '{fine.Description ?? "Fine"}' has been confirmed.",
                            NotificationType = "Payment",
                            NotificationDate = DateTime.Now,
                            IsRead = false,
                            SentBy = adminName
                        });
                    }

                    await _context.SaveChangesAsync();
                    await LogAction("Toggle Fine Payment", $"Marked fine ID {fineId} as Paid via checkbox");

                    return Json(new { 
                        success = true, 
                        message = "Fine payment confirmed successfully.",
                        newStatus = "Paid",
                        amountPaid = fine.AmountPaid,
                        balance = 0
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Use the Revoke function to undo payments." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling fine payment");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
            END OF DISABLED CODE */
        }

        // =========================================================
        // AJAX: REVOKE FINE PAYMENT - DISABLED FOR ADMIN
        // =========================================================
        /// <summary>
        /// DISABLED: Admin cannot revoke fine payment status.
        /// Payment revocation is handled by Class Treasurers (before remittance only).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeFinePayment(int fineId)
        {
            // DISABLED: Admin cannot revoke payments - Class Treasurers handle this
            return Json(new { success = false, message = "Admin cannot revoke payments. Please contact the Class Treasurer." });

            /* ORIGINAL CODE - DISABLED
            try
            {
                var fine = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FineId == fineId);

                if (fine == null)
                {
                    return Json(new { success = false, message = "Fine record not found." });
                }

                if (fine.FinesStatus?.ToLower() == "excused" || fine.FinesStatus?.ToLower() == "waived")
                {
                    return Json(new { success = false, message = "Cannot revoke an excused fine." });
                }

                var adminUser = await _userManager.GetUserAsync(User);
                var adminName = adminUser?.UserName ?? "Admin";

                var previousAmountPaid = fine.AmountPaid;

                var transactions = await _context.FinePaymentTransactions
                    .Where(t => t.FineId == fineId)
                    .ToListAsync();

                if (transactions.Any())
                {
                    _context.FinePaymentTransactions.RemoveRange(transactions);
                }

                fine.AmountPaid = 0;
                fine.FinesStatus = "Unpaid";
                _context.Fines.Update(fine);

                if (!string.IsNullOrEmpty(fine.StudentNum))
                {
                    _context.Notifications.Add(new Notification
                    {
                        StudentNum = fine.StudentNum,
                        Title = "Fine Payment Revoked",
                        Message = $"Your payment record for '{fine.Description ?? "Fine"}' (₱{previousAmountPaid:N2}) has been revoked.",
                        NotificationType = "Payment",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = adminName
                    });
                }

                await _context.SaveChangesAsync();
                await LogAction("Revoke Fine Payment", $"Revoked payment for fine ID {fineId}. Previous: ₱{previousAmountPaid:N2}, {transactions.Count} transaction(s) deleted.");

                return Json(new { 
                    success = true, 
                    message = $"Fine payment revoked. {transactions.Count} transaction(s) deleted.",
                    newStatus = "Unpaid",
                    amountPaid = 0,
                    balance = fine.Amount ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking fine payment");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
            END OF DISABLED CODE */
        }

        // =========================================================
        // AJAX: MARK FINE AS EXCUSED - DISABLED FOR ADMIN
        // Note: Excused status is now handled by Org Secretary only
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFineExcused(int fineId, string? reason)
        {
            try
            {
                var fine = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FineId == fineId);

                if (fine == null)
                {
                    return Json(new { success = false, message = "Fine record not found." });
                }

                if (fine.FinesStatus?.ToLower() == "paid")
                {
                    return Json(new { success = false, message = "Cannot excuse a paid fine. Revoke payment first." });
                }

                var adminUser = await _userManager.GetUserAsync(User);
                var adminName = adminUser?.UserName ?? "Admin";

                fine.FinesStatus = "Excused";
                fine.AmountPaid = 0;
                _context.Fines.Update(fine);

                var transactions = await _context.FinePaymentTransactions
                    .Where(t => t.FineId == fineId)
                    .ToListAsync();

                if (transactions.Any())
                {
                    _context.FinePaymentTransactions.RemoveRange(transactions);
                }

                if (!string.IsNullOrEmpty(fine.StudentNum))
                {
                    _context.Notifications.Add(new Notification
                    {
                        StudentNum = fine.StudentNum,
                        Title = "Fine Excused",
                        Message = $"Your fine for '{fine.Description ?? "Fine"}' (₱{fine.Amount:N2}) has been excused. Reason: {reason ?? "Not specified"}",
                        NotificationType = "Fine",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = adminName
                    });
                }

                await _context.SaveChangesAsync();
                await LogAction("Mark Fine Excused", $"Marked fine ID {fineId} as Excused. Reason: {reason ?? "Not specified"}");

                return Json(new
                {
                    success = true,
                    message = "Fine has been marked as excused.",
                    newStatus = "Excused"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking fine as excused");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // =========================================================
        // ACTION: DELETE FEE RECORD (Admin Only)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFee(int feeId)
        {
            try
            {
                var fee = await _context.Fees.FindAsync(feeId);
                if (fee == null)
                {
                    TempData["Error"] = "Fee record not found.";
                    return RedirectToAction(nameof(Payments));
                }

                var transactions = await _context.PaymentTransactions
                    .Where(t => t.FeeId == feeId)
                    .ToListAsync();

                if (transactions.Any())
                {
                    _context.PaymentTransactions.RemoveRange(transactions);
                }

                _context.Fees.Remove(fee);
                await _context.SaveChangesAsync();

                await LogAction("Delete Fee", $"Deleted fee ID {feeId} ({fee.FeeName}) and {transactions.Count} transaction(s)");
                TempData["Message"] = "Fee record deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fee");
                TempData["Error"] = $"Error deleting fee: {ex.Message}";
            }

            return RedirectToAction(nameof(Payments));
        }

        // =========================================================
        // ACTION: DELETE FINE RECORD (Admin Only)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFine(int fineId)
        {
            try
            {
                var fine = await _context.Fines.FindAsync(fineId);
                if (fine == null)
                {
                    TempData["Error"] = "Fine record not found.";
                    return RedirectToAction(nameof(Fines));
                }

                var transactions = await _context.FinePaymentTransactions
                    .Where(t => t.FineId == fineId)
                    .ToListAsync();

                if (transactions.Any())
                {
                    _context.FinePaymentTransactions.RemoveRange(transactions);
                }

                _context.Fines.Remove(fine);
                await _context.SaveChangesAsync();

                await LogAction("Delete Fine", $"Deleted fine ID {fineId} ({fine.Description}) and {transactions.Count} transaction(s)");
                TempData["Message"] = "Fine record deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fine");
                TempData["Error"] = $"Error deleting fine: {ex.Message}";
            }

            return RedirectToAction(nameof(Fines));
        }



        //ACTION EXPORT ACTIVITY LOGS
        [HttpGet]
        public IActionResult ExportActivityLogs()
        {
            // 1. Fetch the data from your database
            // Replace '_context.ActivityLogs' with your actual database call
            var logs = _context.ActivityLogs.OrderByDescending(x => x.Timestamp).ToList();

            // 2. Create the CSV Content
            var builder = new StringBuilder();
            builder.AppendLine("Timestamp,Action,Description,Performed By,IP Address");

            foreach (var log in logs)
            {
                // Use quotes to handle commas within descriptions
                builder.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.Action}\",\"{log.Description}\",\"{log.PerformedBy}\",\"{log.IpAddress ?? "Unknown"}\"");
            }

            // 3. Return the file
            var csvData = Encoding.UTF8.GetBytes(builder.ToString());
            var fileName = $"ActivityLogs_{DateTime.Now:yyyyMMdd_HHmm}.csv";

            return File(csvData, "text/csv", fileName);
        }

    }

    public class ManualFineBatchViewModel
    {
        public string BatchId { get; set; }
        public string Reason { get; set; }
        public decimal Amount { get; set; }
        public DateOnly? DateCreated { get; set; }
        public int StudentCount { get; set; }
    }

    public class ManualPaymentBatchViewModel
    {
        public string BatchId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime? DateCreated { get; set; }
        public int StudentCount { get; set; }
    }
}



