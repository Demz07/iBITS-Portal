#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace iBITS_Portal.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LockoutModel : PageModel
    {
        public string LockoutEndIso { get; set; }

        public void OnGet()
        {
            // Read manually from TempData to avoid [TempData] DateTime/String cast conflict
            if (TempData.TryGetValue("LockoutEnd", out var lockoutEndVal) && lockoutEndVal != null)
            {
                LockoutEndIso = lockoutEndVal.ToString();
                // Keep TempData alive so refreshing lockout page still shows countdown
                TempData.Keep("LockoutEnd");
            }
            else
            {
                // Fallback: 5 minutes from now
                LockoutEndIso = DateTimeOffset.UtcNow.AddMinutes(5).ToString("o");
            }
        }
    }
}
