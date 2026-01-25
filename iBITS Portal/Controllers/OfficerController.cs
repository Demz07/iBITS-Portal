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

            // Pass section info for Class Secretary
            if (User.IsInRole("Class Secretary") && !User.IsInRole("Org Secretary"))
            {
                var user = await _userManager.GetUserAsync(User);
                var secretary = await _context.Students.FindAsync(user.UserName);
                ViewBag.SecretarySection = secretary?.YearLevelSection;
            }

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
        // GET FINES DATA (AJAX Endpoint for Payments View)
        // ============================================================
        [Authorize(Roles = "Org Treasurer, Class Treasurer")]
        public async Task<JsonResult> GetFinesData()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user.UserName);

                if (treasurer == null)
                {
                    return Json(new { success = false, message = "Treasurer not found." });
                }

                var finesQuery = _context.Fines.Include(f => f.StudentNumNavigation).AsQueryable();
                bool isOrg = await _userManager.IsInRoleAsync(user, "Org Treasurer");
                bool isClass = await _userManager.IsInRoleAsync(user, "Class Treasurer");

                // Filter for Class Treasurer
                if (isClass && !isOrg)
                {
                    if (string.IsNullOrEmpty(treasurer.YearLevelSection))
                    {
                        return Json(new { success = false, message = "No section assigned." });
                    }
                    finesQuery = finesQuery.Where(f => f.StudentNumNavigation.YearLevelSection == treasurer.YearLevelSection);
                }

                var fines = await finesQuery
                    .OrderBy(f => f.FinesStatus)
                    .ThenByDescending(f => f.FinesDueDate)
                    .Select(f => new
                    {
                        fineId = f.FineId,
                        studentNum = f.StudentNum,
                        studentName = f.StudentNumNavigation != null ? f.StudentNumNavigation.StudentFn + " " + f.StudentNumNavigation.StudentLn : "N/A",
                        fineReason = f.Description ?? "Fine",
                        amount = f.Amount ?? 0,
                        finesDueDate = f.FinesDueDate,
                        finesStatus = f.FinesStatus ?? "Unpaid"
                    })
                    .ToListAsync();

                return Json(new { success = true, fines = fines });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading fines data." });
            }
        }

        // ============================================================
        // REALTIME STATUS UPDATE (Replaces Remittance Logic)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Class Treasurer, Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFeeAsPaid(int feeId, string? paymentMethod, string? transactionRef, string? notes)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user.UserName);
                var fee = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);

                if (fee == null)
                {
                    TempData["Error"] = "Fee not found.";
                    return RedirectToAction("Payments");
                }

                if (treasurer == null)
                {
                    TempData["Error"] = "Treasurer profile not found.";
                    return RedirectToAction("Payments");
                }

                // Security check for Class Treasurer
                if (User.IsInRole("Class Treasurer") && !User.IsInRole("Org Treasurer"))
                {
                    if (fee.StudentNumNavigation?.YearLevelSection != treasurer.YearLevelSection)
                    {
                        TempData["Error"] = "Unauthorized: Student belongs to a different section.";
                        return RedirectToAction("Payments");
                    }
                }

                // Check if already paid
                if (fee.FeeStatus?.ToUpper() == "PAID")
                {
                    TempData["Warning"] = "This fee is already marked as paid.";
                    return RedirectToAction("Payments");
                }

                // Get current academic year from system settings
                var currentAcadYear = await _context.SystemSettings
                    .Where(s => s.SettingKey == "CurrentAcademicYear")
                    .Select(s => s.SettingValue)
                    .FirstOrDefaultAsync();

                // Update fee status
                fee.FeeStatus = "Paid";
                _context.Update(fee);

                // Create payment transaction record for audit trail
                var transaction = new PaymentTransaction
                {
                    FeeId = feeId,
                    StudentNum = fee.StudentNum ?? "",
                    Amount = fee.Amount ?? 0,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = paymentMethod ?? "Cash",
                    ProcessedBy = treasurer.StudentNum ?? "",
                    TransactionReference = transactionRef,
                    Notes = notes,
                    // AcademicYear not in FinePaymentTransaction model
                };

                _context.PaymentTransactions.Add(transaction);

                // Send notification to student
                _context.Notifications.Add(new Notification
                {
                    StudentNum = fee.StudentNum,
                    Title = "Payment Confirmed",
                    Message = $"Your payment of Ã¢â€šÂ±{fee.Amount} for '{fee.FeeName}' has been confirmed by {treasurer.FullName}. " +
                             $"Payment method: {paymentMethod ?? "Cash"}. " +
                             (string.IsNullOrEmpty(transactionRef) ? "" : $"Reference: {transactionRef}."),
                    NotificationType = "Payment",
                    NotificationDate = DateTime.Now,
                    IsRead = false,
                    SentBy = treasurer.StudentNum
                });

                await _context.SaveChangesAsync();

                TempData["Message"] = $"Payment of Ã¢â€šÂ±{fee.Amount} marked as PAID successfully.";
                return RedirectToAction("Payments");
            }
            catch (Exception ex)
            {
                // Log the exception
                TempData["Error"] = "An error occurred while processing the payment.";
                return RedirectToAction("Payments");
            }
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

        // ============================================================
        // MARK FINE AS PAID (New Feature)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Class Treasurer, Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFineAsPaid(int fineId, string? paymentMethod, string? transactionRef, string? notes)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user.UserName);
                var fine = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FineId == fineId);

                if (fine == null)
                {
                    TempData["Error"] = "Fine not found.";
                    return RedirectToAction("Payments");
                }

                if (treasurer == null)
                {
                    TempData["Error"] = "Treasurer profile not found.";
                    return RedirectToAction("Payments");
                }

                // Security check for Class Treasurer
                if (User.IsInRole("Class Treasurer") && !User.IsInRole("Org Treasurer"))
                {
                    if (fine.StudentNumNavigation?.YearLevelSection != treasurer.YearLevelSection)
                    {
                        TempData["Error"] = "Unauthorized: Student belongs to a different section.";
                        return RedirectToAction("Payments");
                    }
                }

                // Check if already paid
                if (fine.FinesStatus?.ToUpper() == "PAID")
                {
                    TempData["Warning"] = "This fine is already marked as paid.";
                    return RedirectToAction("Payments");
                }

                // Get current academic year
                var currentAcadYear = await _context.SystemSettings
                    .Where(s => s.SettingKey == "CurrentAcademicYear")
                    .Select(s => s.SettingValue)
                    .FirstOrDefaultAsync();

                // Update fine status
                fine.FinesStatus = "Paid";
                _context.Update(fine);

                // Create fine payment transaction record
                var transaction = new FinePaymentTransaction
                {
                    FineId = fineId,
                    StudentNum = fine.StudentNum ?? "",
                    Amount = fine.Amount ?? 0,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = paymentMethod ?? "Cash",
                    ProcessedBy = treasurer.StudentNum ?? "",
                    TransactionReference = transactionRef,
                    Notes = notes,
                    // AcademicYear not in FinePaymentTransaction model
                };

                _context.FinePaymentTransactions.Add(transaction);

                // Send notification to student
                _context.Notifications.Add(new Notification
                {
                    StudentNum = fine.StudentNum,
                    Title = "Fine Payment Confirmed",
                    Message = $"Your payment of Ã¢â€šÂ±{fine.Amount} for fine has been confirmed by {treasurer.FullName}. " +
                             $"Payment method: {paymentMethod ?? "Cash"}. " +
                             (string.IsNullOrEmpty(transactionRef) ? "" : $"Reference: {transactionRef}."),
                    NotificationType = "Payment",
                    NotificationDate = DateTime.Now,
                    IsRead = false,
                    SentBy = treasurer.StudentNum
                });

                await _context.SaveChangesAsync();

                TempData["Message"] = $"Fine payment of Ã¢â€šÂ±{fine.Amount} marked as PAID successfully.";
                return RedirectToAction("Payments");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while processing the fine payment.";
                return RedirectToAction("Payments");
            }
        }

        // ============================================================
        // ORG TREASURER DASHBOARD
        // ============================================================
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> OrgTreasurerDashboard()
        {
            // Get all fees and fines
            var fees = await _context.Fees.Include(f => f.StudentNumNavigation).ToListAsync();
            var fines = await _context.Fines.Include(f => f.StudentNumNavigation).ToListAsync();

            // Calculate statistics
            ViewBag.TotalFeesCollected = fees.Where(f => f.FeeStatus?.ToUpper() == "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.TotalFinesCollected = fines.Where(f => f.FinesStatus?.ToUpper() == "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.PendingFees = fees.Where(f => f.FeeStatus?.ToUpper() != "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.PendingFines = fines.Where(f => f.FinesStatus?.ToUpper() != "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.TotalCollections = ViewBag.TotalFeesCollected + ViewBag.TotalFinesCollected;

            // Section breakdown
            var sectionStats = fees.GroupBy(f => f.StudentNumNavigation?.YearLevelSection ?? "Unknown")
                .Select(g => new
                {
                    Section = g.Key,
                    TotalFees = g.Sum(f => f.Amount ?? 0),
                    PaidFees = g.Where(f => f.FeeStatus?.ToUpper() == "PAID").Sum(f => f.Amount ?? 0)
                }).ToList();

            ViewBag.SectionStats = sectionStats;

            return View();
        }

        // ============================================================
        // POST PAYMENT REMINDER (Org Treasurer)
        // ============================================================
        [Authorize(Roles = "Org Treasurer")]
        public IActionResult PaymentReminders()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostPaymentReminder(string content, string targetAudience)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Content required.";
                return RedirectToAction("PaymentReminders");
            }

            var user = await _userManager.GetUserAsync(User);
            var treasurer = await _context.Students.FindAsync(user.UserName);
            string poster = treasurer != null ? $"{treasurer.StudentFn} {treasurer.StudentLn}" : "Org Treasurer";

            var announcement = new Announcement
            {
                Title = "Payment Reminder",
                Content = content,
                PostedBy = poster,
                Timestamp = DateTime.Now,
                AnnouncementType = "Payment Reminder",
                TargetAudience = targetAudience
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Payment reminder posted successfully.";
            return RedirectToAction("PaymentReminders");
        }

        // ============================================================
        // VIEW ALL PAYMENT RECORDS (Org Treasurer)
        // ============================================================
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> PaymentRecords(string? section, DateTime? startDate, DateTime? endDate)
        {
            var feeTransactions = _context.PaymentTransactions
                .Include(t => t.Student)
                .Include(t => t.Fee)
                .Include(t => t.Treasurer)
                .AsQueryable();

            var fineTransactions = _context.FinePaymentTransactions
                .Include(t => t.Student)
                .Include(t => t.Fine)
                .Include(t => t.Treasurer)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(section))
            {
                feeTransactions = feeTransactions.Where(t => t.Student.YearLevelSection == section);
                fineTransactions = fineTransactions.Where(t => t.Student.YearLevelSection == section);
            }

            if (startDate.HasValue)
            {
                feeTransactions = feeTransactions.Where(t => t.PaymentDate >= startDate.Value);
                fineTransactions = fineTransactions.Where(t => t.PaymentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                feeTransactions = feeTransactions.Where(t => t.PaymentDate <= endDate.Value);
                fineTransactions = fineTransactions.Where(t => t.PaymentDate <= endDate.Value);
            }

            ViewBag.FeeTransactions = await feeTransactions.OrderByDescending(t => t.PaymentDate).ToListAsync();
            ViewBag.FineTransactions = await fineTransactions.OrderByDescending(t => t.PaymentDate).ToListAsync();
            ViewBag.Sections = await _context.Students.Select(s => s.YearLevelSection).Distinct().OrderBy(s => s).ToListAsync();

            return View();
        }

        // ============================================================
        // ORG SECRETARY DASHBOARD
        // ============================================================
        [Authorize(Roles = "Org Secretary")]
        public async Task<IActionResult> OrgSecretaryDashboard()
        {
            var attendances = await _context.Attendances
                .Include(a => a.StudentNumNavigation)
                .Include(a => a.Event)
                .ToListAsync();

            ViewBag.TotalPresent = attendances.Count(a => a.AttendanceStatus?.ToUpper() == "PRESENT");
            ViewBag.TotalAbsent = attendances.Count(a => a.AttendanceStatus?.ToUpper() == "ABSENT");
            ViewBag.TotalExcused = attendances.Count(a => a.AttendanceStatus?.ToUpper() == "EXCUSED");

            return View();
        }

        // ============================================================
        // ATTENDANCE RECORDS WITH FILTERS (Org Secretary)
        // ============================================================
        [Authorize(Roles = "Org Secretary")]
        public async Task<IActionResult> AttendanceRecords(int? eventId, string? program, string? yearLevel)
        {
            var query = _context.Attendances
                .Include(a => a.StudentNumNavigation)
                .Include(a => a.Event)
                .AsQueryable();

            // Apply filters
            if (eventId.HasValue)
            {
                query = query.Where(a => a.EventId == eventId.Value);
            }

            if (!string.IsNullOrEmpty(program))
            {
                query = query.Where(a => a.StudentNumNavigation.Course.Contains(program));
            }

            if (!string.IsNullOrEmpty(yearLevel))
            {
                query = query.Where(a => a.StudentNumNavigation.YearLevelSection.Contains(yearLevel));
            }

            ViewBag.Events = await _context.Events.OrderByDescending(e => e.EventDate).ToListAsync();
            ViewBag.Programs = await _context.Students.Select(s => s.Course).Distinct().OrderBy(p => p).ToListAsync();
            ViewBag.YearLevels = new List<string> { "1st Year", "2nd Year", "3rd Year", "4th Year" };

            var attendances = await query.OrderByDescending(a => a.Event.EventDate).ToListAsync();
            return View(attendances);
        }

        // ============================================================
        // CREATE MANUAL FEE (Org Secretary)
        // ============================================================
        [Authorize(Roles = "Org Secretary")]
        public IActionResult CreateManualFee()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Org Secretary")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateManualFee(string feeName, decimal amount, DateOnly dueDate, string targetScope, string? specificSection)
        {
            if (string.IsNullOrWhiteSpace(feeName) || amount <= 0)
            {
                TempData["Error"] = "Invalid fee details.";
                return RedirectToAction("CreateManualFee");
            }

            var studentsQuery = _context.Students.Where(s => s.Classification == "Active");

            if (targetScope == "Section" && !string.IsNullOrEmpty(specificSection))
            {
                studentsQuery = studentsQuery.Where(s => s.YearLevelSection == specificSection);
            }

            var students = await studentsQuery.ToListAsync();

            foreach (var student in students)
            {
                _context.Fees.Add(new Fee
                {
                    FeeName = feeName,
                    Amount = amount,
                    FeesDueDate = dueDate,
                    FeeStatus = "Unpaid",
                    StudentNum = student.StudentNum
                });
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = $"Fee '{feeName}' created for {students.Count} students.";
            return RedirectToAction("CreateManualFee");
        }

        // ============================================================
        // CREATE MANUAL FINE (Org Secretary)
        // ============================================================
        [Authorize(Roles = "Org Secretary")]
        public async Task<IActionResult> CreateManualFine()
        {
            ViewBag.Students = await _context.Students.Where(s => s.Classification == "Active").OrderBy(s => s.StudentLn).ToListAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Org Secretary")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateManualFine(string studentNum, decimal amount, DateOnly dueDate, string reason)
        {
            if (string.IsNullOrEmpty(studentNum) || amount <= 0)
            {
                TempData["Error"] = "Invalid fine details.";
                return RedirectToAction("CreateManualFine");
            }

            var fine = new Fine
            {
                StudentNum = studentNum,
                Amount = amount,
                FinesDueDate = dueDate,
                FinesStatus = "Unpaid",
                Description = reason
            };

            _context.Fines.Add(fine);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Manual fine created successfully.";
            return RedirectToAction("CreateManualFine");
        }

        // ============================================================
        // CLASS TREASURY DASHBOARD (Updated with Fines)
        // ============================================================
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> ClassTreasuryDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var treasurer = await _context.Students.FindAsync(user.UserName);

            if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
            {
                TempData["Error"] = "No section assigned to your account.";
                return View(new List<Fee>());
            }

            var section = treasurer.YearLevelSection;

            // Get fees and fines for the section
            var fees = await _context.Fees
                .Include(f => f.StudentNumNavigation)
                .Where(f => f.StudentNumNavigation.YearLevelSection == section)
                .ToListAsync();

            var fines = await _context.Fines
                .Include(f => f.StudentNumNavigation)
                .Where(f => f.StudentNumNavigation.YearLevelSection == section)
                .ToListAsync();

            ViewBag.TotalFeesCollected = fees.Where(f => f.FeeStatus?.ToUpper() == "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.TotalFinesCollected = fines.Where(f => f.FinesStatus?.ToUpper() == "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.PendingFees = fees.Where(f => f.FeeStatus?.ToUpper() != "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.PendingFines = fines.Where(f => f.FinesStatus?.ToUpper() != "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.TotalCollections = ViewBag.TotalFeesCollected + ViewBag.TotalFinesCollected;
            ViewBag.Section = section;

            ViewData["TreasuryTitle"] = $"Section {section} Treasury";

            return View(fees);
        }
    }
}

