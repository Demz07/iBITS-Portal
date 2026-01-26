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
        // FIXED: Better error handling and logging for debugging
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Class Treasurer, Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFeeAsPaid(int feeId, string? paymentMethod, string? transactionRef, string? notes)
        {
            try
            {
                // Debug logging - remove in production
                System.Diagnostics.Debug.WriteLine($"[MarkFeeAsPaid] Starting - FeeId: {feeId}, PaymentMethod: {paymentMethod}");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["Error"] = "User session expired. Please login again.";
                    return RedirectToAction("Payments");
                }

                var treasurer = await _context.Students.FindAsync(user.UserName);
                var fee = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);

                if (fee == null)
                {
                    TempData["Error"] = $"Fee with ID {feeId} not found.";
                    return RedirectToAction("Payments");
                }

                if (treasurer == null)
                {
                    TempData["Error"] = "Treasurer profile not found. Please contact administrator.";
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

                // Update fee status - use explicit "Paid" with capital P for consistency
                fee.FeeStatus = "Paid";
                _context.Fees.Update(fee);

                // Create payment transaction record for audit trail
                var transaction = new PaymentTransaction
                {
                    FeeId = feeId,
                    StudentNum = fee.StudentNum ?? "",
                    Amount = fee.Amount ?? 0,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod.Trim(),
                    ProcessedBy = treasurer.StudentNum ?? "",
                    TransactionReference = string.IsNullOrWhiteSpace(transactionRef) ? null : transactionRef.Trim(),
                    Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
                };

                _context.PaymentTransactions.Add(transaction);

                // Send notification to student
                if (!string.IsNullOrEmpty(fee.StudentNum))
                {
                    _context.Notifications.Add(new Notification
                    {
                        StudentNum = fee.StudentNum,
                        Title = "Payment Confirmed",
                        Message = $"Your payment of ₱{fee.Amount:N2} for '{fee.FeeName}' has been confirmed by {treasurer.FullName}. " +
                                 $"Payment method: {transaction.PaymentMethod}. " +
                                 (string.IsNullOrEmpty(transaction.TransactionReference) ? "" : $"Reference: {transaction.TransactionReference}."),
                        NotificationType = "Payment",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = treasurer.StudentNum
                    });
                }

                // Save all changes in a single transaction
                var savedCount = await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[MarkFeeAsPaid] SaveChanges result: {savedCount} entities saved");

                TempData["Message"] = $"Payment of ₱{fee.Amount:N2} for {fee.StudentNumNavigation?.FullName ?? fee.StudentNum} marked as PAID successfully.";
                return RedirectToAction("Payments");
            }
            catch (Exception ex)
            {
                // Log the full exception for debugging
                System.Diagnostics.Debug.WriteLine($"[MarkFeeAsPaid] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MarkFeeAsPaid] Stack: {ex.StackTrace}");

                TempData["Error"] = $"An error occurred while processing the payment: {ex.Message}";
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
        // FIXED: Better error handling and logging for debugging
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Class Treasurer, Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFineAsPaid(int fineId, string? paymentMethod, string? transactionRef, string? notes)
        {
            try
            {
                // Debug logging - remove in production
                System.Diagnostics.Debug.WriteLine($"[MarkFineAsPaid] Starting - FineId: {fineId}, PaymentMethod: {paymentMethod}");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["Error"] = "User session expired. Please login again.";
                    return RedirectToAction("Payments");
                }

                var treasurer = await _context.Students.FindAsync(user.UserName);
                var fine = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FineId == fineId);

                if (fine == null)
                {
                    TempData["Error"] = $"Fine with ID {fineId} not found.";
                    return RedirectToAction("Payments");
                }

                if (treasurer == null)
                {
                    TempData["Error"] = "Treasurer profile not found. Please contact administrator.";
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

                // Update fine status - use explicit "Paid" with capital P for consistency
                fine.FinesStatus = "Paid";
                _context.Fines.Update(fine);

                // Create fine payment transaction record
                var transaction = new FinePaymentTransaction
                {
                    FineId = fineId,
                    StudentNum = fine.StudentNum ?? "",
                    Amount = fine.Amount ?? 0,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod.Trim(),
                    ProcessedBy = treasurer.StudentNum ?? "",
                    TransactionReference = string.IsNullOrWhiteSpace(transactionRef) ? null : transactionRef.Trim(),
                    Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
                };

                _context.FinePaymentTransactions.Add(transaction);

                // Send notification to student
                if (!string.IsNullOrEmpty(fine.StudentNum))
                {
                    _context.Notifications.Add(new Notification
                    {
                        StudentNum = fine.StudentNum,
                        Title = "Fine Payment Confirmed",
                        Message = $"Your payment of ₱{fine.Amount:N2} for '{fine.Description ?? "Fine"}' has been confirmed by {treasurer.FullName}. " +
                                 $"Payment method: {transaction.PaymentMethod}. " +
                                 (string.IsNullOrEmpty(transaction.TransactionReference) ? "" : $"Reference: {transaction.TransactionReference}."),
                        NotificationType = "Payment",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = treasurer.StudentNum
                    });
                }

                // Save all changes in a single transaction
                var savedCount = await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[MarkFineAsPaid] SaveChanges result: {savedCount} entities saved");

                TempData["Message"] = $"Fine payment of ₱{fine.Amount:N2} for {fine.StudentNumNavigation?.FullName ?? fine.StudentNum} marked as PAID successfully.";
                return RedirectToAction("Payments");
            }
            catch (Exception ex)
            {
                // Log the full exception for debugging
                System.Diagnostics.Debug.WriteLine($"[MarkFineAsPaid] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MarkFineAsPaid] Stack: {ex.StackTrace}");

                TempData["Error"] = $"An error occurred while processing the fine payment: {ex.Message}";
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

        // ============================================================
        // ORG TREASURER - FEES MANAGEMENT (Admin-style UI)
        // ============================================================
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> OrgFees()
        {
            var fees = await _context.Fees
                .Include(f => f.StudentNumNavigation)
                .OrderByDescending(f => f.FeeId)
                .ToListAsync();

            // Calculate statistics
            var totalExpected = fees.Sum(f => f.Amount ?? 0);
            var totalCollected = fees.Where(f => f.FeeStatus?.ToLower() == "paid").Sum(f => f.Amount ?? 0);
            var totalPending = totalExpected - totalCollected;
            var collectionRate = totalExpected > 0 ? Math.Round((totalCollected / totalExpected) * 100, 1) : 0;

            ViewBag.TotalExpected = totalExpected;
            ViewBag.TotalCollected = totalCollected;
            ViewBag.TotalPending = totalPending;
            ViewBag.CollectionRate = collectionRate;

            // Get unique fee names for filter dropdown
            ViewBag.FeeNames = fees.Select(f => f.FeeName).Distinct().OrderBy(n => n).ToList();

            // Get unique sections for filter dropdown
            ViewBag.Sections = await _context.Students
                .Where(s => !string.IsNullOrEmpty(s.YearLevelSection))
                .Select(s => s.YearLevelSection)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            // Chart Data - Collected by Section
            var collectedBreakdown = fees
                .Where(f => f.FeeStatus?.ToLower() == "paid" && f.StudentNumNavigation != null)
                .GroupBy(f => f.StudentNumNavigation.YearLevelSection ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount ?? 0));

            // Chart Data - Pending by Section
            var pendingBreakdown = fees
                .Where(f => f.FeeStatus?.ToLower() != "paid" && f.StudentNumNavigation != null)
                .GroupBy(f => f.StudentNumNavigation.YearLevelSection ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount ?? 0));

            ViewBag.CollectedBreakdown = collectedBreakdown;
            ViewBag.PendingBreakdown = pendingBreakdown;

            return View(fees);
        }

        // ============================================================
        // ORG TREASURER - FINES MANAGEMENT (Admin-style UI)
        // ============================================================
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> OrgFines()
        {
            var fines = await _context.Fines
                .Include(f => f.StudentNumNavigation)
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.Event)
                .OrderByDescending(f => f.FineId)
                .ToListAsync();

            // Calculate statistics (exclude waived from expected)
            var totalExpected = fines.Where(f => f.FinesStatus?.ToLower() != "waived").Sum(f => f.Amount ?? 0);
            var totalCollected = fines.Where(f => f.FinesStatus?.ToLower() == "paid").Sum(f => f.Amount ?? 0);
            var totalPending = totalExpected - totalCollected;
            var collectionRate = totalExpected > 0 ? Math.Round((totalCollected / totalExpected) * 100, 1) : 0;

            ViewBag.TotalExpected = totalExpected;
            ViewBag.TotalCollected = totalCollected;
            ViewBag.TotalPending = totalPending;
            ViewBag.CollectionRate = collectionRate;

            // Get unique sections for filter dropdown
            ViewBag.Sections = await _context.Students
                .Where(s => !string.IsNullOrEmpty(s.YearLevelSection))
                .Select(s => s.YearLevelSection)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            // Get events for filter
            ViewBag.Events = await _context.Events.OrderByDescending(e => e.EventDate).ToListAsync();

            // Get unique manual fine reasons
            ViewBag.ManualFineReasons = fines
                .Where(f => f.AttendanceId == null && !string.IsNullOrEmpty(f.Description))
                .Select(f => f.Description)
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            // Chart Data - Paid by Section
            var paidBreakdown = fines
                .Where(f => f.FinesStatus?.ToLower() == "paid" && f.StudentNumNavigation != null)
                .GroupBy(f => f.StudentNumNavigation.YearLevelSection ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount ?? 0));

            // Chart Data - Unpaid by Section
            var unpaidBreakdown = fines
                .Where(f => f.FinesStatus?.ToLower() == "unpaid" && f.StudentNumNavigation != null)
                .GroupBy(f => f.StudentNumNavigation.YearLevelSection ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount ?? 0));

            ViewBag.PaidBreakdown = paidBreakdown;
            ViewBag.UnpaidBreakdown = unpaidBreakdown;

            return View(fines);
        }

        // ============================================================
        // CLASS TREASURER - FEES MANAGEMENT (Admin-style UI)
        // ============================================================
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> ClassFees()
        {
            var user = await _userManager.GetUserAsync(User);
            var treasurer = await _context.Students.FindAsync(user.UserName);

            if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
            {
                TempData["Error"] = "No section assigned to your account.";
                return RedirectToAction("ClassTreasuryDashboard");
            }

            var section = treasurer.YearLevelSection;

            var fees = await _context.Fees
                .Include(f => f.StudentNumNavigation)
                .Where(f => f.StudentNumNavigation.YearLevelSection == section)
                .OrderByDescending(f => f.FeeId)
                .ToListAsync();

            // Calculate statistics
            var totalExpected = fees.Sum(f => f.Amount ?? 0);
            var totalCollected = fees.Where(f => f.FeeStatus?.ToLower() == "paid").Sum(f => f.Amount ?? 0);
            var totalPending = totalExpected - totalCollected;
            var collectionRate = totalExpected > 0 ? Math.Round((totalCollected / totalExpected) * 100, 1) : 0;

            ViewBag.TotalExpected = totalExpected;
            ViewBag.TotalCollected = totalCollected;
            ViewBag.TotalPending = totalPending;
            ViewBag.CollectionRate = collectionRate;
            ViewBag.Section = section;

            // Get unique fee names for filter dropdown
            ViewBag.FeeNames = fees.Select(f => f.FeeName).Distinct().OrderBy(n => n).ToList();

            // Chart Data - Collected by Fee Type
            var collectedBreakdown = fees
                .Where(f => f.FeeStatus?.ToLower() == "paid")
                .GroupBy(f => f.FeeName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount ?? 0));

            // Chart Data - Pending by Fee Type
            var pendingBreakdown = fees
                .Where(f => f.FeeStatus?.ToLower() != "paid")
                .GroupBy(f => f.FeeName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount ?? 0));

            ViewBag.CollectedBreakdown = collectedBreakdown;
            ViewBag.PendingBreakdown = pendingBreakdown;

            return View(fees);
        }

        // ============================================================
        // CLASS TREASURER - FINES MANAGEMENT (Admin-style UI)
        // ============================================================
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> ClassFines()
        {
            var user = await _userManager.GetUserAsync(User);
            var treasurer = await _context.Students.FindAsync(user.UserName);

            if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
            {
                TempData["Error"] = "No section assigned to your account.";
                return RedirectToAction("ClassTreasuryDashboard");
            }

            var section = treasurer.YearLevelSection;

            var fines = await _context.Fines
                .Include(f => f.StudentNumNavigation)
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.Event)
                .Where(f => f.StudentNumNavigation.YearLevelSection == section)
                .OrderByDescending(f => f.FineId)
                .ToListAsync();

            // Calculate statistics (exclude waived from expected)
            var totalExpected = fines.Where(f => f.FinesStatus?.ToLower() != "waived").Sum(f => f.Amount ?? 0);
            var totalCollected = fines.Where(f => f.FinesStatus?.ToLower() == "paid").Sum(f => f.Amount ?? 0);
            var totalPending = totalExpected - totalCollected;
            var collectionRate = totalExpected > 0 ? Math.Round((totalCollected / totalExpected) * 100, 1) : 0;

            ViewBag.TotalExpected = totalExpected;
            ViewBag.TotalCollected = totalCollected;
            ViewBag.TotalPending = totalPending;
            ViewBag.CollectionRate = collectionRate;
            ViewBag.Section = section;

            // Get events for filter
            ViewBag.Events = await _context.Events.OrderByDescending(e => e.EventDate).ToListAsync();

            // Get unique manual fine reasons for this section
            ViewBag.ManualFineReasons = fines
                .Where(f => f.AttendanceId == null && !string.IsNullOrEmpty(f.Description))
                .Select(f => f.Description)
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            // Chart Data - Paid by Fine Type/Event
            var paidBreakdown = fines
                .Where(f => f.FinesStatus?.ToLower() == "paid")
                .GroupBy(f => f.Description ?? f.Attendance?.Event?.EventName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount ?? 0));

            // Chart Data - Unpaid by Fine Type/Event
            var unpaidBreakdown = fines
                .Where(f => f.FinesStatus?.ToLower() == "unpaid")
                .GroupBy(f => f.Description ?? f.Attendance?.Event?.EventName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount ?? 0));

            ViewBag.PaidBreakdown = paidBreakdown;
            ViewBag.UnpaidBreakdown = unpaidBreakdown;

            return View(fines);
        }

        // ============================================================
        // EXPORT FEES FOR ORG TREASURER
        // ============================================================
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> ExportOrgFees()
        {
            var fees = await _context.Fees
                .Include(f => f.StudentNumNavigation)
                .OrderBy(f => f.StudentNumNavigation.YearLevelSection)
                .ThenBy(f => f.StudentNumNavigation.StudentLn)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Fee ID,Student ID,Student Name,Section,Program,Fee Name,Amount,Due Date,Status");

            foreach (var fee in fees)
            {
                var student = fee.StudentNumNavigation;
                csv.AppendLine($"{fee.FeeId},{fee.StudentNum},\"{student?.FullName ?? "N/A"}\",{student?.YearLevelSection ?? "N/A"},{student?.Course ?? "N/A"},\"{fee.FeeName}\",{fee.Amount:F2},{fee.FeesDueDate?.ToString("yyyy-MM-dd") ?? "N/A"},{fee.FeeStatus}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"OrgFees_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // ============================================================
        // EXPORT FINES FOR ORG TREASURER
        // ============================================================
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> ExportOrgFines()
        {
            var fines = await _context.Fines
                .Include(f => f.StudentNumNavigation)
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.Event)
                .OrderBy(f => f.StudentNumNavigation.YearLevelSection)
                .ThenBy(f => f.StudentNumNavigation.StudentLn)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Fine ID,Student ID,Student Name,Section,Program,Description,Amount,Due Date,Status");

            foreach (var fine in fines)
            {
                var student = fine.StudentNumNavigation;
                var description = !string.IsNullOrEmpty(fine.Description) ? fine.Description : (fine.Attendance?.Event?.EventName ?? "Unknown");
                csv.AppendLine($"{fine.FineId},{fine.StudentNum},\"{student?.FullName ?? "N/A"}\",{student?.YearLevelSection ?? "N/A"},{student?.Course ?? "N/A"},\"{description}\",{fine.Amount:F2},{fine.FinesDueDate?.ToString("yyyy-MM-dd") ?? "N/A"},{fine.FinesStatus}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"OrgFines_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // ============================================================
        // EXPORT FEES FOR CLASS TREASURER
        // ============================================================
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> ExportClassFees()
        {
            var user = await _userManager.GetUserAsync(User);
            var treasurer = await _context.Students.FindAsync(user.UserName);

            if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
            {
                TempData["Error"] = "No section assigned.";
                return RedirectToAction("ClassFees");
            }

            var section = treasurer.YearLevelSection;

            var fees = await _context.Fees
                .Include(f => f.StudentNumNavigation)
                .Where(f => f.StudentNumNavigation.YearLevelSection == section)
                .OrderBy(f => f.StudentNumNavigation.StudentLn)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Fee ID,Student ID,Student Name,Fee Name,Amount,Due Date,Status");

            foreach (var fee in fees)
            {
                var student = fee.StudentNumNavigation;
                csv.AppendLine($"{fee.FeeId},{fee.StudentNum},\"{student?.FullName ?? "N/A"}\",\"{fee.FeeName}\",{fee.Amount:F2},{fee.FeesDueDate?.ToString("yyyy-MM-dd") ?? "N/A"},{fee.FeeStatus}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Section_{section}_Fees_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // ============================================================
        // EXPORT FINES FOR CLASS TREASURER
        // ============================================================
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> ExportClassFines()
        {
            var user = await _userManager.GetUserAsync(User);
            var treasurer = await _context.Students.FindAsync(user.UserName);

            if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
            {
                TempData["Error"] = "No section assigned.";
                return RedirectToAction("ClassFines");
            }

            var section = treasurer.YearLevelSection;

            var fines = await _context.Fines
                .Include(f => f.StudentNumNavigation)
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.Event)
                .Where(f => f.StudentNumNavigation.YearLevelSection == section)
                .OrderBy(f => f.StudentNumNavigation.StudentLn)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Fine ID,Student ID,Student Name,Description,Amount,Due Date,Status");

            foreach (var fine in fines)
            {
                var student = fine.StudentNumNavigation;
                var description = !string.IsNullOrEmpty(fine.Description) ? fine.Description : (fine.Attendance?.Event?.EventName ?? "Unknown");
                csv.AppendLine($"{fine.FineId},{fine.StudentNum},\"{student?.FullName ?? "N/A"}\",\"{description}\",{fine.Amount:F2},{fine.FinesDueDate?.ToString("yyyy-MM-dd") ?? "N/A"},{fine.FinesStatus}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Section_{section}_Fines_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // ============================================================
        // PREVIEW STUDENT COUNT FOR CREATE FEE/FINE (Org Treasurer)
        // ============================================================
        [Authorize(Roles = "Org Treasurer")]
        public async Task<JsonResult> PreviewStudentCount(string programFilter, string yearFilter)
        {
            var query = _context.Students.Where(s => s.Classification == "Active");

            if (programFilter != "all" && !string.IsNullOrEmpty(programFilter))
            {
                query = query.Where(s => s.Course.ToLower().Contains(programFilter.ToLower()));
            }

            if (yearFilter != "all" && !string.IsNullOrEmpty(yearFilter))
            {
                query = query.Where(s => s.YearLevelSection.Contains(yearFilter));
            }

            var students = await query.ToListAsync();
            var bsitCount = students.Count(s => s.Course?.ToUpper().Contains("BSIT") == true);
            var ditCount = students.Count(s => s.Course?.ToUpper().Contains("DIT") == true);

            return Json(new { success = true, total = students.Count, bsit = bsitCount, dit = ditCount });
        }

        // ============================================================
        // CREATE FEE FOR ORG TREASURER (with filters)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrgFee(string feeName, decimal amount, DateOnly? feesDueDate, string programFilter, string yearFilter)
        {
            if (string.IsNullOrWhiteSpace(feeName) || amount <= 0)
            {
                TempData["Error"] = "Invalid fee details.";
                return RedirectToAction("OrgFees");
            }

            var query = _context.Students.Where(s => s.Classification == "Active");

            if (programFilter != "all" && !string.IsNullOrEmpty(programFilter))
            {
                query = query.Where(s => s.Course.ToLower().Contains(programFilter.ToLower()));
            }

            if (yearFilter != "all" && !string.IsNullOrEmpty(yearFilter))
            {
                query = query.Where(s => s.YearLevelSection.Contains(yearFilter));
            }

            var students = await query.ToListAsync();
            var batchId = Guid.NewGuid().ToString();

            foreach (var student in students)
            {
                _context.Fees.Add(new Fee
                {
                    FeeName = feeName,
                    Amount = amount,
                    FeesDueDate = feesDueDate ?? DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
                    FeeStatus = "Unpaid",
                    StudentNum = student.StudentNum,
                    BatchId = batchId
                });
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = $"Fee '{feeName}' created for {students.Count} students.";
            return RedirectToAction("OrgFees");
        }

        // ============================================================
        // CREATE FINE FOR ORG TREASURER (with filters)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrgFine(string fineReason, decimal amount, DateOnly? finesDueDate, string programFilter, string yearFilter)
        {
            if (string.IsNullOrWhiteSpace(fineReason) || amount <= 0)
            {
                TempData["Error"] = "Invalid fine details.";
                return RedirectToAction("OrgFines");
            }

            var query = _context.Students.Where(s => s.Classification == "Active");

            if (programFilter != "all" && !string.IsNullOrEmpty(programFilter))
            {
                query = query.Where(s => s.Course.ToLower().Contains(programFilter.ToLower()));
            }

            if (yearFilter != "all" && !string.IsNullOrEmpty(yearFilter))
            {
                query = query.Where(s => s.YearLevelSection.Contains(yearFilter));
            }

            var students = await query.ToListAsync();
            var batchId = Guid.NewGuid().ToString();

            foreach (var student in students)
            {
                _context.Fines.Add(new Fine
                {
                    Description = fineReason,
                    Amount = amount,
                    FinesDueDate = finesDueDate ?? DateOnly.FromDateTime(DateTime.Now.AddDays(15)),
                    FinesStatus = "Unpaid",
                    StudentNum = student.StudentNum,
                    BatchId = batchId
                });
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = $"Fine '{fineReason}' created for {students.Count} students.";
            return RedirectToAction("OrgFines");
        }
    }
}

