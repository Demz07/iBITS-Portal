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

        public string EventName { get; set; } = null!;

        public string EventLocation { get; set; } = null!;

        public DateOnly? EventDate { get; set; }

        public TimeOnly? StartTime { get; set; }

        public DateOnly? EndDate { get; set; }

        public TimeOnly? EndTime { get; set; }

        public string EventDuration { get; set; } = null!;

        public string AcadYear { get; set; } = null!;

        public string EventDesc { get; set; } = null!;

        public string EventType { get; set; } = null!;

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

        [Required]
        public string ArchiveReason { get; set; } = "Event Deletion";

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