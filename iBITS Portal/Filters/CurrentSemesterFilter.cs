using iBITS_Portal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace iBITS_Portal.Filters
{
    public class CurrentSemesterFilter : IActionFilter
    {
        private readonly PortaliBitsContext _context;

        public CurrentSemesterFilter(PortaliBitsContext context)
        {
            _context = context;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                var currentSemester = _context.Semesters
                    .AsNoTracking()
                    .Include(s => s.AcademicYear)
                    .FirstOrDefault(s => s.IsCurrent == true);

                controller.ViewBag.CurrentSemester = currentSemester;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Nothing to do after action executes
        }
    }
}
