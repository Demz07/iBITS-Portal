// ============================================================
// FILE PATH: Controllers/HomeController.cs
// ============================================================
// UPDATED: Injected the pending role change check into the
// Index() method's login workflow.
// ============================================================

using iBITS_Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return View("Gateway");
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

            bool hasProfilePicture = !string.IsNullOrEmpty(student.StudentImage);
            if (!hasProfilePicture)
            {
                return RedirectToAction("ProfileSetup", "Account");
            }

            // ============================================================
            // NEW: CHECK FOR PENDING ROLE CHANGE
            // ============================================================
            var pendingChange = await _context.PendingRoleChanges
                .FirstOrDefaultAsync(p => p.StudentNumber == user.UserName && !p.IsConfirmed && !p.IsDeclined);

            if (pendingChange != null)
            {
                // If a pending change exists, redirect to the dedicated confirmation page.
                return RedirectToPage("/Account/ConfirmRoleChange", new { area = "Identity" });
            }
            // ============================================================

            // All checks passed, proceed to the dashboard
            var today = DateOnly.FromDateTime(DateTime.Now);
            var nextEvent = await _context.Events
                .Where(e => e.EventDate >= today)
                .OrderBy(e => e.EventDate)
                .FirstOrDefaultAsync();

            var upcomingEvents = await _context.Events
                .Where(e => e.EventDate >= today)
                .OrderBy(e => e.EventDate)
                .Take(3)
                .ToListAsync();

            var announcements = await _context.Announcements
                .OrderByDescending(a => a.Timestamp)
                .Take(3)
                .ToListAsync();

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

            var totalFeesUnpaid = unpaidFees.Sum(f => f.Amount);
            var totalFinesUnpaid = unpaidFines.Sum(f => f.Amount);
            var totalBalanceDue = totalFeesUnpaid + totalFinesUnpaid;

            ViewBag.UnpaidFeesCount = unpaidFees.Count;
            ViewBag.UnpaidFinesCount = unpaidFines.Count;
            ViewBag.TotalBalanceDue = totalBalanceDue;

            // ============================================================
            // NEW: ATTENDANCE RATE
            // ============================================================
            var totalEvents = await _context.Events
                .Where(e => e.EventDate < today) // Only count past events
                .CountAsync();

            var studentAttendances = await _context.Attendances
                .Include(a => a.Event)
                .Where(a => a.StudentNum == user.UserName &&
                           a.AttendanceStatus == "Present" &&
                           a.Event != null &&
                           a.Event.EventDate < today)
                .CountAsync();

            var attendanceRate = totalEvents > 0
                ? Math.Round((double)studentAttendances / totalEvents * 100, 1)
                : 0;

            ViewBag.AttendanceRate = attendanceRate;
            ViewBag.EventsAttended = studentAttendances;
            ViewBag.TotalPastEvents = totalEvents;

            ViewBag.Student = student;
            ViewBag.NextEvent = nextEvent;
            ViewBag.UpcomingEvents = upcomingEvents;
            ViewBag.Announcements = announcements;

            return View("StudentDashboard");
        }

        [AllowAnonymous]
        public IActionResult Gateway()
        {
            if (_signInManager.IsSignedIn(User)) { return RedirectToAction("Index"); }
            return View();
        }

        public IActionResult Privacy() { return View(); }
    }
}