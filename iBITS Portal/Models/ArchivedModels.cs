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

    [Table("ArchivedFees")]
    public class ArchivedFee
    {
        [Key]
        public int ArchivedFeeId { get; set; }

        // Original Fee Data
        public int FeeId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FeeName { get; set; } = null!;
        
        [Required]
        [StringLength(450)]
        public string StudentNum { get; set; } = null!;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }
        
        public DateTime? DueDate { get; set; }
        
        public DateTime? CollectionDate { get; set; }
        
        [StringLength(20)]
        public string? Status { get; set; }
        
        public string? AcadYear { get; set; }

        // Archive Metadata
        [Required]
        public DateTime ArchivedDate { get; set; }

        [Required]
        [StringLength(450)]
        public string ArchivedBy { get; set; } = null!;

        [Required]
        public string ArchiveReason { get; set; } = "Semester Closure";

        [StringLength(500)]
        public string? ArchiveNotes { get; set; }
        
        public int? SemesterId { get; set; }
    }

    [Table("ArchivedFines")]
    public class ArchivedFine
    {
        [Key]
        public int ArchivedFineId { get; set; }

        // Original Fine Data
        public int FineId { get; set; }
        
        [Required]
        [StringLength(450)]
        public string StudentNum { get; set; } = null!;
        
        public int? EventId { get; set; }
        
        [StringLength(200)]
        public string? EventName { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }
        
        [StringLength(50)]
        public string? Reason { get; set; }
        
        public DateTime? FineDate { get; set; }
        
        public DateTime? CollectionDate { get; set; }
        
        [StringLength(20)]
        public string? Status { get; set; }

        // Archive Metadata
        [Required]
        public DateTime ArchivedDate { get; set; }

        [Required]
        [StringLength(450)]
        public string ArchivedBy { get; set; } = null!;

        [Required]
        public string ArchiveReason { get; set; } = "Semester Closure";

        [StringLength(500)]
        public string? ArchiveNotes { get; set; }
        
        public int? SemesterId { get; set; }
    }

    [Table("ArchivedStudents")]
    public class ArchivedStudent
    {
        [Key]
        public int ArchivedStudentId { get; set; }

        // Original Student Data
        [Required]
        [StringLength(450)]
        public string StudentNum { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string StudentFn { get; set; } = null!;
        
        [StringLength(100)]
        public string? StudentMn { get; set; }
        
        [Required]
        [StringLength(100)]
        public string StudentLn { get; set; } = null!;
        
        [StringLength(50)]
        public string? Program { get; set; }
        
        public int? YearLevel { get; set; }
        
        [StringLength(50)]
        public string? Section { get; set; }
        
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(20)]
        public string? ContactNum { get; set; }

        // Archive Metadata
        [Required]
        public DateTime ArchivedDate { get; set; }

        [Required]
        [StringLength(450)]
        public string ArchivedBy { get; set; } = null!;

        [Required]
        public string ArchiveReason { get; set; } = "Semester Closure";

        [StringLength(500)]
        public string? ArchiveNotes { get; set; }
        
        public int? SemesterId { get; set; }
        
        public int FeesArchived { get; set; }
        
        public int FinesArchived { get; set; }
    }
}
