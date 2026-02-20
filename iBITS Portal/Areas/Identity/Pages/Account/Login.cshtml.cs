// ============================================================
// FILE PATH: Areas/Identity/Pages/Account/Login.cshtml.cs
// ============================================================
// UPDATED: Added pending role change check after successful login.
// If student has a pending role change, redirect to confirmation page.
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
using iBITS_Portal.Models;

namespace iBITS_Portal.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly PortaliBitsContext _context;

        public LoginModel(
            SignInManager<IdentityUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<IdentityUser> userManager,
            PortaliBitsContext context)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        // =========================================================
        // FIXED: Added this back so your View doesn't crash
        // =========================================================
        [BindProperty(SupportsGet = true)]
        public string UserType { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

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
                    {
                        userName = userObj.UserName;
                    }
                }

                // 2. Attempt Login
                var result = await _signInManager.PasswordSignInAsync(userName, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // 3. Role-based Access Control
                    var currentUser = await _userManager.FindByNameAsync(userName);
                    bool isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");
                    
                    // Check if user type matches the login route
                    if (UserType == "Admin" && !isAdmin)
                    {
                        // Non-admin trying to access Admin Console
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "Access Denied: This login is for Administrators only.");
                        return Page();
                    }
                    else if (UserType == "Member" && isAdmin)
                    {
                        // Admin trying to access Member portal
                        await _signInManager.SignOutAsync();
                        ModelState.AddModelError(string.Empty, "Access Denied: Administrators must use the Admin Console login.");
                        return Page();
                    }
                    
                    // Redirect to appropriate dashboard
                    if (isAdmin)
                    {
                        return RedirectToAction("Index", "Admin");
                    }

                    // =========================================================
                    // NEW: Check for pending role change
                    // =========================================================
                    var pendingRoleChange = await _context.PendingRoleChanges
                        .Where(p => p.StudentNumber == userName && !p.IsConfirmed && !p.IsDeclined)
                        .OrderByDescending(p => p.AssignedDate)
                        .FirstOrDefaultAsync();

                    if (pendingRoleChange != null)
                    {
                        // Redirect to role confirmation page
                        _logger.LogInformation($"User {userName} has a pending role change. Redirecting to confirmation page.");
                        return RedirectToPage("./ConfirmRoleChange", new { changeId = pendingRoleChange.Id });
                    }
                    // =========================================================
                    // END: Pending role change check
                    // =========================================================

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
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            return Page();
        }
    }
}
