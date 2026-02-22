using System;

namespace iBITS_Portal.Utilities
{
    /// <summary>
    /// Helper class to get current Philippine Standard Time (UTC+8)
    /// Use PhTimeHelper.Now instead of DateTime.Now throughout the app
    /// </summary>
    public static class PhTimeHelper
    {
        private static readonly TimeZoneInfo PhTimeZone;

        static PhTimeHelper()
        {
            try
            {
                // Windows timezone ID
                PhTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            }
            catch
            {
                try
                {
                    // Linux/Unix timezone ID
                    PhTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
                }
                catch
                {
                    // Fallback: UTC+8 manually
                    PhTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                        "Philippine Standard Time",
                        TimeSpan.FromHours(8),
                        "Philippine Standard Time",
                        "Philippine Standard Time");
                }
            }
        }

        /// <summary>
        /// Gets the current date and time in Philippine Standard Time (UTC+8)
        /// </summary>
        public static DateTime Now =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhTimeZone);

        /// <summary>
        /// Gets today's date in Philippine Standard Time
        /// </summary>
        public static DateOnly Today =>
            DateOnly.FromDateTime(Now);

        /// <summary>
        /// Gets the current time in Philippine Standard Time
        /// </summary>
        public static TimeOnly TimeNow =>
            TimeOnly.FromDateTime(Now);

        /// <summary>
        /// Formats current PH time as a string
        /// </summary>
        public static string NowFormatted =>
            Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
