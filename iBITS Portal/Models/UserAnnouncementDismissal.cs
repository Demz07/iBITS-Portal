// ============================================================
// FILE PATH: Models/UserAnnouncementDismissal.cs
// ============================================================
// Tracks which announcements have been dismissed by which students
// ============================================================

using System;
using iBITS_Portal.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iBITS_Portal.Models
{
    public class UserAnnouncementDismissal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string StudentNum { get; set; } = null!;

        [Required]
        public int AnnouncementId { get; set; }

        [Required]
        public DateTime DismissedAt { get; set; } = PhTime.Now;

        // Navigation properties
        [ForeignKey("StudentNum")]
        public virtual Student? Student { get; set; }

        [ForeignKey("AnnouncementId")]
        public virtual Announcement? Announcement { get; set; }
    }
}
