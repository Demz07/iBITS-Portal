// C:\Users\Dave\OneDrive\Desktop\this where the updated code must be located\Controllers\StudentController.cs
using DocumentFormat.OpenXml.Spreadsheet;
using iBITS_Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iBITS_Portal.Controllers
{
    [Authorize] // Just require authentication, not a specific role
    public class StudentController : Controller
    {
        private readonly PortaliBitsContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentController(PortaliBitsContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Events Page - Display all upcoming and past events
        public async Task<IActionResult> Events()
        {
            var userId = _userManager.GetUserName(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == userId);

            if (student == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Student = student;

            var today = DateOnly.FromDateTime(DateTime.Now);
            var events = await _context.Events
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            var attendances = await _context.Attendances
                .Where(a => a.StudentNum == userId)
                .ToDictionaryAsync(a => a.EventId, a => a);

            ViewBag.Attendances = attendances;
            ViewBag.Today = today;

            return View(events);
        }

        // Timeline Page - Display student's participation timeline
        public async Task<IActionResult> Timeline()
        {
            var userId = _userManager.GetUserName(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == userId);

            if (student == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Student = student;

            var attendances = await _context.Attendances
                .Include(a => a.Event)
                .Where(a => a.StudentNum == userId)
                .OrderByDescending(a => a.Event.EventDate)
                .ToListAsync();

            return View(attendances);
        }

        // Financials Page - Display fees, fines, and payment history
        // FIXED: Added proper Includes and case-insensitive status checks
        public async Task<IActionResult> Financials()
        {
            var userId = _userManager.GetUserName(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == userId);

            if (student == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Student = student;

            // Get fees for this student - explicitly track entities for fresh data
            var fees = await _context.Fees
                .AsNoTracking()  // FIXED: Ensure we get fresh data from database
                .Where(f => f.StudentNum == userId)
                .OrderByDescending(f => f.FeesStartDate)
                .ToListAsync();

            // Get fines for this student (from attendance or direct assignment)
            // FIXED: Include Attendance AND Event for proper display
            var fines = await _context.Fines
                .AsNoTracking()  // FIXED: Ensure we get fresh data from database
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.Event)  // FIXED: Include Event for EventName display
                .Where(f => f.StudentNum == userId || (f.Attendance != null && f.Attendance.StudentNum == userId))
                .OrderByDescending(f => f.FinesStartDate)
                .ToListAsync();

            ViewBag.Fees = fees;
            ViewBag.Fines = fines;

            // Calculate totals - FIXED: Use case-insensitive comparison
            var totalFeesPaid = fees
                .Where(f => string.Equals(f.FeeStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                .Sum(f => f.Amount) ?? 0;
            var totalFeesUnpaid = fees
                .Where(f => !string.Equals(f.FeeStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                .Sum(f => f.Amount) ?? 0;
            var totalFinesPaid = fines
                .Where(f => string.Equals(f.FinesStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                .Sum(f => f.Amount) ?? 0;
            var totalFinesUnpaid = fines
                .Where(f => !string.Equals(f.FinesStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                .Sum(f => f.Amount) ?? 0;

            ViewBag.TotalFeesPaid = totalFeesPaid;
            ViewBag.TotalFeesUnpaid = totalFeesUnpaid;
            ViewBag.TotalFinesPaid = totalFinesPaid;
            ViewBag.TotalFinesUnpaid = totalFinesUnpaid;
            ViewBag.TotalBalanceDue = totalFeesUnpaid + totalFinesUnpaid;

            // Debug logging - can be removed in production
            System.Diagnostics.Debug.WriteLine($"[Financials] Student: {userId}");
            System.Diagnostics.Debug.WriteLine($"[Financials] Fees count: {fees.Count}, Paid: {fees.Count(f => string.Equals(f.FeeStatus, "Paid", StringComparison.OrdinalIgnoreCase))}");
            System.Diagnostics.Debug.WriteLine($"[Financials] Fines count: {fines.Count}, Paid: {fines.Count(f => string.Equals(f.FinesStatus, "Paid", StringComparison.OrdinalIgnoreCase))}");

            return View();
        }

        // Action to get payment transaction history (AJAX) - FIXED VERSION
        public async Task<JsonResult> GetPaymentHistory()
        {
            try
            {
                var userId = _userManager.GetUserName(User);

                // Get fee payment transactions
                var feeTransactions = await _context.PaymentTransactions
                    .Include(t => t.Fee)
                    .Include(t => t.Treasurer)  // FIXED: Changed from ProcessedByNavigation
                    .Where(t => t.StudentNum == userId)
                    .OrderByDescending(t => t.PaymentDate)
                    .Select(t => new
                    {
                        type = "Fee",
                        description = t.Fee != null ? t.Fee.FeeName : "Fee Payment",
                        amount = t.Amount,
                        paymentDate = t.PaymentDate,
                        paymentMethod = t.PaymentMethod,
                        processedBy = t.Treasurer != null ? t.Treasurer.StudentFn + " " + t.Treasurer.StudentLn : "System",  // FIXED
                        transactionReference = t.TransactionReference,
                        notes = t.Notes
                    })
                    .ToListAsync();

                // Get fine payment transactions
                var fineTransactions = await _context.FinePaymentTransactions
                    .Include(t => t.Fine)
                    .Include(t => t.Treasurer)  // FIXED: Changed from ProcessedByNavigation
                    .Where(t => t.StudentNum == userId)
                    .OrderByDescending(t => t.PaymentDate)
                    .Select(t => new
                    {
                        type = "Fine",
                        description = t.Fine != null ? (t.Fine.Description ?? "Fine Payment") : "Fine Payment",  // FIXED: Changed from FineReason
                        amount = t.Amount,
                        paymentDate = t.PaymentDate,
                        paymentMethod = t.PaymentMethod,
                        processedBy = t.Treasurer != null ? t.Treasurer.StudentFn + " " + t.Treasurer.StudentLn : "System",  // FIXED
                        transactionReference = t.TransactionReference,
                        notes = t.Notes
                    })
                    .ToListAsync();

                // Combine and sort by date
                var allTransactions = feeTransactions.Concat(fineTransactions)
                    .OrderByDescending(t => t.paymentDate)
                    .ToList();

                return Json(new { success = true, transactions = allTransactions });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading payment history." });
            }
        }
    }
}
