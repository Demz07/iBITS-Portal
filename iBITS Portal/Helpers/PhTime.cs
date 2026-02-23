// ============================================================
// FILE: Helpers/PhTime.cs
// PURPOSE: Centralized Philippine Standard Time (UTC+8) helper
// ============================================================
// Use PhTime.Now instead of DateTime.Now everywhere in the app
// to ensure all timestamps use Philippine Standard Time (PST/UTC+8).
// ============================================================

namespace iBITS_Portal.Helpers
{
    public static class PhTime
    {
        private static readonly TimeZoneInfo _pstZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows()
                    ? "Singapore Standard Time"  // Windows timezone ID for UTC+8
                    : "Asia/Manila");             // Linux/macOS timezone ID

        /// <summary>
        /// Gets the current Philippine Standard Time (UTC+8).
        /// Use this instead of DateTime.Now throughout the application.
        /// </summary>
        public static DateTime Now =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _pstZone);

        /// <summary>
        /// Gets today's date in Philippine Standard Time.
        /// Use this instead of DateTime.Today throughout the application.
        /// </summary>
        public static DateTime Today => Now.Date;

        /// <summary>
        /// Gets today as a DateOnly in Philippine Standard Time.
        /// Use this instead of DateOnly.FromDateTime(DateTime.Today).
        /// </summary>
        public static DateOnly TodayOnly => DateOnly.FromDateTime(Today);
    }
}
