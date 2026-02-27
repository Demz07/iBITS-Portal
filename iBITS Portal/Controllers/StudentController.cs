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
                .OrderByDescending(f => f.FeesDueDate)
                .ToListAsync();

            // Get fines - FIXED: Load Attendance and Event for proper naming
            var fines = await _context.Fines
                .AsNoTracking()
                .Include(f => f.Attendance)
                    .ThenInclude(a => a.Event)
                .Where(f => f.StudentNum == userId)
                .OrderByDescending(f => f.FinesDueDate)
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
        // Action to get payment transaction history (AJAX)
        // FIXED: Joined with Attendance and Event to show the actual Event Name in receipts
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
                        paymentDate = t.PaymentDate,
                        processedBy = t.Treasurer != null ? t.Treasurer.StudentFn + " " + t.Treasurer.StudentLn : "System"
                    })
                    .ToListAsync();

                // Get fine payment transactions
                // FIXED: Include chain t -> Fine -> Attendance -> Event
                var fineTransactions = await _context.FinePaymentTransactions
                    .Include(t => t.Fine)
                        .ThenInclude(f => f.Attendance)
                            .ThenInclude(a => a.Event)
                    .Include(t => t.Treasurer)
                    .Where(t => t.StudentNum == userId)
                    .OrderByDescending(t => t.PaymentDate)
                    .Select(t => new
                    {
                        type = "Fine",
                        // Logic: Use Manual Description IF available, ELSE use Event Name, ELSE fallback
                        description = t.Fine != null
                            ? (t.Fine.Description ?? (t.Fine.Attendance != null && t.Fine.Attendance.Event != null ? t.Fine.Attendance.Event.EventName : "Fine Payment"))
                            : "Fine Payment",
                        amount = t.Amount,
                        paymentDate = t.PaymentDate,
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
                return Json(new { success = false, message = "Error loading payment history." });
            }
        }

        // Handles the Dashboard "View All Events" Popup
        [HttpGet]
        // ADD THIS TO StudentController.cs
        public async Task<IActionResult> GetDetailsJson(string id)
        {
            var s = await _context.Students.FirstOrDefaultAsync(x => x.StudentNum == id);
            if (s == null) return NotFound();

            return Json(new
            {
                firstName = s.StudentFn,
                lastName = s.StudentLn,
                studentNum = s.StudentNum,
                course = s.Course,
                yearLevel = s.YearLevelSection,
                birthday = s.Birthday?.ToString("MMM dd, yyyy"),
                image = s.StudentImage ?? "/images/default-avatar.png",
                email = s.StudentEmail,
                type = s.StudentType,
                classification = s.Classification
            });
        }

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
                    isClosed = e.IsClosed,
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

        // ==============================================================
        // UPDATE ACTIONS (for Profile, Email, Password)
        // ==============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile ProfilePicture)
        {
            if (ProfilePicture == null || ProfilePicture.Length == 0)
                return Json(new { success = false, message = "No file selected." });

            var userId = _userManager.GetUserName(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == userId);
            if (student == null) return Json(new { success = false, message = "Student not found." });

            try
            {
                // Ensure directory exists
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                string fileName = $"{userId}_{DateTime.Now.Ticks}{Path.GetExtension(ProfilePicture.FileName)}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfilePicture.CopyToAsync(fileStream);
                }

                // Update DB path
                student.StudentImage = $"/uploads/profiles/{fileName}";
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Profile picture updated.", newImageUrl = student.StudentImage });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Internal error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmail(string NewEmail)
        {
            if (string.IsNullOrWhiteSpace(NewEmail))
                return Json(new { success = false, message = "Email is required." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "User not found." });

            var token = await _userManager.GenerateChangeEmailTokenAsync(user, NewEmail);
            var result = await _userManager.ChangeEmailAsync(user, NewEmail, token);

            if (result.Succeeded)
            {
                // Update username as well to keep them synced
                await _userManager.SetUserNameAsync(user, NewEmail);

                // Update the student record if needed
                var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == user.UserName);
                if (student != null)
                {
                    student.StudentEmail = NewEmail;
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Email updated successfully." });
            }

            return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            if (NewPassword != ConfirmPassword)
                return Json(new { success = false, message = "Passwords do not match." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "User not found." });

            var result = await _userManager.ChangePasswordAsync(user, OldPassword, NewPassword);

            if (result.Succeeded)
            {
                return Json(new { success = true, message = "Password updated successfully." });
            }

            return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBirthdate(string birthday)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "User not found." });

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == user.UserName);
            if (student == null) return Json(new { success = false, message = "Student record not found." });

            if (!DateOnly.TryParse(birthday, out DateOnly bday))
                return Json(new { success = false, message = "Invalid date format." });

            student.Birthday = bday;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
