// ============================================================
// FILE PATH: Areas/Identity/Pages/Account/Login.cshtml.cs
// ============================================================
// UPDATED: Added pending role change check after successful login.
// If student has a pending role change, redirect to confirmation page.
// UPDATED: Added login lockout after 5 failed attempts (15 min lock).
// Uses IMemoryCache - no DB migration needed.
// ============================================================

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using iBITS_Portal.Models;

namespace iBITS_Portal.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly PortaliBitsContext _context;
        private readonly IMemoryCache _cache;

        // =========================================================
        // LOCKOUT CONFIGURATION
        // =========================================================
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 3;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<IdentityUser> userManager,
            PortaliBitsContext context,
            IMemoryCache cache)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
            _context = context;
            _cache = cache;
        }

        [BindProperty(SupportsGet = true)]
        public string UserType { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        // =========================================================
        // LOCKOUT PROPERTIES (passed to View)
        // =========================================================
        public bool IsLockedOut { get; set; } = false;
        public int LockoutSecondsRemaining { get; set; } = 0;
        public int FailedAttempts { get; set; } = 0;

        public class InputModel
        {
            [Required]
            [Display(Name = "Student ID / Username")]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        // =========================================================
        // HELPER: Get cache keys for a username
        // =========================================================
        private string AttemptsKey(string username) => $"login_attempts_{username.ToLower().Trim()}";
        private string LockoutKey(string username) => $"lockout_until_{username.ToLower().Trim()}";

        // =========================================================
        // HELPER: Check if username is currently locked out
        // =========================================================
        private (bool isLocked, int secondsRemaining) CheckLockout(string username)
        {
            if (_cache.TryGetValue(LockoutKey(username), out DateTime lockoutUntil))
            {
                var remaining = (int)(lockoutUntil - DateTime.UtcNow).TotalSeconds;
                if (remaining > 0)
                    return (true, remaining);

                // Lockout expired - clean up
                _cache.Remove(LockoutKey(username));
                _cache.Remove(AttemptsKey(username));
            }
            return (false, 0);
        }

        // =========================================================
        // HELPER: Get current failed attempt count
        // =========================================================
        private int GetFailedAttempts(string username)
        {
            return _cache.TryGetValue(AttemptsKey(username), out int attempts) ? attempts : 0;
        }

        // =========================================================
        // HELPER: Increment failed attempt count, lock if needed
        // Returns (newCount, isNowLocked)
        // =========================================================
        private (int newCount, bool isNowLocked) IncrementFailedAttempts(string username)
        {
            var key = AttemptsKey(username);
            var current = GetFailedAttempts(username);
            var newCount = current + 1;

            // Store attempts for 20 minutes
            _cache.Set(key, newCount, TimeSpan.FromMinutes(20));

            if (newCount >= MaxFailedAttempts)
            {
                // Set lockout
                var lockoutUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                _cache.Set(LockoutKey(username), lockoutUntil, TimeSpan.FromMinutes(LockoutMinutes + 1));
                return (newCount, true);
            }

            return (newCount, false);
        }

        // =========================================================
        // HELPER: Reset failed attempts on successful login
        // =========================================================
        private void ResetFailedAttempts(string username)
        {
            _cache.Remove(AttemptsKey(username));
            _cache.Remove(LockoutKey(username));
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // 1. Resolve Username vs Email
                var userName = Input.Email;
                if (userName.Contains("@"))
                {
                    var userObj = await _userManager.FindByEmailAsync(Input.Email);
                    if (userObj != null)
                        userName = userObj.UserName;
                }

                // 2. Check if currently locked out
                var (isLocked, secondsRemaining) = CheckLockout(userName);
                if (isLocked)
                {
                    IsLockedOut = true;
                    LockoutSecondsRemaining = secondsRemaining;
                    FailedAttempts = MaxFailedAttempts;
                    return Page();
                }

                // 3. Check if user exists in the system (Existence check)
                var userCheck = await _userManager.FindByNameAsync(userName);
                if (userCheck == null)
                {
                    _logger.LogWarning($"Login attempt for non-existent user: {userName}");
                    ModelState.AddModelError(string.Empty, "User ID not found in the system.");
                    return Page();
                }

                // 4. Security Setup Check: If user exists but hasn't completed setup (no profile photo)
                var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentNum == userName);
                bool needsSecuritySetup = (student != null && string.IsNullOrEmpty(student.StudentImage));

                // 5. Attempt Login
                var result = await _signInManager.PasswordSignInAsync(userName, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // Reset failed attempts on success
                    ResetFailedAttempts(userName);

                    // Redirect to Security Setup if profile photo is missing (initial setup)
                    if (needsSecuritySetup)
                    {
                        return RedirectToAction("SecuritySetup", "Account");
                    }

                    // 6. Role-based Access Control
                    var currentUser = await _userManager.FindByNameAsync(userName);
                    bool isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

                    if (UserType == "Admin" && !isAdmin)
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "Access Denied: This login is for Administrators only.");
                        return Page();
                    }
                    else if (UserType == "Member" && isAdmin)
                    {
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "Access Denied: Administrators must use the Admin Console login.");
                        return Page();
                    }

                    if (isAdmin)
                        return RedirectToAction("Index", "Admin");

                    // Check for pending role change
                    var pendingRoleChange = await _context.PendingRoleChanges
                        .Where(p => p.StudentNumber == userName && !p.IsConfirmed && !p.IsDeclined)
                        .OrderByDescending(p => p.AssignedDate)
                        .FirstOrDefaultAsync();

                    if (pendingRoleChange != null)
                    {
                        _logger.LogInformation($"User {userName} has a pending role change. Redirecting to confirmation page.");
                        return RedirectToPage("./ConfirmRoleChange", new { changeId = pendingRoleChange.Id });
                    }

                    return LocalRedirect(returnUrl);
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }

                // 7. Failed login - with "Needs Setup" override
                if (needsSecuritySetup)
                {
                    _logger.LogInformation($"User {userName} needs setup but entered wrong password.");
                    ModelState.AddModelError(string.Empty, "Account requires initial security setup. Please use your default credentials.");
                    return Page();
                }

                // 8. Standard Failed login - increment counter
                var (newCount, isNowLocked) = IncrementFailedAttempts(userName);
                FailedAttempts = newCount;

                if (isNowLocked)
                {
                    IsLockedOut = true;
                    LockoutSecondsRemaining = LockoutMinutes * 60;
                    _logger.LogWarning($"User {userName} locked out after {MaxFailedAttempts} failed attempts.");
                    return Page();
                }

                var attemptsLeft = MaxFailedAttempts - newCount;
                if (attemptsLeft <= 2)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Invalid credentials. {attemptsLeft} attempt{(attemptsLeft == 1 ? "" : "s")} remaining before lockout.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }

                return Page();
            }

            return Page();
        }
    }
}
