using DocumentFormat.OpenXml.Spreadsheet;
using iBITS_Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters; 
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
                // DO NOT change UserName — keep it as StudentNum (login remains by StudentNum)
                // (UserName and NormalizedUserName stay as StudentNum)

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

