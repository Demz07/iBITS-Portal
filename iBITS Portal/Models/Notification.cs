// ============================================================
// FILE PATH: Models/Notification.cs
// ============================================================
// Notification Model for iBITS Portal
// Stores notifications sent to students by admin
// ============================================================

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iBITS_Portal.Models
{
    public partial class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        [StringLength(450)]
        public string StudentNum { get; set; } = null!;

        [StringLength(200)]
        public string? Title { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Message { get; set; }

        public DateTime? NotificationDate { get; set; }

        public bool IsRead { get; set; } = false;

        [StringLength(50)]
        public string? NotificationType { get; set; } // "Admin", "System", "Payment", "Event", etc.

        [StringLength(100)]
        public string? SentBy { get; set; } // Admin username who sent the notification

        // Navigation property
        [ForeignKey("StudentNum")]
        public virtual Student? StudentNumNavigation { get; set; }
    }
}
