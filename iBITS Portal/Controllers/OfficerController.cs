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

                    // CATEGORY CLOSURE CHECK: If this fee category has been validated, Class Treasurer cannot edit anymore
                    var validatedRemittance = await _context.Remittances
                        .FirstOrDefaultAsync(r => r.Section == treasurer.YearLevelSection
                            && r.FeeName == fee.FeeName
                            && r.RemittanceType == RemittanceType.Fee
                            && r.Status == RemittanceStatus.Validated);

                    if (validatedRemittance != null)
                    {
                        TempData["Error"] = $"This fee category '{fee.FeeName}' has been validated and is now closed. Only the Org Treasurer can update remaining unpaid items. Batch: {validatedRemittance.BatchCode}";
                        return RedirectToAction("ClassFees");
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
                
                // Track collection metadata based on role
                if (User.IsInRole("Class Treasurer") && !User.IsInRole("Org Treasurer"))
                {
                    // Class Treasurer collects payment - NOT yet validated, needs remittance
                    fee.CollectedBy = treasurer.StudentNum;
                    fee.CollectionDate = DateTime.Now;
                    // RemittanceStatus remains "NotRemitted" (default) - will not appear in Org Treasurer stats
                }
                else if (User.IsInRole("Org Treasurer"))
                {
                    // Org Treasurer direct payment marking - bypass remittance system
                    fee.CollectedBy = treasurer.StudentNum;
                    fee.CollectionDate = DateTime.Now;
                    fee.RemittanceStatus = FeeRemittanceStatus.Remitted; // Immediately validated
                    fee.OfficialPaymentDate = DateTime.Now; // Official record date
                    fee.RemittanceId = null; // Not part of batch remittance
                }
                
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
                    .Include(f => f.Attendance)
                        .ThenInclude(a => a.Event)
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

                    // CATEGORY CLOSURE CHECK: If this fine category has been validated, Class Treasurer cannot edit anymore
                    // Fine category is determined by Description (or Event name)
                    var fineCategory = fine.Description ?? fine.Attendance?.Event?.EventName ?? "Other";
                    var validatedRemittance = await _context.Remittances
                        .FirstOrDefaultAsync(r => r.Section == treasurer.YearLevelSection
                            && r.FeeName == fineCategory
                            && r.RemittanceType == RemittanceType.Fine
                            && r.Status == RemittanceStatus.Validated);

                    if (validatedRemittance != null)
                    {
                        TempData["Error"] = $"This fine category '{fineCategory}' has been validated and is now closed. Only the Org Treasurer can update remaining unpaid items. Batch: {validatedRemittance.BatchCode}";
                        return RedirectToAction("ClassFines");
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
                
                // Track collection metadata based on role
                if (User.IsInRole("Class Treasurer") && !User.IsInRole("Org Treasurer"))
                {
                    // Class Treasurer collects payment - NOT yet validated, needs remittance
                    fine.CollectedBy = treasurer.StudentNum;
                    fine.CollectionDate = DateTime.Now;
                    // RemittanceStatus remains "NotRemitted" (default) - will not appear in Org Treasurer stats
                }
                else if (User.IsInRole("Org Treasurer"))
                {
                    // Org Treasurer direct payment marking - bypass remittance system
                    fine.CollectedBy = treasurer.StudentNum;
                    fine.CollectionDate = DateTime.Now;
                    fine.RemittanceStatus = FeeRemittanceStatus.Remitted; // Immediately validated
                    fine.OfficialPaymentDate = DateTime.Now; // Official record date
                    fine.RemittanceId = null; // Not part of batch remittance
                }
                
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
        // Only counts VALIDATED remittances (not just paid by Class Treasurer)
        // ============================================================
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> OrgTreasurerDashboard()
        {
            // Get all fees and fines
            var fees = await _context.Fees.Include(f => f.StudentNumNavigation).ToListAsync();
            var fines = await _context.Fines.Include(f => f.StudentNumNavigation).ToListAsync();

            // Calculate statistics - ONLY count validated remittances (RemittanceStatus == "Remitted")
            // This ensures only payments that have been validated by Org Treasurer are counted
            ViewBag.TotalFeesCollected = fees
                .Where(f => f.FeeStatus?.ToUpper() == "PAID" && f.RemittanceStatus == FeeRemittanceStatus.Remitted)
                .Sum(f => f.Amount ?? 0);
            ViewBag.TotalFinesCollected = fines
                .Where(f => f.FinesStatus?.ToUpper() == "PAID" && f.RemittanceStatus == FeeRemittanceStatus.Remitted)
                .Sum(f => f.Amount ?? 0);
            
            // Pending includes: unpaid + paid but not yet remitted/validated
            ViewBag.PendingFees = fees
                .Where(f => f.FeeStatus?.ToUpper() != "PAID" || f.RemittanceStatus != FeeRemittanceStatus.Remitted)
                .Sum(f => f.Amount ?? 0);
            ViewBag.PendingFines = fines
                .Where(f => f.FinesStatus?.ToUpper() != "PAID" || f.RemittanceStatus != FeeRemittanceStatus.Remitted)
                .Sum(f => f.Amount ?? 0);
            
            ViewBag.TotalCollections = ViewBag.TotalFeesCollected + ViewBag.TotalFinesCollected;

            // Count pending remittances awaiting validation
            ViewBag.PendingRemittanceCount = await _context.Remittances
                .CountAsync(r => r.Status == RemittanceStatus.Pending);

            // Section breakdown - only count validated remittances as "Paid"
            var sectionStats = fees.GroupBy(f => f.StudentNumNavigation?.YearLevelSection ?? "Unknown")
                .Select(g => new
                {
                    Section = g.Key,
                    TotalFees = g.Sum(f => f.Amount ?? 0),
                    PaidFees = g.Where(f => f.FeeStatus?.ToUpper() == "PAID" && f.RemittanceStatus == FeeRemittanceStatus.Remitted).Sum(f => f.Amount ?? 0)
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
        // ORG SECRETARY DASHBOARD (WITH DYNAMIC ANALYTICS FILTERS)
        // ============================================================
        [Authorize(Roles = "Org Secretary")]
        public async Task<IActionResult> OrgSecretaryDashboard(int? eventId, string program, string yearLevel)
        {
            // --- 1. STATS CARDS & GAUGE LOGIC ---
            var globalQuery = _context.Attendances.Include(a => a.StudentNumNavigation).AsQueryable();
            if (eventId.HasValue) globalQuery = globalQuery.Where(a => a.EventId == eventId);
            if (!string.IsNullOrEmpty(program)) globalQuery = globalQuery.Where(a => a.StudentNumNavigation.Course == program);
            if (!string.IsNullOrEmpty(yearLevel)) globalQuery = globalQuery.Where(a => a.StudentNumNavigation.YearLevelSection.Contains(yearLevel));

            ViewBag.TotalPresent = await globalQuery.CountAsync(a => a.AttendanceStatus == "Present");
            ViewBag.TotalAbsent = await globalQuery.CountAsync(a => a.AttendanceStatus == "Absent");
            ViewBag.TotalExcused = await globalQuery.CountAsync(a => a.AttendanceStatus == "Excused");

            // --- 2. BAR GRAPH LOGIC (Attendee Breakdown) ---
            var distQuery = _context.Attendances.Include(a => a.StudentNumNavigation)
                .Where(a => a.AttendanceStatus == "Present");

            if (eventId.HasValue) distQuery = distQuery.Where(a => a.EventId == eventId);

            var distribution = await distQuery
                .GroupBy(a => new { a.StudentNumNavigation.Course, a.StudentNumNavigation.YearLevelSection })
                .Select(g => new {
                    Label = g.Key.Course + " " + g.Key.YearLevelSection,
                    Count = g.Count()
                }).OrderByDescending(x => x.Count).ToListAsync();

            ViewBag.DistLabels = distribution.Select(x => x.Label).ToList();
            ViewBag.DistCounts = distribution.Select(x => x.Count).ToList();

            // --- 3. RE-POPULATE DROPDOWNS ---
            ViewBag.Events = await _context.Events.OrderByDescending(e => e.EventDate).ToListAsync();
            ViewBag.Programs = await _context.Students.Select(s => s.Course).Distinct().Where(c => c != null).ToListAsync();
            ViewBag.YearLevels = await _context.Students.Select(s => s.YearLevelSection).Distinct().Where(y => y != null).ToListAsync();

            return View();
        }
        // ============================================================
        // ATTENDANCE RECORDS WITH FILTERS (Org Secretary)
        // ============================================================
        [Authorize(Roles = "Org Secretary, Class Secretary")]
        public async Task<IActionResult> AttendanceRecords(int? eventId, string program, string yearLevel, string status, string searchQuery)
        {
            var query = _context.Attendances
                .Include(a => a.StudentNumNavigation)
                .Include(a => a.Event)
                .AsQueryable();

            // ============================================================
            // SECURITY FILTER: Restrict Class Secretary to their own Program & Section
            // ============================================================
            if (User.IsInRole("Class Secretary") && !User.IsInRole("Org Secretary"))
            {
                var userStudentNum = User.Identity.Name;
                var secretary = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == userStudentNum);

                if (secretary != null)
                {
                    // FORCE FILTER: Only classmates in the same Program AND same Section
                    query = query.Where(a => a.StudentNumNavigation.Course == secretary.Course &&
                                             a.StudentNumNavigation.YearLevelSection == secretary.YearLevelSection);

                    // Pass to ViewBag to lock the UI
                    ViewBag.LockedSection = secretary.YearLevelSection;
                    ViewBag.LockedProgram = secretary.Course;
                }
            }

            // --- Apply other filters (Search and Status) ---
            if (!string.IsNullOrEmpty(searchQuery))
                query = query.Where(a => a.StudentNum.Contains(searchQuery) ||
                                         a.StudentNumNavigation.StudentFn.Contains(searchQuery) ||
                                         a.StudentNumNavigation.StudentLn.Contains(searchQuery));

            if (eventId.HasValue) query = query.Where(a => a.EventId == eventId);
            if (!string.IsNullOrEmpty(status)) query = query.Where(a => a.AttendanceStatus == status);

            // Only allow manual Program/Year filter if the user is NOT a restricted Class Secretary
            if (ViewBag.LockedProgram == null && !string.IsNullOrEmpty(program))
                query = query.Where(a => a.StudentNumNavigation.Course == program);

            if (ViewBag.LockedSection == null && !string.IsNullOrEmpty(yearLevel))
                query = query.Where(a => a.StudentNumNavigation.YearLevelSection.Contains(yearLevel));

            var records = await query.ToListAsync();

            // Re-populate Dropdowns
            ViewBag.Events = await _context.Events.OrderByDescending(e => e.EventDate).ToListAsync();
            ViewBag.Programs = await _context.Students.Select(s => s.Course).Distinct().ToListAsync();
            ViewBag.YearLevels = await _context.Students.Select(s => s.YearLevelSection).Distinct().ToListAsync();
            ViewBag.SearchQuery = searchQuery;

            return View(records);
        }


        // ============================================================
        // STATUS UPDATES
        // ============================================================
        // 1. UPDATED FOR INDIVIDUAL UPDATES (Automatically removes Fines)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Org Secretary")]
        public async Task<IActionResult> UpdateAttendanceStatus(int attendanceId, string newStatus)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Event)
                .Include(a => a.StudentNumNavigation)
                .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

            if (attendance != null)
            {
                attendance.AttendanceStatus = newStatus;

                // 1. CLEAR FINES if marked Present or Excused
                if (newStatus == "Present" || newStatus == "Excused")
                {
                    var associatedFines = _context.Fines.Where(f => f.AttendanceId == attendanceId);
                    _context.Fines.RemoveRange(associatedFines);
                }
                // 2. ADD FINE if marked Absent
                else if (newStatus == "Absent")
                {
                    var existingFine = await _context.Fines.AnyAsync(f => f.AttendanceId == attendanceId);
                    if (!existingFine)
                    {
                        // Determine fine amount based on role
                        decimal fineAmount = 100; // Default for Members
                        var targetUser = await _userManager.FindByNameAsync(attendance.StudentNum);

                        if (targetUser != null)
                        {
                            var roles = await _userManager.GetRolesAsync(targetUser);
                            if (roles.Contains("Org Treasurer") || roles.Contains("Org Secretary") || roles.Contains("Officer"))
                            {
                                fineAmount = 300;
                            }
                            else if (roles.Contains("Class Treasurer") || roles.Contains("Class Secretary"))
                            {
                                fineAmount = 150;
                            }
                        }

                        _context.Fines.Add(new Fine
                        {
                            StudentNum = attendance.StudentNum,
                            AttendanceId = attendanceId,
                            Amount = fineAmount,
                            FinesStatus = "Unpaid",
                            FinesDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                            Description = $"Absence Fine: {attendance.Event?.EventName ?? "Event"}",
                            AmountPaid = 0
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Status updated to {newStatus}. Fine of ₱{((newStatus == "Absent") ? "calculated" : "0")} applied.";
            }
            return RedirectToAction(nameof(AttendanceRecords));
        }

        // 2. UPDATED FOR BULK UPDATES (Automatically removes Fines)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Org Secretary")]
        public async Task<IActionResult> BulkUpdateStatus(List<int> attendanceIds, string bulkStatus)
        {
            if (attendanceIds != null && attendanceIds.Any())
            {
                var records = await _context.Attendances
                    .Include(a => a.Event)
                    .Where(a => attendanceIds.Contains(a.AttendanceId))
                    .ToListAsync();

                foreach (var r in records)
                {
                    r.AttendanceStatus = bulkStatus;

                    if (bulkStatus == "Absent")
                    {
                        var exists = await _context.Fines.AnyAsync(f => f.AttendanceId == r.AttendanceId);
                        if (!exists)
                        {
                            // Determine fine amount based on role
                            decimal fineAmount = 100;
                            var targetUser = await _userManager.FindByNameAsync(r.StudentNum);

                            if (targetUser != null)
                            {
                                var roles = await _userManager.GetRolesAsync(targetUser);
                                if (roles.Contains("Org Treasurer") || roles.Contains("Org Secretary") || roles.Contains("Officer"))
                                {
                                    fineAmount = 300;
                                }
                                else if (roles.Contains("Class Treasurer") || roles.Contains("Class Secretary"))
                                {
                                    fineAmount = 150;
                                }
                            }

                            _context.Fines.Add(new Fine
                            {
                                StudentNum = r.StudentNum,
                                AttendanceId = r.AttendanceId,
                                Amount = fineAmount,
                                FinesStatus = "Unpaid",
                                FinesDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                                Description = $"Absence Fine: {r.Event?.EventName ?? "Event"}",
                                AmountPaid = 0
                            });
                        }
                    }
                }

                if (bulkStatus == "Present" || bulkStatus == "Excused")
                {
                    var finesToRemove = _context.Fines.Where(f => f.AttendanceId.HasValue && attendanceIds.Contains(f.AttendanceId.Value));
                    _context.Fines.RemoveRange(finesToRemove);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Updated {records.Count} records. Fines applied based on officer/member status.";
            }
            return RedirectToAction(nameof(AttendanceRecords));
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

            // Get paid fees and fines
            var paidFees = fees.Where(f => f.FeeStatus?.ToUpper() == "PAID").ToList();
            var paidFines = fines.Where(f => f.FinesStatus?.ToUpper() == "PAID").ToList();

            // Total collected by Class Treasurer (all paid items)
            ViewBag.TotalFeesCollected = paidFees.Sum(f => f.Amount ?? 0);
            ViewBag.TotalFinesCollected = paidFines.Sum(f => f.Amount ?? 0);
            ViewBag.TotalCollections = ViewBag.TotalFeesCollected + ViewBag.TotalFinesCollected;

            // Breakdown by remittance status for FEES
            ViewBag.FeesNotRemitted = paidFees.Where(f => f.RemittanceStatus == FeeRemittanceStatus.NotRemitted).Sum(f => f.Amount ?? 0);
            ViewBag.FeesNotRemittedCount = paidFees.Count(f => f.RemittanceStatus == FeeRemittanceStatus.NotRemitted);
            ViewBag.FeesPendingValidation = paidFees.Where(f => f.RemittanceStatus == FeeRemittanceStatus.PendingRemittance).Sum(f => f.Amount ?? 0);
            ViewBag.FeesPendingValidationCount = paidFees.Count(f => f.RemittanceStatus == FeeRemittanceStatus.PendingRemittance);
            ViewBag.FeesValidated = paidFees.Where(f => f.RemittanceStatus == FeeRemittanceStatus.Remitted).Sum(f => f.Amount ?? 0);
            ViewBag.FeesValidatedCount = paidFees.Count(f => f.RemittanceStatus == FeeRemittanceStatus.Remitted);

            // Breakdown by remittance status for FINES
            ViewBag.FinesNotRemitted = paidFines.Where(f => f.RemittanceStatus == FeeRemittanceStatus.NotRemitted).Sum(f => f.Amount ?? 0);
            ViewBag.FinesNotRemittedCount = paidFines.Count(f => f.RemittanceStatus == FeeRemittanceStatus.NotRemitted);
            ViewBag.FinesPendingValidation = paidFines.Where(f => f.RemittanceStatus == FeeRemittanceStatus.PendingRemittance).Sum(f => f.Amount ?? 0);
            ViewBag.FinesPendingValidationCount = paidFines.Count(f => f.RemittanceStatus == FeeRemittanceStatus.PendingRemittance);
            ViewBag.FinesValidated = paidFines.Where(f => f.RemittanceStatus == FeeRemittanceStatus.Remitted).Sum(f => f.Amount ?? 0);
            ViewBag.FinesValidatedCount = paidFines.Count(f => f.RemittanceStatus == FeeRemittanceStatus.Remitted);

            // Unpaid amounts
            ViewBag.PendingFees = fees.Where(f => f.FeeStatus?.ToUpper() != "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.PendingFines = fines.Where(f => f.FinesStatus?.ToUpper() != "PAID").Sum(f => f.Amount ?? 0);
            
            ViewBag.Section = section;

            ViewData["TreasuryTitle"] = $"Section {section} Treasury";

            return View(fees);
        }

        // ============================================================
        // UPDATE FEE STATUS (Org Treasurer Only)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFeeStatus(int feeId, string newStatus, string? returnUrl)
        {
            try
            {
                var fee = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);

                if (fee == null)
                {
                    TempData["Error"] = $"Fee with ID {feeId} not found.";
                    return Redirect(returnUrl ?? Url.Action("OrgFees"));
                }

                // Validate status (Fees can only be Paid or Unpaid)
                if (newStatus != "Paid" && newStatus != "Unpaid")
                {
                    TempData["Error"] = "Invalid status. Fees can only be 'Paid' or 'Unpaid'.";
                    return Redirect(returnUrl ?? Url.Action("OrgFees"));
                }

                var oldStatus = fee.FeeStatus;

                // LOCK CHECK: Cannot revoke PAID items that are already remitted/validated
                if (oldStatus?.ToUpper() == "PAID" && newStatus == "Unpaid" && fee.RemittanceStatus == FeeRemittanceStatus.Remitted)
                {
                    TempData["Error"] = "Cannot revoke payment. This fee has already been validated through remittance and is locked.";
                    return Redirect(returnUrl ?? Url.Action("OrgFees"));
                }

                fee.FeeStatus = newStatus;
                _context.Fees.Update(fee);

                // If marking as Paid, create a transaction record and set remittance metadata
                if (newStatus == "Paid" && oldStatus?.ToLower() != "paid")
                {
                    var user = await _userManager.GetUserAsync(User);
                    var treasurer = await _context.Students.FindAsync(user.UserName);

                    // Org Treasurer direct marking - bypass remittance system
                    fee.CollectedBy = treasurer?.StudentNum;
                    fee.CollectionDate = DateTime.Now;
                    fee.RemittanceStatus = FeeRemittanceStatus.Remitted; // Immediately validated
                    fee.OfficialPaymentDate = DateTime.Now; // Official record date
                    fee.RemittanceId = null; // Not part of batch remittance
                    _context.Fees.Update(fee);

                    var transaction = new PaymentTransaction
                    {
                        FeeId = feeId,
                        StudentNum = fee.StudentNum ?? "",
                        Amount = fee.Amount ?? 0,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "Status Update",
                        ProcessedBy = treasurer?.StudentNum ?? "",
                        Notes = $"Status updated from '{oldStatus}' to '{newStatus}' by Org Treasurer"
                    };
                    _context.PaymentTransactions.Add(transaction);

                    // Notify student
                    if (!string.IsNullOrEmpty(fee.StudentNum))
                    {
                        _context.Notifications.Add(new Notification
                        {
                            StudentNum = fee.StudentNum,
                            Title = "Fee Status Updated",
                            Message = $"Your fee '{fee.FeeName}' (₱{fee.Amount:N2}) has been marked as {newStatus}.",
                            NotificationType = "Payment",
                            NotificationDate = DateTime.Now,
                            IsRead = false,
                            SentBy = treasurer?.StudentNum
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Message"] = $"Fee status for {fee.StudentNumNavigation?.FullName ?? fee.StudentNum} updated to '{newStatus}'.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating fee status: {ex.Message}";
            }

            return Redirect(returnUrl ?? Url.Action("OrgFees"));
        }

        // ============================================================
        // UPDATE FINE STATUS (Org Treasurer Only)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFineStatus(int fineId, string newStatus, string? returnUrl)
        {
            try
            {
                var fine = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .Include(f => f.Attendance)
                    .FirstOrDefaultAsync(f => f.FineId == fineId);

                if (fine == null)
                {
                    TempData["Error"] = $"Fine with ID {fineId} not found.";
                    return Redirect(returnUrl ?? Url.Action("OrgFines"));
                }

                // Validate status
                // Excused is only valid for event-based fines (those with AttendanceId)
                bool isEventFine = fine.AttendanceId != null;
                
                if (newStatus == "Excused" && !isEventFine)
                {
                    TempData["Error"] = "Only event-based fines can be marked as 'Excused'. Manual fines can only be 'Paid' or 'Unpaid'.";
                    return Redirect(returnUrl ?? Url.Action("OrgFines"));
                }

                if (newStatus != "Paid" && newStatus != "Unpaid" && newStatus != "Excused")
                {
                    TempData["Error"] = "Invalid status. Valid options are 'Paid', 'Unpaid', or 'Excused' (event fines only).";
                    return Redirect(returnUrl ?? Url.Action("OrgFines"));
                }

                var oldStatus = fine.FinesStatus;

                // LOCK CHECK: Cannot edit PAID items that are pending remittance or already remitted
                if (oldStatus?.ToUpper() == "PAID" && newStatus != "Paid")
                {
                    if (fine.RemittanceStatus == FeeRemittanceStatus.PendingRemittance)
                    {
                        TempData["Error"] = "Cannot change status. This fine is currently in a pending remittance batch and is locked until validated or rejected by Org Treasurer.";
                        return Redirect(returnUrl ?? Url.Action("OrgFines"));
                    }
                    if (fine.RemittanceStatus == FeeRemittanceStatus.Remitted)
                    {
                        TempData["Error"] = "Cannot change status. This fine has already been validated through remittance and is permanently locked.";
                        return Redirect(returnUrl ?? Url.Action("OrgFines"));
                    }
                }

                // LOCK CHECK: Cannot mark as PAID if already in remittance process
                if (newStatus == "Paid" && oldStatus?.ToUpper() != "PAID")
                {
                    if (fine.RemittanceStatus == FeeRemittanceStatus.PendingRemittance)
                    {
                        TempData["Error"] = "Cannot mark as paid. This fine is currently in a pending remittance batch.";
                        return Redirect(returnUrl ?? Url.Action("OrgFines"));
                    }
                    if (fine.RemittanceStatus == FeeRemittanceStatus.Remitted)
                    {
                        TempData["Error"] = "Cannot mark as paid. This fine has already been validated through remittance.";
                        return Redirect(returnUrl ?? Url.Action("OrgFines"));
                    }
                }
                fine.FinesStatus = newStatus;
                _context.Fines.Update(fine);

                // If marking as Paid, create a transaction record and set remittance metadata
                if (newStatus == "Paid" && oldStatus?.ToLower() != "paid")
                {
                    var user = await _userManager.GetUserAsync(User);
                    var treasurer = await _context.Students.FindAsync(user.UserName);

                    // Org Treasurer direct marking - bypass remittance system
                    fine.CollectedBy = treasurer?.StudentNum;
                    fine.CollectionDate = DateTime.Now;
                    fine.RemittanceStatus = FeeRemittanceStatus.Remitted; // Immediately validated
                    fine.OfficialPaymentDate = DateTime.Now; // Official record date
                    fine.RemittanceId = null; // Not part of batch remittance
                    _context.Fines.Update(fine);

                    var transaction = new FinePaymentTransaction
                    {
                        FineId = fineId,
                        StudentNum = fine.StudentNum ?? "",
                        Amount = fine.Amount ?? 0,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "Status Update",
                        ProcessedBy = treasurer?.StudentNum ?? "",
                        Notes = $"Status updated from '{oldStatus}' to '{newStatus}' by Org Treasurer"
                    };
                    _context.FinePaymentTransactions.Add(transaction);
                }

                // Notify student
                var currentUser = await _userManager.GetUserAsync(User);
                var currentTreasurer = await _context.Students.FindAsync(currentUser.UserName);
                
                if (!string.IsNullOrEmpty(fine.StudentNum))
                {
                    _context.Notifications.Add(new Notification
                    {
                        StudentNum = fine.StudentNum,
                        Title = "Fine Status Updated",
                        Message = $"Your fine '{fine.Description ?? "Fine"}' (₱{fine.Amount:N2}) has been marked as {newStatus}.",
                        NotificationType = newStatus == "Excused" ? "Excuse" : "Payment",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = currentTreasurer?.StudentNum
                    });
                }

                await _context.SaveChangesAsync();
                TempData["Message"] = $"Fine status for {fine.StudentNumNavigation?.FullName ?? fine.StudentNum} updated to '{newStatus}'.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating fine status: {ex.Message}";
            }

            return Redirect(returnUrl ?? Url.Action("OrgFines"));
        }

        // ============================================================
        // RECORD PARTIAL FEE PAYMENT (Org Treasurer)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordFeePayment(int feeId, decimal paymentAmount, string? paymentMethod, string? transactionRef, string? notes)
        {
            try
            {
                var fee = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);

                if (fee == null)
                {
                    TempData["Error"] = $"Fee with ID {feeId} not found.";
                    return RedirectToAction("OrgFees");
                }

                // Validate payment amount
                var remainingBalance = (fee.Amount ?? 0) - fee.AmountPaid;
                if (paymentAmount <= 0)
                {
                    TempData["Error"] = "Payment amount must be greater than zero.";
                    return RedirectToAction("OrgFees");
                }

                if (paymentAmount > remainingBalance)
                {
                    TempData["Error"] = $"Payment amount (₱{paymentAmount:N2}) cannot exceed the remaining balance (₱{remainingBalance:N2}).";
                    return RedirectToAction("OrgFees");
                }

                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user.UserName);

                // Update the fee's AmountPaid
                fee.AmountPaid += paymentAmount;

                // Update status based on payment
                if (fee.AmountPaid >= (fee.Amount ?? 0))
                {
                    fee.FeeStatus = "Paid";
                }
                else if (fee.AmountPaid > 0)
                {
                    fee.FeeStatus = "Partial";
                }

                _context.Fees.Update(fee);

                // Create payment transaction record
                var transaction = new PaymentTransaction
                {
                    FeeId = feeId,
                    StudentNum = fee.StudentNum ?? "",
                    Amount = paymentAmount,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod.Trim(),
                    ProcessedBy = treasurer?.StudentNum ?? "",
                    TransactionReference = string.IsNullOrWhiteSpace(transactionRef) ? null : transactionRef.Trim(),
                    Notes = string.IsNullOrWhiteSpace(notes) ? $"Partial payment recorded" : notes.Trim()
                };
                _context.PaymentTransactions.Add(transaction);

                // Notify student
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
                        SentBy = treasurer?.StudentNum
                    });
                }

                await _context.SaveChangesAsync();

                var statusMsg = fee.FeeStatus == "Paid" ? "FULLY PAID" : $"Partial payment recorded (Balance: ₱{(fee.Amount ?? 0) - fee.AmountPaid:N2})";
                TempData["Message"] = $"Payment of ₱{paymentAmount:N2} for {fee.StudentNumNavigation?.FullName ?? fee.StudentNum} recorded successfully. Status: {statusMsg}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error recording payment: {ex.Message}";
            }

            return RedirectToAction("OrgFees");
        }

        // ============================================================
        // RECORD PARTIAL FINE PAYMENT (Org Treasurer)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordFinePayment(int fineId, decimal paymentAmount, string? paymentMethod, string? transactionRef, string? notes)
        {
            try
            {
                var fine = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FineId == fineId);

                if (fine == null)
                {
                    TempData["Error"] = $"Fine with ID {fineId} not found.";
                    return RedirectToAction("OrgFines");
                }

                // Check if already excused
                if (fine.FinesStatus?.ToLower() == "excused" || fine.FinesStatus?.ToLower() == "waived")
                {
                    TempData["Error"] = "Cannot record payment for an excused fine.";
                    return RedirectToAction("OrgFines");
                }

                // Validate payment amount
                var remainingBalance = (fine.Amount ?? 0) - fine.AmountPaid;
                if (paymentAmount <= 0)
                {
                    TempData["Error"] = "Payment amount must be greater than zero.";
                    return RedirectToAction("OrgFines");
                }

                if (paymentAmount > remainingBalance)
                {
                    TempData["Error"] = $"Payment amount (₱{paymentAmount:N2}) cannot exceed the remaining balance (₱{remainingBalance:N2}).";
                    return RedirectToAction("OrgFines");
                }

                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user.UserName);

                // Update the fine's AmountPaid
                fine.AmountPaid += paymentAmount;

                // Update status based on payment
                if (fine.AmountPaid >= (fine.Amount ?? 0))
                {
                    fine.FinesStatus = "Paid";
                }
                else if (fine.AmountPaid > 0)
                {
                    fine.FinesStatus = "Partial";
                }

                _context.Fines.Update(fine);

                // Create fine payment transaction record
                var transaction = new FinePaymentTransaction
                {
                    FineId = fineId,
                    StudentNum = fine.StudentNum ?? "",
                    Amount = paymentAmount,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod.Trim(),
                    ProcessedBy = treasurer?.StudentNum ?? "",
                    TransactionReference = string.IsNullOrWhiteSpace(transactionRef) ? null : transactionRef.Trim(),
                    Notes = string.IsNullOrWhiteSpace(notes) ? $"Partial payment recorded" : notes.Trim()
                };
                _context.FinePaymentTransactions.Add(transaction);

                // Notify student
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
                        SentBy = treasurer?.StudentNum
                    });
                }

                await _context.SaveChangesAsync();

                var statusMsg = fine.FinesStatus == "Paid" ? "FULLY PAID" : $"Partial payment recorded (Balance: ₱{(fine.Amount ?? 0) - fine.AmountPaid:N2})";
                TempData["Message"] = $"Payment of ₱{paymentAmount:N2} for {fine.StudentNumNavigation?.FullName ?? fine.StudentNum} recorded successfully. Status: {statusMsg}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error recording payment: {ex.Message}";
            }

            return RedirectToAction("OrgFines");
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

            // Calculate statistics - ONLY count validated (remitted) fees
            var totalExpected = fees.Sum(f => f.Amount ?? 0);
            var totalCollected = fees.Where(f => f.FeeStatus?.ToLower() == "paid" && f.RemittanceStatus == FeeRemittanceStatus.Remitted).Sum(f => f.Amount ?? 0);
            var totalPending = totalExpected - totalCollected;

            ViewBag.TotalExpected = totalExpected;
            ViewBag.TotalCollected = totalCollected;
            ViewBag.TotalPending = totalPending;

            // Get unique fee names for filter dropdown
            ViewBag.FeeNames = fees.Select(f => f.FeeName).Distinct().OrderBy(n => n).ToList();

            // Get unique sections for filter dropdown
            ViewBag.Sections = await _context.Students
                .Where(s => !string.IsNullOrEmpty(s.YearLevelSection))
                .Select(s => s.YearLevelSection)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            // Chart Data - Paid by Program-Year (ONLY VALIDATED REMITTANCES)
            var collectedBreakdown = fees
                .Where(f => f.FeeStatus?.ToLower() == "paid" && f.RemittanceStatus == FeeRemittanceStatus.Remitted && f.StudentNumNavigation != null && !string.IsNullOrEmpty(f.StudentNumNavigation.YearLevelSection))
                .GroupBy(f => {
                    var section = f.StudentNumNavigation.YearLevelSection ?? "Unknown";
                    var parts = section.Split('-');
                    if (parts.Length >= 2)
                    {
                        var program = parts[0];
                        var yearWithSection = parts[1];
                        var year = new string(yearWithSection.TakeWhile(char.IsDigit).ToArray());
                        return $"{program}-{year}";
                    }
                    return "Unknown";
                })
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount ?? 0));

            // Chart Data - Unpaid by Program-Year (includes unpaid + paid but not yet validated)
            var pendingBreakdown = fees
                .Where(f => (f.FeeStatus?.ToLower() != "paid" || f.RemittanceStatus != FeeRemittanceStatus.Remitted) && f.StudentNumNavigation != null && !string.IsNullOrEmpty(f.StudentNumNavigation.YearLevelSection))
                .GroupBy(f => {
                    var section = f.StudentNumNavigation.YearLevelSection ?? "Unknown";
                    var parts = section.Split('-');
                    if (parts.Length >= 2)
                    {
                        var program = parts[0];
                        var yearWithSection = parts[1];
                        var year = new string(yearWithSection.TakeWhile(char.IsDigit).ToArray());
                        return $"{program}-{year}";
                    }
                    return "Unknown";
                })
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
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.StudentNumNavigation)
                .OrderByDescending(f => f.FineId)
                .ToListAsync();

            // Calculate statistics - ONLY count validated (remitted) fines (exclude waived from expected)
            var totalExpected = fines.Where(f => f.FinesStatus?.ToLower() != "waived").Sum(f => f.Amount ?? 0);
            var totalCollected = fines.Where(f => f.FinesStatus?.ToLower() == "paid" && f.RemittanceStatus == FeeRemittanceStatus.Remitted).Sum(f => f.Amount ?? 0);
            var totalPending = totalExpected - totalCollected;

            ViewBag.TotalExpected = totalExpected;
            ViewBag.TotalCollected = totalCollected;
            ViewBag.TotalPending = totalPending;

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

            // Chart Data - Paid by Program-Year (ONLY VALIDATED REMITTANCES)
            var paidBreakdown = fines
                .Where(f => f.FinesStatus?.ToLower() == "paid" && f.RemittanceStatus == FeeRemittanceStatus.Remitted && f.StudentNumNavigation != null && !string.IsNullOrEmpty(f.StudentNumNavigation.YearLevelSection))
                .GroupBy(f => {
                    var section = f.StudentNumNavigation.YearLevelSection ?? "Unknown";
                    var parts = section.Split('-');
                    if (parts.Length >= 2)
                    {
                        var program = parts[0];
                        var yearWithSection = parts[1];
                        var year = new string(yearWithSection.TakeWhile(char.IsDigit).ToArray());
                        return $"{program}-{year}";
                    }
                    return "Unknown";
                })
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount ?? 0));

            // Chart Data - Unpaid by Program-Year (includes unpaid + paid but not validated)
            var unpaidBreakdown = fines
                .Where(f => (f.FinesStatus?.ToLower() == "unpaid" || (f.FinesStatus?.ToLower() == "paid" && f.RemittanceStatus != FeeRemittanceStatus.Remitted)) && f.StudentNumNavigation != null && !string.IsNullOrEmpty(f.StudentNumNavigation.YearLevelSection))
                .GroupBy(f => {
                    var section = f.StudentNumNavigation.YearLevelSection ?? "Unknown";
                    var parts = section.Split('-');
                    if (parts.Length >= 2)
                    {
                        var program = parts[0];
                        var yearWithSection = parts[1];
                        var year = new string(yearWithSection.TakeWhile(char.IsDigit).ToArray());
                        return $"{program}-{year}";
                    }
                    return "Unknown";
                })
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount ?? 0));

            ViewBag.PaidBreakdown = paidBreakdown;
            ViewBag.UnpaidBreakdown = unpaidBreakdown;

            return View(fines);
        }

        // ============================================================
        // RECORD PARTIAL FEE PAYMENT (Class Treasurer)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Class Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordClassFeePayment(int feeId, decimal paymentAmount, string? paymentMethod, string? transactionRef, string? notes)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user.UserName);

                if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
                {
                    TempData["Error"] = "No section assigned to your account.";
                    return RedirectToAction("ClassFees");
                }

                var fee = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);

                if (fee == null)
                {
                    TempData["Error"] = $"Fee with ID {feeId} not found.";
                    return RedirectToAction("ClassFees");
                }

                // Verify the fee belongs to a student in the treasurer's section
                if (fee.StudentNumNavigation?.YearLevelSection != treasurer.YearLevelSection)
                {
                    TempData["Error"] = "You can only process payments for students in your section.";
                    return RedirectToAction("ClassFees");
                }

                var remainingBalance = (fee.Amount ?? 0) - fee.AmountPaid;
                if (paymentAmount <= 0)
                {
                    TempData["Error"] = "Payment amount must be greater than zero.";
                    return RedirectToAction("ClassFees");
                }

                if (paymentAmount > remainingBalance)
                {
                    TempData["Error"] = $"Payment amount (₱{paymentAmount:N2}) cannot exceed the remaining balance (₱{remainingBalance:N2}).";
                    return RedirectToAction("ClassFees");
                }

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
                    ProcessedBy = treasurer.StudentNum ?? "",
                    TransactionReference = string.IsNullOrWhiteSpace(transactionRef) ? null : transactionRef.Trim(),
                    Notes = string.IsNullOrWhiteSpace(notes) ? "Partial payment recorded by Class Treasurer" : notes.Trim()
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
                        SentBy = treasurer.StudentNum
                    });
                }

                await _context.SaveChangesAsync();

                var statusMsg = fee.FeeStatus == "Paid" ? "FULLY PAID" : $"Partial (Balance: ₱{(fee.Amount ?? 0) - fee.AmountPaid:N2})";
                TempData["Message"] = $"Payment of ₱{paymentAmount:N2} for {fee.StudentNumNavigation?.FullName ?? fee.StudentNum} recorded. Status: {statusMsg}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error recording payment: {ex.Message}";
            }

            return RedirectToAction("ClassFees");
        }

        // ============================================================
        // RECORD PARTIAL FINE PAYMENT (Class Treasurer)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Class Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordClassFinePayment(int fineId, decimal paymentAmount, string? paymentMethod, string? transactionRef, string? notes)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user.UserName);

                if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
                {
                    TempData["Error"] = "No section assigned to your account.";
                    return RedirectToAction("ClassFines");
                }

                var fine = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FineId == fineId);

                if (fine == null)
                {
                    TempData["Error"] = $"Fine with ID {fineId} not found.";
                    return RedirectToAction("ClassFines");
                }

                // Verify the fine belongs to a student in the treasurer's section
                if (fine.StudentNumNavigation?.YearLevelSection != treasurer.YearLevelSection)
                {
                    TempData["Error"] = "You can only process payments for students in your section.";
                    return RedirectToAction("ClassFines");
                }

                if (fine.FinesStatus?.ToLower() == "excused" || fine.FinesStatus?.ToLower() == "waived")
                {
                    TempData["Error"] = "Cannot record payment for an excused fine.";
                    return RedirectToAction("ClassFines");
                }

                var remainingBalance = (fine.Amount ?? 0) - fine.AmountPaid;
                if (paymentAmount <= 0)
                {
                    TempData["Error"] = "Payment amount must be greater than zero.";
                    return RedirectToAction("ClassFines");
                }

                if (paymentAmount > remainingBalance)
                {
                    TempData["Error"] = $"Payment amount (₱{paymentAmount:N2}) cannot exceed the remaining balance (₱{remainingBalance:N2}).";
                    return RedirectToAction("ClassFines");
                }

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
                    ProcessedBy = treasurer.StudentNum ?? "",
                    TransactionReference = string.IsNullOrWhiteSpace(transactionRef) ? null : transactionRef.Trim(),
                    Notes = string.IsNullOrWhiteSpace(notes) ? "Partial payment recorded by Class Treasurer" : notes.Trim()
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
                        SentBy = treasurer.StudentNum
                    });
                }

                await _context.SaveChangesAsync();

                var statusMsg = fine.FinesStatus == "Paid" ? "FULLY PAID" : $"Partial (Balance: ₱{(fine.Amount ?? 0) - fine.AmountPaid:N2})";
                TempData["Message"] = $"Payment of ₱{paymentAmount:N2} for {fine.StudentNumNavigation?.FullName ?? fine.StudentNum} recorded. Status: {statusMsg}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error recording payment: {ex.Message}";
            }

            return RedirectToAction("ClassFines");
        }

        // ============================================================
        // MARK CLASS FINE AS PAID - AJAX (Class Treasurer)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> MarkClassFinePaid(int fineId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user?.UserName);

                if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
                {
                    return Json(new { success = false, message = "No section assigned to your account." });
                }

                var fine = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .Include(f => f.Attendance)
                        .ThenInclude(a => a.StudentNumNavigation) // Ensure indirect student data is loaded
                    .Include(f => f.Attendance)
                        .ThenInclude(a => a.Event)
                    .FirstOrDefaultAsync(f => f.FineId == fineId);

                if (fine == null)
                {
                    return Json(new { success = false, message = "Fine not found." });
                }

                // ============================================================
                // FIX: Check for the student record via direct link OR through the attendance record.
                // This ensures event-based fines are correctly validated.
                // ============================================================
                var student = fine.StudentNumNavigation ?? fine.Attendance?.StudentNumNavigation;
                if (student?.YearLevelSection != treasurer.YearLevelSection)
                {
                    return Json(new { success = false, message = "You can only process payments for students in your section." });
                }

                // CATEGORY CLOSURE CHECK
                var fineCategory = fine.Description ?? fine.Attendance?.Event?.EventName ?? "Other";
                var validatedRemittance = await _context.Remittances
                    .FirstOrDefaultAsync(r => r.Section == treasurer.YearLevelSection
                        && (r.FineCategory == fineCategory)
                        && r.RemittanceType == RemittanceType.Fine
                        && r.Status == RemittanceStatus.Validated);

                if (validatedRemittance != null)
                {
                    return Json(new { success = false, message = $"This fine category '{fineCategory}' has been validated and is now locked." });
                }

                // Check if fine is already remitted (locked)
                if (fine.RemittanceStatus != FeeRemittanceStatus.NotRemitted)
                {
                    return Json(new { success = false, message = "Cannot modify: This fine has been remitted or is pending remittance." });
                }

                if (fine.FinesStatus?.ToLower() == "excused" || fine.FinesStatus?.ToLower() == "waived")
                {
                    return Json(new { success = false, message = "Cannot modify an excused fine." });
                }

                if (fine.FinesStatus?.ToUpper() == "PAID")
                {
                    return Json(new { success = false, message = "This fine is already marked as paid." });
                }

                // Mark as PAID
                fine.FinesStatus = "Paid";
                fine.AmountPaid = fine.Amount ?? 0;
                fine.CollectedBy = treasurer.StudentNum;
                fine.CollectionDate = DateTime.Now;

                _context.Fines.Update(fine);

                // ============================================================
                // FIX: Ensure StudentNum is correctly populated for event-based fines.
                // ============================================================
                var transaction = new FinePaymentTransaction
                {
                    FineId = fineId,
                    StudentNum = student?.StudentNum ?? "", // Use the student object found earlier
                    Amount = fine.Amount ?? 0,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "Cash",
                    ProcessedBy = treasurer.StudentNum ?? "",
                    Notes = "Full payment recorded by Class Treasurer"
                };
                _context.FinePaymentTransactions.Add(transaction);

                // Notify student
                if (student != null && !string.IsNullOrEmpty(student.StudentNum))
                {
                    _context.Notifications.Add(new Notification
                    {
                        StudentNum = student.StudentNum,
                        Title = "✅ Fine Payment Receipt",
                        Message = $"Your fine '{fine.Description ?? "Fine"}' has been PAID.\n\nAmount: ₱{fine.Amount:N2}\nCollected by: {treasurer.FullName}",
                        NotificationType = "Payment",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = treasurer.StudentNum
                    });
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"✅ Fine marked as PAID for {student?.FullName}."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error marking fine as paid: {ex.Message}" });
            }
        }


        // ============================================================
        // REVOKE CLASS FINE PAYMENT - AJAX (Class Treasurer)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> RevokeClassFinePaid(int fineId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user?.UserName);

                if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
                {
                    return Json(new { success = false, message = "No section assigned to your account." });
                }

                var fine = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .Include(f => f.Attendance) // ADDED: Include attendance for the check
                        .ThenInclude(a => a.StudentNumNavigation)
                    .Include(f => f.Attendance)
                        .ThenInclude(a => a.Event)
                    .FirstOrDefaultAsync(f => f.FineId == fineId);

                if (fine == null)
                {
                    return Json(new { success = false, message = "Fine not found." });
                }

                // ============================================================
                // FIX: Check for the student record via direct link OR through the attendance record.
                // ============================================================
                var student = fine.StudentNumNavigation ?? fine.Attendance?.StudentNumNavigation;
                if (student?.YearLevelSection != treasurer.YearLevelSection)
                {
                    return Json(new { success = false, message = "You can only process payments for students in your section." });
                }

                // CATEGORY CLOSURE CHECK
                var fineCategory = fine.Description ?? fine.Attendance?.Event?.EventName ?? "Other";
                var validatedRemittance = await _context.Remittances
                    .FirstOrDefaultAsync(r => r.Section == treasurer.YearLevelSection
                        && (r.FineCategory == fineCategory || r.FeeName == fineCategory)
                        && r.RemittanceType == RemittanceType.Fine
                        && r.Status == RemittanceStatus.Validated);

                if (validatedRemittance != null)
                {
                    return Json(new { success = false, message = $"This fine category '{fineCategory}' has been validated and is now locked. Only the Org Treasurer can update fines in this category. Batch: {validatedRemittance.BatchCode}" });
                }

                // Check if fine is already remitted (locked)
                if (fine.RemittanceStatus != FeeRemittanceStatus.NotRemitted)
                {
                    return Json(new { success = false, message = "Cannot revoke: This fine has been remitted or is pending remittance." });
                }

                if (fine.FinesStatus?.ToUpper() != "PAID")
                {
                    return Json(new { success = false, message = "This fine is not marked as paid." });
                }

                // Revoke payment
                fine.FinesStatus = "Unpaid";
                fine.AmountPaid = 0;
                fine.CollectedBy = null;
                fine.CollectionDate = null;

                _context.Fines.Update(fine);

                // Delete the payment transaction(s) for this fine to remove from payment history
                var transactions = await _context.FinePaymentTransactions
                    .Where(t => t.FineId == fineId)
                    .ToListAsync();

                if (transactions.Any())
                {
                    _context.FinePaymentTransactions.RemoveRange(transactions);
                }

                // Notify student with details
                if (!string.IsNullOrEmpty(student.StudentNum))
                {
                    _context.Notifications.Add(new Notification
                    {
                        StudentNum = student.StudentNum,
                        Title = "⚠️ Fine Payment Revoked",
                        Message = $"Your fine payment has been REVOKED.\n\n" +
                                  $"Fine: {fine.Description ?? "Fine"}\n" +
                                  $"Amount: ₱{fine.Amount:N2}\n" +
                                  $"Status: UNPAID\n" +
                                  $"Revoked by: {treasurer.FullName}\n" +
                                  $"Date: {DateTime.Now:MMM dd, yyyy hh:mm tt}\n\n" +
                                  $"Please contact your Class Treasurer for more details.",
                        NotificationType = "Payment",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = treasurer.StudentNum
                    });
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"✅ Payment revoked for {student?.FullName ?? fine.StudentNum}. Student has been notified."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error revoking payment: {ex.Message}" });
            }
        }

        // ============================================================
        // PENDING COLLECTIONS - Shows fees/fines ready for remittance (Class Treasurer)
        // ============================================================
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> PendingCollections(string? type)
        {
            var user = await _userManager.GetUserAsync(User);
            var treasurer = await _context.Students.FindAsync(user?.UserName);

            if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
            {
                TempData["Error"] = "No section assigned to your account.";
                return RedirectToAction("ClassTreasuryDashboard");
            }

            var section = treasurer.YearLevelSection;
            ViewBag.Section = section;
            ViewBag.Type = type ?? "fees"; // Default to fees

            if (type == "fines")
            {
                // Get fines ready for remittance (paid but not yet remitted)
                var pendingFines = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .Include(f => f.Attendance)
                        .ThenInclude(a => a.Event)
                    .Include(f => f.Attendance)
                        .ThenInclude(a => a.StudentNumNavigation)
                    .Where(f => (f.StudentNumNavigation != null || f.Attendance.StudentNumNavigation != null) && 
                                ((f.StudentNumNavigation != null && f.StudentNumNavigation.YearLevelSection == section) ||
                                 (f.Attendance != null && f.Attendance.StudentNumNavigation != null && f.Attendance.StudentNumNavigation.YearLevelSection == section)) &&
                                f.FinesStatus == "Paid" &&
                                f.RemittanceStatus == FeeRemittanceStatus.NotRemitted)
                    .OrderBy(f => f.Description)
                    .ThenBy(f => f.StudentNumNavigation != null ? f.StudentNumNavigation.StudentLn : f.Attendance.StudentNumNavigation.StudentLn)
                    .ToListAsync();

                // Group by description/category
                var fineCategories = pendingFines
                    .GroupBy(f => f.Description ?? "Other")
                    .Select(g => g.Key)
                    .OrderBy(c => c)
                    .ToList();

                ViewBag.FineNames = fineCategories;
                ViewBag.TotalPendingAmount = pendingFines.Sum(f => f.Amount ?? 0);
                ViewBag.TotalPendingCount = pendingFines.Count;

                return View(pendingFines);
            }
            else
            {
                // Get fees ready for remittance (paid but not yet remitted)
                var pendingFees = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .Where(f => f.StudentNumNavigation != null &&
                                f.StudentNumNavigation.YearLevelSection == section &&
                                f.FeeStatus == "Paid" &&
                                f.RemittanceStatus == FeeRemittanceStatus.NotRemitted)
                    .OrderBy(f => f.FeeName)
                    .ThenBy(f => f.StudentNumNavigation.StudentLn)
                    .ToListAsync();

                // Get unique fee names
                var feeNames = pendingFees.Select(f => f.FeeName).Distinct().OrderBy(n => n).ToList();

                ViewBag.FeeNames = feeNames;
                ViewBag.TotalPendingAmount = pendingFees.Sum(f => f.Amount ?? 0);
                ViewBag.TotalPendingCount = pendingFees.Count;

                return View(pendingFees);
            }
        }

        // ============================================================
        // INITIATE REMITTANCE - Review before submitting (Class Treasurer)
        // ============================================================
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> InitiateRemittance(string feeName, string? type)
        {
            var user = await _userManager.GetUserAsync(User);
            var treasurer = await _context.Students.FindAsync(user?.UserName);

            if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
            {
                TempData["Error"] = "No section assigned to your account.";
                return RedirectToAction("ClassTreasuryDashboard");
            }

            if (string.IsNullOrWhiteSpace(feeName))
            {
                TempData["Error"] = "Fee/Fine name is required.";
                return RedirectToAction("PendingCollections", new { type });
            }

            var section = treasurer.YearLevelSection;
            ViewBag.Section = section;
            ViewBag.FeeName = feeName;
            ViewBag.TreasurerName = treasurer.FullName;
            ViewBag.Type = type ?? "fee";

            if (type == "fine")
            {
                // Get fines for this category (match by Description for manual fines, or Event Name for event fines)
                var fines = await _context.Fines
                    .Include(f => f.StudentNumNavigation)
                    .Include(f => f.Attendance)
                        .ThenInclude(a => a.Event)
                    .Include(f => f.Attendance)
                        .ThenInclude(a => a.StudentNumNavigation)
                    .Where(f => (f.StudentNumNavigation != null || f.Attendance.StudentNumNavigation != null) &&
                                ((f.StudentNumNavigation != null && f.StudentNumNavigation.YearLevelSection == section) ||
                                 (f.Attendance != null && f.Attendance.StudentNumNavigation != null && f.Attendance.StudentNumNavigation.YearLevelSection == section)) &&
                                (f.Description == feeName || (f.Attendance != null && f.Attendance.Event != null && f.Attendance.Event.EventName == feeName)) &&
                                f.FinesStatus == "Paid" &&
                                f.RemittanceStatus == FeeRemittanceStatus.NotRemitted)
                    .OrderBy(f => f.StudentNumNavigation != null ? f.StudentNumNavigation.StudentLn : f.Attendance.StudentNumNavigation.StudentLn)
                    .ToListAsync();

                if (!fines.Any())
                {
                    TempData["Warning"] = "No fines found ready for remittance.";
                    return RedirectToAction("PendingCollections", new { type = "fines" });
                }

                ViewBag.TotalAmount = fines.Sum(f => f.Amount ?? 0);
                ViewBag.TotalStudents = fines.Count;

                return View(fines);
            }
            else
            {
                // Get fees for this category
                var fees = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .Where(f => f.StudentNumNavigation != null &&
                                f.StudentNumNavigation.YearLevelSection == section &&
                                f.FeeName == feeName &&
                                f.FeeStatus == "Paid" &&
                                f.RemittanceStatus == FeeRemittanceStatus.NotRemitted)
                    .OrderBy(f => f.StudentNumNavigation.StudentLn)
                    .ToListAsync();

                if (!fees.Any())
                {
                    TempData["Warning"] = "No fees found ready for remittance.";
                    return RedirectToAction("PendingCollections");
                }

                ViewBag.TotalAmount = fees.Sum(f => f.Amount ?? 0);
                ViewBag.TotalStudents = fees.Count;

                return View(fees);
            }
        }

        // ============================================================
        // CONFIRM REMITTANCE - Submit to Org Treasurer (Class Treasurer)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Class Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRemittance(string feeName, string? type)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user?.UserName);

                if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
                {
                    TempData["Error"] = "No section assigned to your account.";
                    return RedirectToAction("ClassTreasuryDashboard");
                }

                if (string.IsNullOrWhiteSpace(feeName))
                {
                    TempData["Error"] = "Fee/Fine name is required.";
                    return RedirectToAction("PendingCollections", new { type });
                }

                var section = treasurer.YearLevelSection;
                var isFinetype = type == "fine";

                if (isFinetype)
                {
                    // Get fines for this category (match by Description for manual fines, or Event Name for event fines)
                    var fines = await _context.Fines
                        .Include(f => f.StudentNumNavigation)
                        .Include(f => f.Attendance)
                            .ThenInclude(a => a.Event)
                        .Include(f => f.Attendance)
                            .ThenInclude(a => a.StudentNumNavigation)
                        .Where(f => (f.StudentNumNavigation != null || f.Attendance.StudentNumNavigation != null) &&
                                    ((f.StudentNumNavigation != null && f.StudentNumNavigation.YearLevelSection == section) ||
                                     (f.Attendance != null && f.Attendance.StudentNumNavigation != null && f.Attendance.StudentNumNavigation.YearLevelSection == section)) &&
                                    (f.Description == feeName || (f.Attendance != null && f.Attendance.Event != null && f.Attendance.Event.EventName == feeName)) &&
                                    f.FinesStatus == "Paid" &&
                                    f.RemittanceStatus == FeeRemittanceStatus.NotRemitted)
                        .ToListAsync();

                    if (!fines.Any())
                    {
                        TempData["Warning"] = "No fines found to remit.";
                        return RedirectToAction("PendingCollections", new { type = "fines" });
                    }

                    // Get academic year
                    var acadYear = await _context.SystemSettings
                        .Where(s => s.SettingKey == "CurrentAcademicYear")
                        .Select(s => s.SettingValue)
                        .FirstOrDefaultAsync() ?? $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}";

                    // Generate batch code
                    var batchCode = await GenerateRemittanceBatchCode();

                    // Create remittance record
                    var remittance = new Remittance
                    {
                        BatchCode = batchCode,
                        FineCategory = feeName,
                        RemittanceType = RemittanceType.Fine,
                        Section = section,
                        TotalAmount = fines.Sum(f => f.Amount ?? 0),
                        TotalStudents = fines.Count,
                        SubmittedBy = treasurer.StudentNum,
                        SubmittedDate = DateTime.Now,
                        Status = RemittanceStatus.Pending,
                        AcademicYear = acadYear
                    };

                    _context.Remittances.Add(remittance);
                    await _context.SaveChangesAsync(); // Save to get RemittanceId

                    // Create remittance items and update fine statuses
                    foreach (var fine in fines)
                    {
                        // Create remittance item
                        // Get StudentNum from either direct reference (manual fines) or Attendance (event fines)
                        var studentNum = fine.StudentNum ?? fine.Attendance?.StudentNum ?? string.Empty;
                        var studentName = fine.StudentNumNavigation?.FullName ?? fine.Attendance?.StudentNumNavigation?.FullName;
                        
                        var item = new RemittanceItem
                        {
                            RemittanceId = remittance.RemittanceId,
                            FineId = fine.FineId,
                            StudentNum = studentNum,
                            StudentName = studentName,
                            Amount = fine.Amount ?? 0,
                            CollectionDate = fine.CollectionDate ?? DateTime.Now,
                            PaymentMethod = "Cash" // Default, could be enhanced
                        };
                        _context.RemittanceItems.Add(item);

                        // Update fine - mark as pending remittance (LOCKED)
                        fine.RemittanceStatus = FeeRemittanceStatus.PendingRemittance;
                        fine.RemittanceId = remittance.RemittanceId;
                        _context.Fines.Update(fine);
                    }

                    await _context.SaveChangesAsync();

                    TempData["Message"] = $"Remittance {batchCode} submitted successfully! Total: ₱{remittance.TotalAmount:N2} from {remittance.TotalStudents} students. Awaiting Org Treasurer validation.";
                    return RedirectToAction("RemittanceHistory");
                }
                else
                {
                    // Get fees for this category
                    var fees = await _context.Fees
                        .Include(f => f.StudentNumNavigation)
                        .Where(f => f.StudentNumNavigation != null &&
                                    f.StudentNumNavigation.YearLevelSection == section &&
                                    f.FeeName == feeName &&
                                    f.FeeStatus == "Paid" &&
                                    f.RemittanceStatus == FeeRemittanceStatus.NotRemitted)
                        .ToListAsync();

                    if (!fees.Any())
                    {
                        TempData["Warning"] = "No fees found to remit.";
                        return RedirectToAction("PendingCollections");
                    }

                    // Mark all fees as pending remittance
                    foreach (var fee in fees)
                    {
                        fee.RemittanceStatus = FeeRemittanceStatus.PendingRemittance;
                    }

                    // Create remittance record
                    var remittance = new Remittance
                    {
                        BatchCode = $"RMT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
                        Section = section,
                        FeeName = feeName,
                        TotalAmount = fees.Sum(f => f.Amount ?? 0),
                        TotalStudents = fees.Count,
                        SubmittedBy = treasurer.StudentNum,
                        SubmittedDate = DateTime.Now,
                        Status = RemittanceStatus.Pending,
                        RemittanceType = RemittanceType.Fee
                    };

                    _context.Remittances.Add(remittance);
                    await _context.SaveChangesAsync();

                    TempData["Message"] = $"✅ Remittance submitted successfully! {fees.Count} payment(s) for '{feeName}' (₱{remittance.TotalAmount:N2}) sent to Org Treasurer for validation.";
                    return RedirectToAction("ClassFees");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error submitting remittance: {ex.Message}";
                return RedirectToAction("PendingCollections", new { type });
            }
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

            // Get validated remittances for this section to determine which categories are closed
            var validatedCategories = await _context.Remittances
                .Where(r => r.Section == section 
                    && r.RemittanceType == RemittanceType.Fee
                    && r.Status == RemittanceStatus.Validated)
                .Select(r => r.FeeName)
                .ToListAsync();

            ViewBag.ValidatedCategories = validatedCategories;

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
        // FINAL FIX: Robust query to handle both direct and indirect student links.
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

            // ============================================================
            // FIX: This robust query checks for a matching section via TWO paths:
            // 1. The direct link: Fine -> Student -> Section (for all new/fixed fines)
            // 2. The indirect link: Fine -> Attendance -> Student -> Section (as a fallback)
            // This ensures all relevant fines for the section are displayed.
            // ============================================================
            var fines = await _context.Fines
                .Include(f => f.StudentNumNavigation)
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.Event)
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.StudentNumNavigation)
                .Where(f =>
                    (f.StudentNumNavigation != null && f.StudentNumNavigation.YearLevelSection == section) ||
                    (f.Attendance.StudentNumNavigation != null && f.Attendance.StudentNumNavigation.YearLevelSection == section)
                )
                .OrderByDescending(f => f.FineId)
                .ToListAsync();

            // Get validated categories
            var validatedCategories = await _context.Remittances
                .Where(r => r.Section == section
                    && r.RemittanceType == RemittanceType.Fine
                    && r.Status == RemittanceStatus.Validated)
                .Select(r => r.FineCategory)
                .ToListAsync();

            ViewBag.ValidatedCategories = validatedCategories;

            // Calculate statistics
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

            // Get unique manual fine reasons
            ViewBag.ManualFineReasons = fines
                .Where(f => f.AttendanceId == null && !string.IsNullOrEmpty(f.Description))
                .Select(f => f.Description)
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            // Chart Data
            var paidBreakdown = fines
                .Where(f => f.FinesStatus?.ToLower() == "paid")
                .GroupBy(f => f.Description ?? f.Attendance?.Event?.EventName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Sum(f => f.Amount ?? 0));

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
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.StudentNumNavigation)
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

        // ============================================================
        // AJAX: TOGGLE FEE PAYMENT (Class/Org Treasurer)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Class Treasurer, Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeePayment(int feeId, bool markAsPaid, string? returnUrl)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user.UserName);

                if (treasurer == null)
                {
                    return Json(new { success = false, message = "Treasurer profile not found." });
                }

                var fee = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);

                if (fee == null)
                {
                    return Json(new { success = false, message = "Fee record not found." });
                }

                // Security: Class Treasurer can only manage their section
                bool isOrgTreasurer = User.IsInRole("Org Treasurer");
                if (!isOrgTreasurer && User.IsInRole("Class Treasurer"))
                {
                    if (fee.StudentNumNavigation?.YearLevelSection != treasurer.YearLevelSection)
                    {
                        return Json(new { success = false, message = "Unauthorized: Student belongs to a different section." });
                    }
                }

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
                        ProcessedBy = treasurer.StudentNum ?? "",
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
                            Message = $"Your payment of ₱{remainingBalance:N2} for '{fee.FeeName}' has been confirmed by {treasurer.FullName}.",
                            NotificationType = "Payment",
                            NotificationDate = DateTime.Now,
                            IsRead = false,
                            SentBy = treasurer.StudentNum
                        });
                    }

                    await _context.SaveChangesAsync();

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
                    // Officers cannot revoke payments - only Admin can
                    return Json(new { success = false, message = "Officers cannot revoke payments. Please contact an administrator." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ============================================================
        // AJAX: TOGGLE FINE PAYMENT (Class/Org Treasurer)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Class Treasurer, Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFinePayment(int fineId, bool markAsPaid, string? returnUrl)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user.UserName);

                if (treasurer == null)
                {
                    return Json(new { success = false, message = "Treasurer profile not found." });
                }

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

                // Security: Class Treasurer can only manage their section
                bool isOrgTreasurer = User.IsInRole("Org Treasurer");
                if (!isOrgTreasurer && User.IsInRole("Class Treasurer"))
                {
                    if (fine.StudentNumNavigation?.YearLevelSection != treasurer.YearLevelSection)
                    {
                        return Json(new { success = false, message = "Unauthorized: Student belongs to a different section." });
                    }
                }

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
                        ProcessedBy = treasurer.StudentNum ?? "",
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
                            Message = $"Your payment of ₱{remainingBalance:N2} for '{fine.Description ?? "Fine"}' has been confirmed by {treasurer.FullName}.",
                            NotificationType = "Payment",
                            NotificationDate = DateTime.Now,
                            IsRead = false,
                            SentBy = treasurer.StudentNum
                        });
                    }

                    await _context.SaveChangesAsync();

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
                    // Officers cannot revoke payments - only Admin can
                    return Json(new { success = false, message = "Officers cannot revoke payments. Please contact an administrator." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ============================================================
        // AJAX: MARK FINE AS EXCUSED (Org Treasurer Only)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFineExcusedAjax(int fineId, string? reason)
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
                    return Json(new { success = false, message = "Fine record not found." });
                }

                // Only event fines can be excused
                if (fine.AttendanceId == null)
                {
                    return Json(new { success = false, message = "Only event-based fines can be excused." });
                }

                if (fine.FinesStatus?.ToLower() == "paid")
                {
                    return Json(new { success = false, message = "Cannot excuse a paid fine." });
                }

                fine.FinesStatus = "Excused";
                fine.AmountPaid = 0;
                _context.Fines.Update(fine);

                // Delete any partial payments
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
                        Message = $"Your fine for '{fine.Description ?? "Fine"}' (₱{fine.Amount:N2}) has been excused by {treasurer?.FullName ?? "Org Treasurer"}. Reason: {reason ?? "Not specified"}",
                        NotificationType = "Fine",
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        SentBy = treasurer?.StudentNum
                    });
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Fine has been marked as excused.",
                    newStatus = "Excused"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ============================================================
        // ============================================================
        //          REMITTANCE SYSTEM - CLASS TREASURER METHODS
        // ============================================================
        // ============================================================

        #region Class Treasurer - Remittance Methods

        /// <summary>
        /// Helper: Generate unique batch code for remittance
        /// </summary>
        private async Task<string> GenerateRemittanceBatchCode()
        {
            var year = DateTime.Now.Year.ToString();
            var lastBatch = await _context.Remittances
                .Where(r => r.BatchCode.StartsWith($"RMT-{year}-"))
                .OrderByDescending(r => r.BatchCode)
                .FirstOrDefaultAsync();

            int nextNum = 1;
            if (lastBatch != null)
            {
                var lastNumStr = lastBatch.BatchCode.Split('-').LastOrDefault();
                if (int.TryParse(lastNumStr, out int lastNum))
                {
                    nextNum = lastNum + 1;
                }
            }

            return $"RMT-{year}-{nextNum:D4}";
        }

        /// <summary>
        /// CLASS TREASURER: Mark fee as paid (with remittance tracking)
        /// Only works if fee is NOT yet remitted
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Class Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CollectFeePayment(int feeId, string? paymentMethod, string? transactionRef, string? notes)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Json(new { success = false, message = "Session expired." });

                var treasurer = await _context.Students.FindAsync(user.UserName);
                if (treasurer == null) return Json(new { success = false, message = "Treasurer not found." });

                var fee = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);

                if (fee == null) return Json(new { success = false, message = "Fee not found." });

                // Security: Section check
                if (fee.StudentNumNavigation?.YearLevelSection != treasurer.YearLevelSection)
                {
                    return Json(new { success = false, message = "Unauthorized: Student belongs to a different section." });
                }

                // Check if already remitted (locked)
                if (fee.RemittanceStatus != FeeRemittanceStatus.NotRemitted)
                {
                    return Json(new { success = false, message = "This fee has already been remitted and cannot be modified." });
                }

                // Check if already paid
                if (fee.FeeStatus?.ToUpper() == "PAID")
                {
                    return Json(new { success = false, message = "This fee is already marked as paid." });
                }

                // Mark as paid with collection tracking
                fee.FeeStatus = "Paid";
                fee.AmountPaid = fee.Amount ?? 0;
                fee.CollectedBy = treasurer.StudentNum;
                fee.CollectionDate = DateTime.Now;
                // RemittanceStatus stays "NotRemitted" until Class Treasurer initiates remittance

                _context.Fees.Update(fee);

                // Create transaction record
                var transaction = new PaymentTransaction
                {
                    FeeId = feeId,
                    StudentNum = fee.StudentNum,
                    Amount = fee.Amount ?? 0,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod.Trim(),
                    ProcessedBy = treasurer.StudentNum,
                    TransactionReference = transactionRef?.Trim(),
                    Notes = notes?.Trim()
                };
                _context.PaymentTransactions.Add(transaction);

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Payment collected from {fee.StudentNumNavigation?.FullName}.",
                    studentName = fee.StudentNumNavigation?.FullName,
                    amount = fee.Amount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// CLASS TREASURER: Unmark fee as paid (revoke collection)
        /// Only works if fee is NOT yet remitted
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Class Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeFeesCollection(int feeId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user?.UserName);
                if (treasurer == null) return Json(new { success = false, message = "Treasurer not found." });

                var fee = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);

                if (fee == null) return Json(new { success = false, message = "Fee not found." });

                // Security: Section check
                if (fee.StudentNumNavigation?.YearLevelSection != treasurer.YearLevelSection)
                {
                    return Json(new { success = false, message = "Unauthorized." });
                }

                // Check if already remitted (locked)
                if (fee.RemittanceStatus != FeeRemittanceStatus.NotRemitted)
                {
                    return Json(new { success = false, message = "Cannot revoke: This fee has already been remitted." });
                }

                // Revoke payment
                fee.FeeStatus = "Unpaid";
                fee.AmountPaid = 0;
                fee.CollectedBy = null;
                fee.CollectionDate = null;

                _context.Fees.Update(fee);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Payment revoked successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// CLASS TREASURER: Get pending collections ready for remittance
        /// Shows all paid fees that haven't been remitted yet
        /// </summary>

        /// <summary>
        /// CLASS TREASURER: View remittance history
        /// </summary>
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> RemittanceHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            var treasurer = await _context.Students.FindAsync(user?.UserName);

            if (treasurer == null || string.IsNullOrEmpty(treasurer.YearLevelSection))
            {
                TempData["Error"] = "No section assigned.";
                return RedirectToAction("ClassTreasuryDashboard");
            }

            var remittances = await _context.Remittances
                .Include(r => r.ValidatedByNavigation)
                .Where(r => r.Section == treasurer.YearLevelSection)
                .OrderByDescending(r => r.SubmittedDate)
                .ToListAsync();

            ViewBag.Section = treasurer.YearLevelSection;

            return View(remittances);
        }

        /// <summary>
        /// CLASS TREASURER: View details of a specific remittance
        /// </summary>
        [Authorize(Roles = "Class Treasurer, Org Treasurer")]
        public async Task<IActionResult> RemittanceDetails(int id)
        {
            var remittance = await _context.Remittances
                .Include(r => r.SubmittedByNavigation)
                .Include(r => r.ValidatedByNavigation)
                .Include(r => r.RemittanceItems)
                    .ThenInclude(i => i.Student)
                .FirstOrDefaultAsync(r => r.RemittanceId == id);

            if (remittance == null)
            {
                TempData["Error"] = "Remittance not found.";
                return RedirectToAction("RemittanceHistory");
            }

            // Security check for Class Treasurer
            if (User.IsInRole("Class Treasurer") && !User.IsInRole("Org Treasurer"))
            {
                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user?.UserName);
                if (treasurer?.YearLevelSection != remittance.Section)
                {
                    TempData["Error"] = "Unauthorized access.";
                    return RedirectToAction("RemittanceHistory");
                }
            }

            return View(remittance);
        }

        #endregion



        // ============================================================
        // ============================================================
        //          REMITTANCE SYSTEM - ORG TREASURER METHODS
        // ============================================================
        // ============================================================

        #region Org Treasurer - Remittance Methods

        /// <summary>
        /// ORG TREASURER: View all pending remittances from all sections
        /// </summary>
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> PendingRemittances()
        {
            var pendingRemittances = await _context.Remittances
                .Include(r => r.SubmittedByNavigation)
                .Where(r => r.Status == RemittanceStatus.Pending)
                .OrderBy(r => r.SubmittedDate)
                .ToListAsync();

            // Group by section for better organization
            var groupedBySection = pendingRemittances
                .GroupBy(r => r.Section)
                .OrderBy(g => g.Key)
                .ToList();

            ViewBag.TotalPendingAmount = pendingRemittances.Sum(r => r.TotalAmount);
            ViewBag.TotalPendingCount = pendingRemittances.Count;
            ViewBag.GroupedRemittances = groupedBySection;

            return View(pendingRemittances);
        }

        /// <summary>
        /// ORG TREASURER: Validate (Accept) a remittance
        /// Sets the official payment date and syncs to all student records
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateRemittance(int remittanceId, string? validationNotes)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var orgTreasurer = await _context.Students.FindAsync(user?.UserName);

                if (orgTreasurer == null)
                {
                    TempData["Error"] = "Org Treasurer profile not found.";
                    return RedirectToAction("PendingRemittances");
                }

                var remittance = await _context.Remittances
                    .Include(r => r.RemittanceItems)
                    .FirstOrDefaultAsync(r => r.RemittanceId == remittanceId);

                if (remittance == null)
                {
                    TempData["Error"] = "Remittance not found.";
                    return RedirectToAction("PendingRemittances");
                }

                if (remittance.Status != RemittanceStatus.Pending)
                {
                    TempData["Warning"] = "This remittance has already been processed.";
                    return RedirectToAction("PendingRemittances");
                }

                // THE OFFICIAL VALIDATION DATE IS NOW
                var validationDate = DateTime.Now;

                // Update remittance status
                remittance.Status = RemittanceStatus.Validated;
                remittance.ValidatedBy = orgTreasurer.StudentNum;
                remittance.ValidationDate = validationDate;
                remittance.ValidationNotes = validationNotes?.Trim();
                remittance.UpdatedAt = DateTime.Now;

                _context.Remittances.Update(remittance);

                // Update all associated fees - set official payment date and mark as Remitted
                if (remittance.RemittanceType == RemittanceType.Fee)
                {
                    var feeIds = remittance.RemittanceItems.Where(i => i.FeeId.HasValue).Select(i => i.FeeId.Value).ToList();
                    var fees = await _context.Fees
                        .Include(f => f.StudentNumNavigation)
                        .Where(f => feeIds.Contains(f.FeeId))
                        .ToListAsync();

                    foreach (var fee in fees)
                    {
                        fee.RemittanceStatus = FeeRemittanceStatus.Remitted;
                        fee.OfficialPaymentDate = validationDate; // THE OFFICIAL DATE
                        _context.Fees.Update(fee);

                        // Notify student
                        if (!string.IsNullOrEmpty(fee.StudentNum))
                        {
                            _context.Notifications.Add(new Notification
                            {
                                StudentNum = fee.StudentNum,
                                Title = "Payment Officially Validated",
                                Message = $"Your payment of ₱{fee.Amount:N2} for '{fee.FeeName}' has been officially validated by the Organization Treasurer on {validationDate:MMMM dd, yyyy}.",
                                NotificationType = "Payment",
                                NotificationDate = DateTime.Now,
                                IsRead = false,
                                SentBy = orgTreasurer.StudentNum
                            });
                        }
                    }
                }
                else if (remittance.RemittanceType == RemittanceType.Fine)
                {
                    var fineIds = remittance.RemittanceItems.Where(i => i.FineId.HasValue).Select(i => i.FineId.Value).ToList();
                    var fines = await _context.Fines
                        .Include(f => f.StudentNumNavigation)
                        .Where(f => fineIds.Contains(f.FineId))
                        .ToListAsync();

                    foreach (var fine in fines)
                    {
                        fine.RemittanceStatus = FeeRemittanceStatus.Remitted;
                        fine.OfficialPaymentDate = validationDate;
                        _context.Fines.Update(fine);

                        // Notify student
                        if (!string.IsNullOrEmpty(fine.StudentNum))
                        {
                            _context.Notifications.Add(new Notification
                            {
                                StudentNum = fine.StudentNum,
                                Title = "Fine Payment Officially Validated",
                                Message = $"Your fine payment of ₱{fine.Amount:N2} for '{fine.Description}' has been officially validated.",
                                NotificationType = "Payment",
                                NotificationDate = DateTime.Now,
                                IsRead = false,
                                SentBy = orgTreasurer.StudentNum
                            });
                        }
                    }
                }

                // Notify the Class Treasurer
                _context.Notifications.Add(new Notification
                {
                    StudentNum = remittance.SubmittedBy,
                    Title = "Remittance Validated",
                    Message = $"Your remittance {remittance.BatchCode} for '{remittance.CategoryName}' (₱{remittance.TotalAmount:N2}) has been validated by {orgTreasurer.FullName}.",
                    NotificationType = "Remittance",
                    NotificationDate = DateTime.Now,
                    IsRead = false,
                    SentBy = orgTreasurer.StudentNum
                });

                await _context.SaveChangesAsync();

                TempData["Message"] = $"Remittance {remittance.BatchCode} validated successfully! ₱{remittance.TotalAmount:N2} from {remittance.Section}.";
                return RedirectToAction("PendingRemittances");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error validating remittance: {ex.Message}";
                return RedirectToAction("PendingRemittances");
            }
        }

        /// <summary>
        /// ORG TREASURER: Reject a remittance
        /// Returns control back to Class Treasurer for corrections
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRemittance(int remittanceId, string rejectionReason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    TempData["Error"] = "Please provide a reason for rejection.";
                    return RedirectToAction("RemittanceDetails", new { id = remittanceId });
                }

                var user = await _userManager.GetUserAsync(User);
                var orgTreasurer = await _context.Students.FindAsync(user?.UserName);

                var remittance = await _context.Remittances
                    .Include(r => r.RemittanceItems)
                    .FirstOrDefaultAsync(r => r.RemittanceId == remittanceId);

                if (remittance == null)
                {
                    TempData["Error"] = "Remittance not found.";
                    return RedirectToAction("PendingRemittances");
                }

                if (remittance.Status != RemittanceStatus.Pending)
                {
                    TempData["Warning"] = "This remittance has already been processed.";
                    return RedirectToAction("PendingRemittances");
                }

                // Update remittance status to Rejected
                remittance.Status = RemittanceStatus.Rejected;
                remittance.ValidatedBy = orgTreasurer?.StudentNum;
                remittance.ValidationDate = DateTime.Now;
                remittance.RejectionReason = rejectionReason.Trim();
                remittance.UpdatedAt = DateTime.Now;

                _context.Remittances.Update(remittance);

                // Unlock the fees - return them to NotRemitted so Class Treasurer can edit
                if (remittance.RemittanceType == RemittanceType.Fee)
                {
                    var feeIds = remittance.RemittanceItems.Where(i => i.FeeId.HasValue).Select(i => i.FeeId.Value).ToList();
                    var fees = await _context.Fees.Where(f => feeIds.Contains(f.FeeId)).ToListAsync();

                    foreach (var fee in fees)
                    {
                        fee.RemittanceStatus = FeeRemittanceStatus.NotRemitted;
                        fee.RemittanceId = null;
                        _context.Fees.Update(fee);
                    }
                }
                else if (remittance.RemittanceType == RemittanceType.Fine)
                {
                    var fineIds = remittance.RemittanceItems.Where(i => i.FineId.HasValue).Select(i => i.FineId.Value).ToList();
                    var fines = await _context.Fines.Where(f => fineIds.Contains(f.FineId)).ToListAsync();

                    foreach (var fine in fines)
                    {
                        fine.RemittanceStatus = FeeRemittanceStatus.NotRemitted;
                        fine.RemittanceId = null;
                        _context.Fines.Update(fine);
                    }
                }

                // Notify the Class Treasurer
                _context.Notifications.Add(new Notification
                {
                    StudentNum = remittance.SubmittedBy,
                    Title = "Remittance Rejected",
                    Message = $"Your remittance {remittance.BatchCode} for '{remittance.CategoryName}' has been rejected. Reason: {rejectionReason}. Please review and re-submit.",
                    NotificationType = "Remittance",
                    NotificationDate = DateTime.Now,
                    IsRead = false,
                    SentBy = orgTreasurer?.StudentNum
                });

                await _context.SaveChangesAsync();

                TempData["Message"] = $"Remittance {remittance.BatchCode} has been rejected. The Class Treasurer has been notified.";
                return RedirectToAction("PendingRemittances");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error rejecting remittance: {ex.Message}";
                return RedirectToAction("PendingRemittances");
            }
        }

        /// <summary>
        /// ORG TREASURER: View all validated remittances (history)
        /// </summary>
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> ValidatedRemittances(string? section, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Remittances
                .Include(r => r.SubmittedByNavigation)
                .Include(r => r.ValidatedByNavigation)
                .Where(r => r.Status == RemittanceStatus.Validated)
                .AsQueryable();

            if (!string.IsNullOrEmpty(section))
            {
                query = query.Where(r => r.Section == section);
            }

            if (startDate.HasValue)
            {
                query = query.Where(r => r.ValidationDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.ValidationDate <= endDate.Value.AddDays(1));
            }

            var remittances = await query.OrderByDescending(r => r.ValidationDate).ToListAsync();

            ViewBag.Sections = await _context.Remittances.Select(r => r.Section).Distinct().OrderBy(s => s).ToListAsync();
            ViewBag.TotalValidatedAmount = remittances.Sum(r => r.TotalAmount);

            return View(remittances);
        }

        /// <summary>
        /// ORG TREASURER: Edit unpaid fee records (Administrative Override)
        /// Can ONLY edit UNPAID and NOT REMITTED records
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminEditFee(int feeId, string newStatus)
        {
            try
            {
                var fee = await _context.Fees
                    .Include(f => f.StudentNumNavigation)
                    .FirstOrDefaultAsync(f => f.FeeId == feeId);

                if (fee == null)
                {
                    return Json(new { success = false, message = "Fee not found." });
                }

                // CRITICAL: Cannot edit PAID or REMITTED records
                if (fee.FeeStatus?.ToUpper() == "PAID")
                {
                    return Json(new { success = false, message = "Cannot edit: This fee is already marked as PAID. Only UNPAID records can be modified." });
                }

                if (fee.RemittanceStatus == FeeRemittanceStatus.Remitted || 
                    fee.RemittanceStatus == FeeRemittanceStatus.PendingRemittance)
                {
                    return Json(new { success = false, message = "Cannot edit: This fee has been remitted or is pending remittance." });
                }

                // Allow status update for unpaid records
                var user = await _userManager.GetUserAsync(User);
                var orgTreasurer = await _context.Students.FindAsync(user?.UserName);

                if (newStatus == "Paid")
                {
                    fee.FeeStatus = "Paid";
                    fee.AmountPaid = fee.Amount ?? 0;
                    fee.CollectedBy = orgTreasurer?.StudentNum;
                    fee.CollectionDate = DateTime.Now;
                    fee.OfficialPaymentDate = DateTime.Now; // Direct payment by Org Treasurer
                    fee.RemittanceStatus = FeeRemittanceStatus.Remitted; // Mark as remitted directly

                    // Create transaction
                    _context.PaymentTransactions.Add(new PaymentTransaction
                    {
                        FeeId = feeId,
                        StudentNum = fee.StudentNum,
                        Amount = fee.Amount ?? 0,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "Admin Override",
                        ProcessedBy = orgTreasurer?.StudentNum ?? "",
                        Notes = "Payment recorded directly by Org Treasurer (Admin Override)"
                    });

                    // Notify student
                    if (!string.IsNullOrEmpty(fee.StudentNum))
                    {
                        _context.Notifications.Add(new Notification
                        {
                            StudentNum = fee.StudentNum,
                            Title = "Fee Payment Recorded",
                            Message = $"Your fee '{fee.FeeName}' (₱{fee.Amount:N2}) has been marked as paid by the Organization Treasurer.",
                            NotificationType = "Payment",
                            NotificationDate = DateTime.Now,
                            IsRead = false,
                            SentBy = orgTreasurer?.StudentNum
                        });
                    }
                }

                _context.Fees.Update(fee);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Fee updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// ORG TREASURER DASHBOARD - Updated with remittance stats
        /// </summary>
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> OrgTreasurerDashboardWithRemittance()
        {
            // Get all fees and fines (only those with valid sections for program-year grouping)
            var fees = await _context.Fees
                .Include(f => f.StudentNumNavigation)
                .Where(f => f.StudentNumNavigation != null && !string.IsNullOrEmpty(f.StudentNumNavigation.YearLevelSection))
                .ToListAsync();
            
            var fines = await _context.Fines
                .Include(f => f.StudentNumNavigation)
                .Where(f => f.StudentNumNavigation != null && !string.IsNullOrEmpty(f.StudentNumNavigation.YearLevelSection))
                .ToListAsync();

            // Group by Program-Year first, then calculate statistics
            var feesByProgramYear = fees
                .GroupBy(f => {
                    var section = f.StudentNumNavigation.YearLevelSection ?? "Unknown";
                    var parts = section.Split('-');
                    if (parts.Length >= 2)
                    {
                        var program = parts[0];
                        var yearWithSection = parts[1];
                        var year = new string(yearWithSection.TakeWhile(char.IsDigit).ToArray());
                        return $"{program}-{year}";
                    }
                    return "Unknown";
                })
                .ToList();

            var finesByProgramYear = fines
                .GroupBy(f => {
                    var section = f.StudentNumNavigation.YearLevelSection ?? "Unknown";
                    var parts = section.Split('-');
                    if (parts.Length >= 2)
                    {
                        var program = parts[0];
                        var yearWithSection = parts[1];
                        var year = new string(yearWithSection.TakeWhile(char.IsDigit).ToArray());
                        return $"{program}-{year}";
                    }
                    return "Unknown";
                })
                .ToList();

            // Calculate statistics from program-year grouped data
            ViewBag.TotalFeesCollected = fees.Where(f => f.FeeStatus?.ToUpper() == "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.TotalFinesCollected = fines.Where(f => f.FinesStatus?.ToUpper() == "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.PendingFees = fees.Where(f => f.FeeStatus?.ToUpper() != "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.PendingFines = fines.Where(f => f.FinesStatus?.ToUpper() != "PAID").Sum(f => f.Amount ?? 0);
            ViewBag.TotalCollections = ViewBag.TotalFeesCollected + ViewBag.TotalFinesCollected;

            // Remittance statistics
            var pendingRemittances = await _context.Remittances
                .Where(r => r.Status == RemittanceStatus.Pending)
                .ToListAsync();

            ViewBag.PendingRemittanceCount = pendingRemittances.Count;
            ViewBag.PendingRemittanceAmount = pendingRemittances.Sum(r => r.TotalAmount);

            var validatedThisMonth = await _context.Remittances
                .Where(r => r.Status == RemittanceStatus.Validated)
                .Where(r => r.ValidationDate.HasValue && r.ValidationDate.Value.Month == DateTime.Now.Month)
                .ToListAsync();

            ViewBag.ValidatedThisMonthCount = validatedThisMonth.Count;
            ViewBag.ValidatedThisMonthAmount = validatedThisMonth.Sum(r => r.TotalAmount);

            // Program-Year breakdown (group by program and year level only, not by section)
            var programYearStats = fees
                .Where(f => f.StudentNumNavigation != null && !string.IsNullOrEmpty(f.StudentNumNavigation.YearLevelSection))
                .GroupBy(f => {
                    var section = f.StudentNumNavigation.YearLevelSection ?? "Unknown";
                    // Extract program and year (e.g., "BSIT-1A" -> "BSIT-1", "BSCS-2B" -> "BSCS-2")
                    var parts = section.Split('-');
                    if (parts.Length >= 2)
                    {
                        var program = parts[0]; // e.g., "BSIT"
                        var yearWithSection = parts[1]; // e.g., "1A" or "2B"
                        var year = new string(yearWithSection.TakeWhile(char.IsDigit).ToArray()); // Extract year number
                        return $"{program}-{year}";
                    }
                    return "Unknown";
                })
                .Select(g => new
                {
                    ProgramYear = g.Key,
                    TotalFees = g.Sum(f => f.Amount ?? 0),
                    PaidFees = g.Where(f => f.FeeStatus?.ToUpper() == "PAID").Sum(f => f.Amount ?? 0),
                    RemittedFees = g.Where(f => f.RemittanceStatus == FeeRemittanceStatus.Remitted).Sum(f => f.Amount ?? 0)
                })
                .OrderBy(s => s.ProgramYear)
                .ToList();

            ViewBag.ProgramYearStats = programYearStats;

            // Calculate monthly trends for current academic year (Aug - Present)
            var currentYear = DateTime.Now.Year;
            var academicYearStart = DateTime.Now.Month >= 8 
                ? new DateTime(currentYear, 8, 1) 
                : new DateTime(currentYear - 1, 8, 1);

            var monthlyTrends = Enumerable.Range(0, (DateTime.Now.Year - academicYearStart.Year) * 12 + DateTime.Now.Month - academicYearStart.Month + 1)
                .Select(offset => {
                    var month = academicYearStart.AddMonths(offset);
                    var monthStart = new DateTime(month.Year, month.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var feesInMonth = fees.Where(f => f.CollectionDate >= monthStart && f.CollectionDate <= monthEnd && f.FeeStatus?.ToUpper() == "PAID");
                    var finesInMonth = fines.Where(f => f.CollectionDate >= monthStart && f.CollectionDate <= monthEnd && f.FinesStatus?.ToUpper() == "PAID");

                    return new {
                        Month = month.ToString("MMM yyyy"),
                        FeesCollected = feesInMonth.Sum(f => f.Amount ?? 0),
                        FinesCollected = finesInMonth.Sum(f => f.Amount ?? 0)
                    };
                })
                .ToList();

            ViewBag.MonthlyTrends = monthlyTrends;

            return View("OrgTreasurerDashboard");
        }


        // ============================================================
        // BULK ACTIONS FOR CLASS TREASURER - FINES
        // ============================================================
        
        [HttpPost]
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> BulkMarkFinesAsPaid([FromBody] BulkFineActionRequest request)
        {
            try
            {
                if (request.FineIds == null || request.FineIds.Count == 0)
                {
                    return Json(new { success = false, message = "No fines selected" });
                }

                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user?.UserName);

                if (treasurer == null)
                {
                    return Json(new { success = false, message = "Treasurer profile not found" });
                }

                int successCount = 0;
                int failCount = 0;

                foreach (var fineId in request.FineIds)
                {
                    var fine = await _context.Fines
                        .Include(f => f.StudentNumNavigation)
                        .Include(f => f.Attendance)
                            .ThenInclude(a => a.Event)
                        .FirstOrDefaultAsync(f => f.FineId == fineId);

                    if (fine == null || fine.FinesStatus == "Paid" || fine.RemittanceStatus != FeeRemittanceStatus.NotRemitted)
                    {
                        failCount++;
                        continue;
                    }

                    // Mark as paid
                    fine.FinesStatus = "Paid";
                    fine.CollectionDate = DateTime.Now;
                    fine.CollectedBy = treasurer.StudentNum;

                    // Create payment transaction for history
                    var transaction = new FinePaymentTransaction
                    {
                        FineId = fine.FineId,
                        StudentNum = fine.StudentNum ?? "",
                        Amount = fine.Amount ?? 0,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "Cash",
                        ProcessedBy = treasurer.StudentNum ?? "",
                        Notes = "Bulk payment - Class Treasurer"
                    };
                    _context.FinePaymentTransactions.Add(transaction);

                    successCount++;
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Successfully marked {successCount} fine(s) as paid" + (failCount > 0 ? $" ({failCount} skipped)" : "")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> BulkRevokeFines([FromBody] BulkFineActionRequest request)
        {
            try
            {
                if (request.FineIds == null || request.FineIds.Count == 0)
                {
                    return Json(new { success = false, message = "No fines selected" });
                }

                int successCount = 0;
                int failCount = 0;

                foreach (var fineId in request.FineIds)
                {
                    var fine = await _context.Fines.FindAsync(fineId);

                    if (fine == null || fine.FinesStatus != "Paid" || fine.RemittanceStatus != FeeRemittanceStatus.NotRemitted)
                    {
                        failCount++;
                        continue;
                    }

                    // Revoke payment
                    fine.FinesStatus = "Unpaid";
                    fine.CollectionDate = null;
                    fine.CollectedBy = null;

                    successCount++;
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Successfully revoked {successCount} fine payment(s)" + (failCount > 0 ? $" ({failCount} skipped)" : "")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ============================================================
        // BULK FEE ACTIONS - CLASS TREASURER
        // ============================================================

        [HttpPost]
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> BulkMarkFeesAsPaid([FromBody] BulkFeeActionRequest request)
        {
            try
            {
                if (request.FeeIds == null || request.FeeIds.Count == 0)
                {
                    return Json(new { success = false, message = "No fees selected" });
                }

                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user?.UserName);

                if (treasurer == null)
                {
                    return Json(new { success = false, message = "Treasurer profile not found" });
                }

                int successCount = 0;
                int failCount = 0;

                foreach (var feeId in request.FeeIds)
                {
                    var fee = await _context.Fees
                        .Include(f => f.StudentNumNavigation)
                        .FirstOrDefaultAsync(f => f.FeeId == feeId);

                    if (fee == null || fee.FeeStatus == "Paid" || fee.RemittanceStatus != FeeRemittanceStatus.NotRemitted)
                    {
                        failCount++;
                        continue;
                    }

                    // Mark as paid
                    fee.FeeStatus = "Paid";
                    fee.CollectionDate = DateTime.Now;
                    fee.CollectedBy = treasurer.StudentNum;

                    // Create payment transaction for history
                    var transaction = new PaymentTransaction
                    {
                        FeeId = fee.FeeId,
                        StudentNum = fee.StudentNum ?? "",
                        Amount = fee.Amount ?? 0,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "Cash",
                        ProcessedBy = treasurer.StudentNum ?? "",
                        Notes = "Bulk payment - Class Treasurer"
                    };
                    _context.PaymentTransactions.Add(transaction);

                    successCount++;
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Successfully marked {successCount} fee(s) as paid" + (failCount > 0 ? $" ({failCount} skipped)" : "")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Class Treasurer")]
        public async Task<IActionResult> BulkRevokeFees([FromBody] BulkFeeActionRequest request)
        {
            try
            {
                if (request.FeeIds == null || request.FeeIds.Count == 0)
                {
                    return Json(new { success = false, message = "No fees selected" });
                }

                int successCount = 0;
                int failCount = 0;

                foreach (var feeId in request.FeeIds)
                {
                    var fee = await _context.Fees.FindAsync(feeId);

                    if (fee == null || fee.FeeStatus != "Paid" || fee.RemittanceStatus != FeeRemittanceStatus.NotRemitted)
                    {
                        failCount++;
                        continue;
                    }

                    // Revoke payment
                    fee.FeeStatus = "Unpaid";
                    fee.CollectionDate = null;
                    fee.CollectedBy = null;

                    successCount++;
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Successfully revoked {successCount} fee payment(s)" + (failCount > 0 ? $" ({failCount} skipped)" : "")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ============================================================
        // BULK FINE ACTIONS - ORG TREASURER
        // ============================================================

        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> BulkMarkOrgFinesAsPaid([FromBody] BulkFineActionRequest request)
        {
            try
            {
                if (request.FineIds == null || request.FineIds.Count == 0)
                {
                    return Json(new { success = false, message = "No fines selected" });
                }

                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user?.UserName);

                if (treasurer == null)
                {
                    return Json(new { success = false, message = "Treasurer profile not found" });
                }

                int successCount = 0;
                int failCount = 0;

                foreach (var fineId in request.FineIds)
                {
                    var fine = await _context.Fines
                        .Include(f => f.StudentNumNavigation)
                        .Include(f => f.Attendance)
                            .ThenInclude(a => a.Event)
                        .FirstOrDefaultAsync(f => f.FineId == fineId);

                    // Can only mark as paid if it's currently unpaid and not locked by class treasurer
                    if (fine == null || fine.FinesStatus == "Paid" || fine.FinesStatus == "Partial" || fine.FinesStatus == "Excused")
                    {
                        failCount++;
                        continue;
                    }

                    // Mark as paid and remitted (Org Treasurer validates immediately)
                    fine.FinesStatus = "Paid";
                    fine.CollectionDate = DateTime.Now;
                    fine.CollectedBy = treasurer.StudentNum;
                    fine.RemittanceStatus = FeeRemittanceStatus.Remitted;
                    fine.OfficialPaymentDate = DateTime.Now;

                    // Create payment transaction for history
                    var transaction = new FinePaymentTransaction
                    {
                        FineId = fine.FineId,
                        StudentNum = fine.StudentNum ?? "",
                        Amount = fine.Amount ?? 0,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "Cash",
                        ProcessedBy = treasurer.StudentNum ?? "",
                        Notes = "Bulk payment - Org Treasurer (Validated)"
                    };
                    _context.FinePaymentTransactions.Add(transaction);

                    successCount++;
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Successfully marked {successCount} fine(s) as paid" + (failCount > 0 ? $" ({failCount} skipped)" : "")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> BulkRevokeOrgFines([FromBody] BulkFineActionRequest request)
        {
            try
            {
                if (request.FineIds == null || request.FineIds.Count == 0)
                {
                    return Json(new { success = false, message = "No fines selected" });
                }

                int successCount = 0;
                int failCount = 0;

                foreach (var fineId in request.FineIds)
                {
                    var fine = await _context.Fines.FindAsync(fineId);

                    // Can only revoke if it's paid but not officially validated (remitted)
                    if (fine == null || fine.FinesStatus != "Paid" || fine.RemittanceStatus == FeeRemittanceStatus.Remitted)
                    {
                        failCount++;
                        continue;
                    }

                    // Revoke payment
                    fine.FinesStatus = "Unpaid";
                    fine.CollectionDate = null;
                    fine.CollectedBy = null;
                    fine.RemittanceStatus = FeeRemittanceStatus.NotRemitted;

                    successCount++;
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Successfully revoked {successCount} fine payment(s)" + (failCount > 0 ? $" ({failCount} skipped)" : "")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // ============================================================
        // BULK FEE ACTIONS - ORG TREASURER
        // ============================================================

        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> BulkMarkOrgFeesAsPaid([FromBody] BulkFeeActionRequest request)
        {
            try
            {
                if (request.FeeIds == null || request.FeeIds.Count == 0)
                {
                    return Json(new { success = false, message = "No fees selected" });
                }

                var user = await _userManager.GetUserAsync(User);
                var treasurer = await _context.Students.FindAsync(user?.UserName);

                if (treasurer == null)
                {
                    return Json(new { success = false, message = "Treasurer profile not found" });
                }

                int successCount = 0;
                int failCount = 0;

                foreach (var feeId in request.FeeIds)
                {
                    var fee = await _context.Fees
                        .Include(f => f.StudentNumNavigation)
                        .FirstOrDefaultAsync(f => f.FeeId == feeId);

                    // Can only mark as paid if it's currently unpaid and not locked by class treasurer
                    if (fee == null || fee.FeeStatus == "Paid" || fee.FeeStatus == "Partial")
                    {
                        failCount++;
                        continue;
                    }

                    // Mark as paid and remitted (Org Treasurer validates immediately)
                    fee.FeeStatus = "Paid";
                    fee.CollectionDate = DateTime.Now;
                    fee.CollectedBy = treasurer.StudentNum;
                    fee.RemittanceStatus = FeeRemittanceStatus.Remitted;
                    fee.OfficialPaymentDate = DateTime.Now;

                    // Create payment transaction for history
                    var transaction = new PaymentTransaction
                    {
                        FeeId = fee.FeeId,
                        StudentNum = fee.StudentNum ?? "",
                        Amount = fee.Amount ?? 0,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "Cash",
                        ProcessedBy = treasurer.StudentNum ?? "",
                        Notes = "Bulk payment - Org Treasurer (Validated)"
                    };
                    _context.PaymentTransactions.Add(transaction);

                    successCount++;
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Successfully marked {successCount} fee(s) as paid" + (failCount > 0 ? $" ({failCount} skipped)" : "")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Org Treasurer")]
        public async Task<IActionResult> BulkRevokeOrgFees([FromBody] BulkFeeActionRequest request)
        {
            try
            {
                if (request.FeeIds == null || request.FeeIds.Count == 0)
                {
                    return Json(new { success = false, message = "No fees selected" });
                }

                int successCount = 0;
                int failCount = 0;

                foreach (var feeId in request.FeeIds)
                {
                    var fee = await _context.Fees.FindAsync(feeId);

                    // Can only revoke if it's paid but not officially validated (remitted)
                    if (fee == null || fee.FeeStatus != "Paid" || fee.RemittanceStatus == FeeRemittanceStatus.Remitted)
                    {
                        failCount++;
                        continue;
                    }

                    // Revoke payment
                    fee.FeeStatus = "Unpaid";
                    fee.CollectionDate = null;
                    fee.CollectedBy = null;
                    fee.RemittanceStatus = FeeRemittanceStatus.NotRemitted;

                    successCount++;
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Successfully revoked {successCount} fee payment(s)" + (failCount > 0 ? $" ({failCount} skipped)" : "")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        #endregion

    }

    // ============================================================
    // REQUEST MODELS FOR BULK ACTIONS
    // ============================================================
    public class BulkFineActionRequest
    {
        public List<int> FineIds { get; set; }
        public string Category { get; set; }
    }

    public class BulkFeeActionRequest
    {
        public List<int> FeeIds { get; set; }
        public string Category { get; set; }
    }
}
