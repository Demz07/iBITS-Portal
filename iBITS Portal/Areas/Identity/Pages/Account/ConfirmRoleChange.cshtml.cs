// ============================================================
// FILE PATH: Areas/Identity/Pages/Account/ConfirmRoleChange.cshtml.cs
// ============================================================
// NEW FILE: Handles the logic for accepting/declining a role
// change and forces a re-login to prevent stale sessions.
// ============================================================

using iBITS_Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace iBITS_Portal.Areas.Identity.Pages.Account
{
    [Authorize]
    public class ConfirmRoleChangeModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly PortaliBitsContext _context;

        public ConfirmRoleChangeModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            PortaliBitsContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [BindProperty]
        public PendingRoleChange PendingChange { get; set; }
        public string StudentFullName { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.UserName == null) return Challenge();

            PendingChange = await _context.PendingRoleChanges
                .FirstOrDefaultAsync(p => p.StudentNumber == user.UserName && !p.IsConfirmed && !p.IsDeclined);

            if (PendingChange == null)
            {
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentNum == user.UserName);
            StudentFullName = student?.FullName ?? "Student";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int changeId, string action)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.UserName == null) return Challenge();

            var pendingChange = await _context.PendingRoleChanges.FindAsync(changeId);
            if (pendingChange == null || pendingChange.StudentNumber != user.UserName)
            {
                return NotFound("Pending change request not found or you are not authorized to view it.");
            }

            if (action == "accept")
            {
                var student = await _context.Students.Include(s => s.Officer).FirstOrDefaultAsync(s => s.StudentNum == user.UserName);
                if (student == null) return NotFound("Student record not found.");

                await _userManager.RemoveFromRoleAsync(user, pendingChange.OldRole);
                await _userManager.AddToRoleAsync(user, pendingChange.NewRole);

                bool isNewRoleOfficer = pendingChange.NewRole != "Member";
                if (isNewRoleOfficer)
                {
                    if (student.Officer == null)
                    {
                        var newOfficer = new Officer { Position = pendingChange.NewRole, Classification = GetClassificationForRole(pendingChange.NewRole) };
                        _context.Officers.Add(newOfficer);
                        await _context.SaveChangesAsync();
                        student.OfficerId = newOfficer.OfficerId;
                    }
                    else
                    {
                        student.Officer.Position = pendingChange.NewRole;
                        student.Officer.Classification = GetClassificationForRole(pendingChange.NewRole);
                    }
                }
                else
                {
                    if (student.Officer != null)
                    {
                        var officerRecord = await _context.Officers.FindAsync(student.OfficerId);
                        if (officerRecord != null) _context.Officers.Remove(officerRecord);
                        student.OfficerId = null;
                    }
                }

                pendingChange.IsConfirmed = true;
                pendingChange.ConfirmedDate = DateTime.Now;
                _context.PendingRoleChanges.Update(pendingChange);
                await _context.SaveChangesAsync();

                // SEND NOTIFICATION TO ADMIN
                await SendAdminNotification(pendingChange.AssignedByAdminId, 
                    $"Role Assignment Accepted",
                    $"{student.FullName} has ACCEPTED the role assignment to '{pendingChange.NewRole}'.");

                // CRITICAL FIX: Sign out the user to force a session refresh
                await _signInManager.SignOutAsync();
                TempData["LoginMessage"] = $"Role updated to {pendingChange.NewRole}! Please log in again to continue.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            else
            {
                var student = await _context.Students.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.StudentNum == user.UserName);
                
                pendingChange.IsDeclined = true;
                pendingChange.DeclinedDate = DateTime.Now;
                _context.PendingRoleChanges.Update(pendingChange);
                await _context.SaveChangesAsync();

                // SEND NOTIFICATION TO ADMIN
                await SendAdminNotification(pendingChange.AssignedByAdminId, 
                    $"Role Assignment Declined",
                    $"{student?.FullName ?? user.UserName} has DECLINED the role assignment to '{pendingChange.NewRole}'. The student will remain in their current role '{pendingChange.OldRole}'.");

                TempData["Message"] = "You have declined the role change. No changes were made.";
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }

        private string GetClassificationForRole(string roleName)
        {
            if (roleName.Contains("Class")) return "Class Officer";
            if (roleName.Contains("Org") || roleName == "Officer") return "Org Officer";
            return "Member";
        }

        /// <summary>
        /// Sends a notification to the admin who assigned the role.
        /// </summary>
        private async Task SendAdminNotification(string adminUserId, string title, string message)
        {
            try
            {
                // Find admin's student record (if admin is also a student) or use a general admin notification system
                var adminUser = await _userManager.FindByIdAsync(adminUserId);
                
                if (adminUser != null)
                {
                    var notification = new Notification
                    {
                        StudentNum = adminUser.UserName ?? "ADMIN", // Use admin username
                        Title = title,
                        Message = message,
                        NotificationDate = DateTime.Now,
                        IsRead = false,
                        NotificationType = "RoleManagement",
                        SentBy = "System"
                    };
                    
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the main operation
                Console.WriteLine($"Error sending admin notification: {ex.Message}");
            }
        }
    }
}