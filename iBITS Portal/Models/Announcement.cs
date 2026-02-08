// C:\Users\Dave\OneDrive\Desktop\copies of code bases from the iBITS Portal\Announcement.cs
// FILE PATH: Models/Announcement.cs
// UPDATED: Added AnnouncementType and TargetAudience properties

using System.ComponentModel.DataAnnotations;

namespace iBITS_Portal.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public string PostedBy { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Type of announcement: "Announcement", "Payment Reminder", "Event", etc.
        /// </summary>
        [StringLength(50)]
        public string? AnnouncementType { get; set; }

        /// <summary>
        /// Target audience: "All Students", "1st Year", "2nd Year", "BSIT", "BSCS", etc.
        /// </summary>
        [StringLength(100)]
        public string? TargetAudience { get; set; }

        /// <summary>
        /// Expiry date for the announcement. After this date, the announcement will be automatically hidden.
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Semester ID for historical record tracking
        /// </summary>
        public int? SemesterId { get; set; }

        /// <summary>
        /// Navigation property to Semester
        /// </summary>
        public virtual Semester? Semester { get; set; }
    }
}
