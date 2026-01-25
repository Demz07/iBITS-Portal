// ============================================================
// FILE PATH: Helpers/StringHelper.cs
// ============================================================
// NEW FILE: Centralized helper class for string manipulations.
// ============================================================

namespace iBITS_Portal.Helpers
{
    public static class StringHelper
    {
        /// <summary>
        /// Extracts the year level (1, 2, 3, 4) from a YearLevelSection string.
        /// Handles various formats like: "3-1", "BSIT 1-A", "First Year", "2ND YEAR".
        /// Returns 0 if no year level can be determined.
        /// </summary>
        /// <param name="yearLevelSection">The string to parse.</param>
        /// <returns>An integer representing the year level, or 0 if not found.</returns>
        public static int ExtractYearLevel(string? yearLevelSection)
        {
            if (string.IsNullOrWhiteSpace(yearLevelSection))
                return 0;

            var input = yearLevelSection.Trim().ToUpper();

            // Check for word-based year levels first
            if (input.Contains("FIRST") || input.Contains("1ST"))
                return 1;
            if (input.Contains("SECOND") || input.Contains("2ND"))
                return 2;
            if (input.Contains("THIRD") || input.Contains("3RD"))
                return 3;
            if (input.Contains("FOURTH") || input.Contains("4TH"))
                return 4;

            // Extract first digit found in the string (handles "3-1", "1-A", etc.)
            foreach (char c in input)
            {
                if (char.IsDigit(c))
                {
                    int year = c - '0';
                    if (year >= 1 && year <= 4)
                        return year;
                }
            }

            return 0; // Unknown year level
        }
    }
}