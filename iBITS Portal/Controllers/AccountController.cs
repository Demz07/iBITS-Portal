// ============================================================
// FILE PATH: Controllers/AccountController.cs (FINAL COMPLETE)
// ============================================================

using iBITS_Portal.Models;
using iBITS_Portal.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

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

        public class ChangePasswordAjaxViewModel
        {
            [Required, DataType(DataType.Password)]
            [Display(Name = "Current Password")]
            public string? OldPassword { get; set; }

            [Required, StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string? ConfirmPassword { get; set; }
        }

        public class ProfileSetupViewModel
        {
            [Required(ErrorMessage = "Profile picture is required")]
            [Display(Name = "Profile Picture")]
            public IFormFile? ProfilePicture { get; set; }
        }

        public class ProfileUpdateViewModel
        {
            [Display(Name = "Profile Picture")]
            public IFormFile? ProfilePicture { get; set; }
        }

        public class EmailUpdateViewModel
        {
            [Required, EmailAddress]
            [Display(Name = "New Email")]
            public string? NewEmail { get; set; }
        }

        // ============================================================
        // SECURITY SETUP (PASSWORD CHANGE) - STEP 1
        // ============================================================
        [HttpGet]
        public IActionResult SecuritySetup() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SecuritySetup(SecuritySetupViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) { return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'."); }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword!);

            if (result.Succeeded)
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == user.UserName);
                if (student != null && string.IsNullOrEmpty(student.StudentImage))
                {
                    TempData["Message"] = "Password updated successfully! Now please upload your profile photo.";
                    return RedirectToAction("ProfileSetup", "Account");
                }

                TempData["Message"] = "Your password has been updated. Welcome to the portal!";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors) { ModelState.AddModelError(string.Empty, error.Description); }
            return View(model);
        }

        // ============================================================
        // AJAX ACTIONS (MODAL)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePasswordAjax(ChangePasswordAjaxViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br/>", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "User session expired." });

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword!, model.NewPassword!);

            if (result.Succeeded) return Json(new { success = true, message = "Password updated successfully! Please remember your new password." });

            if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
                return Json(new { success = false, message = "Error: The Current Password you entered is incorrect." });

            return Json(new { success = false, message = string.Join("<br/>", result.Errors.Select(e => e.Description)) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfileAjax(ProfileUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br/>", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "User not found." });

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == user.UserName);
            if (student == null) return Json(new { success = false, message = "Student record not found." });

            string imagePath = student.StudentImage;

            if (model.ProfilePicture != null)
            {
                try
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext)) return Json(new { success = false, message = "Only JPG, JPEG, and PNG files are allowed." });
                    if (model.ProfilePicture.Length > 5 * 1024 * 1024) return Json(new { success = false, message = "File size must be less than 5MB." });

                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder); // ✅ ensure folder exists

                    var fileName = $"{student.StudentNum}_{PhTimeHelper.Now:yyyyMMddHHmmss}{ext}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(stream);
                    }

                    imagePath = $"/uploads/profiles/{fileName}";

                    // Delete old file (if any)
                    if (!string.IsNullOrEmpty(student.StudentImage))
                    {
                        var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, student.StudentImage.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                    }

                    student.StudentImage = imagePath;
                    _context.Students.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"An error occurred during photo upload: {ex.Message}" });
                }
            }

            return Json(new { success = true, message = "Profile photo updated successfully!", imagePath = imagePath });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmailAjax(EmailUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br/>", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "User not found." });

            if (await _userManager.FindByEmailAsync(model.NewEmail!) != null && user.Email != model.NewEmail)
            {
                return Json(new { success = false, message = "This email is already in use." });
            }

            user.Email = model.NewEmail;
            user.NormalizedEmail = _userManager.NormalizeEmail(model.NewEmail);
            user.EmailConfirmed = true;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return Json(new { success = false, message = string.Join("<br/>", result.Errors.Select(e => e.Description)) });
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == user.UserName);
            if (student != null)
            {
                student.StudentEmail = model.NewEmail;
                _context.Update(student);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Email updated successfully!" });
        }

        // ============================================================
        // PROFILE SETUP (FIRST LOGIN STEP 2)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> ProfileSetup()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) { return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'."); }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == user.UserName);
            if (student != null && !string.IsNullOrEmpty(student.StudentImage)) { return RedirectToAction("Index", "Home"); }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfileSetup(ProfileSetupViewModel model)
        {
            if (!ModelState.IsValid) { return View(model); }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) { return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'."); }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == user.UserName);
            if (student == null) { return NotFound($"Unable to find student record for '{user.UserName}'."); }

            if (model.ProfilePicture == null || model.ProfilePicture.Length == 0)
            { ModelState.AddModelError("ProfilePicture", "Please select a valid image file."); return View(model); }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            { ModelState.AddModelError("ProfilePicture", "Only JPG, JPEG, and PNG files are allowed."); return View(model); }

            if (model.ProfilePicture.Length > 5 * 1024 * 1024)
            { ModelState.AddModelError("ProfilePicture", "File size must be less than 5MB."); return View(model); }

            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                if (!string.IsNullOrEmpty(student.StudentImage))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, student.StudentImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                }

                var fileName = $"{student.StudentNum}_{PhTimeHelper.Now:yyyyMMddHHmmss}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(stream);
                }

                student.StudentImage = $"/uploads/profiles/{fileName}";
                _context.Students.Update(student);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Profile photo uploaded successfully! Welcome to the iBITS Portal!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred while uploading the file: {ex.Message}");
                return View(model);
            }
        }

        // ============================================================
        // COMPAT: Update Profile Picture (Legacy)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) { return Json(new { success = false, message = "User not found." }); }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentNum == user.UserName);
            if (student == null) { return Json(new { success = false, message = "Student record not found." }); }

            if (profilePicture == null || profilePicture.Length == 0) { return Json(new { success = false, message = "Please select a valid image file." }); }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext)) { return Json(new { success = false, message = "Only JPG, JPEG, and PNG files are allowed." }); }
            if (profilePicture.Length > 5 * 1024 * 1024) { return Json(new { success = false, message = "File size must be less than 5MB." }); }

            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                if (!string.IsNullOrEmpty(student.StudentImage))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, student.StudentImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                }

                var fileName = $"{student.StudentNum}_{PhTimeHelper.Now:yyyyMMddHHmmss}{ext}";
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