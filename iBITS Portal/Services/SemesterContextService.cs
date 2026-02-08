using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using iBITS_Portal.Models;

namespace iBITS_Portal.Services
{
    /// <summary>
    /// Implementation of semester context service
    /// Manages semester selection and historical mode for the application
    /// </summary>
    public class SemesterContextService : ISemesterContextService
    {
        private readonly PortaliBitsContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _cache;

        public SemesterContextService(
            PortaliBitsContext context,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache cache)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

        public async Task<Semester?> GetCurrentSemesterAsync()
        {
            // Cache current semester for 5 minutes to reduce database queries
            return await _cache.GetOrCreateAsync("CurrentSemester", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                
                return await _context.Semesters
                    .Include(s => s.AcademicYear)
                    .FirstOrDefaultAsync(s => s.IsCurrent && s.IsActive);
            });
        }

        public async Task<Semester?> GetSelectedSemesterAsync()
        {
            try
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                var semesterId = session?.GetInt32("ViewingSemesterId");

                if (semesterId.HasValue)
                {
                    var semester = await _context.Semesters
                        .Include(s => s.AcademicYear)
                        .FirstOrDefaultAsync(s => s.SemesterId == semesterId.Value);

                    // If semester not found (deleted or inactive), fall back to current
                    if (semester == null)
                    {
                        session?.Remove("ViewingSemesterId");
                        return await GetCurrentSemesterAsync();
                    }

                    return semester;
                }
            }
            catch (Exception)
            {
                // If session access fails, fall back to current semester
            }

            // Default to current semester
            return await GetCurrentSemesterAsync();
        }

        public async Task<bool> IsHistoricalModeAsync()
        {
            var current = await GetCurrentSemesterAsync();
            var viewing = await GetSelectedSemesterAsync();

            return current?.SemesterId != viewing?.SemesterId;
        }

        public Task SetViewingSemesterAsync(int semesterId)
        {
            _httpContextAccessor.HttpContext?.Session.SetInt32("ViewingSemesterId", semesterId);
            return Task.CompletedTask;
        }

        public Task ClearViewingSemesterAsync()
        {
            _httpContextAccessor.HttpContext?.Session.Remove("ViewingSemesterId");
            return Task.CompletedTask;
        }
    }
}
