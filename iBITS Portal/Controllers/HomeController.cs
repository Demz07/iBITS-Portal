// ============================================================
// FILE PATH: Controllers/HomeController.cs
// ============================================================

using iBITS_Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using iBITS_Portal.Helpers;

namespace iBITS_Portal.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly PortaliBitsContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            PortaliBitsContext context,
            ILogger<HomeController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        // =========================================================
        // GLOBAL OVERRIDE: SMART CALENDAR & AUTO-ROLLOVER
        // =========================================================
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 1. Fetch All Settings at once
            var settings = await _context.SystemSettings.ToListAsync();
            string? GetSetting(string key) => settings.FirstOrDefault(s => s.SettingKey == key)?.SettingValue;

            // 2. Load Current Data
            string currentAy = GetSetting("CurrentAcademicYear") ?? "Not Set";
            ViewBag.Sem1Start = GetSetting("Sem1Start");
            ViewBag.Sem1End = GetSetting("Sem1End");
            ViewBag.Sem2Start = GetSetting("Sem2Start");
            ViewBag.Sem2End = GetSetting("Sem2End");
            ViewBag.SummerStart = GetSetting("SummerStart");
            ViewBag.SummerEnd = GetSetting("SummerEnd");

            // 3. Parse Dates Safely
            DateTime.TryParse(ViewBag.Sem1Start as string, out DateTime s1Start);
            DateTime.TryParse(ViewBag.Sem1End as string, out DateTime s1End);
            DateTime.TryParse(ViewBag.Sem2Start as string, out DateTime s2Start);
            DateTime.TryParse(ViewBag.Sem2End as string, out DateTime s2End);
            DateTime.TryParse(ViewBag.SummerStart as string, out DateTime sumStart);
            DateTime.TryParse(ViewBag.SummerEnd as string, out DateTime sumEnd);

            var today = iBITS_Portal.Helpers.PhTime.Now.Date;
            string activeSem = "Not Configured";

            // 4. AUTO-ROLLOVER LOGIC
            // Determine the absolute end of the current calendar configuration
            DateTime latestDate = new[] { s1End, s2End, sumEnd }.Max();

            if (latestDate != default && today > latestDate)
            {
                // logic: The calendar has finished. Increment Year & Reset Semesters.
                var parts = currentAy.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int startYear) && int.TryParse(parts[1], out int endYear))
                {
                    // A. Increment Year
                    currentAy = $"{startYear + 1}-{endYear + 1}";

                    var aySetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == "CurrentAcademicYear");
                    if (aySetting != null)
                    {
                        aySetting.SettingValue = currentAy;
                        _context.SystemSettings.Update(aySetting);
                    }

                    // B. Reset Semesters (Clear DB Dates)
                    var keysToReset = new[] {
                "Sem1Start", "Sem1End",
                "Sem2Start", "Sem2End",
                "SummerStart", "SummerEnd",
                "CurrentSemester"
            };

                    var settingsToClear = await _context.SystemSettings
                        .Where(s => keysToReset.Contains(s.SettingKey))
                        .ToListAsync();

                    foreach (var s in settingsToClear)
                    {
                        s.SettingValue = ""; // Clear values
                        _context.SystemSettings.Update(s);
                    }

                    await _context.SaveChangesAsync();

                    // C. Reset Local Variables so the badge updates immediately
                    ViewBag.Sem1Start = ViewBag.Sem1End = "";
                    ViewBag.Sem2Start = ViewBag.Sem2End = "";
                    ViewBag.SummerStart = ViewBag.SummerEnd = "";
                    s1Start = s1End = s2Start = s2End = sumStart = sumEnd = default;
                    activeSem = "Not Configured"; // Force badge to show needs config
                }
            }
            else
            {
                // 5. NORMAL SEMESTER CALCULATION (If not rolling over)
                if (s1Start != default && s1End != default && today >= s1Start && today <= s1End)
                    activeSem = "1st Semester";
                else if (s2Start != default && s2End != default && today >= s2Start && today <= s2End)
                    activeSem = "2nd Semester";
                else if (sumStart != default && sumEnd != default && today >= sumStart && today <= sumEnd)
                    activeSem = "Summer";
                else if (s1Start != default)
                    activeSem = "Semester Break";
            }

            // 6. Push final values to View
            ViewBag.CurrentAcademicYear = currentAy;
            ViewBag.CurrentSemester = activeSem;

            await next();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("LandingPage");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.UserName == null)
            {
                await _signInManager.SignOutAsync();
                return View("Gateway");
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            // If still using default password, force password setup
            bool isDefaultPassword = await _userManager.CheckPasswordAsync(user, user.UserName);
            if (isDefaultPassword)
            {
                return RedirectToAction("SecuritySetup", "Account");
            }

            var student = await _context.Students
                .Include(s => s.Officer)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentNum == user.UserName);

            if (student == null)
            {
                _logger.LogError($"CRITICAL: Identity user '{user.UserName}' exists but has no matching Student record.");
                await _signInManager.SignOutAsync();
                TempData["Error"] = "Your student record could not be found. Please contact an administrator.";
                return RedirectToAction("Gateway");
            }

            // If you still require profile photo before entering dashboard:
            bool hasProfilePicture = !string.IsNullOrEmpty(student.StudentImage);
            if (!hasProfilePicture)
            {
                return RedirectToAction("ProfileSetup", "Account");
            }

            // Pending role change (optional feature)
            var pendingChange = await _context.PendingRoleChanges
                .FirstOrDefaultAsync(p => p.StudentNumber == user.UserName && !p.IsConfirmed && !p.IsDeclined);

            if (pendingChange != null)
            {
                return RedirectToPage("/Account/ConfirmRoleChange", new { area = "Identity" });
            }

            // ================== DASHBOARD DATA ==================
            var today = DateOnly.FromDateTime(PhTime.Today);

            // Current events: all for today (not closed)
            var currentEvents = await _context.Events
                .Where(e => e.EventDate.HasValue && e.EventDate.Value == today && !e.IsClosed)
                .OrderBy(e => e.StartTime.HasValue ? 0 : 1)
                .ThenBy(e => e.StartTime)
                .ThenBy(e => e.EventName)
                .ToListAsync();

            var earliestStart = currentEvents
                .Where(e => e.StartTime.HasValue)
                .OrderBy(e => e.StartTime)
                .Select(e => e.StartTime!.Value.ToString("h:mm tt"))
                .FirstOrDefault();

            ViewBag.NextEventTime = earliestStart;
            ViewBag.CurrentEvents = currentEvents;

            // Upcoming events: strictly future
            var upcomingEvents = await _context.Events
                .Where(e => e.EventDate.HasValue && e.EventDate.Value > today && !e.IsClosed)
                .OrderBy(e => e.EventDate)
                .ThenBy(e => e.StartTime)
                .Take(3)
                .ToListAsync();

            // Announcements (not dismissed and not expired)
            var dismissedIds = await _context.UserAnnouncementDismissals
                .Where(d => d.StudentNum == user.UserName)
                .Select(d => d.AnnouncementId)
                .ToListAsync();

            // Announcements
            // - not dismissed
            // - not expired
            // - exclude Admin Notice
            // - supports comma-separated target audiences
            var allAnnouncements = await _context.Announcements
                .Where(a => (a.ExpiryDate == null || a.ExpiryDate > PhTime.Now)
                            && a.AnnouncementType != "Admin Notice"
                            && !dismissedIds.Contains(a.Id))
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            var announcements = allAnnouncements
                .Where(a =>
                {
                    if (string.IsNullOrWhiteSpace(a.TargetAudience))
                        return false;

                    var audiences = a.TargetAudience.Split(',')
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    return audiences.Any(audience =>
                    {
                        if (audience == "All Students")
                            return true;

                        if (audience == student.Course)
                            return true;

                        if (audience == "Students with Outstanding Balance")
                            return true; // (optional) filter by fees later

                        if (audience.Contains("Year") && student.YearLevelSection != null)
                        {
                            // Supports:
                            // - "BSIT 1st Year" / "DIT 3rd Year"
                            // - "1st Year" / "2nd Year" (all programs)
                            var parts = audience.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (parts.Length >= 3 && parts[2] == "Year")
                            {
                                var program = parts[0];
                                var yearPrefix = parts[1];
                                var yearNumber = yearPrefix
                                    .Replace("st", "")
                                    .Replace("nd", "")
                                    .Replace("rd", "")
                                    .Replace("th", "");

                                return student.Course == program
                                    && student.YearLevelSection.StartsWith(yearNumber + "-");
                            }

                            var yearPrefixAll = parts[0];
                            var yearNumberAll = yearPrefixAll
                                .Replace("st", "")
                                .Replace("nd", "")
                                .Replace("rd", "")
                                .Replace("th", "");

                            return student.YearLevelSection.StartsWith(yearNumberAll + "-");
                        }

                        return false;
                    });
                })
                .Take(10)
                .ToList();

            // ============================================================
            // NEW: FINANCIAL SUMMARY
            // ============================================================
            var unpaidFees = await _context.Fees
                .Where(f => f.StudentNum == user.UserName && f.FeeStatus != "Paid")
                .ToListAsync();

            var unpaidFines = await _context.Fines
                .Include(f => f.Attendance)
                .Where(f => f.Attendance != null && f.Attendance.StudentNum == user.UserName && f.FinesStatus != "Paid")
                .ToListAsync();

            ViewBag.UnpaidFeesCount = unpaidFees.Count;
            ViewBag.UnpaidFinesCount = unpaidFines.Count;
            ViewBag.TotalBalanceDue = unpaidFees.Sum(f => f.Amount) + unpaidFines.Sum(f => f.Amount);

            // Attendance rate (past events only)
            var totalEvents = await _context.Events
                .Where(e => e.EventDate.HasValue && e.EventDate.Value < today)
                .CountAsync();

            var studentAttendances = await _context.Attendances
                .Include(a => a.Event)
                .Where(a => a.StudentNum == user.UserName &&
                            a.AttendanceStatus == "Present" &&
                            a.Event != null &&
                            a.Event.EventDate.HasValue &&
                            a.Event.EventDate.Value < today)
                .CountAsync();

            var attendanceRate = totalEvents > 0
                ? Math.Round((double)studentAttendances / totalEvents * 100, 1)
                : 0;

            ViewBag.AttendanceRate = attendanceRate;
            ViewBag.EventsAttended = studentAttendances;
            ViewBag.TotalPastEvents = totalEvents;

            // Pass to View
            ViewBag.Student = student;
            ViewBag.UpcomingEvents = upcomingEvents;
            ViewBag.Announcements = announcements;

            // ? Provide header avatar to _StudentLayout.cshtml
            ViewBag.StudentImage = string.IsNullOrWhiteSpace(student.StudentImage)
                ? "/images/default-avatar.png"
                : student.StudentImage;

            // ================== ADMIN NOTICES ==================
            // Fetch individual notifications (Admin Notices) that haven't been read/dismissed
            var adminNotices = await _context.Notifications
    .Where(n => n.StudentNum == user.UserName && !n.IsRead && n.NotificationType == "Admin Notice")
    .OrderByDescending(n => n.NotificationDate)
    .ToListAsync();

            ViewBag.AdminNotices = adminNotices;
            // ===================================================



            return View("StudentDashboard");
        }

        [AllowAnonymous]
        public async Task<IActionResult> LandingPage()
        {
            if (_signInManager.IsSignedIn(User)) { return RedirectToAction("Index"); }

            var totalStudents = await _context.Students.CountAsync();
            var totalEvents = await _context.Events.CountAsync();
            var totalTransactions = await _context.PaymentTransactions.CountAsync();
            var totalScans = await _context.Attendances.Where(a => a.AttendanceStatus == "Present").CountAsync();

            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalEvents = totalEvents;
            ViewBag.TotalTransactions = totalTransactions;
            ViewBag.TotalScans = totalScans;

            return View();
        }

        [AllowAnonymous]
        public IActionResult Gateway()
        {
            if (_signInManager.IsSignedIn(User)) { return RedirectToAction("Index"); }
            return View();
        }

        public IActionResult Privacy() => View();

        // ================== ANNOUNCEMENT DISMISS ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissAnnouncement(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.UserName == null)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return Json(new { success = false, message = "Announcement not found" });
            }

            var existing = await _context.UserAnnouncementDismissals
                .FirstOrDefaultAsync(d => d.StudentNum == user.UserName && d.AnnouncementId == id);

            if (existing != null)
            {
                return Json(new { success = true, message = "Already dismissed" });
            }

            _context.UserAnnouncementDismissals.Add(new UserAnnouncementDismissal
            {
                StudentNum = user.UserName,
                AnnouncementId = id,
                DismissedAt = PhTime.Now
            });
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Announcement dismissed successfully" });
        }

        // ================== ADMIN NOTICE DISMISS ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissNotification(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.UserName == null)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            // Find the specific notification
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
            {
                return Json(new { success = false, message = "Notice not found" });
            }

            // Security check: ensure this notice actually belongs to the logged-in student
            if (notification.StudentNum != user.UserName)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            // Mark as Read (this removes it from the "Unread" list in your dashboard)
            notification.IsRead = true;
            _context.Notifications.Update(notification);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Notice dismissed" });
        }

        // ================== GET ALL EVENTS (JSON) for MODAL ==================
        [HttpGet]
        public async Task<IActionResult> GetAllEventsJson()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "User not found" });

            var today = DateOnly.FromDateTime(PhTime.Today);

            // 1. Upcoming Events (Strictly FUTURE events only)
            // Changed '>=' to '>' so today's events are excluded
            var upcoming = await _context.Events
                .Where(e => e.EventDate.HasValue && e.EventDate.Value > today && !e.IsClosed)
                .OrderBy(e => e.EventDate)
                .ThenBy(e => e.StartTime)
                .Select(e => new
                {
                    eventName = e.EventName,
                    eventDate = e.EventDate.Value.ToString("yyyy-MM-dd"),
                    eventLocation = e.EventLocation,
                    isClosed = e.IsClosed
                })
                .ToListAsync();

            // 2. Event History (Past events OR Closed events)
            var history = await _context.Events
                .Where(e => (e.EventDate.HasValue && e.EventDate.Value < today) || e.IsClosed)
                .OrderByDescending(e => e.EventDate)
                .Select(e => new
                {
                    eventName = e.EventName,
                    eventDate = e.EventDate.Value.ToString("yyyy-MM-dd"),
                    eventLocation = e.EventLocation,
                    isClosed = e.IsClosed
                })
                .ToListAsync();

            // Return the data as JSON
            return Json(new { upcoming = upcoming, history = history });
        }

        // Landing pages
        [AllowAnonymous] public IActionResult LandingPageAbout() => View();
        [AllowAnonymous] public IActionResult LandingPagePeople() => View();
        [AllowAnonymous] public IActionResult LandingDevelopers() => View();
    }
}

