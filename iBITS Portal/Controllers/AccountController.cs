// ============================================================
// FILE PATH: Controllers/AccountController.cs
// ============================================================

using iBITS_Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace iBITS_Portal.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly PortaliBitsContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(
            UserManager<IdentityUser> userManager,
            PortaliBitsContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ============================================================
        // VIEW MODELS
        // ============================================================

        /// <summary>
        /// ViewModel for Security Setup (Password Change) - STEP 1
        /// </summary>
        public class SecuritySetupViewModel
        {
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string? ConfirmPassword { get; set; }
        }

        /// <summary>
        /// ViewModel for Profile Setup (Profile Picture Upload) - STEP 2
        /// </summary>
        public class ProfileSetupViewModel
        {
            [Required(ErrorMessage = "Profile picture is required")]
            [Display(Name = "Profile Picture")]
            public IFormFile? ProfilePicture { get; set; }
        }

        // ============================================================
        // SECURITY SETUP (PASSWORD CHANGE) - STEP 1 OF FIRST LOGIN
        // ============================================================

        /// <summary>
        /// GET: /Account/SecuritySetup
        /// Displays the password change form for first-time login
        /// </summary>
        [HttpGet]
        public IActionResult SecuritySetup()
        {
            return View();
        }

        /// <summary>
        /// POST: /Account/SecuritySetup
        /// Processes the new password submission
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SecuritySetup(SecuritySetupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Generate password reset token and reset the password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword!);

            if (result.Succeeded)
            {
                // Check if user needs to set up profile picture
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentNum == user.UserName);

                if (student != null && string.IsNullOrEmpty(student.StudentImage))
                {
                    // Redirect to Profile Setup (Step 2)
                    TempData["Message"] = "Password updated successfully! Now please upload your profile photo.";
                    return RedirectToAction("ProfileSetup", "Account");
                }

                // If profile picture already exists, go to dashboard
                TempData["Message"] = "Your password has been updated. Welcome to the portal!";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ============================================================
        // PROFILE SETUP (PROFILE PICTURE UPLOAD) - STEP 2 OF FIRST LOGIN
        // ============================================================

        /// <summary>
        /// GET: /Account/ProfileSetup
        /// Displays the profile picture upload form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ProfileSetup()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Check if student already has a profile picture
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentNum == user.UserName);

            if (student != null && !string.IsNullOrEmpty(student.StudentImage))
            {
                // Already has profile picture, redirect to dashboard
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        /// <summary>
        /// POST: /Account/ProfileSetup
        /// Processes the profile picture upload
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfileSetup(ProfileSetupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Get the student record
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentNum == user.UserName);

            if (student == null)
            {
                return NotFound($"Unable to find student record for '{user.UserName}'.");
            }

            // Validate file
            if (model.ProfilePicture == null || model.ProfilePicture.Length == 0)
            {
                ModelState.AddModelError("ProfilePicture", "Please select a valid image file.");
                return View(model);
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("ProfilePicture", "Only JPG, JPEG, and PNG files are allowed.");
                return View(model);
            }

            // Validate file size (max 5MB)
            if (model.ProfilePicture.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ProfilePicture", "File size must be less than 5MB.");
                return View(model);
            }

            try
            {
                // Create upload directory if it doesn't exist
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(student.StudentImage))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, student.StudentImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Generate unique filename using student number and timestamp
                var fileName = $"{student.StudentNum}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(stream);
                }

                // Update student's profile picture path in database
                student.StudentImage = $"/uploads/profiles/{fileName}";
                _context.Students.Update(student);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Profile photo uploaded successfully! Welcome to the iBITS Portal!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred while uploading the file: {ex.Message}");
            }

            return View(model);
        }

        // ============================================================
        // HELPER: Update Profile Picture (For dashboard/settings use)
        // ============================================================

        /// <summary>
        /// POST: /Account/UpdateProfilePicture
        /// Allows users to update their profile picture from dashboard/settings
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentNum == user.UserName);

            if (student == null)
            {
                return Json(new { success = false, message = "Student record not found." });
            }

            if (profilePicture == null || profilePicture.Length == 0)
            {
                return Json(new { success = false, message = "Please select a valid image file." });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return Json(new { success = false, message = "Only JPG, JPEG, and PNG files are allowed." });
            }

            // Validate file size (max 5MB)
            if (profilePicture.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "File size must be less than 5MB." });
            }

            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete old profile picture
                if (!string.IsNullOrEmpty(student.StudentImage))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, student.StudentImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                var fileName = $"{student.StudentNum}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                student.StudentImage = $"/uploads/profiles/{fileName}";
                _context.Students.Update(student);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Profile picture updated successfully!", imagePath = student.StudentImage });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
