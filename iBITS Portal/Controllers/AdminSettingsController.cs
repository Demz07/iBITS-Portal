using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using iBITS_Portal.Models;
using System.Threading.Tasks;

namespace iBITS_Portal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSettingsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly PortaliBitsContext _context;
        private readonly ILogger<AdminSettingsController> _logger;

        public AdminSettingsController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            PortaliBitsContext context,
            ILogger<AdminSettingsController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        // GET: /AdminSettings
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new PasswordChangeModel
            {
                Email = user.Email ?? ""
            };

            return View(model);
        }

        // POST: /AdminSettings/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(PasswordChangeModel model)
        {
            // Manual validation for password tab
            if (string.IsNullOrEmpty(model.CurrentPassword))
                ModelState.AddModelError("CurrentPassword", "Current password is required.");
            if (string.IsNullOrEmpty(model.NewPassword))
                ModelState.AddModelError("NewPassword", "New password is required.");
            if (model.NewPassword != model.ConfirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Verify current password
            var passwordCheck = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!passwordCheck)
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View("Index", model);
            }

            // Change password
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                    ModelState.AddModelError("", error.Description);
                return View("Index", model);
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["Message"] = "Password changed successfully.";
            return RedirectToAction("Index");
        }

        // POST: /AdminSettings/UpdateEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmail(PasswordChangeModel model)
        {
            // Manual validation for email tab
            if (string.IsNullOrEmpty(model.Email))
                ModelState.AddModelError("Email", "New email address is required.");
            if (string.IsNullOrEmpty(model.CurrentPassword))
                ModelState.AddModelError("CurrentPassword", "Current password is required to verify identity.");

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Verify current password
            var passwordCheck = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!passwordCheck)
            {
                ModelState.AddModelError("CurrentPassword", "Incorrect password. Email was not updated.");
                return View("Index", model);
            }

            // Update email
            user.Email = model.Email;
            user.UserName = model.Email; // Keep ID and Email synced for admin
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View("Index", model);
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["Message"] = "Admin account email updated successfully.";
            return RedirectToAction("Index");
        }
    }
}
