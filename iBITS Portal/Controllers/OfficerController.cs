// ============================================================
// FILE PATH: Controllers/OfficerController.cs
// ============================================================
// FIXED: Removed duplicate methods.
//        Consolidated Status Updates into 'MarkFeeAsPaid'
//        Cleaned up Remittance logic as per request.
// ============================================================

using iBITS_Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iBITS_Portal.Controllers
{
    public class OfficerController : Controller
    {
        private readonly PortaliBitsContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private const string QR_PREFIX = "iBITS:";

        public OfficerController(PortaliBitsContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // HELPER: Parse Student Number from QR Code
        // ============================================================
        private string ParseStudentNumFromQr(string scannedData)
        {
            if (string.IsNullOrEmpty(scannedData)) return string.Empty;
            if (scannedData.StartsWith(QR_PREFIX, StringComparison.OrdinalIgnoreCase))
                return scannedData.Substring(QR_PREFIX.Length);
            return scannedData;
        }

        // ============================================================
        // ANNOUNCEMENTS
        // ============================================================
        [Authorize(Roles = "Officer")]
        public async Task<IActionResult> Announcements()
        {
            var announcements = await _context.Announcements.OrderByDescending(a => a.Timestamp).Take(10).ToListAsync();
            return View(announcements);
        }

        [HttpPost]
        [Authorize(Roles = "Officer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostAnnouncement(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) { TempData["Error"] = "Content required."; return RedirectToAction("Announcements"); }
            var user = await _userManager.GetUserAsync(User);
            var student = await _context.Students.FindAsync(user.UserName);
            string poster = (student != null) ? $"{student.StudentFn} {student.StudentLn}" : "Officer";
            _context.Announcements.Add(new Announcement { Title = "Announcement", Content = content, PostedBy = poster, Timestamp = DateTime.Now });
            await _context.SaveChangesAsync();
            return RedirectToAction("Announcements");
        }

        // ============================================================
        // EVENT MANAGEMENT
        // ============================================================
        [Authorize(Roles = "Officer")]
        public async Task<IActionResult> EventManagement()
        {
            return View(await _context.Events.OrderByDescending(e => e.EventDate).ToListAsync());
        }

        [HttpPost]
        [Authorize(Roles = "Officer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event newEvent)
        {
            if (ModelState.IsValid)
            {
                _context.Events.Add(newEvent);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Event created.";
            }
            else TempData["Error"] = "Invalid data.";
            return RedirectToAction("EventManagement");
        }

        // ============================================================
        // GLOBAL OVERSIGHT
        // ============================================================
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> GlobalPayments()
        {
            return View(await _context.Fees.Include(f => f.StudentNumNavigation).OrderByDescending(f => f.FeeId).ToListAsync());
        }

        [Authorize(Roles = "Org Secretary")]
        public async Task<IActionResult> GlobalAttendance()
        {
            return View(await _context.Attendances.Include(a => a.StudentNumNavigation).Include(a => a.Event).ToListAsync());
        }

        // ============================================================
        // QR SCANNER
        // ============================================================
        [Authorize(Roles = "Org Secretary, Class Secretary")]
        public async Task<IActionResult> Scanner()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var events = await _context.Events.Where(e => e.EventDate >= today).OrderBy(e => e.EventDate).ToListAsync();
            return View(events);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Org Secretary, Class Secretary")]
        public async Task<JsonResult> ProcessScan([FromBody] ScanRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.ScannedData))
                {
                    return Json(new { success = false, message = "Invalid QR code data." });
                }

                string studentId = ParseStudentNumFromQr(request.ScannedData);
                if (string.IsNullOrEmpty(studentId))
                {
                    return Json(new { success = false, message = "QR code is not in the correct format." });
                }

                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    return Json(new { success = false, message = $"Student with ID '{studentId}' not found." });
                }

                // Security: Check Section for Class Secretary role
                if (User.IsInRole("Class Secretary") && !User.IsInRole("Org Secretary"))
                {
                    var secretaryUser = await _context.Students.FindAsync(_userManager.GetUserName(User));
                    if (secretaryUser?.YearLevelSection != student.YearLevelSection)
                    {
                        return Json(new { success = false, message = $"Scan failed: Student belongs to a different section ({student.YearLevelSection})." });
                    }
                }

                // Check for duplicate scan
                if (await _context.Attendances.AnyAsync(a => a.StudentNum == studentId && a.EventId == request.EventId))
                {
                    return Json(new { success = false, message = $"{student.FullName} has already been scanned for this event." });
                }

                // Add attendance record
                _context.Attendances.Add(new Attendance { StudentNum = studentId, EventId = request.EventId, AttendanceStatus = "Present" });
                await _context.SaveChangesAsync();

                // Return rich data for the UI
                return Json(new
                {
                    success = true,
                    message = "Attendance recorded successfully.",
                    scanTime = DateTime.Now.ToString("h:mm:ss tt"),
                    studentId = student.StudentNum,
                    studentName = student.FullName,
                    profileImage = student.StudentImage, // <-- ADDED
                    section = student.YearLevelSection ?? "N/A", // <-- ADDED
                    status = "Present"
                });
            }
            catch (Exception ex)
            {
                // Log the exception ex
                return Json(new { success = false, message = "An unexpected server error occurred." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Org Secretary, Class Secretary")]
        public async Task<JsonResult> VerifyStudentQr([FromBody] VerifyRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.ScannedData))
                {
                    return Json(new { success = false, message = "Invalid QR code data." });
                }

                string studentId = ParseStudentNumFromQr(request.ScannedData);
                if (string.IsNullOrEmpty(studentId))
                {
                    return Json(new { success = false, message = "QR code is not in the correct format." });
                }

                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    return Json(new { success = false, message = $"Student with ID '{studentId}' not found." });
                }

                // Return rich data for the UI
                return Json(new
                {
                    success = true,
                    studentNum = student.StudentNum,
                    fullName = student.FullName,
                    profileImage = student.StudentImage, // <-- ADDED
                    yearLevelSection = student.YearLevelSection ?? "N/A" // <-- ADDED
                });
            }
            catch (Exception ex)
            {
                // Log the exception ex
                return Json(new { success = false, message = "An unexpected server error occurred." });
            }
        }

        public class VerifyRequest { public string ScannedData { get; set; } = ""; }
        public class ScanRequest { public string ScannedData { get; set; } = ""; public int EventId { get; set; } }

        // ============================================================
        // TREASURER PAYMENTS DASHBOARD (Unified)
        // ============================================================
        [Authorize(Roles = "Org Treasurer, Class Treasurer")]
        public async Task<IActionResult> Payments()
        {
            var user = await _userManager.GetUserAsync(User);
            var treasurer = await _context.Students.FindAsync(user.UserName);
            if (treasurer == null) return NotFound("Profile error.");

            var query = _context.Fees.Include(f => f.StudentNumNavigation).AsQueryable();
            bool isOrg = await _userManager.IsInRoleAsync(user, "Org Treasurer");
            bool isClass = await _userManager.IsInRoleAsync(user, "Class Treasurer");

            // Filter for Class Treasurer
            if (isClass && !isOrg)
            {
                if (string.IsNullOrEmpty(treasurer.YearLevelSection)) { TempData["Error"] = "No section assigned."; return View(new List<Fee>()); }
                query = query.Where(f => f.StudentNumNavigation.YearLevelSection == treasurer.YearLevelSection);
                ViewData["SectionFilter"] = treasurer.YearLevelSection;
            }

            var fees = await query.OrderBy(f => f.FeeStatus).ThenByDescending(f => f.FeesDueDate).ToListAsync();

            // Stats
            ViewBag.TotalCollections = fees.Where(f => f.FeeStatus?.ToUpper() == "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.TotalExpected = fees.Sum(f => f.Amount ?? 0);
            ViewBag.PendingCount = fees.Count(f => f.FeeStatus?.ToUpper() != "PAID");
            ViewBag.FeeNames = fees.Select(f => f.FeeName).Distinct().OrderBy(n => n).ToList();
            ViewBag.IsOrgTreasurer = isOrg;
            ViewBag.IsClassTreasurer = isClass;
            ViewBag.CanCreateFee = isOrg;

            // Chart Data
            var paidGroups = fees.Where(f => f.FeeStatus?.ToUpper() == "PAID" && f.StudentNumNavigation != null)
                .GroupBy(f => f.StudentNumNavigation.YearLevelSection ?? "Unknown")
                .Select(g => new { Label = g.Key, Value = g.Sum(f => f.Amount ?? 0) }).Where(g => g.Value > 0).ToList();

            var pendingGroups = fees.Where(f => f.FeeStatus?.ToUpper() != "PAID" && f.StudentNumNavigation != null)
                .GroupBy(f => f.StudentNumNavigation.YearLevelSection ?? "Unknown")
                .Select(g => new { Label = g.Key, Value = g.Sum(f => f.Amount ?? 0) }).Where(g => g.Value > 0).ToList();

            ViewBag.PaidChartData = paidGroups;
            ViewBag.PendingChartData = pendingGroups;

            return View(fees);
        }

        // ============================================================
        // REALTIME STATUS UPDATE (Replaces Remittance Logic)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Class Treasurer, Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFeeAsPaid(int feeId)
        {
            var user = await _userManager.GetUserAsync(User);
            var treasurer = await _context.Students.FindAsync(user.UserName);
            var fee = await _context.Fees.Include(f => f.StudentNumNavigation).FirstOrDefaultAsync(f => f.FeeId == feeId);

            if (fee == null) return RedirectToAction("Payments");

            // Security check for Class Treasurer
            if (User.IsInRole("Class Treasurer") && !User.IsInRole("Org Treasurer"))
            {
                if (fee.StudentNumNavigation.YearLevelSection != treasurer.YearLevelSection)
                {
                    TempData["Error"] = "Unauthorized section.";
                    return RedirectToAction("Payments");
                }
            }

            fee.FeeStatus = "Paid";
            _context.Update(fee);

            // Notification
            _context.Notifications.Add(new Notification
            {
                StudentNum = fee.StudentNum,
                Title = "Payment Confirmed",
                Message = $"Fee '{fee.FeeName}' marked as PAID by {treasurer.FullName}.",
                NotificationType = "Payment",
                NotificationDate = DateTime.Now,
                IsRead = false,
                SentBy = treasurer.StudentNum
            });

            await _context.SaveChangesAsync();
            TempData["Message"] = "Payment marked as PAID.";
            return RedirectToAction("Payments");
        }

        // ============================================================
        // CREATE FEE (Org Only)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrganizationFee(string feeName, decimal amount, DateOnly dueDate)
        {
            if (string.IsNullOrWhiteSpace(feeName) || amount <= 0) return RedirectToAction("Payments");

            var students = await _context.Students.Where(s => s.Classification == "Active").ToListAsync();
            foreach (var s in students)
            {
                _context.Fees.Add(new Fee { FeeName = feeName, Amount = amount, FeesDueDate = dueDate, FeeStatus = "Unpaid", StudentNum = s.StudentNum });
            }
            await _context.SaveChangesAsync();
            TempData["Message"] = $"Fee created for {students.Count} students.";
            return RedirectToAction("Payments");
        }
    }
}