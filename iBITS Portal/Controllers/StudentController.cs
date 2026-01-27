using DocumentFormat.OpenXml.Spreadsheet;
using iBITS_Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iBITS_Portal.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly PortaliBitsContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentController(PortaliBitsContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==============================================================
        // PAGE ACTIONS (Return Views)
        // ==============================================================

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
        public async Task<IActionResult> Financials()
        {
            var userId = _userManager.GetUserName(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == userId);

            if (student == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Student = student;

            // Get fees
            var fees = await _context.Fees
                .AsNoTracking()
                .Where(f => f.StudentNum == userId)
                .OrderByDescending(f => f.FeesStartDate)
                .ToListAsync();

            // Get fines
            var fines = await _context.Fines
                .AsNoTracking()
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.Event)
                .Where(f => f.StudentNum == userId || (f.Attendance != null && f.Attendance.StudentNum == userId))
                .OrderByDescending(f => f.FinesStartDate)
                .ToListAsync();

            ViewBag.Fees = fees;
            ViewBag.Fines = fines;

            // Calculate totals - Only unpaid amounts shown to students
            var totalFeesUnpaid = fees
                .Where(f => !string.Equals(f.FeeStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                .Sum(f => f.Amount) ?? 0;
            var totalFinesUnpaid = fines
                .Where(f => !string.Equals(f.FinesStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                .Sum(f => f.Amount) ?? 0;
            
            ViewBag.TotalBalanceDue = totalFeesUnpaid + totalFinesUnpaid;

            return View();
        }

        // ==============================================================
        // JSON ACTIONS (for AJAX calls from the frontend)
        // ==============================================================

        // Action to get payment transaction history (AJAX)
        // UPDATED: Simplified to show only essential information for students
        public async Task<JsonResult> GetPaymentHistory()
        {
            try
            {
                var userId = _userManager.GetUserName(User);

                // Get fee payment transactions
                var feeTransactions = await _context.PaymentTransactions
                    .Include(t => t.Fee)
                    .Include(t => t.Treasurer)
                    .Where(t => t.StudentNum == userId)
                    .OrderByDescending(t => t.PaymentDate)
                    .Select(t => new
                    {
                        type = "Fee",
                        description = t.Fee != null ? t.Fee.FeeName : "Fee Payment",
                        amount = t.Amount,
                        paymentDate = t.PaymentDate, // Kept for sorting purposes
                        processedBy = t.Treasurer != null ? t.Treasurer.StudentFn + " " + t.Treasurer.StudentLn : "System"
                    })
                    .ToListAsync();

                // Get fine payment transactions
                var fineTransactions = await _context.FinePaymentTransactions
                    .Include(t => t.Fine)
                    .Include(t => t.Treasurer)
                    .Where(t => t.StudentNum == userId)
                    .OrderByDescending(t => t.PaymentDate)
                    .Select(t => new
                    {
                        type = "Fine",
                        description = t.Fine != null ? (t.Fine.Description ?? "Fine Payment") : "Fine Payment",
                        amount = t.Amount,
                        paymentDate = t.PaymentDate, // Kept for sorting purposes
                        processedBy = t.Treasurer != null ? t.Treasurer.StudentFn + " " + t.Treasurer.StudentLn : "System"
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
                // Log the exception ex here
                return Json(new { success = false, message = "Error loading payment history." });
            }
        }

        // Handles the Dashboard "View All Events" Popup
        [HttpGet]
        public async Task<JsonResult> GetAllEventsJson()
        {
            try
            {
                var userId = _userManager.GetUserName(User);

                // 1. Get All Events (Sorted by Date)
                var events = await _context.Events
                    .AsNoTracking()
                    .OrderByDescending(e => e.EventDate)
                    .ToListAsync();

                // 2. Get User's Attendance Record
                var userAttendance = await _context.Attendances
                    .AsNoTracking()
                    .Where(a => a.StudentNum == userId)
                    .ToDictionaryAsync(a => a.EventId, a => a.AttendanceStatus);

                // 3. Merge Data
                var eventList = events.Select(e => new
                {
                    eventId = e.EventId,
                    eventName = e.EventName,
                    eventDate = e.EventDate,
                    startTime = e.StartTime.HasValue ? e.StartTime.Value.ToString(@"hh\:mm tt") : null,
                    endTime = e.EndTime.HasValue ? e.EndTime.Value.ToString(@"hh\:mm tt") : null,
                    eventLocation = e.EventLocation,
                    eventDesc = e.EventDesc,
                    attendanceStatus = userAttendance.ContainsKey(e.EventId) ? userAttendance[e.EventId] : "Not Registered"
                });

                return Json(eventList);
            }
            catch (Exception ex)
            {
                 // Log the exception ex here
                return Json(new { error = "Failed to fetch events" });
            }
        }
        
        // Action to get basic profile data for the Account Settings modal
        [HttpGet]
        public async Task<JsonResult> GetProfileData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentNum == user.UserName);

            return Json(new
            {
                success = true,
                username = user.UserName,
                phoneNumber = await _userManager.GetPhoneNumberAsync(user),
                fullName = student?.FullName,
                course = student != null ? $"{student.Course} | {student.YearLevelSection}" : ""
            });
        }
    }
}
