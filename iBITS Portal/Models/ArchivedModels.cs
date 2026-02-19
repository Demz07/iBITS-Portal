using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iBITS_Portal.Models
{
    [Table("ArchivedEvents")]
    public class ArchivedEvent
    {
        [Key]
        public int ArchivedEventId { get; set; }

        public int EventId { get; set; }

        [Required]
        public string EventName { get; set; } = null!;

        // ADDED '?' TO ALLOW NULLS FROM DB
        public string? EventLocation { get; set; }

        public DateOnly? EventDate { get; set; }

        public TimeOnly? StartTime { get; set; }

        public DateOnly? EndDate { get; set; }

        public TimeOnly? EndTime { get; set; }

        // ADDED '?' TO ALLOW NULLS FROM DB
        public string? EventDuration { get; set; }

        // ADDED '?' TO ALLOW NULLS FROM DB
        public string? AcadYear { get; set; }

        // ADDED '?' TO ALLOW NULLS FROM DB
        public string? EventDesc { get; set; }

        // ADDED '?' TO ALLOW NULLS FROM DB
        public string? EventType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FineForMember { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FineForClassOfficer { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FineForOrgOfficer { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? NonIbitsFineForMember { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? NonIbitsFineForClassOfficer { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? NonIbitsFineForOrgOfficer { get; set; }

        [Required]
        public DateTime ArchivedDate { get; set; }

        [Required]
        [StringLength(450)]
        public string ArchivedBy { get; set; } = null!;

        // Removed [Required] here because it might be null in old records
        public string? ArchiveReason { get; set; } = "Event Deletion";

        [StringLength(500)]
        public string? ArchiveNotes { get; set; }

        public int AttendanceRecordsAffected { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalFinesAffected { get; set; }
    }

    [Table("ArchivedAnnouncements")]
    public class ArchivedAnnouncement
    {
        [Key]
        public int ArchivedAnnouncementId { get; set; }

        public int AnnouncementId { get; set; }

        public string Title { get; set; } = null!;

        public string Content { get; set; } = null!;

        public DateTime PostedDate { get; set; }

        public DateTime ArchivedDate { get; set; }

        [Required]
        [StringLength(450)]
        public string ArchivedBy { get; set; } = null!;

        [Required]
        public string ArchiveReason { get; set; } = "Announcement Deletion";

        [StringLength(500)]
        public string? ArchiveNotes { get; set; }
    }
}