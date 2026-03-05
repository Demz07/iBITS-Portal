using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using iBITS_Portal.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

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

        // =========================================================
        // GLOBAL OVERRIDE: EXECUTES BEFORE EVERY ACTION
        // =========================================================
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 1. Fetch Academic Year
            var aySetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "CurrentAcademicYear");
            ViewBag.CurrentAcademicYear = aySetting?.SettingValue ?? "Not Set";

            // 2. Fetch Semester
            var semSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "CurrentSemester");
            ViewBag.CurrentSemester = semSetting?.SettingValue ?? "1st Semester";

            await next();
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
        [HttpGet]
        public async Task<IActionResult> GetTwoFactorStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            return Json(new
            {
                isEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                hasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null
            });
        }

        [HttpGet]
        public async Task<IActionResult> LoadAuthenticatorKey()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            return Json(new { 
                sharedKey = unformattedKey,
                authenticatorUri = $"otpauth://totp/iBITS%20Portal:{user.Email}?secret={unformattedKey}&issuer=iBITS%20Portal"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyAndEnable2FA(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var verificationCode = code.Replace(" ", string.Empty).Replace("-", "");
            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                return Json(new { success = false, message = "Verification code is invalid." });
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            
            // Generate recovery codes (usually 10)
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            
            await _signInManager.RefreshSignInAsync(user);

            return Json(new { 
                success = true, 
                recoveryCodes = recoveryCodes 
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTwoFactor(bool enable)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            await _userManager.SetTwoFactorEnabledAsync(user, enable);
            if (!enable)
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
            }
            await _signInManager.RefreshSignInAsync(user);

            return Json(new { success = true, isEnabled = enable });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!isTwoFactorEnabled)
            {
                return Json(new { success = false, message = "2FA must be enabled to generate recovery codes." });
            }

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            return Json(new { success = true, recoveryCodes = recoveryCodes });
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
