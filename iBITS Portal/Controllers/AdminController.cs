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

            // DYNAMIC: Populate Year filter options from actual data
            // Handle formats: "1-1", "2-2", "BSIT 3-1", "DIT 3-1", etc.
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

            ViewBag.Sections = allYearSections
                .Select(ys => ExtractSectionFromYearLevelSection(ys))
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
        public async Task<IActionResult> GetDashboardData()
        {
            var data = await BuildDashboardData();
            return Json(data);
        }

        // =========================================================
        // AJAX: Get Chart Details on Click
        // Returns detailed breakdown when user clicks a chart segment
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetChartDetails(string chartType, string segment, string status)
        {
            try
            {
                // Helper to extract year level
                int ExtractYearLevel(string? yearLevelSection)
                {
                    if (string.IsNullOrWhiteSpace(yearLevelSection)) return 0;
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
                            if (year >= 1 && year <= 4) return year;
                        }
                    }
                    return 0;
                }

                // Parse segment (e.g., "BSIT3" -> program="BSIT", year=3)
                var program = segment.StartsWith("BSIT") ? "BSIT" : segment.StartsWith("DIT") ? "DIT" : "";
                var yearStr = segment.Replace("BSIT", "").Replace("DIT", "");
                int.TryParse(yearStr, out int yearLevel);

                if (string.IsNullOrEmpty(program) || yearLevel == 0)
                {
                    return Json(new { success = false, message = "Invalid segment" });
                }

                // Get fees based on chart type and status
                var fees = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .Where(f => f.StudentNumNavigation != null &&
                                f.StudentNumNavigation.Course != null &&
                                f.StudentNumNavigation.Course.ToUpper().Contains(program))
                    .ToListAsync();

                // Filter by year level
                fees = fees.Where(f => ExtractYearLevel(f.StudentNumNavigation!.YearLevelSection) == yearLevel).ToList();

                // Filter by status (paid or pending)
                if (status == "paid")
                {
                    fees = fees.Where(f => !string.IsNullOrWhiteSpace(f.FeeStatus) && f.FeeStatus.ToUpper() == "PAID").ToList();
                }
                else if (status == "pending")
                {
                    fees = fees.Where(f => string.IsNullOrWhiteSpace(f.FeeStatus) ||
                                          f.FeeStatus.ToUpper() == "PENDING" ||
                                          f.FeeStatus.ToUpper() == "UNPAID").ToList();
                }

                // Group by fee name to get breakdown
                var feeBreakdown = fees
                    .GroupBy(f => f.FeeName ?? "Unnamed Fee")
                    .Select(g => new
                    {
                        FeeName = g.Key,
                        Amount = g.First().Amount ?? 0,
                        StudentCount = g.Count(),
                        TotalAmount = g.Sum(f => f.Amount ?? 0)
                    })
                    .OrderByDescending(f => f.TotalAmount)
                    .ToList();

                // Get student list
                var students = fees
                    .Where(f => f.StudentNumNavigation != null)
                    .Select(f => new
                    {
                        StudentNum = f.StudentNumNavigation!.StudentNum,
                        Name = $"{f.StudentNumNavigation.StudentFn} {f.StudentNumNavigation.StudentLn}",
                        FeeName = f.FeeName ?? "Unnamed Fee",
                        Amount = f.Amount ?? 0,
                        Status = f.FeeStatus ?? "Pending",
                        DueDate = f.FeesDueDate.HasValue ? f.FeesDueDate.Value.ToString("MMM dd, yyyy") : "No Due Date"
                    })
                    .OrderBy(s => s.Name)
                    .Take(50) // Limit to 50 students
                    .ToList();

                var totalAmount = fees.Sum(f => f.Amount ?? 0);
                var studentCount = fees.Select(f => f.StudentNum).Distinct().Count();

                return Json(new
                {
                    success = true,
                    segment = segment,
                    program = program,
                    yearLevel = yearLevel,
                    status = status,
                    summary = new
                    {
                        totalAmount = totalAmount,
                        studentCount = studentCount,
                        feeCount = fees.Count,
                        averagePerStudent = studentCount > 0 ? totalAmount / studentCount : 0
                    },
                    feeBreakdown = feeBreakdown,
                    students = students
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart details");
                return Json(new { success = false, message = "Error loading details" });
            }
        }

        // =========================================================
        // AJAX: Get Fines Details on Click
        // Returns detailed breakdown when user clicks fines chart segment
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetFinesDetails(string segment)
        {
            try
            {
                // Helper to extract year level
                int ExtractYearLevel(string? yearLevelSection)
                {
                    if (string.IsNullOrWhiteSpace(yearLevelSection)) return 0;
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
                            if (year >= 1 && year <= 4) return year;
                        }
                    }
                    return 0;
                }

                // Parse segment
                var program = segment.StartsWith("BSIT") ? "BSIT" : segment.StartsWith("DIT") ? "DIT" : "";
                var yearStr = segment.Replace("BSIT", "").Replace("DIT", "");
                int.TryParse(yearStr, out int yearLevel);

                if (string.IsNullOrEmpty(program) || yearLevel == 0)
                {
                    return Json(new { success = false, message = "Invalid segment" });
                }

                // Get fines with attendance and student info
                var fines = await _context.Fines
                    .Include(f => f.Attendance)
                        .ThenInclude(a => a.StudentNumNavigation)
                    .Include(f => f.Attendance)
                        .ThenInclude(a => a.Event)
                    .Where(f => f.Attendance != null &&
                                f.Attendance.StudentNumNavigation != null &&
                                f.Attendance.StudentNumNavigation.Course != null &&
                                f.Attendance.StudentNumNavigation.Course.ToUpper().Contains(program))
                    .ToListAsync();

                // Filter by year level
                fines = fines.Where(f => ExtractYearLevel(f.Attendance!.StudentNumNavigation!.YearLevelSection) == yearLevel).ToList();

                // Group by event to get breakdown
                var eventBreakdown = fines
                    .Where(f => f.Attendance?.Event != null)
                    .GroupBy(f => f.Attendance!.Event!.EventName ?? "Unknown Event")
                    .Select(g => new
                    {
                        EventName = g.Key,
                        FineAmount = g.First().Amount ?? 0,
                        StudentCount = g.Count(),
                        TotalAmount = g.Sum(f => f.Amount ?? 0)
                    })
                    .OrderByDescending(e => e.TotalAmount)
                    .ToList();

                // Get student list
                var students = fines
                    .Where(f => f.Attendance?.StudentNumNavigation != null)
                    .Select(f => new
                    {
                        StudentNum = f.Attendance!.StudentNumNavigation!.StudentNum,
                        Name = $"{f.Attendance.StudentNumNavigation.StudentFn} {f.Attendance.StudentNumNavigation.StudentLn}",
                        EventName = f.Attendance.Event?.EventName ?? "Unknown Event",
                        Amount = f.Amount ?? 0,
                        Status = f.FinesStatus ?? "Pending",
                        DueDate = f.FinesDueDate.HasValue ? f.FinesDueDate.Value.ToString("MMM dd, yyyy") : "No Due Date"
                    })
                    .OrderBy(s => s.Name)
                    .Take(50)
                    .ToList();

                var totalAmount = fines.Sum(f => f.Amount ?? 0);
                var studentCount = fines.Select(f => f.Attendance?.StudentNum).Distinct().Count();

                return Json(new
                {
                    success = true,
                    segment = segment,
                    program = program,
                    yearLevel = yearLevel,
                    summary = new
                    {
                        totalAmount = totalAmount,
                        studentCount = studentCount,
                        fineCount = fines.Count,
                        averagePerStudent = studentCount > 0 ? totalAmount / studentCount : 0
                    },
                    eventBreakdown = eventBreakdown,
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
                    .Where(s => s.IsArchived != true && !string.IsNullOrEmpty(s.StudentEmail))
                    .ToListAsync();

                // Apply filters
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

                if (!students.Any())
                {
                    return Json(new { success = false, message = "No students found matching the criteria." });
                }

                // Create notification records for each student
                var notificationCount = 0;
                foreach (var student in students)
                {
                    var notification = new Notification
                    {
                        StudentNum = student.StudentNum,
                        Title = subject,
                        Message = message,
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        NotificationType = "Admin"
                    };
                    _context.Notifications.Add(notification);
                    notificationCount++;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Notification sent: '{subject}' to {notificationCount} students");

                return Json(new
                {
                    success = true,
                    message = $"Notification sent successfully to {notificationCount} students.",
                    recipientCount = notificationCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                return Json(new { success = false, message = "Error sending notification. Please try again." });
            }
        }

        // =========================================================
        // AJAX: Get Notification Preview (recipient count)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetNotificationPreview(string recipientFilter, string? programFilter, string? yearFilter)
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
                    .Where(s => s.IsArchived != true)
                    .ToListAsync();

                // Apply filters
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
        // AJAX: Export Dashboard Report
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> ExportDashboardReport(string format, string reportType)
        {
            try
            {
                var data = await BuildDashboardData();
                var students = await _context.Students.Where(s => s.IsArchived != true).ToListAsync();
                var fees = await _context.Fees.Include(f => f.StudentNumNavigation).ToListAsync();
                var events = await _context.Events.ToListAsync();
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

                    ws.Cell(5, 1).Value = "Total Students (BSIT + DIT)";
                    ws.Cell(5, 2).Value = data.TotalStudents;
                    ws.Cell(6, 1).Value = "BSIT Students";
                    ws.Cell(6, 2).Value = data.StudentsPerProgram["BSIT"];
                    ws.Cell(7, 1).Value = "DIT Students";
                    ws.Cell(7, 2).Value = data.StudentsPerProgram["DIT"];
                    ws.Cell(8, 1).Value = "Total Expected (Fees)";
                    ws.Cell(8, 2).Value = data.TotalExpected;
                    ws.Cell(9, 1).Value = "Total Collected";
                    ws.Cell(9, 2).Value = data.TotalCollected;
                    ws.Cell(10, 1).Value = "Total Pending";
                    ws.Cell(10, 2).Value = data.TotalPending;
                    ws.Cell(11, 1).Value = "Collection Rate";
                    ws.Cell(11, 2).Value = $"{data.CollectionRate}%";
                    ws.Cell(12, 1).Value = "Total Fines";
                    ws.Cell(12, 2).Value = data.TotalFines;

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
            var data = await BuildDashboardData();

            ViewBag.StudentsPerProgram = data.StudentsPerProgram;
            ViewBag.TotalStudents = data.TotalStudents;
            ViewBag.YearLevelCounts = data.YearLevelCounts;
            ViewBag.MonthlyActiveStudents = data.MonthlyActiveStudents;
            ViewBag.ArchiveCount = data.ArchiveCount;
            ViewBag.ActiveInactive = data.ActiveInactive;
            // Payments (Paid)
            ViewBag.PaymentsByProgram = data.PaymentsByProgram;
            ViewBag.TotalPayments = data.TotalPayments;
            // Pending Payments (Unpaid)
            ViewBag.PendingByProgram = data.PendingByProgram;
            ViewBag.TotalPending = data.TotalPending;
            // Financial Summary
            ViewBag.TotalExpected = data.TotalExpected;
            ViewBag.TotalCollected = data.TotalCollected;
            ViewBag.CollectionRate = data.CollectionRate;
            // Fines
            ViewBag.FinesByProgram = data.FinesByProgram;
            ViewBag.TotalFines = data.TotalFines;
            // Events
            ViewBag.MonthlyEvents = data.MonthlyEvents;
            ViewBag.EventStatus = data.EventStatus;
            ViewBag.CurrentAcademicYear = await GetCurrentAcademicYear();
        }

        // =========================================================
        // HELPER: Build Dashboard Data Object
        // =========================================================
        private async Task<DashboardDataModel> BuildDashboardData()
        {
            // Fetch all data from database - exclude archived students from active counts
            var allStudents = await _context.Students.ToListAsync();
            var events = await _context.Events.ToListAsync();
            var fees = await _context.Fees.Include(f => f.StudentNumNavigation).ToListAsync();

            // For Fines: Need to include Attendance AND its StudentNumNavigation
            var fines = await _context.Fines
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.StudentNumNavigation)
                .ToListAsync();

            // Filter: Only non-archived students for main dashboard stats
            var students = allStudents.Where(s => s.IsArchived != true).ToList();

            // Log for debugging (can be removed in production)
            _logger.LogInformation($"Dashboard Data: Total Students={allStudents.Count}, Non-Archived={students.Count}");
            _logger.LogInformation($"Dashboard Data: Total Fees={fees.Count}, Total Fines={fines.Count}");

            // Debug: Log sample YearLevelSection values to understand the format
            var sampleYearLevels = students.Take(5).Select(s => s.YearLevelSection).ToList();
            _logger.LogInformation($"Sample YearLevelSection values: {string.Join(", ", sampleYearLevels.Select(y => y ?? "null"))}");

            // Debug: Log sample Course values
            var sampleCourses = students.Take(5).Select(s => s.Course).ToList();
            _logger.LogInformation($"Sample Course values: {string.Join(", ", sampleCourses.Select(c => c ?? "null"))}");

            // =====================================================
            // HELPER: Extract Year Level from YearLevelSection
            // Handles formats like: "3-1", "1-A", "BSIT 1-A", "1A", "First Year", etc.
            // =====================================================
            int ExtractYearLevel(string? yearLevelSection)
            {
                if (string.IsNullOrWhiteSpace(yearLevelSection))
                    return 0;

                var input = yearLevelSection.Trim().ToUpper();

                // Check for word-based year levels first
                if (input.Contains("FIRST") || input.Contains("1ST"))
                    return 1;
                if (input.Contains("SECOND") || input.Contains("2ND"))
                    return 2;
                if (input.Contains("THIRD") || input.Contains("3RD"))
                    return 3;
                if (input.Contains("FOURTH") || input.Contains("4TH"))
                    return 4;

                // Extract first digit found in the string (handles "3-1", "1-A", etc.)
                foreach (char c in input)
                {
                    if (char.IsDigit(c))
                    {
                        int year = c - '0';
                        if (year >= 1 && year <= 4)
                            return year;
                    }
                }

                return 0; // Unknown year level
            }

            // =====================================================
            // HELPER: Check if student is considered "Active"
            // Handles null, empty, or various classification values
            // =====================================================
            bool IsActiveStudent(Student s)
            {
                // If Classification is null or empty, consider student as active (default)
                if (string.IsNullOrWhiteSpace(s.Classification))
                    return true;

                var classification = s.Classification.Trim().ToUpper();
                // Student is active if not explicitly marked as inactive/archived
                return classification != "INACTIVE" &&
                       classification != "ARCHIVED" &&
                       classification != "DROPPED" &&
                       classification != "GRADUATED";
            }

            // =====================================================
            // HELPER: Check if student belongs to a course/program
            // Case-insensitive matching
            // =====================================================
            bool IsBSIT(Student s) => !string.IsNullOrWhiteSpace(s.Course) && s.Course.ToUpper().Contains("BSIT");
            bool IsDIT(Student s) => !string.IsNullOrWhiteSpace(s.Course) && s.Course.ToUpper().Contains("DIT");

            // =====================================================
            // Students per Program (BSIT vs DIT ONLY)
            // Filter to only include BSIT and DIT students
            // =====================================================
            var bsitDitStudents = students.Where(s => IsBSIT(s) || IsDIT(s)).ToList();
            var bsitCount = bsitDitStudents.Count(IsBSIT);
            var ditCount = bsitDitStudents.Count(IsDIT);
            var totalStudents = bsitCount + ditCount;

            _logger.LogInformation($"Program Counts: BSIT={bsitCount}, DIT={ditCount}, Total (BSIT+DIT only)={totalStudents}");

            // Debug: Log year level extraction results
            foreach (var s in students.Take(5))
            {
                var extractedYear = ExtractYearLevel(s.YearLevelSection);
                _logger.LogInformation($"Student {s.StudentNum}: Course={s.Course}, YearLevelSection={s.YearLevelSection}, ExtractedYear={extractedYear}");
            }

            // =====================================================
            // Year Level Counts by Program
            // Using Dictionary for consistent JSON property names
            // =====================================================
            var yearLevelCounts = new Dictionary<string, int>
            {
                { "BSIT1", students.Count(s => IsBSIT(s) && ExtractYearLevel(s.YearLevelSection) == 1) },
                { "BSIT2", students.Count(s => IsBSIT(s) && ExtractYearLevel(s.YearLevelSection) == 2) },
                { "BSIT3", students.Count(s => IsBSIT(s) && ExtractYearLevel(s.YearLevelSection) == 3) },
                { "BSIT4", students.Count(s => IsBSIT(s) && ExtractYearLevel(s.YearLevelSection) == 4) },
                { "DIT1", students.Count(s => IsDIT(s) && ExtractYearLevel(s.YearLevelSection) == 1) },
                { "DIT2", students.Count(s => IsDIT(s) && ExtractYearLevel(s.YearLevelSection) == 2) },
                { "DIT3", students.Count(s => IsDIT(s) && ExtractYearLevel(s.YearLevelSection) == 3) }
            };

            _logger.LogInformation($"Year Level Counts: BSIT1={yearLevelCounts["BSIT1"]}, BSIT2={yearLevelCounts["BSIT2"]}, BSIT3={yearLevelCounts["BSIT3"]}, BSIT4={yearLevelCounts["BSIT4"]}");
            _logger.LogInformation($"Year Level Counts: DIT1={yearLevelCounts["DIT1"]}, DIT2={yearLevelCounts["DIT2"]}, DIT3={yearLevelCounts["DIT3"]}");

            // =====================================================
            // Monthly Active Students (for line chart)
            // Shows active student count - same for all months (current snapshot)
            // =====================================================
            var currentYear = DateTime.Now.Year;
            var monthlyActiveStudents = new int[12];
            var activeStudentCount = students.Count(IsActiveStudent);

            // Fill all months with current active count
            for (int i = 0; i < 12; i++)
            {
                monthlyActiveStudents[i] = activeStudentCount;
            }

            // =====================================================
            // Archive Count - students marked as archived OR inactive
            // =====================================================
            var archiveCount = allStudents.Count(s =>
                s.IsArchived == true ||
                (!string.IsNullOrWhiteSpace(s.Classification) &&
                 (s.Classification.ToUpper() == "ARCHIVED" ||
                  s.Classification.ToUpper() == "INACTIVE" ||
                  s.Classification.ToUpper() == "GRADUATED")));

            // =====================================================
            // Active vs Inactive Distribution (for pie chart)
            // Using Dictionary for consistent JSON property names
            // =====================================================
            var activeCount = students.Count(IsActiveStudent);
            var inactiveCount = allStudents.Count - activeCount;

            var activeInactive = new Dictionary<string, int>
            {
                { "Active", activeCount },
                { "Inactive", inactiveCount }
            };

            _logger.LogInformation($"Status: Active={activeCount}, Inactive={inactiveCount}, Archived={archiveCount}");

            // =====================================================
            // Payments by Program (sum of PAID fees)
            // =====================================================
            decimal GetPaidFees(Func<Student, bool> programCheck, int yearLevel)
            {
                return fees
                    .Where(f => f.StudentNumNavigation != null &&
                                programCheck(f.StudentNumNavigation) &&
                                ExtractYearLevel(f.StudentNumNavigation.YearLevelSection) == yearLevel &&
                                !string.IsNullOrWhiteSpace(f.FeeStatus) &&
                                f.FeeStatus.ToUpper() == "PAID")
                    .Sum(f => f.Amount ?? 0);
            }

            var paymentsByProgram = new Dictionary<string, decimal>
            {
                { "BSIT1", GetPaidFees(IsBSIT, 1) },
                { "BSIT2", GetPaidFees(IsBSIT, 2) },
                { "BSIT3", GetPaidFees(IsBSIT, 3) },
                { "BSIT4", GetPaidFees(IsBSIT, 4) },
                { "DIT1", GetPaidFees(IsDIT, 1) },
                { "DIT2", GetPaidFees(IsDIT, 2) },
                { "DIT3", GetPaidFees(IsDIT, 3) }
            };

            // =====================================================
            // Pending Payments by Program (sum of PENDING/UNPAID fees)
            // =====================================================
            decimal GetPendingFees(Func<Student, bool> programCheck, int yearLevel)
            {
                return fees
                    .Where(f => f.StudentNumNavigation != null &&
                                programCheck(f.StudentNumNavigation) &&
                                ExtractYearLevel(f.StudentNumNavigation.YearLevelSection) == yearLevel &&
                                (string.IsNullOrWhiteSpace(f.FeeStatus) ||
                                 f.FeeStatus.ToUpper() == "PENDING" ||
                                 f.FeeStatus.ToUpper() == "UNPAID"))
                    .Sum(f => f.Amount ?? 0);
            }

            var pendingByProgram = new Dictionary<string, decimal>
            {
                { "BSIT1", GetPendingFees(IsBSIT, 1) },
                { "BSIT2", GetPendingFees(IsBSIT, 2) },
                { "BSIT3", GetPendingFees(IsBSIT, 3) },
                { "BSIT4", GetPendingFees(IsBSIT, 4) },
                { "DIT1", GetPendingFees(IsDIT, 1) },
                { "DIT2", GetPendingFees(IsDIT, 2) },
                { "DIT3", GetPendingFees(IsDIT, 3) }
            };

            // =====================================================
            // Financial Summary Calculations
            // =====================================================
            var totalExpected = fees.Sum(f => f.Amount ?? 0);
            var totalCollected = fees
                .Where(f => !string.IsNullOrWhiteSpace(f.FeeStatus) && f.FeeStatus.ToUpper() == "PAID")
                .Sum(f => f.Amount ?? 0);
            var totalPending = fees
                .Where(f => string.IsNullOrWhiteSpace(f.FeeStatus) ||
                           f.FeeStatus.ToUpper() == "PENDING" ||
                           f.FeeStatus.ToUpper() == "UNPAID")
                .Sum(f => f.Amount ?? 0);
            var collectionRate = totalExpected > 0 ? Math.Round((totalCollected / totalExpected) * 100, 1) : 0;

            // =====================================================
            // Fines by Program (sum of fines)
            // =====================================================

            // Debug: Log fines data to check if Attendance and Student are loaded
            var finesWithStudent = fines.Where(f => f.Attendance?.StudentNumNavigation != null).ToList();
            var finesWithoutStudent = fines.Where(f => f.Attendance?.StudentNumNavigation == null).ToList();
            _logger.LogInformation($"Fines with Student loaded: {finesWithStudent.Count}, without Student: {finesWithoutStudent.Count}");

            // Debug: Log sample fine details
            foreach (var f in finesWithStudent.Take(3))
            {
                var student = f.Attendance!.StudentNumNavigation!;
                _logger.LogInformation($"Fine ID={f.FineId}, Amount={f.Amount}, Student={student.StudentNum}, Course={student.Course}, YearLevel={student.YearLevelSection}");
            }

            // Calculate total fines regardless of program/year for debugging
            var totalFinesAmount = fines.Sum(f => f.Amount ?? 0);
            _logger.LogInformation($"Total Fines Amount (all): {totalFinesAmount}");

            decimal GetFines(Func<Student, bool> programCheck, int yearLevel)
            {
                return fines
                    .Where(f => f.Attendance?.StudentNumNavigation != null &&
                                programCheck(f.Attendance.StudentNumNavigation) &&
                                ExtractYearLevel(f.Attendance.StudentNumNavigation.YearLevelSection) == yearLevel)
                    .Sum(f => f.Amount ?? 0);
            }

            var finesByProgram = new Dictionary<string, decimal>
            {
                { "BSIT1", GetFines(IsBSIT, 1) },
                { "BSIT2", GetFines(IsBSIT, 2) },
                { "BSIT3", GetFines(IsBSIT, 3) },
                { "BSIT4", GetFines(IsBSIT, 4) },
                { "DIT1", GetFines(IsDIT, 1) },
                { "DIT2", GetFines(IsDIT, 2) },
                { "DIT3", GetFines(IsDIT, 3) }
            };

            _logger.LogInformation($"Fines by Program: BSIT1={finesByProgram["BSIT1"]}, BSIT2={finesByProgram["BSIT2"]}, BSIT3={finesByProgram["BSIT3"]}, BSIT4={finesByProgram["BSIT4"]}");
            _logger.LogInformation($"Fines by Program: DIT1={finesByProgram["DIT1"]}, DIT2={finesByProgram["DIT2"]}, DIT3={finesByProgram["DIT3"]}");

            // =====================================================
            // Monthly Events (for line chart)
            // =====================================================
            var monthlyEvents = new int[12];
            foreach (var evt in events)
            {
                if (evt.EventDate.HasValue && evt.EventDate.Value.Year == currentYear)
                {
                    var month = evt.EventDate.Value.Month - 1;
                    if (month >= 0 && month < 12)
                    {
                        monthlyEvents[month]++;
                    }
                }
            }

            // =====================================================
            // Event Status (Completed vs Upcoming)
            // Using Dictionary for consistent JSON property names
            // =====================================================
            var today = DateOnly.FromDateTime(DateTime.Now);
            var eventStatus = new Dictionary<string, int>
            {
                { "Completed", events.Count(e => e.EventDate.HasValue && e.EventDate.Value < today) },
                { "Upcoming", events.Count(e => e.EventDate.HasValue && e.EventDate.Value >= today) }
            };

            // Calculate totals for payments and fines
            var totalPayments = paymentsByProgram.Values.Sum();
            var totalFinesSum = finesByProgram.Values.Sum();

            return new DashboardDataModel
            {
                StudentsPerProgram = new Dictionary<string, int> { { "BSIT", bsitCount }, { "DIT", ditCount } },
                TotalStudents = totalStudents,
                YearLevelCounts = yearLevelCounts,
                MonthlyActiveStudents = monthlyActiveStudents,
                ArchiveCount = archiveCount,
                ActiveInactive = activeInactive,
                // Payments (Paid)
                PaymentsByProgram = paymentsByProgram,
                TotalPayments = totalPayments,
                // Pending Payments (Unpaid)
                PendingByProgram = pendingByProgram,
                TotalPending = totalPending,
                // Financial Summary
                TotalExpected = totalExpected,
                TotalCollected = totalCollected,
                CollectionRate = collectionRate,
                // Fines
                FinesByProgram = finesByProgram,
                TotalFines = totalFinesSum,
                // Events
                MonthlyEvents = monthlyEvents,
                EventStatus = eventStatus
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
            // Payments (Paid)
            public Dictionary<string, decimal> PaymentsByProgram { get; set; }
            public decimal TotalPayments { get; set; }
            // Pending Payments (Unpaid)
            public Dictionary<string, decimal> PendingByProgram { get; set; }
            public decimal TotalPending { get; set; }
            // Financial Summary
            public decimal TotalExpected { get; set; }
            public decimal TotalCollected { get; set; }
            public decimal CollectionRate { get; set; }
            // Fines
            public Dictionary<string, decimal> FinesByProgram { get; set; }
            public decimal TotalFines { get; set; }
            // Events
            public int[] MonthlyEvents { get; set; }
            public Dictionary<string, int> EventStatus { get; set; }
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

            if (!string.IsNullOrEmpty(programFilter)) { studentsQuery = studentsQuery.Where(s => s.Course == programFilter); }

            // IMPROVED: Handle year and section filters for multiple formats
            // Supports: "1-1", "2-2", "BSIT 3-1", "DIT 3-1", etc.
            // Uses EF Core compatible string methods (Contains, StartsWith, EndsWith)
            if (!string.IsNullOrEmpty(yearFilter) && !string.IsNullOrEmpty(sectionFilter))
            {
                // Both filters applied - match both year AND section
                // Match patterns like: "3-1", "BSIT 3-1", "DIT 3-1"
                string exactMatch = $"{yearFilter}-{sectionFilter}";
                studentsQuery = studentsQuery.Where(s =>
                    s.YearLevelSection != null &&
                    (s.YearLevelSection == exactMatch || // Exact match: "3-1"
                     s.YearLevelSection.EndsWith(" " + exactMatch))); // With prefix: "BSIT 3-1"
            }
            else if (!string.IsNullOrEmpty(yearFilter))
            {
                // Only year filter - match year part
                // Match patterns like: "3-", " 3-" (for "BSIT 3-1", "DIT 3-1", or "3-1")
                string yearPattern = yearFilter + "-";
                studentsQuery = studentsQuery.Where(s =>
                    s.YearLevelSection != null &&
                    (s.YearLevelSection.StartsWith(yearPattern) || // "3-1", "3-2"
                     s.YearLevelSection.Contains(" " + yearPattern))); // "BSIT 3-1", "DIT 3-1"
            }
            else if (!string.IsNullOrEmpty(sectionFilter))
            {
                // Only section filter - match section part
                // Match patterns like: "-1", "-2"
                string sectionPattern = "-" + sectionFilter;
                studentsQuery = studentsQuery.Where(s =>
                    s.YearLevelSection != null &&
                    s.YearLevelSection.EndsWith(sectionPattern));
            }
            if (!string.IsNullOrEmpty(typeFilter)) { studentsQuery = studentsQuery.Where(s => s.StudentType == typeFilter); }
            if (!string.IsNullOrEmpty(statusFilter)) { studentsQuery = studentsQuery.Where(s => s.Classification == statusFilter); }
            if (!string.IsNullOrEmpty(roleFilter))
            {
                if (roleFilter == "Officer") studentsQuery = studentsQuery.Where(s => s.OfficerId != null);
                else if (roleFilter == "Member") studentsQuery = studentsQuery.Where(s => s.OfficerId == null);
            }

            switch (sortOrder)
            {
                case "name_desc": studentsQuery = studentsQuery.OrderByDescending(s => s.StudentLn); break;
                case "id_asc": studentsQuery = studentsQuery.OrderBy(s => s.StudentNum); break;
                case "id_desc": studentsQuery = studentsQuery.OrderByDescending(s => s.StudentNum); break;
                default: studentsQuery = studentsQuery.OrderBy(s => s.StudentLn); break;
            }

            var pagedStudents = await PagedList<Student>.CreateAsync(studentsQuery, pageNumber, pageSize);
            await PopulateFilterDropdowns();

            var allStudents = await _context.Students.Include(s => s.Officer).ToListAsync();
            ViewBag.TotalStudents = allStudents.Count;
            ViewBag.TotalOfficers = allStudents.Count(s => s.OfficerId != null);
            ViewBag.ActiveMembers = allStudents.Count(s => s.Classification == "Active");
            ViewBag.ArchivedMembers = allStudents.Count(s => s.Classification == "Archived");

            // NEW: Pass Current Academic Year and dropdown options to view
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
        public async Task<IActionResult> Events(int pageNumber = 1, int pageSize = 10)
        {
            ViewBag.PageSize = pageSize;
            var eventsQuery = _context.Events
                .Include(e => e.Attendances)
                .OrderByDescending(e => e.EventDate)
                .AsQueryable();

            var pagedEvents = await PagedList<Event>.CreateAsync(eventsQuery, pageNumber, pageSize);
            return View(pagedEvents);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseEventAndAssignFines(int eventId)
        {
            try
            {
                var eventToClose = await _context.Events.FindAsync(eventId);
                if (eventToClose == null)
                {
                    TempData["Error"] = "Event not found.";
                    return RedirectToAction("Events");
                }

                // Get all active students
                var activeStudents = await _context.Students
                    .Include(s => s.Officer)
                    .Where(s => s.Classification == "Active")
                    .ToListAsync();

                // Get existing attendance records
                var existingAttendance = await _context.Attendances
                    .Where(a => a.EventId == eventId)
                    .ToListAsync();

                int finesGenerated = 0;
                int attendanceCreated = 0;

                foreach (var student in activeStudents)
                {
                    var attendance = existingAttendance.FirstOrDefault(a => a.StudentNum == student.StudentNum);

                    // If no attendance record exists, create one as "Absent"
                    if (attendance == null)
                    {
                        attendance = new Attendance
                        {
                            StudentNum = student.StudentNum,
                            EventId = eventId,
                            AttendanceStatus = "Absent"
                        };
                        _context.Attendances.Add(attendance);
                        await _context.SaveChangesAsync(); // Save to get AttendanceId
                        attendanceCreated++;
                    }

                    // Generate fine if absent or excused (uses our new helper method)
                    if (attendance.AttendanceStatus == "Absent" || attendance.AttendanceStatus == "Excused")
                    {
                        bool fineCreated = await GenerateFineForAttendance(attendance.AttendanceId, notifyStudent: true);
                        if (fineCreated) finesGenerated++;
                    }
                }

                await LogAction("Close Event",
                    $"Closed event '{eventToClose.EventName}'. Created {attendanceCreated} absence records. " +
                    $"Issued {finesGenerated} fines.");

                TempData["Message"] = $"Event closed successfully. " +
                                     $"Created {attendanceCreated} absence records and issued {finesGenerated} fines. " +
                                     $"Students have been notified.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing event and assigning fines");
                TempData["Error"] = "An error occurred while closing the event and calculating fines.";
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
                var attendance = await _context.Attendances
                    .Include(a => a.StudentNumNavigation)
                        .ThenInclude(s => s.Officer)
                    .Include(a => a.Event)
                    .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

                if (attendance == null)
                {
                    _logger.LogWarning($"Attendance ID {attendanceId} not found for fine generation.");
                    return false;
                }

                if (attendance.AttendanceStatus != "Absent" && attendance.AttendanceStatus != "Excused")
                    return false;

                var existingFine = await _context.Fines.FirstOrDefaultAsync(f => f.AttendanceId == attendanceId);
                if (existingFine != null)
                {
                    _logger.LogInformation($"Fine already exists for Attendance ID {attendanceId}. Skipping.");
                    return false;
                }

                decimal fineAmount = CalculateFineAmount(attendance);
                if (fineAmount <= 0)
                {
                    _logger.LogInformation($"No fine configured. Attendance ID: {attendanceId}");
                    return false;
                }

                var fine = new Fine
                {
                    AttendanceId = attendanceId,
                    Amount = fineAmount,
                    FinesStatus = "Unpaid",
                    FinesStartDate = DateOnly.FromDateTime(DateTime.Now),
                    FinesDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30))
                };

                _context.Fines.Add(fine);
                await _context.SaveChangesAsync();

                await LogAction("Generate Fine",
                    $"Fine of ₱{fineAmount} created for {attendance.StudentNumNavigation?.FullName} " +
                    $"(Event: {attendance.Event?.EventName}, Status: {attendance.AttendanceStatus})");

                if (notifyStudent && attendance.StudentNum != null)
                    await SendFineNotification(attendance.StudentNum, fine, attendance.Event?.EventName ?? "Unknown Event");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating fine for Attendance ID {attendanceId}");
                return false;
            }
        }

        private decimal CalculateFineAmount(Attendance attendance)
        {
            if (attendance.Event == null || attendance.StudentNumNavigation == null)
                return 0;

            string roleType = "Member";
            if (attendance.StudentNumNavigation.Officer != null)
            {
                if (attendance.StudentNumNavigation.Officer.Classification == "Class Officer")
                    roleType = "Class Officer";
                else if (attendance.StudentNumNavigation.Officer.Classification == "Org Officer")
                    roleType = "Org Officer";
            }

            decimal fineAmount = 0;
            if (attendance.Event.EventType == "iBITS Event")
            {
                if (roleType == "Member")
                    fineAmount = attendance.Event.FineForMember ?? 0;
                else if (roleType == "Class Officer")
                    fineAmount = attendance.Event.FineForClassOfficer ?? 0;
                else if (roleType == "Org Officer")
                    fineAmount = attendance.Event.FineForOrgOfficer ?? 0;
            }
            else
            {
                if (roleType == "Member")
                    fineAmount = attendance.Event.NonIbitsFineForMember ?? 0;
                else if (roleType == "Class Officer")
                    fineAmount = attendance.Event.NonIbitsFineForClassOfficer ?? 0;
                else if (roleType == "Org Officer")
                    fineAmount = attendance.Event.NonIbitsFineForOrgOfficer ?? 0;
            }
            return fineAmount;
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
        // ATTENDANCE - VIEW ONLY WITH FILTERS
        // =========================================================
        public async Task<IActionResult> Attendance(string? eventFilter, string? programFilter, string? yearFilter)
        {
            // Start with base query
            var query = _context.Attendances
                .Include(a => a.Event)
                .Include(a => a.StudentNumNavigation)
                .AsQueryable();

            // Apply Event filter
            if (!string.IsNullOrEmpty(eventFilter) && int.TryParse(eventFilter, out int eventId))
            {
                query = query.Where(a => a.EventId == eventId);
            }

            // Apply Program filter (Course)
            if (!string.IsNullOrEmpty(programFilter))
            {
                query = query.Where(a => a.StudentNumNavigation != null &&
                                         a.StudentNumNavigation.Course != null &&
                                         a.StudentNumNavigation.Course.ToUpper().Contains(programFilter.ToUpper()));
            }

            // Apply Year filter
            if (!string.IsNullOrEmpty(yearFilter))
            {
                var attendances = await query.ToListAsync();
                attendances = attendances.Where(a => ExtractYearFromYearLevelSection(a.StudentNumNavigation?.YearLevelSection) == yearFilter).ToList();

                // Populate ViewBag data
                await PopulateAttendanceFilters();
                ViewData["EventFilter"] = eventFilter;
                ViewData["ProgramFilter"] = programFilter;
                ViewData["YearFilter"] = yearFilter;

                return View(attendances.OrderByDescending(a => a.Event != null ? a.Event.EventDate : DateOnly.MinValue).ToList());
            }

            var result = await query
                .OrderByDescending(a => a.Event != null ? a.Event.EventDate : DateOnly.MinValue)
                .ToListAsync();

            // Populate ViewBag data
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
        // PAYMENTS PAGE (FEES ONLY)
        // =========================================================
        public async Task<IActionResult> Payments()
        {
            // Fetch Fees only (Fines moved to separate page)
            var fees = await _context.Fees
                .Include(f => f.StudentNumNavigation)
                .OrderBy(f => f.FeeStatus)
                .ThenByDescending(f => f.FeesDueDate)
                .ToListAsync();

            // Calculate Summary Statistics (FEES ONLY)
            decimal feesCollected = fees.Where(f => f.FeeStatus?.ToUpper() == "PAID" || f.FeeStatus?.ToUpper() == "COMPLETED").Sum(f => f.Amount ?? 0);
            decimal feesExpected = fees.Sum(f => f.Amount ?? 0);

            ViewBag.TotalCollections = feesCollected;
            ViewBag.TotalExpected = feesExpected;

            // 5. Populate Fee Name Dropdown
            ViewBag.FeeNames = await _context.Fees
                .Where(f => !string.IsNullOrEmpty(f.FeeName))
                .Select(f => f.FeeName)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            // 6. Pie Chart Data (based on Fees)
            int ExtractYear(string? yls)
            {
                if (string.IsNullOrEmpty(yls)) return 0;
                if (yls.Contains("1")) return 1;
                if (yls.Contains("2")) return 2;
                if (yls.Contains("3")) return 3;
                if (yls.Contains("4")) return 4;
                return 0;
            }

            bool IsBSIT(string? course) => course != null && course.ToUpper().Contains("BSIT");
            bool IsDIT(string? course) => course != null && course.ToUpper().Contains("DIT");
            decimal SumFees(Func<Fee, bool> criteria) => fees.Where(criteria).Sum(f => f.Amount ?? 0);

            ViewBag.PaidBreakdown = new Dictionary<string, decimal>
            {
                { "BSIT1", SumFees(f => f.FeeStatus?.ToUpper() == "PAID" && f.StudentNumNavigation != null && IsBSIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 1) },
                { "BSIT2", SumFees(f => f.FeeStatus?.ToUpper() == "PAID" && f.StudentNumNavigation != null && IsBSIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 2) },
                { "BSIT3", SumFees(f => f.FeeStatus?.ToUpper() == "PAID" && f.StudentNumNavigation != null && IsBSIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 3) },
                { "BSIT4", SumFees(f => f.FeeStatus?.ToUpper() == "PAID" && f.StudentNumNavigation != null && IsBSIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 4) },
                { "DIT1", SumFees(f => f.FeeStatus?.ToUpper() == "PAID" && f.StudentNumNavigation != null && IsDIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 1) },
                { "DIT2", SumFees(f => f.FeeStatus?.ToUpper() == "PAID" && f.StudentNumNavigation != null && IsDIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 2) },
                { "DIT3", SumFees(f => f.FeeStatus?.ToUpper() == "PAID" && f.StudentNumNavigation != null && IsDIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 3) }
            };

            ViewBag.PendingBreakdown = new Dictionary<string, decimal>
            {
                { "BSIT1", SumFees(f => (f.FeeStatus?.ToUpper() == "PENDING" || f.FeeStatus?.ToUpper() == "UNPAID") && f.StudentNumNavigation != null && IsBSIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 1) },
                { "BSIT2", SumFees(f => (f.FeeStatus?.ToUpper() == "PENDING" || f.FeeStatus?.ToUpper() == "UNPAID") && f.StudentNumNavigation != null && IsBSIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 2) },
                { "BSIT3", SumFees(f => (f.FeeStatus?.ToUpper() == "PENDING" || f.FeeStatus?.ToUpper() == "UNPAID") && f.StudentNumNavigation != null && IsBSIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 3) },
                { "BSIT4", SumFees(f => (f.FeeStatus?.ToUpper() == "PENDING" || f.FeeStatus?.ToUpper() == "UNPAID") && f.StudentNumNavigation != null && IsBSIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 4) },
                { "DIT1", SumFees(f => (f.FeeStatus?.ToUpper() == "PENDING" || f.FeeStatus?.ToUpper() == "UNPAID") && f.StudentNumNavigation != null && IsDIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 1) },
                { "DIT2", SumFees(f => (f.FeeStatus?.ToUpper() == "PENDING" || f.FeeStatus?.ToUpper() == "UNPAID") && f.StudentNumNavigation != null && IsDIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 2) },
                { "DIT3", SumFees(f => (f.FeeStatus?.ToUpper() == "PENDING" || f.FeeStatus?.ToUpper() == "UNPAID") && f.StudentNumNavigation != null && IsDIT(f.StudentNumNavigation.Course) && ExtractYear(f.StudentNumNavigation.YearLevelSection) == 3) }
            };

            return View(fees); // Pass fees as the primary model
        }


        // =========================================================
        // ACTION: UPDATE FINE STATUS
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFineStatus(int fineId, string status)
        {
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
                        AcadYear = AcadYear, // NEW: Added Academic Year
                        StudentNum = student.StudentNum
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
                _logger.LogInformation($"Fee created: {FeeName}, Amount: {Amount}, AcadYear: {AcadYear}, Applied to: {feesCreated} students ({programDesc} - {yearDesc})");
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
        // Exports only the visible columns based on user selection
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
                // Build query with same filters as StudentRecords
                var studentsQuery = _context.Students
                    .Include(s => s.Officer)
                    .AsQueryable();

                // Apply filters
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
                    studentsQuery = studentsQuery.Where(s => s.Course == programFilter);

                if (!string.IsNullOrEmpty(yearFilter))
                    studentsQuery = studentsQuery.Where(s => s.YearLevelSection != null && s.YearLevelSection.StartsWith(yearFilter));

                if (!string.IsNullOrEmpty(sectionFilter))
                {
                    string sectionSuffix = "-" + sectionFilter;
                    studentsQuery = studentsQuery.Where(s => s.YearLevelSection != null && s.YearLevelSection.EndsWith(sectionSuffix));
                }

                if (!string.IsNullOrEmpty(typeFilter))
                    studentsQuery = studentsQuery.Where(s => s.StudentType == typeFilter);

                if (!string.IsNullOrEmpty(statusFilter))
                    studentsQuery = studentsQuery.Where(s => s.Classification == statusFilter);

                if (!string.IsNullOrEmpty(roleFilter))
                {
                    if (roleFilter == "Officer")
                        studentsQuery = studentsQuery.Where(s => s.OfficerId != null);
                    else if (roleFilter == "Member")
                        studentsQuery = studentsQuery.Where(s => s.OfficerId == null);
                }

                // Order by last name
                var students = await studentsQuery.OrderBy(s => s.StudentLn).ToListAsync();

                // Parse visible columns (default to all if not specified)
                var visibleColumns = string.IsNullOrEmpty(columns)
                    ? new[] { "Id", "Name", "Program", "Section", "Year", "Type", "Role", "Status" }
                    : columns.Split(',', StringSplitOptions.RemoveEmptyEntries);

                // Create Excel workbook using ClosedXML
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Student Records");

                    // Build dynamic headers based on visible columns
                    var headersList = new List<string>();
                    var columnOrder = new List<string>();

                    // Map column names to header text (in order they appear in UI)
                    if (visibleColumns.Contains("Id")) { headersList.Add("Student ID"); columnOrder.Add("Id"); }
                    if (visibleColumns.Contains("Name")) { headersList.Add("Full Name"); headersList.Add("Email"); columnOrder.Add("Name"); columnOrder.Add("Email"); }
                    if (visibleColumns.Contains("Program")) { headersList.Add("Course/Program"); columnOrder.Add("Program"); }
                    if (visibleColumns.Contains("Section")) { headersList.Add("Year & Section"); columnOrder.Add("Section"); }
                    if (visibleColumns.Contains("Year")) { headersList.Add("School Year Enrolled"); columnOrder.Add("Year"); }
                    if (visibleColumns.Contains("Type")) { headersList.Add("Student Type"); columnOrder.Add("Type"); }
                    if (visibleColumns.Contains("Role")) { headersList.Add("Role"); columnOrder.Add("Role"); }
                    if (visibleColumns.Contains("Status")) { headersList.Add("Status"); columnOrder.Add("Status"); }

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

                    // Populate data rows based on visible columns
                    int row = 2;
                    foreach (var student in students)
                    {
                        int col = 1;

                        if (visibleColumns.Contains("Id"))
                        {
                            worksheet.Cell(row, col++).Value = student.StudentNum;
                        }
                        if (visibleColumns.Contains("Name"))
                        {
                            // Full Name format: Surname, Firstname M.I.
                            string middleInitial = !string.IsNullOrEmpty(student.StudentMn) ? student.StudentMn.Substring(0, 1) + "." : "";
                            string fullName = $"{student.StudentLn ?? ""}, {student.StudentFn ?? ""} {middleInitial}".Trim();
                            worksheet.Cell(row, col++).Value = fullName;
                            // Email
                            worksheet.Cell(row, col++).Value = student.StudentEmail ?? "";
                        }
                        if (visibleColumns.Contains("Program"))
                        {
                            worksheet.Cell(row, col++).Value = student.Course ?? "";
                        }
                        if (visibleColumns.Contains("Section"))
                        {
                            worksheet.Cell(row, col++).Value = student.YearLevelSection ?? "";
                        }
                        if (visibleColumns.Contains("Year"))
                        {
                            worksheet.Cell(row, col++).Value = student.SchoolYearEnrolled ?? "";
                        }
                        if (visibleColumns.Contains("Type"))
                        {
                            worksheet.Cell(row, col++).Value = student.StudentType ?? "";
                        }
                        if (visibleColumns.Contains("Role"))
                        {
                            worksheet.Cell(row, col++).Value = student.Officer?.Position ?? "Member";
                        }
                        if (visibleColumns.Contains("Status"))
                        {
                            worksheet.Cell(row, col++).Value = student.Classification ?? "";
                        }

                        // Add borders to data cells
                        for (int c = 1; c < col; c++)
                        {
                            worksheet.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        }

                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Add summary row
                    row++;
                    worksheet.Cell(row, 1).Value = $"Total Records: {students.Count}";
                    worksheet.Cell(row, 1).Style.Font.Bold = true;
                    worksheet.Range(row, 1, row, 3).Merge();

                    row++;
                    worksheet.Cell(row, 1).Value = $"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    worksheet.Cell(row, 1).Style.Font.Italic = true;
                    worksheet.Range(row, 1, row, 3).Merge();

                    // Generate file
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Position = 0;

                        string fileName = $"iBITS_StudentRecords_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                        await LogAction("Export Excel", $"Exported {students.Count} student records to Excel (Columns: {columns ?? "All"}).");

                        return File(
                            stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName
                        );
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
            if (newRole == "Class Secretary" || newRole == "Class Treasurer")
            {
                if (!string.IsNullOrEmpty(student.YearLevelSection))
                {
                    // Extract year-section pattern (e.g., "3-1" from "BSIT 3-1" or "3-1")
                    var studentSection = ExtractYearSection(student.YearLevelSection);

                    var usersInRole = await _userManager.GetUsersInRoleAsync(newRole);
                    foreach (var u in usersInRole)
                    {
                        if (u.UserName == studentNum) continue;
                        var otherStudent = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentNum == u.UserName);
                        if (otherStudent != null && !string.IsNullOrEmpty(otherStudent.YearLevelSection))
                        {
                            // Compare extracted sections (e.g., "3-1" vs "3-1" even if one is "BSIT 3-1")
                            var otherSection = ExtractYearSection(otherStudent.YearLevelSection);
                            if (otherSection == studentSection)
                            {
                                TempData["Error"] = $"Action Denied: Section {studentSection} already has a {newRole} ({otherStudent.StudentFn} {otherStudent.StudentLn}).";
                                return RedirectToAction(returnAction);
                            }
                        }
                    }
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
        // ACTION: UPDATE FEE STATUS (Mark as Paid/Unpaid)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFeeStatus(int feeId, string status)
        {
            var fee = await _context.Fees.FindAsync(feeId);
            if (fee == null)
            {
                TempData["Error"] = "Fee record not found.";
                return RedirectToAction(nameof(Payments));
            }

            fee.FeeStatus = status;
            _context.Fees.Update(fee);
            await _context.SaveChangesAsync();

            await LogAction("Update Fee Status", $"Updated fee ID {feeId} status to {status}");
            TempData["Message"] = "Fee status updated successfully.";

            return RedirectToAction(nameof(Payments));
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


    }
}
