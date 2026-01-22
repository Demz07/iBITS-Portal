using iBITS_Portal.Data;
using iBITS_Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting; // For file uploads
using System.IO; // For file operations

namespace iBITS_Portal.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly PortaliBitsContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment; // To get wwwroot path

        public StudentController(PortaliBitsContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
        }

        // Action for the "Participation Timeline" link
        public async Task<IActionResult> Timeline()
        {
            var userId = _userManager.GetUserName(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // FIXED: Include fine information with attendance records
            var records = await _context.Attendances
                .Include(a => a.Event)
                .Include(a => a.Fines) // Include fines associated with attendance
                .Where(a => a.StudentNum == userId)
                .OrderByDescending(a => a.Event != null ? a.Event.EventDate : DateOnly.MinValue)
                .ToListAsync();

            return View(records);
        }

        // Action for the "My Financials" link
        public async Task<IActionResult> Financials()
        {
            var userId = _userManager.GetUserName(User);

            // FIXED: Get ALL fees for the student (both paid and unpaid)
            var allFees = await _context.Fees
                .Where(f => f.StudentNum == userId)
                .OrderByDescending(f => f.FeeId)
                .ToListAsync();

            // FIXED: Get ALL fines for the student (both paid and unpaid)
            var allFines = await _context.Fines
                .Include(f => f.Attendance)
                    .ThenInclude(a => a != null ? a.Event : null)
                .Where(f => f.Attendance != null && f.Attendance.StudentNum == userId)
                .OrderByDescending(f => f.FineId)
                .ToListAsync();

            // CRITICAL FIX: The VIEW expects ViewBag.Fees and ViewBag.Fines (not separated)
            ViewBag.Fees = allFees;  // View expects this!
            ViewBag.Fines = allFines; // View expects this!

            // Also provide separated lists for future enhancements
            var unpaidFees = allFees.Where(f => f.FeeStatus != "Paid").ToList();
            var paidFees = allFees.Where(f => f.FeeStatus == "Paid").ToList();
            var unpaidFines = allFines.Where(f => f.FinesStatus != "Paid").ToList();
            var paidFines = allFines.Where(f => f.FinesStatus == "Paid").ToList();

            // Calculate totals BEFORE assigning to ViewBag
            var totalFeesUnpaid = unpaidFees.Sum(f => f.Amount);
            var totalFeesPaid = paidFees.Sum(f => f.Amount);
            var totalFinesUnpaid = unpaidFines.Sum(f => f.Amount);
            var totalFinesPaid = paidFines.Sum(f => f.Amount);
            var grandTotalUnpaid = totalFeesUnpaid + totalFinesUnpaid;
            var grandTotalPaid = totalFeesPaid + totalFinesPaid;

            // Optional: Assign separated lists too
            ViewBag.UnpaidFees = unpaidFees;
            ViewBag.PaidFees = paidFees;
            ViewBag.UnpaidFines = unpaidFines;
            ViewBag.PaidFines = paidFines;
            ViewBag.TotalFeesUnpaid = totalFeesUnpaid;
            ViewBag.TotalFeesPaid = totalFeesPaid;
            ViewBag.TotalFinesUnpaid = totalFinesUnpaid;
            ViewBag.TotalFinesPaid = totalFinesPaid;
            ViewBag.GrandTotalUnpaid = grandTotalUnpaid;
            ViewBag.GrandTotalPaid = grandTotalPaid;

            return View();
        }

        // Action for viewing all upcoming events
        public async Task<IActionResult> Events()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var userId = _userManager.GetUserName(User);

            // Get all upcoming events
            var upcomingEvents = await _context.Events
                .Where(e => e.EventDate >= today)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            // Get user's attendance records to show status
            var userAttendances = await _context.Attendances
                .Where(a => a.StudentNum == userId)
                .ToListAsync();

            // Create view model with events and attendance status
            var eventViewModels = upcomingEvents.Select(e => new
            {
                Event = e,
                AttendanceStatus = userAttendances.FirstOrDefault(a => a.EventId == e.EventId)?.AttendanceStatus ?? "Not Registered"
            }).ToList();

            ViewBag.EventAttendances = eventViewModels;
            return View(upcomingEvents);
        }

        // POST: Student/RegisterForEvent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterForEvent(int eventId)
        {
            var userId = _userManager.GetUserName(User);
            var student = await _context.Students.FindAsync(userId);

            if (student == null)
            {
                return Json(new { success = false, message = "Student not found." });
            }

            var eventToRegister = await _context.Events.FindAsync(eventId);
            if (eventToRegister == null)
            {
                return Json(new { success = false, message = "Event not found." });
            }

            // Check if event is in the future
            if (eventToRegister.EventDate < DateOnly.FromDateTime(DateTime.Now))
            {
                return Json(new { success = false, message = "Cannot register for past events." });
            }

            // Check if already registered
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EventId == eventId && a.StudentNum == userId);

            if (existingAttendance != null)
            {
                return Json(new { success = false, message = "Already registered for this event." });
            }

            // Create attendance record
            var attendance = new Attendance
            {
                EventId = eventId,
                StudentNum = userId ?? string.Empty,
                AttendanceStatus = "Registered" // Initial status
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Successfully registered for the event!" });
        }

        // POST: /Student/UpdateProfilePicture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profileImage)
        {
            if (profileImage != null && profileImage.Length > 0)
            {
                var userId = _userManager.GetUserName(User);
                var student = await _context.Students.FindAsync(userId);
                if (student == null) return NotFound();

                // Define path and ensure directory exists
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // Create a unique file name to prevent conflicts
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(profileImage.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the new image
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(fileStream);
                }

                // Optional: Delete the old image if it exists to save space
                if (!string.IsNullOrEmpty(student.StudentImage))
                {
                    var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, student.StudentImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Update the database with the new path
                student.StudentImage = "/images/profiles/" + uniqueFileName;
                _context.Update(student);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Profile picture updated successfully!";
            }
            else
            {
                TempData["Error"] = "Please select an image file to upload.";
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
