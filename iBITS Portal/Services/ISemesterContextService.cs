using iBITS_Portal.Models;

namespace iBITS_Portal.Services
{
    /// <summary>
    /// Service for managing semester context across the application
    /// Handles current semester, viewing semester, and historical mode detection
    /// </summary>
    public interface ISemesterContextService
    {
        /// <summary>
        /// Gets the current active semester (IsCurrent = true)
        /// </summary>
        Task<Semester?> GetCurrentSemesterAsync();

        /// <summary>
        /// Gets the semester the user is currently viewing (from session)
        /// Falls back to current semester if no viewing semester is set
        /// </summary>
        Task<Semester?> GetSelectedSemesterAsync();

        /// <summary>
        /// Checks if the user is viewing historical data
        /// Returns true if viewing semester is different from current semester
        /// </summary>
        Task<bool> IsHistoricalModeAsync();

        /// <summary>
        /// Sets the viewing semester for the current user session
        /// </summary>
        Task SetViewingSemesterAsync(int semesterId);

        /// <summary>
        /// Clears the viewing semester (reverts to current semester)
        /// </summary>
        Task ClearViewingSemesterAsync();
    }
}
