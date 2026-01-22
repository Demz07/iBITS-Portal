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
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Verify current password
            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, model.CurrentPassword, false);
            if (!passwordCheck.Succeeded)
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return View("Index", model);
            }

            // Change password
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View("Index", model);
            }

            // Re-sign in user with new password
            await _signInManager.SignOutAsync();
            await _signInManager.PasswordSignInAsync(user, model.NewPassword, false, false);

            TempData["Message"] = "Password changed successfully. You have been re-signed in with your new password.";
            return RedirectToAction("Index");
        }

        // POST: /AdminSettings/UpdateEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmail(PasswordChangeModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Verify password
            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, model.CurrentPassword, false);
            if (!passwordCheck.Succeeded)
            {
                ModelState.AddModelError("", "Password is required to update email.");
                return View("Index", model);
            }

            // Update email
            user.Email = model.Email;
            user.UserName = model.Email; // Update username to match email
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View("Index", model);
            }

            TempData["Message"] = "Email updated successfully. Your username has been updated to match your new email.";
            return RedirectToAction("Index");
        }
    }
}
