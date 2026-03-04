using iBITS_Portal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace iBITS_Portal.Utilities
{
    public class GlobalSystemContextFilter : IAsyncActionFilter
    {
        private readonly PortaliBitsContext _context;

        public GlobalSystemContextFilter(PortaliBitsContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                // 1. Fetch Academic Year
                var aySetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "CurrentAcademicYear");
                controller.ViewBag.CurrentAcademicYear = aySetting?.SettingValue ?? "Not Set";

                // 2. Fetch Semester
                var semSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "CurrentSemester");
                controller.ViewBag.CurrentSemester = semSetting?.SettingValue ?? "1st Semester";

                // 3. List of unique years for the "Manage Historical Years" hub (used in Admin layout)
                controller.ViewBag.AllExistingYears = await _context.Students
                    .Where(s => !string.IsNullOrEmpty(s.SchoolYearEnrolled))
                    .Select(s => s.SchoolYearEnrolled)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync();
            }

            await next();
        }
    }
}
